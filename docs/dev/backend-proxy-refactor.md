# Backend: Proxy Refactor - Service-Level Fallback Proxy

## Overview

Refactor proxy from "global/endpoint-specific" model to **service-level fallback proxy**.

**Core Behavior Change**: When a request matches an endpoint but **no rule matches** (and it would normally return the default response), instead fallback to the real API via the service's proxy config.

### Current Flow
```
Request → Endpoint Match → Rule Match → Default Response
       ↘ No Endpoint Match → Global Proxy → 404
```

### New Flow
```
Request → Endpoint Match → Scenario Match? → Return
                         → Rule Match?     → Return
                         → Service Proxy?  → Forward to real API → Return
                         → Default Response → Return
       ↘ No Endpoint Match → Service Proxy (by path prefix)? → Forward → Return
                           → 404
```

---

## Phase 1: Entity & Database Changes

### 1.1 Create `ServiceProxy` Entity (replaces `ProxyConfig`)

**File**: `MockServer.Core/Entities/ServiceProxy.cs` (new)

```csharp
public class ServiceProxy
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;  // FK concept via name
    public string TargetBaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsRecording { get; set; }
    public bool ForwardHeaders { get; set; } = true;
    public string? AdditionalHeaders { get; set; }          // JSON
    public int TimeoutMs { get; set; } = 10000;
    public string? StripPathPrefix { get; set; }
    public bool FallbackEnabled { get; set; } = true;       // NEW: enable/disable fallback per service
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Key changes from `ProxyConfig`:
- Remove `EndpointId` (no longer per-endpoint)
- Add `ServiceName` (unique, links to `MockEndpoint.ServiceName`)
- Add `FallbackEnabled` (controls whether unmatched rules fallback to proxy)
- Rename entity to `ServiceProxy` to clarify intent

### 1.2 EF Core Configuration

**File**: `MockServer.Infrastructure/Data/Configurations/ServiceProxyConfiguration.cs` (new, replaces `ProxyConfigConfiguration.cs`)

- Table name: `service_proxies`
- Unique constraint on `ServiceName`
- Remove FK to `mock_endpoints` (relationship is via `ServiceName` string, not FK)
- Same column types for `AdditionalHeaders` (jsonb), etc.

### 1.3 Database Migration

- Create new `service_proxies` table
- Migrate existing `proxy_configs` data:
  - Global proxy configs → skip (no longer supported)
  - Endpoint-specific configs → resolve `EndpointId` to `ServiceName` via `mock_endpoints`, create `ServiceProxy` per unique `ServiceName`
- Drop `proxy_configs` table after migration

---

## Phase 2: Repository & Cache Layer

### 2.1 Repository Interface

**File**: `MockServer.Core/Interfaces/IServiceProxyRepository.cs` (new, replaces `IProxyConfigRepository.cs`)

```csharp
public interface IServiceProxyRepository
{
    Task<ServiceProxy?> GetByIdAsync(Guid id);
    Task<ServiceProxy?> GetByServiceNameAsync(string serviceName);
    Task<IEnumerable<ServiceProxy>> GetAllAsync();
    Task AddAsync(ServiceProxy proxy);
    Task UpdateAsync(ServiceProxy proxy);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
```

### 2.2 Repository Implementation

**File**: `MockServer.Infrastructure/Repositories/ServiceProxyRepository.cs` (new)

Standard EF Core implementation. Key query: `GetByServiceNameAsync` filters by `ServiceName`.

### 2.3 Service Proxy Cache

**File**: `MockServer.Infrastructure/MockEngine/ServiceProxyCache.cs` (new, replaces `ProxyConfigCache.cs`)

```csharp
public interface IServiceProxyCache
{
    Task LoadAllAsync();
    Task ReloadAsync();
    ServiceProxy? GetActiveByServiceName(string serviceName);
    IReadOnlyList<ServiceProxy> GetAllActive();
}
```

Internal structure:
- `ConcurrentDictionary<string, ServiceProxy>` keyed by `ServiceName` (case-insensitive)
- No more `_globalConfig` field
- `GetActiveByServiceName(serviceName)` returns the proxy config for a given service

---

## Phase 3: Middleware Pipeline Change (Core Logic)

### 3.1 Modify `DynamicMockMiddleware.InvokeAsync`

**File**: `MockServer.Api/Middleware/DynamicMockMiddleware.cs`

This is the critical change. Current flow at line 112-128:

```
if (matchResult != null) → always render response (rule or default)
else → try proxy → 404
```

New flow:

```csharp
if (matchResult != null)
{
    // 1. Scenario match (unchanged)
    // 2. Rule matched → render rule response (unchanged)
    // 3. IsDefaultResponse == true → check service fallback proxy
    //    3a. If service proxy active + fallbackEnabled → forward to real API
    //    3b. Else → render default response (current behavior)
}
else
{
    // No endpoint match
    // Try to find a service proxy by path prefix matching (optional, lower priority)
    // → 404
}
```

Detailed change to the `matchResult != null` branch:

```csharp
if (matchResult != null)
{
    // Scenario check (unchanged, lines 92-109)
    ...

    endpointId = matchResult.Endpoint.Id;

    if (!matchResult.IsDefaultResponse)
    {
        // Rule matched → render as before
        ruleId = matchResult.Rule?.Id;
        isMatched = true;
        await _responseRenderer.RenderAsync(httpContext, matchResult, context, matchResult.PathParams);
        // ... same as current
    }
    else
    {
        // No rule matched, would return default response
        // → Check service-level fallback proxy
        var serviceProxy = _serviceProxyCache
            .GetActiveByServiceName(matchResult.Endpoint.ServiceName);

        if (serviceProxy != null && serviceProxy.FallbackEnabled)
        {
            // Forward to real API
            var proxyResponse = await _proxyEngine.ForwardAsync(context, serviceProxy);
            if (proxyResponse != null)
            {
                // Write proxy response (same pattern as current proxy handling)
                httpContext.Response.StatusCode = proxyResponse.StatusCode;
                foreach (var header in proxyResponse.Headers)
                {
                    if (!IsResponseHopByHopHeader(header.Key))
                        httpContext.Response.Headers.TryAdd(header.Key, header.Value);
                }
                await httpContext.Response.WriteAsync(proxyResponse.Body);

                responseStatusCode = proxyResponse.StatusCode;
                responseBody = proxyResponse.Body;
                isMatched = true;  // endpoint matched, rule didn't
                isProxied = true;
                proxyTargetUrl = proxyResponse.TargetUrl;

                if (serviceProxy.IsRecording)
                    _ = _recordingService.RecordAsync(context, proxyResponse, endpointId);
            }
            else
            {
                // Proxy failed → fallback to default response
                isMatched = true;
                await _responseRenderer.RenderAsync(httpContext, matchResult, context, matchResult.PathParams);
                responseStatusCode = httpContext.Response.StatusCode;
                responseBody = matchResult.Endpoint.DefaultResponse;
            }
        }
        else
        {
            // No service proxy → render default response as before
            isMatched = true;
            await _responseRenderer.RenderAsync(httpContext, matchResult, context, matchResult.PathParams);
            responseStatusCode = httpContext.Response.StatusCode;
            responseBody = matchResult.Endpoint.DefaultResponse;
        }
    }
}
else
{
    // No endpoint match → 404 (remove global proxy logic)
    responseStatusCode = 404;
    isMatched = false;
    httpContext.Response.StatusCode = 404;
    httpContext.Response.ContentType = "application/json";
    responseBody = JsonConvert.SerializeObject(new { error = "No matching endpoint found", path = context.Path, method = context.Method });
    await httpContext.Response.WriteAsync(responseBody);
}
```

### 3.2 Update `ProxyEngine.ForwardAsync` Signature

**File**: `MockServer.Infrastructure/MockEngine/ProxyEngine.cs`

Add overload or modify to accept `ServiceProxy` instead of `ProxyConfig`:

```csharp
Task<ProxyResponse?> ForwardAsync(MockRequestContext context, ServiceProxy config);
```

Since `ServiceProxy` has the same fields (`TargetBaseUrl`, `ForwardHeaders`, `AdditionalHeaders`, `TimeoutMs`, `StripPathPrefix`), either:
- Create a shared interface `IProxyTarget` with these fields, implemented by both entities
- Or simply replace `ProxyConfig` with `ServiceProxy` in the signature

Recommended: use a shared interface `IProxyTarget` for flexibility.

### 3.3 Update `RecordingService`

**File**: `MockServer.Infrastructure/MockEngine/RecordingService.cs`

- When recording from a service proxy fallback, `endpointId` is already known (the matched endpoint)
- The `ServiceName` should be set to the actual service name (not hardcoded `"recorded"`)
- Update `RecordAsync` to accept `serviceName` parameter:

```csharp
Task RecordAsync(MockRequestContext request, ProxyResponse response, Guid? endpointId, string? serviceName = null);
```

When `endpointId` is provided, only create a new rule on the existing endpoint (don't create new endpoint).

### 3.4 Update `CachedEndpoint`

Ensure `CachedEndpoint` exposes `ServiceName` so the middleware can look it up. Check if it's already there.

---

## Phase 4: API Endpoints

### 4.1 New REST API

**File**: `MockServer.Api/Endpoints/ServiceProxyApis.cs` (new, replaces `ProxyConfigApis.cs`)

Route group: `/admin/api/service-proxies`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | List all service proxies |
| GET | `/{id}` | Get by ID |
| GET | `/by-service/{serviceName}` | Get by service name |
| POST | `/` | Create service proxy |
| PUT | `/{id}` | Update service proxy |
| DELETE | `/{id}` | Delete service proxy |
| PATCH | `/{id}/toggle` | Toggle active |
| PATCH | `/{id}/toggle-recording` | Toggle recording |
| PATCH | `/{id}/toggle-fallback` | Toggle fallback |
| GET | `/services` | List distinct service names from endpoints (helper for frontend) |

### 4.2 Validation

- `ServiceName` is required and must match an existing service name in `mock_endpoints`
- `TargetBaseUrl` must be valid HTTP/HTTPS URL (same as current)
- Unique constraint on `ServiceName` - one proxy per service

### 4.3 Helper: List Available Services

```
GET /admin/api/service-proxies/services
```

Returns distinct `ServiceName` values from `mock_endpoints` table, with count of endpoints per service. Helps frontend show a dropdown of available services.

Response:
```json
[
  { "serviceName": "user-service", "endpointCount": 5, "hasProxy": true },
  { "serviceName": "order-service", "endpointCount": 3, "hasProxy": false }
]
```

---

## Phase 5: Cleanup & Migration

### 5.1 Remove Old Code

- Delete `ProxyConfig.cs` entity
- Delete `ProxyConfigConfiguration.cs`
- Delete `ProxyConfigRepository.cs` and `IProxyConfigRepository.cs`
- Delete `ProxyConfigCache.cs` and `IProxyConfigCache.cs`
- Delete `ProxyConfigApis.cs`
- Update DI registration in `Program.cs`

### 5.2 Remove `FindActiveProxyConfig` Method

The private method in `DynamicMockMiddleware` that only checks global proxy is removed. Logic is now inline in the middleware using `_serviceProxyCache`.

### 5.3 Update DI Container

**File**: `MockServer.Api/Program.cs` (or wherever DI is registered)

- Register `IServiceProxyRepository` → `ServiceProxyRepository`
- Register `IServiceProxyCache` → `ServiceProxyCache` (singleton)
- Register new API endpoints `app.MapServiceProxyApis()`
- Remove old proxy config registrations

---

## Phase 6: Import/Export Support

### 6.1 Export

Include `service_proxies` in export JSON alongside endpoints, rules, scenarios.

### 6.2 Import

When importing, match `ServiceProxy` by `ServiceName`. If a service proxy already exists for that service name, update it; otherwise create.

---

## Summary of Files Changed

| Action | File | Description |
|--------|------|-------------|
| **NEW** | `Core/Entities/ServiceProxy.cs` | New entity |
| **NEW** | `Core/Interfaces/IServiceProxyRepository.cs` | New repository interface |
| **NEW** | `Infrastructure/Repositories/ServiceProxyRepository.cs` | New repository impl |
| **NEW** | `Infrastructure/Data/Configurations/ServiceProxyConfiguration.cs` | EF config |
| **NEW** | `Infrastructure/MockEngine/ServiceProxyCache.cs` | New cache |
| **NEW** | `Api/Endpoints/ServiceProxyApis.cs` | New API |
| **MODIFY** | `Api/Middleware/DynamicMockMiddleware.cs` | Core flow change |
| **MODIFY** | `Infrastructure/MockEngine/ProxyEngine.cs` | Accept `ServiceProxy` |
| **MODIFY** | `Infrastructure/MockEngine/RecordingService.cs` | Use real service name |
| **MODIFY** | `Api/Program.cs` | DI registration |
| **DELETE** | `Core/Entities/ProxyConfig.cs` | Old entity |
| **DELETE** | `Core/Interfaces/IProxyConfigRepository.cs` | Old interface |
| **DELETE** | `Infrastructure/Repositories/ProxyConfigRepository.cs` | Old impl |
| **DELETE** | `Infrastructure/Data/Configurations/ProxyConfigConfiguration.cs` | Old EF config |
| **DELETE** | `Infrastructure/MockEngine/ProxyConfigCache.cs` | Old cache |
| **DELETE** | `Api/Endpoints/ProxyConfigApis.cs` | Old API |
| **ADD** | `Infrastructure/Data/Migrations/...` | DB migration |

---

## Suggested Implementation Order

1. Create `ServiceProxy` entity + EF config + migration
2. Create `IServiceProxyRepository` + implementation
3. Create `ServiceProxyCache`
4. Update `ProxyEngine` to use `IProxyTarget` interface
5. **Modify `DynamicMockMiddleware`** (core logic change)
6. Update `RecordingService`
7. Create `ServiceProxyApis`
8. Update DI in `Program.cs`
9. Delete old proxy code
10. Test: endpoint match + no rule → fallback to real API
11. Test: endpoint match + rule → return mock (no proxy)
12. Test: no endpoint match → 404
13. Test: recording creates rule on existing endpoint with correct service name
14. Update import/export

# Mithya Mock Server - Backend Design Document

> Target: .NET Backend Team
> Prerequisites: Familiarity with current codebase (`MockServer.Core`, `MockServer.Infrastructure`, `MockServer.Api`)

---

## Table of Contents

1. [P0: Response Template Engine](#1-p0-response-template-engine)
2. [P1: Fault Injection](#2-p1-fault-injection)
3. [P1: Proxy / Record & Playback](#3-p1-proxy--record--playback)
4. [P2: Stateful Scenarios](#4-p2-stateful-scenarios)
5. [P2: Enhanced Request Matching](#5-p2-enhanced-request-matching)
6. [Database Migration Summary](#6-database-migration-summary)
7. [API Contract Summary](#7-api-contract-summary)

---

## 1. P0: Response Template Engine

### 1.1 Overview

Add Handlebars template rendering to response bodies. When a rule's `isTemplate` flag is `true`, the `ResponseBody` is parsed as a Handlebars template with access to the full request context.

### 1.2 Template Context Model

The template engine should expose the following variables to Handlebars templates:

```
{{request.method}}          -> "POST"
{{request.path}}            -> "/api/user/123"
{{request.body}}            -> raw body string
{{request.headers.Accept}}  -> "application/json"
{{request.query.page}}      -> "2"
{{request.pathParams.id}}   -> "123"

{{jsonPath request.body "$.user.name"}}  -> extract from JSON body
{{now "yyyy-MM-dd"}}                     -> "2026-02-08"
{{uuid}}                                 -> "a1b2c3d4-..."
{{randomInt 1 100}}                      -> 42
{{math 5 "+" 3}}                         -> 8
```

### 1.3 Database Changes

**Table: `mock_rules`** - Add columns:

| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `is_template` | `boolean` | `false` | Whether `response_body` should be parsed as Handlebars template |
| `is_response_headers_template` | `boolean` | `false` | Whether `response_headers` values should also be templated |

Migration:
```sql
ALTER TABLE mock_rules ADD COLUMN is_template BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE mock_rules ADD COLUMN is_response_headers_template BOOLEAN NOT NULL DEFAULT FALSE;
```

### 1.4 New Classes

#### `MockServer.Infrastructure/MockEngine/TemplateEngine.cs`

```csharp
public interface ITemplateEngine
{
    string Render(string template, TemplateContext context);
}

public class HandlebarsTemplateEngine : ITemplateEngine
{
    // Use Handlebars.Net NuGet package
    // Register custom helpers on construction
    public HandlebarsTemplateEngine()
    {
        _handlebars = Handlebars.Create();
        RegisterHelpers();
    }

    public string Render(string template, TemplateContext context)
    {
        var compiled = _handlebars.Compile(template);
        return compiled(context);
    }

    private void RegisterHelpers()
    {
        // jsonPath helper: {{jsonPath body "$.user.name"}}
        // now helper: {{now "yyyy-MM-dd'T'HH:mm:ss"}}
        // uuid helper: {{uuid}}
        // randomInt helper: {{randomInt min max}}
        // math helper: {{math a "+" b}}
        // eq/ne/gt/lt helpers for conditionals
        // stringify helper: {{stringify obj}} -> JSON.serialize
    }
}

public class TemplateContext
{
    public RequestData Request { get; set; }
}

public class RequestData
{
    public string Method { get; set; }
    public string Path { get; set; }
    public string Body { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public Dictionary<string, string> Query { get; set; }
    public Dictionary<string, string> PathParams { get; set; }
}
```

### 1.5 NuGet Dependency

```xml
<PackageReference Include="Handlebars.Net" Version="2.*" />
```

### 1.6 Modification Points

#### `ResponseRenderer.cs` - Integrate template rendering

```csharp
// Current flow:
await response.WriteAsync(rule.ResponseBody ?? "");

// New flow:
string body = rule.ResponseBody ?? "";
if (rule.IsTemplate && !string.IsNullOrEmpty(body))
{
    var templateContext = BuildTemplateContext(matchResult, requestContext);
    body = _templateEngine.Render(body, templateContext);
}
await response.WriteAsync(body);

// Similarly for response headers:
if (rule.IsResponseHeadersTemplate && rule.ResponseHeaders != null)
{
    foreach (var kvp in rule.ResponseHeaders)
    {
        var headerValue = _templateEngine.Render(kvp.Value, templateContext);
        response.Headers[kvp.Key] = headerValue;
    }
}
```

#### `CachedRule` - Add fields

```csharp
public bool IsTemplate { get; set; }
public bool IsResponseHeadersTemplate { get; set; }
```

#### `MockRuleCache.cs` - Map new fields when loading cache

#### `DynamicMockMiddleware.cs` - Pass `MockRequestContext` to `ResponseRenderer`

Current signature: `RenderAsync(HttpContext, MatchResult)`
New signature: `RenderAsync(HttpContext, MatchResult, MockRequestContext)`

This is needed so the template engine can access path params, parsed body, etc.

### 1.7 DI Registration

```csharp
// Program.cs
builder.Services.AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>();
```

### 1.8 API Changes

**Existing endpoints affected:**

`POST /admin/api/endpoints/{endpointId}/rules` and `PUT .../rules/{ruleId}`

Add to `CreateRuleRequest` / `UpdateRuleRequest` DTO:
```csharp
public bool IsTemplate { get; set; } = false;
public bool IsResponseHeadersTemplate { get; set; } = false;
```

**New endpoint - Template preview:**

```
POST /admin/api/templates/preview
```

Request:
```json
{
  "template": "Hello {{request.pathParams.name}}, today is {{now 'yyyy-MM-dd'}}",
  "mockRequest": {
    "method": "GET",
    "path": "/api/greet/John",
    "body": null,
    "headers": {},
    "query": {},
    "pathParams": { "name": "John" }
  }
}
```

Response:
```json
{
  "rendered": "Hello John, today is 2026-02-08",
  "error": null
}
```

### 1.9 Error Handling

- If template rendering fails, log the error and return the raw template string with a warning header `X-Template-Error: true`
- Template compilation errors should be caught and returned in the preview endpoint
- Do not throw exceptions that would break the mock response flow

---

## 2. P1: Fault Injection

### 2.1 Overview

Allow rules to define fault behavior in addition to (or instead of) normal responses. Faults simulate real-world failure conditions for resilience testing.

### 2.2 Fault Types

```csharp
public enum FaultType
{
    None = 0,              // Normal response (default)
    FixedDelay = 1,        // Existing delayMs behavior (keep for backward compat)
    RandomDelay = 2,       // Random delay within [min, max] range
    ConnectionReset = 3,   // Abort TCP connection immediately
    EmptyResponse = 4,     // Return empty body with status code
    MalformedResponse = 5, // Return random bytes as body
    Timeout = 6            // Wait indefinitely (until client timeout)
}
```

### 2.3 Database Changes

**Table: `mock_rules`** - Add columns:

| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `fault_type` | `integer` | `0` (None) | Type of fault to inject |
| `fault_config` | `jsonb` | `null` | Fault-specific configuration |

```sql
ALTER TABLE mock_rules ADD COLUMN fault_type INTEGER NOT NULL DEFAULT 0;
ALTER TABLE mock_rules ADD COLUMN fault_config JSONB;
```

### 2.4 Fault Config Schema

```json
// FaultType.RandomDelay
{ "minDelayMs": 100, "maxDelayMs": 5000 }

// FaultType.MalformedResponse
{ "byteCount": 256 }

// FaultType.Timeout
{ "timeoutMs": 30000 }

// FaultType.EmptyResponse
{ "statusCode": 503 }
```

### 2.5 New Classes

#### `MockServer.Core/Enums/FaultType.cs`

```csharp
public enum FaultType
{
    None = 0,
    FixedDelay = 1,
    RandomDelay = 2,
    ConnectionReset = 3,
    EmptyResponse = 4,
    MalformedResponse = 5,
    Timeout = 6
}
```

#### `MockServer.Infrastructure/MockEngine/FaultInjector.cs`

```csharp
public interface IFaultInjector
{
    Task<bool> ApplyFaultAsync(HttpContext httpContext, CachedRule rule);
    // Returns true if fault was applied (response is done)
    // Returns false if no fault, continue normal response rendering
}

public class FaultInjector : IFaultInjector
{
    public async Task<bool> ApplyFaultAsync(HttpContext httpContext, CachedRule rule)
    {
        switch (rule.FaultType)
        {
            case FaultType.None:
            case FaultType.FixedDelay:
                return false; // Handled by existing delayMs logic

            case FaultType.RandomDelay:
                var config = JsonConvert.DeserializeObject<RandomDelayConfig>(rule.FaultConfig);
                var delay = Random.Shared.Next(config.MinDelayMs, config.MaxDelayMs + 1);
                await Task.Delay(delay);
                return false; // Continue with normal response after delay

            case FaultType.ConnectionReset:
                httpContext.Abort();
                return true;

            case FaultType.EmptyResponse:
                var emptyConfig = JsonConvert.DeserializeObject<EmptyResponseConfig>(rule.FaultConfig);
                httpContext.Response.StatusCode = emptyConfig?.StatusCode ?? 503;
                return true;

            case FaultType.MalformedResponse:
                var malConfig = JsonConvert.DeserializeObject<MalformedConfig>(rule.FaultConfig);
                var bytes = new byte[malConfig?.ByteCount ?? 256];
                Random.Shared.NextBytes(bytes);
                httpContext.Response.ContentType = "application/octet-stream";
                await httpContext.Response.Body.WriteAsync(bytes);
                return true;

            case FaultType.Timeout:
                var timeoutConfig = JsonConvert.DeserializeObject<TimeoutConfig>(rule.FaultConfig);
                await Task.Delay(timeoutConfig?.TimeoutMs ?? 30000);
                httpContext.Abort();
                return true;

            default:
                return false;
        }
    }
}
```

### 2.6 Modification Points

#### `ResponseRenderer.cs`

```csharp
public async Task RenderAsync(HttpContext httpContext, MatchResult matchResult, MockRequestContext requestContext)
{
    var rule = matchResult.Rule;
    if (rule != null)
    {
        // Apply fault first
        var faultApplied = await _faultInjector.ApplyFaultAsync(httpContext, rule);
        if (faultApplied)
            return; // Response already handled by fault

        // Existing delay logic (FixedDelay)
        if (rule.DelayMs > 0)
            await Task.Delay(rule.DelayMs);

        // Normal response rendering...
    }
}
```

#### `CachedRule` - Add fields

```csharp
public FaultType FaultType { get; set; }
public string? FaultConfig { get; set; }
```

### 2.7 API Changes

Add to `CreateRuleRequest` / `UpdateRuleRequest`:
```csharp
public FaultType FaultType { get; set; } = FaultType.None;
public string? FaultConfig { get; set; }
```

### 2.8 Logging

When a fault is applied, the `MockRequestLog` should record:
- `responseStatusCode`: Actual status sent (0 for connection reset/timeout)
- `isMatched`: `true` (it did match a rule)
- Add new column `fault_type_applied` (integer, nullable) to indicate which fault was used

---

## 3. P1: Proxy / Record & Playback

### 3.1 Overview

Enable Mithya to forward unmatched requests to a real upstream API and optionally record the responses as new mock rules.

### 3.2 Operating Modes

```
Mode 1: Mock Only (current behavior)
  -> Match request -> Return mock or 404

Mode 2: Mock + Proxy Fallback
  -> Match request -> If matched, return mock
                   -> If not matched, forward to upstream, return real response

Mode 3: Record
  -> Same as Mode 2, but also save the upstream response as a new MockRule
```

### 3.3 Database Changes

**New Table: `proxy_configs`**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | `uuid` | PK | |
| `endpoint_id` | `uuid` | FK, nullable | If set, proxy only for this endpoint; if null, global |
| `target_base_url` | `varchar(500)` | NOT NULL | Upstream base URL (e.g., `https://api.example.com`) |
| `is_active` | `boolean` | NOT NULL | Enable/disable proxy |
| `is_recording` | `boolean` | NOT NULL | Whether to record responses as rules |
| `forward_headers` | `boolean` | NOT NULL DEFAULT true | Forward original request headers |
| `additional_headers` | `jsonb` | nullable | Extra headers to add (e.g., auth tokens) |
| `timeout_ms` | `integer` | NOT NULL DEFAULT 10000 | Upstream request timeout |
| `strip_path_prefix` | `varchar(200)` | nullable | Path prefix to strip before forwarding |
| `created_at` | `timestamp` | NOT NULL | |
| `updated_at` | `timestamp` | NOT NULL | |

```sql
CREATE TABLE proxy_configs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    endpoint_id UUID REFERENCES mock_endpoints(id) ON DELETE CASCADE,
    target_base_url VARCHAR(500) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_recording BOOLEAN NOT NULL DEFAULT FALSE,
    forward_headers BOOLEAN NOT NULL DEFAULT TRUE,
    additional_headers JSONB,
    timeout_ms INTEGER NOT NULL DEFAULT 10000,
    strip_path_prefix VARCHAR(200),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### 3.4 New Classes

#### `MockServer.Core/Entities/ProxyConfig.cs`

```csharp
public class ProxyConfig
{
    public Guid Id { get; set; }
    public Guid? EndpointId { get; set; }
    public string TargetBaseUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsRecording { get; set; }
    public bool ForwardHeaders { get; set; } = true;
    public string? AdditionalHeaders { get; set; }
    public int TimeoutMs { get; set; } = 10000;
    public string? StripPathPrefix { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public MockEndpoint? Endpoint { get; set; }
}
```

#### `MockServer.Core/Interfaces/IProxyConfigRepository.cs`

```csharp
public interface IProxyConfigRepository
{
    Task<ProxyConfig?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProxyConfig>> GetAllAsync();
    Task<ProxyConfig?> GetActiveByEndpointIdAsync(Guid endpointId);
    Task<ProxyConfig?> GetGlobalActiveAsync();
    Task AddAsync(ProxyConfig config);
    Task UpdateAsync(ProxyConfig config);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
```

#### `MockServer.Infrastructure/MockEngine/ProxyEngine.cs`

```csharp
public interface IProxyEngine
{
    Task<ProxyResponse?> ForwardAsync(MockRequestContext requestContext, ProxyConfig config);
}

public class ProxyEngine : IProxyEngine
{
    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<ProxyResponse?> ForwardAsync(MockRequestContext context, ProxyConfig config)
    {
        var client = _httpClientFactory.CreateClient("ProxyClient");
        client.Timeout = TimeSpan.FromMilliseconds(config.TimeoutMs);

        // Build target URL
        var path = context.Path;
        if (!string.IsNullOrEmpty(config.StripPathPrefix))
            path = path.TrimStart(config.StripPathPrefix);

        var targetUrl = config.TargetBaseUrl.TrimEnd('/') + path;
        if (!string.IsNullOrEmpty(context.QueryString))
            targetUrl += context.QueryString;

        // Build request
        var request = new HttpRequestMessage(new HttpMethod(context.Method), targetUrl);

        // Forward headers
        if (config.ForwardHeaders)
        {
            foreach (var header in context.Headers)
            {
                if (!IsHopByHopHeader(header.Key))
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Additional headers
        if (config.AdditionalHeaders != null)
        {
            var extra = JsonConvert.DeserializeObject<Dictionary<string, string>>(config.AdditionalHeaders);
            foreach (var kvp in extra)
                request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }

        // Forward body
        if (!string.IsNullOrEmpty(context.Body))
            request.Content = new StringContent(context.Body, Encoding.UTF8, "application/json");

        // Execute
        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        var responseHeaders = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

        return new ProxyResponse
        {
            StatusCode = (int)response.StatusCode,
            Body = responseBody,
            Headers = responseHeaders
        };
    }
}

public class ProxyResponse
{
    public int StatusCode { get; set; }
    public string Body { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}
```

#### `MockServer.Infrastructure/MockEngine/RecordingService.cs`

```csharp
public interface IRecordingService
{
    Task RecordAsync(MockRequestContext request, ProxyResponse response, Guid? endpointId);
}

public class RecordingService : IRecordingService
{
    public async Task RecordAsync(MockRequestContext request, ProxyResponse response, Guid? endpointId)
    {
        // 1. If no endpoint exists for this path+method, create one
        if (endpointId == null)
        {
            var endpoint = new MockEndpoint
            {
                Name = $"Recorded: {request.Method} {request.Path}",
                ServiceName = "recorded",
                Protocol = ProtocolType.REST,
                Path = request.Path,
                HttpMethod = request.Method,
                IsActive = true,
            };
            await _endpointRepo.AddAsync(endpoint);
            await _endpointRepo.SaveChangesAsync();
            endpointId = endpoint.Id;
            await _cache.ReloadEndpointAsync(endpoint.Id);
        }

        // 2. Create a rule with the recorded response
        var rule = new MockRule
        {
            EndpointId = endpointId.Value,
            RuleName = $"Recorded at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            Priority = 100,
            MatchConditions = "[]",  // No conditions = matches all
            ResponseStatusCode = response.StatusCode,
            ResponseBody = response.Body,
            ResponseHeaders = JsonConvert.SerializeObject(response.Headers),
            DelayMs = 0,
            IsActive = true,
        };
        await _ruleRepo.AddAsync(rule);
        await _ruleRepo.SaveChangesAsync();
        await _cache.ReloadRulesForEndpointAsync(endpointId.Value);
    }
}
```

### 3.5 Modification Points

#### `DynamicMockMiddleware.cs` - Add proxy fallback

```csharp
public async Task InvokeAsync(HttpContext httpContext)
{
    // ... existing path filtering ...
    // ... build MockRequestContext ...

    var matchResult = await _matchEngine.FindMatchAsync(context);

    if (matchResult != null)
    {
        // Matched a rule or default response -> render as before
        await _responseRenderer.RenderAsync(httpContext, matchResult, context);
    }
    else
    {
        // No match -> try proxy
        var proxyConfig = await FindActiveProxyConfig(context);
        if (proxyConfig != null)
        {
            var proxyResponse = await _proxyEngine.ForwardAsync(context, proxyConfig);
            if (proxyResponse != null)
            {
                // Write proxy response
                httpContext.Response.StatusCode = proxyResponse.StatusCode;
                foreach (var header in proxyResponse.Headers)
                    httpContext.Response.Headers.TryAdd(header.Key, header.Value);
                await httpContext.Response.WriteAsync(proxyResponse.Body);

                // Record if enabled
                if (proxyConfig.IsRecording)
                    await _recordingService.RecordAsync(context, proxyResponse, null);

                // Log with isMatched = false, add proxy indicator
                await LogRequestAsync(context, proxyResponse, isMatched: false, isProxied: true);
                return;
            }
        }

        // No proxy config either -> 404
        httpContext.Response.StatusCode = 404;
        await httpContext.Response.WriteAsJsonAsync(new { error = "No matching endpoint found" });
    }

    // Log request...
}
```

### 3.6 Proxy Config Cache

```csharp
public interface IProxyConfigCache
{
    Task LoadAllAsync();
    ProxyConfig? GetActiveForEndpoint(Guid endpointId);
    ProxyConfig? GetGlobalActive();
    Task ReloadAsync();
}
```

Register as Singleton; load on startup alongside `MockRuleCache.LoadAllAsync()`.

### 3.7 API Endpoints

```
GET    /admin/api/proxy-configs           -> List all proxy configs
GET    /admin/api/proxy-configs/{id}      -> Get proxy config by ID
POST   /admin/api/proxy-configs           -> Create proxy config
PUT    /admin/api/proxy-configs/{id}      -> Update proxy config
DELETE /admin/api/proxy-configs/{id}      -> Delete proxy config
PATCH  /admin/api/proxy-configs/{id}/toggle          -> Toggle active
PATCH  /admin/api/proxy-configs/{id}/toggle-recording -> Toggle recording
```

### 3.8 MockRequestLog Extension

Add columns to `mock_request_logs`:

| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `is_proxied` | `boolean` | `false` | Whether this request was forwarded to upstream |
| `proxy_target_url` | `varchar(500)` | `null` | The actual upstream URL hit |

---

## 4. P2: Stateful Scenarios

### 4.1 Overview

Scenarios allow the same endpoint to return different responses depending on a state machine. Each scenario tracks a current state, and requests trigger state transitions.

### 4.2 Concept

```
Scenario: "User Login Flow"

  State: "logged_out" (initial)
    -> POST /api/login with valid creds -> Response: 200 {token: "abc"} -> Transition to "authenticated"
    -> POST /api/login with bad creds -> Response: 401 -> Stay in "logged_out"

  State: "authenticated"
    -> GET /api/profile -> Response: 200 {name: "John"} -> Stay in "authenticated"
    -> POST /api/logout -> Response: 200 -> Transition to "logged_out"
    -> (after 5 requests) -> auto-transition to "session_expired"

  State: "session_expired"
    -> GET /api/profile -> Response: 401 {error: "expired"} -> Stay in "session_expired"
    -> POST /api/login -> Response: 200 -> Transition to "authenticated"
```

### 4.3 Database Changes

**New Table: `scenarios`**

| Column | Type | Description |
|--------|------|-------------|
| `id` | `uuid` | PK |
| `name` | `varchar(200)` | Scenario name |
| `description` | `text` | Optional description |
| `initial_state` | `varchar(100)` | State name that the scenario starts in |
| `current_state` | `varchar(100)` | Current runtime state |
| `is_active` | `boolean` | Enable/disable |
| `created_at` | `timestamp` | |
| `updated_at` | `timestamp` | |

**New Table: `scenario_steps`**

| Column | Type | Description |
|--------|------|-------------|
| `id` | `uuid` | PK |
| `scenario_id` | `uuid` | FK -> scenarios |
| `state_name` | `varchar(100)` | Which state this step belongs to |
| `endpoint_id` | `uuid` | FK -> mock_endpoints |
| `match_conditions` | `jsonb` | Additional conditions (beyond endpoint match) |
| `response_status_code` | `integer` | |
| `response_body` | `text` | |
| `response_headers` | `jsonb` | |
| `is_template` | `boolean` | Use template engine |
| `delay_ms` | `integer` | |
| `next_state` | `varchar(100)` | State to transition to after this response |
| `priority` | `integer` | Priority within same state |

### 4.4 New Classes

#### `MockServer.Core/Entities/Scenario.cs`

```csharp
public class Scenario
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string InitialState { get; set; }
    public string CurrentState { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ScenarioStep> Steps { get; set; }
}
```

#### `MockServer.Core/Entities/ScenarioStep.cs`

```csharp
public class ScenarioStep
{
    public Guid Id { get; set; }
    public Guid ScenarioId { get; set; }
    public string StateName { get; set; }
    public Guid EndpointId { get; set; }
    public string? MatchConditions { get; set; }
    public int ResponseStatusCode { get; set; }
    public string ResponseBody { get; set; }
    public string? ResponseHeaders { get; set; }
    public bool IsTemplate { get; set; }
    public int DelayMs { get; set; }
    public string? NextState { get; set; }
    public int Priority { get; set; }
    public Scenario Scenario { get; set; }
    public MockEndpoint Endpoint { get; set; }
}
```

#### `MockServer.Infrastructure/MockEngine/ScenarioEngine.cs`

```csharp
public interface IScenarioEngine
{
    Task<ScenarioMatchResult?> TryMatchAsync(MockRequestContext context, Guid endpointId);
    Task ResetScenarioAsync(Guid scenarioId);
}

public class ScenarioEngine : IScenarioEngine
{
    // In-memory state tracking
    private readonly ConcurrentDictionary<Guid, string> _scenarioStates = new();

    public async Task<ScenarioMatchResult?> TryMatchAsync(MockRequestContext context, Guid endpointId)
    {
        // 1. Find active scenarios that have steps linked to this endpoint
        // 2. For each scenario, check current state
        // 3. Find steps that match current state + endpoint + conditions
        // 4. If found, return the step's response + update state
        // 5. Persist new state to database (async, non-blocking)
    }

    public async Task ResetScenarioAsync(Guid scenarioId)
    {
        // Reset to initial state
        var scenario = await _repo.GetByIdAsync(scenarioId);
        _scenarioStates[scenarioId] = scenario.InitialState;
        scenario.CurrentState = scenario.InitialState;
        await _repo.UpdateAsync(scenario);
    }
}
```

### 4.5 Modification Points

#### `DynamicMockMiddleware.cs` - Check scenarios before standard matching

```csharp
// In InvokeAsync, after building MockRequestContext:

// 1. Try scenario match first (takes priority if active)
var scenarioResult = await _scenarioEngine.TryMatchAsync(context, matchedEndpointId);
if (scenarioResult != null)
{
    await _responseRenderer.RenderScenarioAsync(httpContext, scenarioResult, context);
    return;
}

// 2. Fall back to standard rule matching
var matchResult = await _matchEngine.FindMatchAsync(context);
// ...existing logic...
```

### 4.6 API Endpoints

```
GET    /admin/api/scenarios                    -> List all scenarios
GET    /admin/api/scenarios/{id}               -> Get scenario with steps
POST   /admin/api/scenarios                    -> Create scenario
PUT    /admin/api/scenarios/{id}               -> Update scenario
DELETE /admin/api/scenarios/{id}               -> Delete scenario
PATCH  /admin/api/scenarios/{id}/toggle        -> Toggle active
POST   /admin/api/scenarios/{id}/reset         -> Reset to initial state
GET    /admin/api/scenarios/{id}/current-state -> Get current state

POST   /admin/api/scenarios/{id}/steps         -> Add step
PUT    /admin/api/scenarios/{id}/steps/{stepId} -> Update step
DELETE /admin/api/scenarios/{id}/steps/{stepId} -> Delete step
```

---

## 5. P2: Enhanced Request Matching

### 5.1 Overview

Upgrade the matching engine to support full JSONPath queries, JSON Schema validation, and AND/OR logic modes.

### 5.2 New Match Operators

```csharp
public enum MatchOperator
{
    // Existing
    Equals = 1,
    Contains = 2,
    Regex = 3,
    StartsWith = 4,
    EndsWith = 5,
    GreaterThan = 6,
    LessThan = 7,
    Exists = 8,

    // New
    NotEquals = 9,
    JsonSchema = 10,    // Value is a JSON Schema; validate extracted field
    IsEmpty = 11,       // Field exists but is empty/null
    NotExists = 12      // Field does not exist
}
```

### 5.3 Logic Mode

**Table: `mock_rules`** - Add column:

| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `logic_mode` | `varchar(3)` | `'AND'` | How to combine conditions: `AND` or `OR` |

```csharp
public enum LogicMode { AND, OR }
```

### 5.4 Modification Points

#### `MatchEngine.cs` - Support AND/OR

```csharp
private bool EvaluateAllConditions(CachedRule rule, MockRequestContext context, ...)
{
    if (rule.LogicMode == LogicMode.OR)
    {
        return rule.Conditions.Any(c => EvaluateSingleCondition(c, context, ...));
    }
    else // AND (default)
    {
        return rule.Conditions.All(c => EvaluateSingleCondition(c, context, ...));
    }
}
```

#### `OperatorEvaluator.cs` - Add new operators

```csharp
MatchOperator.NotEquals => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
MatchOperator.JsonSchema => ValidateJsonSchema(actual, expected),
MatchOperator.IsEmpty => string.IsNullOrEmpty(actual),
MatchOperator.NotExists => actual == null,
```

For JSON Schema validation, use `NJsonSchema` NuGet package:
```csharp
private static bool ValidateJsonSchema(string? json, string schema)
{
    if (json == null) return false;
    var jsonSchema = NJsonSchema.JsonSchema.FromJsonAsync(schema).Result;
    var errors = jsonSchema.Validate(json);
    return errors.Count == 0;
}
```

### 5.5 JSONPath Enhancement

Current implementation uses `JToken.SelectToken()` which already supports full JSONPath. Verify the following work:

- `$.store.book[0].title` - Array indexing
- `$.store.book[?(@.price < 10)]` - Filter expressions
- `$..author` - Recursive descent
- `$.store.book[*].author` - Wildcard

If not fully supported, consider adding `JsonCons.JsonPath` NuGet for extended JSONPath.

---

## 6. Database Migration Summary

All changes in a single migration for each phase:

### Phase 1 (P0: Templates)
```sql
ALTER TABLE mock_rules ADD COLUMN is_template BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE mock_rules ADD COLUMN is_response_headers_template BOOLEAN NOT NULL DEFAULT FALSE;
```

### Phase 2 (P1: Faults + Proxy)
```sql
-- Fault Injection
ALTER TABLE mock_rules ADD COLUMN fault_type INTEGER NOT NULL DEFAULT 0;
ALTER TABLE mock_rules ADD COLUMN fault_config JSONB;

-- Proxy
CREATE TABLE proxy_configs ( ... );

-- Logging Extension
ALTER TABLE mock_request_logs ADD COLUMN is_proxied BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE mock_request_logs ADD COLUMN proxy_target_url VARCHAR(500);
ALTER TABLE mock_request_logs ADD COLUMN fault_type_applied INTEGER;
```

### Phase 3 (P2: Scenarios + Matching)
```sql
-- Scenarios
CREATE TABLE scenarios ( ... );
CREATE TABLE scenario_steps ( ... );

-- Enhanced Matching
ALTER TABLE mock_rules ADD COLUMN logic_mode VARCHAR(3) NOT NULL DEFAULT 'AND';
```

---

## 7. API Contract Summary

### New Endpoints

| Method | Path | Phase | Description |
|--------|------|-------|-------------|
| `POST` | `/admin/api/templates/preview` | P0 | Preview rendered template |
| `GET` | `/admin/api/proxy-configs` | P1 | List proxy configs |
| `GET` | `/admin/api/proxy-configs/{id}` | P1 | Get proxy config |
| `POST` | `/admin/api/proxy-configs` | P1 | Create proxy config |
| `PUT` | `/admin/api/proxy-configs/{id}` | P1 | Update proxy config |
| `DELETE` | `/admin/api/proxy-configs/{id}` | P1 | Delete proxy config |
| `PATCH` | `/admin/api/proxy-configs/{id}/toggle` | P1 | Toggle proxy active |
| `PATCH` | `/admin/api/proxy-configs/{id}/toggle-recording` | P1 | Toggle recording |
| `GET` | `/admin/api/scenarios` | P2 | List scenarios |
| `GET` | `/admin/api/scenarios/{id}` | P2 | Get scenario with steps |
| `POST` | `/admin/api/scenarios` | P2 | Create scenario |
| `PUT` | `/admin/api/scenarios/{id}` | P2 | Update scenario |
| `DELETE` | `/admin/api/scenarios/{id}` | P2 | Delete scenario |
| `PATCH` | `/admin/api/scenarios/{id}/toggle` | P2 | Toggle scenario active |
| `POST` | `/admin/api/scenarios/{id}/reset` | P2 | Reset scenario state |
| `POST` | `/admin/api/scenarios/{id}/steps` | P2 | Add scenario step |
| `PUT` | `/admin/api/scenarios/{id}/steps/{stepId}` | P2 | Update step |
| `DELETE` | `/admin/api/scenarios/{id}/steps/{stepId}` | P2 | Delete step |

### Modified Endpoints

| Method | Path | Phase | Changes |
|--------|------|-------|---------|
| `POST` | `/admin/api/endpoints/{eid}/rules` | P0+P1+P2 | Add `isTemplate`, `faultType`, `faultConfig`, `logicMode` fields |
| `PUT` | `/admin/api/endpoints/{eid}/rules/{rid}` | P0+P1+P2 | Same as above |
| `GET` | `/admin/api/logs` | P1 | Response includes `isProxied`, `proxyTargetUrl`, `faultTypeApplied` |

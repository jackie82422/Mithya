# Frontend: Proxy Refactor - Service-Level Proxy Management

## Overview

Refactor proxy management UI from "global/endpoint-specific proxy configs" to **service-level proxy management**.

Users manage proxy settings per **service** (grouped by `serviceName`). Each service can have one proxy target URL. When endpoints in that service don't match any rule, requests fallback to the real API.

---

## Phase 1: Type Definitions

### 1.1 Update Types

**File**: `shared/types/index.ts`

Remove:
```typescript
// DELETE these
export interface ProxyConfig { ... }
export interface CreateProxyConfigRequest { ... }
```

Add:
```typescript
export interface ServiceProxy {
  id: string;
  serviceName: string;
  targetBaseUrl: string;
  isActive: boolean;
  isRecording: boolean;
  forwardHeaders: boolean;
  additionalHeaders: string | null;
  timeoutMs: number;
  stripPathPrefix: string | null;
  fallbackEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateServiceProxyRequest {
  serviceName: string;
  targetBaseUrl: string;
  isRecording?: boolean;
  forwardHeaders?: boolean;
  additionalHeaders?: string | null;
  timeoutMs?: number;
  stripPathPrefix?: string | null;
  fallbackEnabled?: boolean;
}

export interface ServiceInfo {
  serviceName: string;
  endpointCount: number;
  hasProxy: boolean;
}
```

---

## Phase 2: API Client

### 2.1 Update API Module

**File**: `modules/proxy/api.ts`

```typescript
import apiClient from '@/shared/api/client';
import type { ServiceProxy, CreateServiceProxyRequest, ServiceInfo } from '@/shared/types';

export const serviceProxyApi = {
  getAll: () =>
    apiClient.get<ServiceProxy[]>('/service-proxies').then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ServiceProxy>(`/service-proxies/${id}`).then((r) => r.data),

  getByServiceName: (serviceName: string) =>
    apiClient.get<ServiceProxy>(`/service-proxies/by-service/${serviceName}`).then((r) => r.data),

  create: (data: CreateServiceProxyRequest) =>
    apiClient.post<ServiceProxy>('/service-proxies', data).then((r) => r.data),

  update: (id: string, data: CreateServiceProxyRequest) =>
    apiClient.put<ServiceProxy>(`/service-proxies/${id}`, data).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/service-proxies/${id}`),

  toggleActive: (id: string) =>
    apiClient.patch<ServiceProxy>(`/service-proxies/${id}/toggle`).then((r) => r.data),

  toggleRecording: (id: string) =>
    apiClient.patch<ServiceProxy>(`/service-proxies/${id}/toggle-recording`).then((r) => r.data),

  toggleFallback: (id: string) =>
    apiClient.patch<ServiceProxy>(`/service-proxies/${id}/toggle-fallback`).then((r) => r.data),

  getServices: () =>
    apiClient.get<ServiceInfo[]>('/service-proxies/services').then((r) => r.data),
};
```

---

## Phase 3: React Query Hooks

### 3.1 Update Hooks

**File**: `modules/proxy/hooks.ts`

Replace all hooks to use `serviceProxyApi`:

```typescript
// Query keys
const KEYS = {
  all: ['serviceProxies'] as const,
  services: ['serviceProxies', 'services'] as const,
};

export function useServiceProxies() { ... }       // replaces useProxyConfigs
export function useAvailableServices() { ... }    // NEW: for dropdown
export function useCreateServiceProxy() { ... }   // replaces useCreateProxyConfig
export function useUpdateServiceProxy() { ... }   // replaces useUpdateProxyConfig
export function useDeleteServiceProxy() { ... }   // replaces useDeleteProxyConfig
export function useToggleServiceProxy() { ... }   // replaces useToggleProxyConfig
export function useToggleRecording() { ... }       // same name, updated API
export function useToggleFallback() { ... }        // NEW
```

Invalidation: all mutations invalidate `KEYS.all`. Service list mutations also invalidate `KEYS.services`.

---

## Phase 4: Component Redesign

### 4.1 ProxyConfigPage → ServiceProxyPage

**File**: `modules/proxy/pages/ProxyConfigPage.tsx`

Layout change:

**Before**: Flat list of ProxyConfigCards (mixed global/endpoint)

**After**: List of **ServiceProxyCard** grouped by service name. Each card represents one service's proxy config.

Page structure:
```
┌─────────────────────────────────────────────┐
│  Service Proxies              [+ Create]    │
│  Manage proxy targets per service           │
├─────────────────────────────────────────────┤
│  ┌─ user-service ─────────────────────────┐ │
│  │ Target: https://api.example.com        │ │
│  │ Endpoints: 5 │ Fallback: ON │ REC: OFF │ │
│  │ [Toggle] [Fallback] [REC] [Edit] [Del] │ │
│  └────────────────────────────────────────┘ │
│  ┌─ order-service ────────────────────────┐ │
│  │ Target: https://orders.example.com     │ │
│  │ Endpoints: 3 │ Fallback: ON │ REC: ON  │ │
│  │ [Toggle] [Fallback] [REC] [Edit] [Del] │ │
│  └────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
```

### 4.2 ServiceProxyCard (replaces ProxyConfigCard)

**File**: `modules/proxy/components/ServiceProxyCard.tsx` (new, replaces `ProxyConfigCard.tsx`)

Props:
```typescript
interface ServiceProxyCardProps {
  proxy: ServiceProxy;
  onEdit: (proxy: ServiceProxy) => void;
  onDelete: (id: string) => void;
  onToggle: (id: string) => void;
  onToggleRecording: (id: string) => void;
  onToggleFallback: (id: string) => void;
}
```

Display:
- **Title**: Service name (e.g. `user-service`)
- **StatusBadge**: Active/Inactive
- **Recording badge**: Same pulsing red dot (only when active AND recording)
- **Target URL**: displayed in code block
- **Toggle switches**:
  - Active (enable/disable entire proxy for this service)
  - Fallback (enable/disable rule-miss fallback)
  - Recording (enable/disable recording)
- **Strip path prefix** and **Timeout**: show if configured
- Edit / Delete buttons

### 4.3 ServiceProxyForm (replaces ProxyConfigForm)

**File**: `modules/proxy/components/ServiceProxyForm.tsx` (new, replaces `ProxyConfigForm.tsx`)

Form fields:

| Field | Type | Description |
|-------|------|-------------|
| Service Name | Select (dropdown) | Choose from available services. Options from `GET /service-proxies/services`. Only show services without existing proxy (when creating). |
| Target Base URL | Input | Required. e.g. `https://api.example.com` |
| Strip Path Prefix | Input | Optional. e.g. `/api/v1` |
| Timeout | InputNumber | Default 10000ms |
| Forward Headers | Switch | Default true |
| Fallback Enabled | Switch | Default true. NEW field. |
| Recording | Switch | Default false |
| Additional Headers | CodeEditor | JSON key-value pairs |

Key change from old form:
- Remove "Scope" radio (global/endpoint) - no longer needed
- Replace "Endpoint" dropdown with **"Service Name"** dropdown
- Add "Fallback Enabled" switch

### 4.4 RecordingIndicator

**File**: `modules/proxy/components/RecordingIndicator.tsx`

Update to use `useServiceProxies()` instead of `useProxyConfigs()`:

```typescript
const { data: proxies } = useServiceProxies();
const isRecording = proxies?.some((p) => p.isActive && p.isRecording);
```

Same visual behavior.

---

## Phase 5: Integration with Endpoint Detail

### 5.1 Show Proxy Info on Endpoint Detail Page

**File**: `modules/endpoints/pages/EndpointDetailPage.tsx`

Add a small info section showing the service proxy status for this endpoint's service:

```
┌─ Proxy Fallback ─────────────────────────┐
│ Service: user-service                     │
│ Target: https://api.example.com           │
│ Fallback: Enabled                         │
│ When no rule matches, requests will be    │
│ forwarded to the real API.                │
│                            [Configure →]  │
└───────────────────────────────────────────┘
```

- If no proxy configured for this service: show "No proxy configured" with a link to create one
- If proxy exists but fallback disabled: show "Fallback disabled"
- If proxy exists and fallback enabled: show target URL and status
- `[Configure →]` links to the proxy page

This is a read-only info card. It uses `useServiceProxies()` and filters by the endpoint's `serviceName`.

---

## Phase 6: i18n

### 6.1 Update Translation Keys

**Files**: `shared/i18n/en.json`, `shared/i18n/zh-TW.json`

Remove old keys:
```json
"proxy.scope", "proxy.scopeGlobal", "proxy.scopeEndpoint", "proxy.selectEndpoint"
```

Add new keys:
```json
{
  "proxy": {
    "title": "Service Proxies",
    "subtitle": "Manage proxy targets per service for fallback forwarding",
    "noConfigs": "No service proxies configured",
    "serviceName": "Service Name",
    "selectService": "Select Service",
    "targetBaseUrl": "Target Base URL",
    "targetPlaceholder": "https://api.example.com",
    "fallbackEnabled": "Fallback Enabled",
    "fallbackDescription": "Forward to real API when no rule matches",
    "recording": "REC",
    "enableRecording": "Enable Recording",
    "forwardHeaders": "Forward Headers",
    "additionalHeaders": "Additional Headers",
    "stripPathPrefix": "Strip Path Prefix",
    "timeout": "Timeout",
    "deleteConfirm": "Delete this service proxy?",
    "endpointCount": "{{count}} endpoints",
    "form": {
      "title": "Create Service Proxy",
      "editTitle": "Edit Service Proxy"
    },
    "endpointDetail": {
      "title": "Proxy Fallback",
      "noProxy": "No proxy configured for this service",
      "configure": "Configure",
      "fallbackEnabled": "When no rule matches, requests will be forwarded to the real API.",
      "fallbackDisabled": "Fallback is disabled. Unmatched rules will return the default response."
    }
  }
}
```

---

## Phase 7: Sidebar Navigation

### 7.1 Update Nav Label (optional)

Consider renaming sidebar item from "Proxy Config" to "Service Proxies" for clarity.

**File**: `shared/i18n/en.json` → `nav.proxy` key
**File**: `shared/i18n/zh-TW.json` → `nav.proxy` key

---

## Summary of Files Changed

| Action | File | Description |
|--------|------|-------------|
| **MODIFY** | `shared/types/index.ts` | Replace `ProxyConfig` with `ServiceProxy` types |
| **REWRITE** | `modules/proxy/api.ts` | New API client for `/service-proxies` |
| **REWRITE** | `modules/proxy/hooks.ts` | New React Query hooks |
| **REWRITE** | `modules/proxy/pages/ProxyConfigPage.tsx` | Service-based listing |
| **NEW** | `modules/proxy/components/ServiceProxyCard.tsx` | New card component |
| **NEW** | `modules/proxy/components/ServiceProxyForm.tsx` | New form component |
| **MODIFY** | `modules/proxy/components/RecordingIndicator.tsx` | Use new hook |
| **MODIFY** | `modules/endpoints/pages/EndpointDetailPage.tsx` | Add proxy info section |
| **MODIFY** | `shared/i18n/en.json` | Update proxy translation keys |
| **MODIFY** | `shared/i18n/zh-TW.json` | Update proxy translation keys |
| **DELETE** | `modules/proxy/components/ProxyConfigCard.tsx` | Replaced by ServiceProxyCard |
| **DELETE** | `modules/proxy/components/ProxyConfigForm.tsx` | Replaced by ServiceProxyForm |

---

## Suggested Implementation Order

1. Update `shared/types/index.ts` (new types)
2. Rewrite `modules/proxy/api.ts` (new API client)
3. Rewrite `modules/proxy/hooks.ts` (new hooks)
4. Create `ServiceProxyCard.tsx` component
5. Create `ServiceProxyForm.tsx` component
6. Rewrite `ProxyConfigPage.tsx` (compose new components)
7. Update `RecordingIndicator.tsx`
8. Update i18n files (en.json, zh-TW.json)
9. Add proxy info section to `EndpointDetailPage.tsx`
10. Delete old `ProxyConfigCard.tsx`, `ProxyConfigForm.tsx`
11. Test: create service proxy, toggle, edit, delete
12. Test: recording indicator in header
13. Test: endpoint detail shows proxy fallback info

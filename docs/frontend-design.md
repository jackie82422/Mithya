# Mithya Mock Server - Frontend Design Document

> Target: Frontend Team (React + TypeScript)
> Prerequisites: Familiarity with current codebase patterns (modules, TanStack Query, Ant Design, i18n)

---

## Table of Contents

1. [P0: Response Template Engine UI](#1-p0-response-template-engine-ui)
2. [P1: Fault Injection UI](#2-p1-fault-injection-ui)
3. [P1: Proxy / Record & Playback UI](#3-p1-proxy--record--playback-ui)
4. [P2: Stateful Scenarios UI](#4-p2-stateful-scenarios-ui)
5. [P2: Enhanced Request Matching UI](#5-p2-enhanced-request-matching-ui)
6. [Shared Type Changes](#6-shared-type-changes)
7. [i18n Keys](#7-i18n-keys)
8. [Route Changes](#8-route-changes)

---

## Convention Reference

Before implementation, follow these existing patterns:

| Pattern | Example File | Convention |
|---------|-------------|------------|
| Module structure | `modules/endpoints/` | `pages/`, `components/`, `api.ts`, `hooks.ts` |
| API layer | `modules/endpoints/api.ts` | Named export object, methods return `Promise<T>` |
| Query hooks | `modules/endpoints/hooks.ts` | `useXxx()` with `queryKey`, mutation hooks with `invalidateQueries` |
| Form pattern | `modules/endpoints/components/EndpointForm.tsx` | Modal wrapper, `Form.useForm<T>()`, props: `open/onCancel/onSubmit/loading/editing` |
| Array builder | `modules/rules/components/ConditionBuilder.tsx` | Controlled `value/onChange` props, Card per item, add/remove buttons |
| CSS styling | `shared/components/StatusBadge.tsx` | Inline styles + CSS variables `var(--color-*)`, `.pill-tag` class |
| Error handling | All mutation hooks | `onError: (err) => message.error(getApiErrorMessage(err, t('common.error')))` |
| i18n | All components | `const { t } = useTranslation()`, keys in `en.json` + `zh-TW.json` |

---

## 1. P0: Response Template Engine UI

### 1.1 Overview

Add template support to the existing RuleForm. Users can toggle "template mode" for response body, which enables Handlebars syntax with request context variables. Include a live preview panel.

### 1.2 Changes to Existing Components

#### `modules/rules/components/RuleForm.tsx`

Add a template toggle switch and preview section to the existing form:

```
RuleForm Modal (width: 800px)
├── Rule Name + Priority (existing)
├── ConditionBuilder (existing)
├── Divider
├── ResponseEditor (existing, modified)
│   ├── Status Code + Delay (existing)
│   ├── [NEW] Template Toggle Switch
│   │   └── Label: "Enable Template" / "啟用模板"
│   │       Info tooltip: "Use Handlebars syntax to generate dynamic responses"
│   ├── Response Body CodeEditor (existing)
│   │   └── [MODIFIED] When template=ON: show Handlebars language mode
│   ├── [NEW] Template Variable Reference (collapsible panel)
│   │   └── Quick reference of available variables
│   ├── [NEW] Preview Button -> opens preview drawer
│   └── Response Headers (existing)
│       └── [NEW] Headers Template Toggle
```

#### `modules/rules/components/ResponseEditor.tsx`

Add template toggle and variable reference:

```tsx
// New state
const [isTemplate, setIsTemplate] = useState(false);

// Template toggle in the form
<Form.Item name="isTemplate" valuePropName="checked">
  <Space>
    <Switch onChange={setIsTemplate} />
    <span>{t('rules.enableTemplate')}</span>
    <Tooltip title={t('rules.templateTooltip')}>
      <QuestionCircleOutlined />
    </Tooltip>
  </Space>
</Form.Item>

// Conditionally show variable reference
{isTemplate && <TemplateVariableRef />}

// Preview button
{isTemplate && (
  <Button onClick={() => setPreviewOpen(true)}>
    {t('rules.previewTemplate')}
  </Button>
)}
```

### 1.3 New Components

#### `modules/rules/components/TemplateVariableRef.tsx`

A collapsible reference panel showing available template variables:

```
┌─ Template Variables ─────────────────────────┐
│                                               │
│  Request Context:                             │
│  {{request.method}}      -> HTTP method       │
│  {{request.path}}        -> Request path      │
│  {{request.body}}        -> Raw body          │
│  {{request.headers.X}}   -> Header value      │
│  {{request.query.page}}  -> Query param       │
│  {{request.pathParams.id}} -> Path param      │
│                                               │
│  Helpers:                                     │
│  {{now "yyyy-MM-dd"}}    -> Current date      │
│  {{uuid}}                -> Random UUID       │
│  {{randomInt 1 100}}     -> Random integer    │
│  {{jsonPath request.body "$.user.name"}}      │
│  {{math 5 "+" 3}}        -> Arithmetic        │
│                                               │
│  Conditionals:                                │
│  {{#if (eq request.method "POST")}}...{{/if}} │
│  {{#each items}}{{this}}{{/each}}             │
└───────────────────────────────────────────────┘
```

Implementation: Use Ant Design `Collapse` with a single panel. Content is a `Descriptions` component with `column={1}` layout. Each variable shown as `<code>` + description.

#### `modules/rules/components/TemplatePreview.tsx`

A drawer that lets users test their template with sample request data:

```
┌─ Template Preview ────────────────────────────┐
│                                               │
│  Mock Request:                                │
│  ┌─────────────────────────────────────────┐  │
│  │ {                                       │  │
│  │   "method": "POST",                     │  │
│  │   "path": "/api/user/123",              │  │
│  │   "body": "{\"name\": \"John\"}",       │  │
│  │   "headers": { "Accept": "app/json" },  │  │
│  │   "query": { "page": "1" },             │  │
│  │   "pathParams": { "id": "123" }         │  │
│  │ }                                       │  │
│  └─────────────────────────────────────────┘  │
│                                               │
│  [Preview] button                             │
│                                               │
│  Rendered Result:                             │
│  ┌─────────────────────────────────────────┐  │
│  │ { "userId": 123, "name": "John" }      │  │
│  └─────────────────────────────────────────┘  │
│                                               │
│  Error (if any):                              │
│  ┌─────────────────────────────────────────┐  │
│  │ Template error at line 3: unknown helper│  │
│  └─────────────────────────────────────────┘  │
└───────────────────────────────────────────────┘
```

Props:
```tsx
interface TemplatePreviewProps {
  open: boolean;
  onClose: () => void;
  template: string;
  endpointPath: string;  // Pre-populate pathParams from endpoint pattern
  endpointMethod: string;
}
```

API call:
```tsx
const previewApi = {
  preview: (data: TemplatePreviewRequest) =>
    apiClient.post<TemplatePreviewResponse>('/templates/preview', data).then(r => r.data),
};
```

### 1.4 CodeEditor Enhancement

When template mode is on, ideally switch the CodeMirror language extension from `json()` to a Handlebars-aware mode. If a Handlebars CodeMirror extension isn't readily available, keep JSON mode but add a visual indicator (e.g., info banner above the editor).

```tsx
// CodeEditor.tsx - Add optional language prop
interface CodeEditorProps {
  // ... existing props
  language?: 'json' | 'handlebars';
}

// Use different extension based on language
const extensions = useMemo(() => {
  if (language === 'handlebars') return [/* handlebars or plain text extension */];
  return [json()];
}, [language]);
```

### 1.5 Form Data Changes

Update `CreateRuleRequest` type:

```tsx
interface CreateRuleRequest {
  // ... existing fields
  isTemplate?: boolean;           // NEW
  isResponseHeadersTemplate?: boolean;  // NEW
}
```

### 1.6 RuleCard Display

When a rule has `isTemplate: true`, show an indicator on the `RuleCard`:

```tsx
// In RuleCard, next to the status badge
{rule.isTemplate && (
  <span className="pill-tag" style={{
    background: 'var(--rest-bg)',
    color: 'var(--rest-color)',
    ...pillStyles
  }}>
    {t('rules.template')}
  </span>
)}
```

---

## 2. P1: Fault Injection UI

### 2.1 Overview

Add fault injection settings to the existing RuleForm. This is an extension of the ResponseEditor component.

### 2.2 Changes to ResponseEditor

Add a "Fault Injection" section after the delay field:

```
ResponseEditor
├── Status Code + Delay (existing)
├── [NEW] Fault Injection Section
│   ├── Fault Type Selector (Segmented / Radio)
│   │   Options: None | Random Delay | Connection Reset | Empty Response | Malformed | Timeout
│   ├── [Conditional] Fault Config Fields
│   │   ├── RandomDelay: min (ms) + max (ms) InputNumber
│   │   ├── EmptyResponse: status code InputNumber
│   │   ├── MalformedResponse: byte count InputNumber
│   │   └── Timeout: timeout duration (ms) InputNumber
│   └── Warning Alert: "This fault will override the normal response"
├── Response Body (existing, disabled when fault type is ConnectionReset/EmptyResponse/Malformed/Timeout)
└── Response Headers (existing)
```

### 2.3 New Component

#### `modules/rules/components/FaultInjectionConfig.tsx`

```tsx
interface FaultInjectionConfigProps {
  faultType: FaultType;
  faultConfig: FaultConfig | null;
  onFaultTypeChange: (type: FaultType) => void;
  onFaultConfigChange: (config: FaultConfig | null) => void;
}

export default function FaultInjectionConfig({
  faultType, faultConfig, onFaultTypeChange, onFaultConfigChange
}: FaultInjectionConfigProps) {
  return (
    <div>
      <Form.Item label={t('rules.faultType')}>
        <Segmented
          options={[
            { label: t('rules.faultNone'), value: FaultType.None },
            { label: t('rules.faultRandomDelay'), value: FaultType.RandomDelay },
            { label: t('rules.faultConnectionReset'), value: FaultType.ConnectionReset },
            { label: t('rules.faultEmptyResponse'), value: FaultType.EmptyResponse },
            { label: t('rules.faultMalformed'), value: FaultType.MalformedResponse },
            { label: t('rules.faultTimeout'), value: FaultType.Timeout },
          ]}
          value={faultType}
          onChange={onFaultTypeChange}
        />
      </Form.Item>

      {faultType === FaultType.RandomDelay && (
        <Space>
          <Form.Item label={t('rules.faultMinDelay')}>
            <InputNumber min={0} max={60000} addonAfter="ms" />
          </Form.Item>
          <Form.Item label={t('rules.faultMaxDelay')}>
            <InputNumber min={0} max={60000} addonAfter="ms" />
          </Form.Item>
        </Space>
      )}

      {faultType === FaultType.Timeout && (
        <Form.Item label={t('rules.faultTimeoutDuration')}>
          <InputNumber min={1000} max={120000} addonAfter="ms" defaultValue={30000} />
        </Form.Item>
      )}

      {/* ... other fault type configs */}

      {faultType !== FaultType.None && faultType !== FaultType.RandomDelay && (
        <Alert
          type="warning"
          message={t('rules.faultWarning')}
          showIcon
        />
      )}
    </div>
  );
}
```

### 2.4 Visual Indicators

#### RuleCard - Show fault badge

```tsx
{rule.faultType !== FaultType.None && (
  <span className="pill-tag" style={{
    background: 'var(--delete-bg)',
    color: 'var(--delete-color)',
    ...pillStyles
  }}>
    {FaultTypeLabel[rule.faultType]}
  </span>
)}
```

#### LogTable - Show fault indicator

Add a new column or visual marker when `faultTypeApplied` is set in the log entry.

### 2.5 Form Data Changes

```tsx
interface CreateRuleRequest {
  // ... existing fields
  faultType?: FaultType;        // NEW
  faultConfig?: string | null;  // NEW (JSON string)
}
```

---

## 3. P1: Proxy / Record & Playback UI

### 3.1 Overview

New module for configuring proxy settings. This is a new navigation item and set of pages.

### 3.2 Module Structure

```
modules/proxy/
├── pages/
│   └── ProxyConfigPage.tsx
├── components/
│   ├── ProxyConfigForm.tsx
│   ├── ProxyConfigCard.tsx
│   └── RecordingIndicator.tsx
├── api.ts
└── hooks.ts
```

### 3.3 Navigation Change

Add new menu item in `AppLayout.tsx` sidebar:

```tsx
// Between "Request Logs" and "Import / Export"
{
  key: '/proxy',
  icon: <SwapOutlined />,  // or CloudServerOutlined
  label: t('proxy.title'),  // "Proxy Config" / "代理設定"
}
```

### 3.4 Page Design

#### ProxyConfigPage

```
┌─ Proxy Configuration ────────────────────────────────┐
│  Manage proxy forwarding and traffic recording       │
│                                               [+ New] │
│                                                       │
│  ┌─ Global Proxy ────────────────────────────────┐   │
│  │  Target: https://api.example.com    [Active]  │   │
│  │  Recording: ON  ●                             │   │
│  │  Timeout: 10000ms                             │   │
│  │                          [Edit] [Delete]      │   │
│  └───────────────────────────────────────────────┘   │
│                                                       │
│  ┌─ Endpoint Proxy: GET /api/users ──────────────┐   │
│  │  Target: https://staging.api.com    [Active]  │   │
│  │  Recording: OFF                               │   │
│  │  Strip prefix: /api                           │   │
│  │                          [Edit] [Delete]      │   │
│  └───────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────┘
```

### 3.5 ProxyConfigForm

Modal form with the following fields:

```
┌─ Create Proxy Config ─────────────────────────────┐
│                                                    │
│  Scope:  ○ Global (all unmatched requests)        │
│          ○ Specific Endpoint [Select endpoint ▼]  │
│                                                    │
│  Target Base URL *:                                │
│  ┌──────────────────────────────────────────────┐ │
│  │ https://api.example.com                      │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  Strip Path Prefix:                                │
│  ┌──────────────────────────────────────────────┐ │
│  │ /api/v1                                      │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  Timeout:         ┌──────────┐                     │
│                   │ 10000    │ ms                   │
│                   └──────────┘                     │
│                                                    │
│  ☑ Forward Original Headers                        │
│  ☑ Enable Recording                                │
│                                                    │
│  Additional Headers:                               │
│  ┌──────────────────────────────────────────────┐ │
│  │ { "Authorization": "Bearer xxx" }            │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│                        [Cancel]  [Save]            │
└────────────────────────────────────────────────────┘
```

### 3.6 Recording Indicator

When recording is active, show a persistent indicator:

- In the header bar: A pulsing red dot with "Recording" text
- In the Dashboard stats: Add "Recorded Rules" count
- In the Endpoint list: Badge on endpoints that have recorded rules

```tsx
// RecordingIndicator.tsx - shown in AppLayout header when any proxy has recording=ON
export default function RecordingIndicator() {
  const { data: configs } = useProxyConfigs();
  const isRecording = configs?.some(c => c.isActive && c.isRecording);

  if (!isRecording) return null;

  return (
    <span style={{
      display: 'inline-flex',
      alignItems: 'center',
      gap: 6,
      color: 'var(--delete-color)',
      fontSize: 12,
      fontWeight: 500,
    }}>
      <span style={{
        width: 8, height: 8, borderRadius: '50%',
        background: 'var(--delete-color)',
        animation: 'pulse 1.5s infinite',
      }} />
      {t('proxy.recording')}
    </span>
  );
}
```

### 3.7 API Layer

```tsx
// modules/proxy/api.ts
export const proxyApi = {
  getAll: () =>
    apiClient.get<ProxyConfig[]>('/proxy-configs').then(r => r.data),
  getById: (id: string) =>
    apiClient.get<ProxyConfig>(`/proxy-configs/${id}`).then(r => r.data),
  create: (data: CreateProxyConfigRequest) =>
    apiClient.post<ProxyConfig>('/proxy-configs', data).then(r => r.data),
  update: (id: string, data: CreateProxyConfigRequest) =>
    apiClient.put<ProxyConfig>(`/proxy-configs/${id}`, data).then(r => r.data),
  delete: (id: string) =>
    apiClient.delete(`/proxy-configs/${id}`),
  toggleActive: (id: string) =>
    apiClient.patch<ProxyConfig>(`/proxy-configs/${id}/toggle`).then(r => r.data),
  toggleRecording: (id: string) =>
    apiClient.patch<ProxyConfig>(`/proxy-configs/${id}/toggle-recording`).then(r => r.data),
};
```

### 3.8 Hooks

```tsx
// modules/proxy/hooks.ts
export function useProxyConfigs() {
  return useQuery({
    queryKey: ['proxyConfigs'],
    queryFn: proxyApi.getAll,
  });
}

export function useCreateProxyConfig() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: proxyApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['proxyConfigs'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}
// ... same pattern for update, delete, toggle, toggleRecording
```

### 3.9 Log Table Enhancement

Add "Proxied" indicator to log entries:

```tsx
// In LogTable columns, add visual indicator
{log.isProxied && (
  <span className="pill-tag" style={{
    background: 'var(--put-bg)',
    color: 'var(--put-color)',
    ...pillStyles
  }}>
    Proxied
  </span>
)}
```

In LogDetail drawer, show proxy target URL when `isProxied` is true.

---

## 4. P2: Stateful Scenarios UI

### 4.1 Overview

New module for creating and managing stateful scenarios with a visual state machine builder.

### 4.2 Module Structure

```
modules/scenarios/
├── pages/
│   ├── ScenarioListPage.tsx
│   └── ScenarioDetailPage.tsx
├── components/
│   ├── ScenarioCard.tsx
│   ├── ScenarioForm.tsx
│   ├── StepForm.tsx
│   ├── StateFlowDiagram.tsx
│   └── CurrentStateIndicator.tsx
├── api.ts
└── hooks.ts
```

### 4.3 Navigation Change

Add new menu item in `AppLayout.tsx` sidebar:

```tsx
{
  key: '/scenarios',
  icon: <BranchesOutlined />,
  label: t('scenarios.title'),  // "Scenarios" / "場景管理"
}
```

### 4.4 Page Designs

#### ScenarioListPage

Similar to EndpointListPage with cards:

```
┌─ Scenarios ──────────────────────────────────────────┐
│  Define stateful response workflows                  │
│                                              [+ New] │
│                                                      │
│  ┌─ User Login Flow ─────────────────────────────┐  │
│  │  States: logged_out -> authenticated           │  │
│  │          -> session_expired                    │  │
│  │  Current: authenticated  ● Active              │  │
│  │  3 steps                  [Reset] [Edit] [Del] │  │
│  └────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────┘
```

#### ScenarioDetailPage

Route: `/scenarios/:id`

```
┌─ Scenarios / User Login Flow ─────────────────────────┐
│  ← User Login Flow        [Active] [Reset State]     │
│                                                        │
│  Current State: ● authenticated                        │
│  Initial State: logged_out                             │
│                                                        │
│  ┌─ State Flow Diagram ──────────────────────────┐    │
│  │                                                │    │
│  │  [logged_out] --login ok--> [authenticated]    │    │
│  │       ↑                         │              │    │
│  │       └---login---[expired] <---┘              │    │
│  │                                                │    │
│  └────────────────────────────────────────────────┘    │
│                                                        │
│  ─── Steps (3) ──────────────────── [+ Add Step] ──   │
│                                                        │
│  State: logged_out                                     │
│  ┌────────────────────────────────────────────────┐   │
│  │ #1  POST /api/login                            │   │
│  │     Condition: Body.$.password Equals "123"    │   │
│  │     → 200 {token: "abc"} → Next: authenticated │   │
│  │                              [Edit] [Delete]   │   │
│  └────────────────────────────────────────────────┘   │
│                                                        │
│  State: authenticated                                  │
│  ┌────────────────────────────────────────────────┐   │
│  │ #2  GET /api/profile                           │   │
│  │     → 200 {name: "John"} → Stay: authenticated │   │
│  │                              [Edit] [Delete]   │   │
│  └────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────┘
```

### 4.5 State Flow Diagram Component

Use Mermaid.js for rendering state diagrams:

```tsx
// components/StateFlowDiagram.tsx
interface StateFlowDiagramProps {
  steps: ScenarioStep[];
  initialState: string;
  currentState: string;
}

export default function StateFlowDiagram({ steps, initialState, currentState }: StateFlowDiagramProps) {
  const mermaidCode = useMemo(() => {
    let code = 'stateDiagram-v2\n';
    code += `  [*] --> ${initialState}\n`;

    // Group steps by state, show transitions
    const transitions = new Set<string>();
    steps.forEach(step => {
      if (step.nextState && step.nextState !== step.stateName) {
        const key = `${step.stateName} --> ${step.nextState}`;
        if (!transitions.has(key)) {
          code += `  ${step.stateName} --> ${step.nextState}\n`;
          transitions.add(key);
        }
      }
    });

    return code;
  }, [steps, initialState]);

  // Render using mermaid library or pre-rendered SVG
  return <div className="state-flow-diagram" dangerouslySetInnerHTML={{ __html: renderedSvg }} />;
}
```

Alternatively, use a simpler approach with Ant Design Steps or custom SVG if Mermaid is too heavy.

### 4.6 StepForm

Modal form for adding/editing scenario steps:

```
┌─ Add Scenario Step ─────────────────────────────────┐
│                                                      │
│  State Name *:     ┌────────────────────────────┐   │
│                    │ logged_out              ▼   │   │
│                    └────────────────────────────┘   │
│                    (Select existing or type new)     │
│                                                      │
│  Endpoint *:       ┌────────────────────────────┐   │
│                    │ POST /api/login         ▼   │   │
│                    └────────────────────────────┘   │
│                    (Select from existing endpoints)  │
│                                                      │
│  Additional Conditions:                              │
│  ┌─ ConditionBuilder (reuse existing) ───────────┐  │
│  │  Body.$.password  Equals  "valid123"          │  │
│  └───────────────────────────────────────────────┘  │
│                                                      │
│  Response:                                           │
│  ┌─ ResponseEditor (reuse existing) ─────────────┐  │
│  │  Status: 200  Delay: 0ms                      │  │
│  │  Body: { "token": "abc123" }                  │  │
│  └───────────────────────────────────────────────┘  │
│                                                      │
│  Next State:       ┌────────────────────────────┐   │
│                    │ authenticated           ▼   │   │
│                    └────────────────────────────┘   │
│                    (Select existing or type new)     │
│                    Leave empty = stay in same state  │
│                                                      │
│  Priority:         ┌────────┐                        │
│                    │ 100    │                        │
│                    └────────┘                        │
│                                                      │
│                          [Cancel]  [Save]            │
└──────────────────────────────────────────────────────┘
```

Key: Reuse existing `ConditionBuilder` and `ResponseEditor` components.

### 4.7 API Layer

```tsx
// modules/scenarios/api.ts
export const scenariosApi = {
  getAll: () => apiClient.get<Scenario[]>('/scenarios').then(r => r.data),
  getById: (id: string) => apiClient.get<Scenario>(`/scenarios/${id}`).then(r => r.data),
  create: (data: CreateScenarioRequest) => apiClient.post<Scenario>('/scenarios', data).then(r => r.data),
  update: (id: string, data: UpdateScenarioRequest) => apiClient.put<Scenario>(`/scenarios/${id}`, data).then(r => r.data),
  delete: (id: string) => apiClient.delete(`/scenarios/${id}`),
  toggleActive: (id: string) => apiClient.patch<Scenario>(`/scenarios/${id}/toggle`).then(r => r.data),
  resetState: (id: string) => apiClient.post(`/scenarios/${id}/reset`),
  addStep: (id: string, data: CreateStepRequest) => apiClient.post<ScenarioStep>(`/scenarios/${id}/steps`, data).then(r => r.data),
  updateStep: (id: string, stepId: string, data: CreateStepRequest) => apiClient.put<ScenarioStep>(`/scenarios/${id}/steps/${stepId}`, data).then(r => r.data),
  deleteStep: (id: string, stepId: string) => apiClient.delete(`/scenarios/${id}/steps/${stepId}`),
};
```

---

## 5. P2: Enhanced Request Matching UI

### 5.1 Overview

Enhance the existing ConditionBuilder to support AND/OR logic toggle and new operators.

### 5.2 Changes to ConditionBuilder

#### Logic Mode Toggle

Add a segmented toggle at the top of the ConditionBuilder:

```
┌─ Match Conditions ────────────────────────────────────┐
│                                                        │
│  Logic: [AND] [OR]                                     │
│                                                        │
│  ┌─ Condition 1 ──────────────────────────────────┐   │
│  │ Body  $.user.name  Equals  "John"      [×]    │   │
│  └────────────────────────────────────────────────┘   │
│  {AND/OR indicator between conditions}                 │
│  ┌─ Condition 2 ──────────────────────────────────┐   │
│  │ Header  Authorization  Exists          [×]     │   │
│  └────────────────────────────────────────────────┘   │
│                                                        │
│  [+ Add Condition]                                     │
└────────────────────────────────────────────────────────┘
```

```tsx
// ConditionBuilder.tsx - Add logicMode prop
interface ConditionBuilderProps {
  value?: MatchCondition[];
  onChange?: (conditions: MatchCondition[]) => void;
  logicMode?: LogicMode;             // NEW
  onLogicModeChange?: (mode: LogicMode) => void;  // NEW
}

// Between condition cards, show logic connector
{index < value.length - 1 && (
  <div style={{ textAlign: 'center', padding: '4px 0', color: 'var(--color-text-secondary)' }}>
    <span className="pill-tag" style={{ fontSize: 11 }}>
      {logicMode === 'AND' ? t('rules.logicAnd') : t('rules.logicOr')}
    </span>
  </div>
)}
```

#### New Operators in Dropdown

Add to the operator `Select` options:

```tsx
const operatorOptions = [
  // Existing
  { value: MatchOperator.Equals, label: t('rules.opEquals') },
  { value: MatchOperator.Contains, label: t('rules.opContains') },
  { value: MatchOperator.Regex, label: t('rules.opRegex') },
  { value: MatchOperator.StartsWith, label: t('rules.opStartsWith') },
  { value: MatchOperator.EndsWith, label: t('rules.opEndsWith') },
  { value: MatchOperator.GreaterThan, label: t('rules.opGreaterThan') },
  { value: MatchOperator.LessThan, label: t('rules.opLessThan') },
  { value: MatchOperator.Exists, label: t('rules.opExists') },
  // New
  { value: MatchOperator.NotEquals, label: t('rules.opNotEquals') },
  { value: MatchOperator.JsonSchema, label: t('rules.opJsonSchema') },
  { value: MatchOperator.IsEmpty, label: t('rules.opIsEmpty') },
  { value: MatchOperator.NotExists, label: t('rules.opNotExists') },
];
```

#### Value Field Adaptation

When operator is `Exists`, `IsEmpty`, or `NotExists`, hide the value input field (no value needed):

```tsx
{![MatchOperator.Exists, MatchOperator.IsEmpty, MatchOperator.NotExists].includes(condition.operator) && (
  <Input
    value={condition.value}
    onChange={(e) => update(index, 'value', e.target.value)}
    placeholder={t('rules.matchValue')}
  />
)}
```

When operator is `JsonSchema`, replace the value Input with a CodeEditor for the JSON schema:

```tsx
{condition.operator === MatchOperator.JsonSchema && (
  <CodeEditor
    value={condition.value}
    onChange={(val) => update(index, 'value', val)}
    height={120}
  />
)}
```

### 5.3 Form Data Changes

Add `logicMode` to `CreateRuleRequest`:

```tsx
interface CreateRuleRequest {
  // ... existing fields
  logicMode?: 'AND' | 'OR';  // NEW, default 'AND'
}
```

### 5.4 RuleCard Display

Show logic mode indicator when it's `OR` (since AND is default):

```tsx
{rule.logicMode === 'OR' && (
  <span className="pill-tag" style={{ ... }}>
    OR
  </span>
)}
```

---

## 6. Shared Type Changes

Add to `shared/types/index.ts`:

```tsx
// ── P0: Template ──
// (No new types, just add fields to CreateRuleRequest)

// ── P1: Fault Injection ──
export enum FaultType {
  None = 0,
  FixedDelay = 1,
  RandomDelay = 2,
  ConnectionReset = 3,
  EmptyResponse = 4,
  MalformedResponse = 5,
  Timeout = 6,
}

export const FaultTypeLabel: Record<FaultType, string> = {
  [FaultType.None]: 'None',
  [FaultType.FixedDelay]: 'Fixed Delay',
  [FaultType.RandomDelay]: 'Random Delay',
  [FaultType.ConnectionReset]: 'Connection Reset',
  [FaultType.EmptyResponse]: 'Empty Response',
  [FaultType.MalformedResponse]: 'Malformed Response',
  [FaultType.Timeout]: 'Timeout',
};

// ── P1: Proxy ──
export interface ProxyConfig {
  id: string;
  endpointId: string | null;
  targetBaseUrl: string;
  isActive: boolean;
  isRecording: boolean;
  forwardHeaders: boolean;
  additionalHeaders: string | null;
  timeoutMs: number;
  stripPathPrefix: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProxyConfigRequest {
  endpointId?: string | null;
  targetBaseUrl: string;
  isRecording?: boolean;
  forwardHeaders?: boolean;
  additionalHeaders?: Record<string, string> | null;
  timeoutMs?: number;
  stripPathPrefix?: string | null;
}

// ── P2: Scenarios ──
export interface Scenario {
  id: string;
  name: string;
  description: string | null;
  initialState: string;
  currentState: string;
  isActive: boolean;
  steps: ScenarioStep[];
  createdAt: string;
  updatedAt: string;
}

export interface ScenarioStep {
  id: string;
  scenarioId: string;
  stateName: string;
  endpointId: string;
  matchConditions: string | null;
  responseStatusCode: number;
  responseBody: string;
  responseHeaders: string | null;
  isTemplate: boolean;
  delayMs: number;
  nextState: string | null;
  priority: number;
}

export interface CreateScenarioRequest {
  name: string;
  description?: string;
  initialState: string;
}

export interface CreateStepRequest {
  stateName: string;
  endpointId: string;
  matchConditions?: MatchCondition[];
  responseStatusCode: number;
  responseBody: string;
  responseHeaders?: Record<string, string>;
  isTemplate?: boolean;
  delayMs?: number;
  nextState?: string | null;
  priority?: number;
}

// ── P2: Enhanced Matching ──
export enum MatchOperator {
  // ... existing values 1-8
  NotEquals = 9,
  JsonSchema = 10,
  IsEmpty = 11,
  NotExists = 12,
}

export type LogicMode = 'AND' | 'OR';

// ── Updated MockRule ──
// Add to existing MockRule interface:
//   isTemplate: boolean;
//   isResponseHeadersTemplate: boolean;
//   faultType: FaultType;
//   faultConfig: string | null;
//   logicMode: LogicMode;

// ── Updated MockRequestLog ──
// Add to existing MockRequestLog interface:
//   isProxied: boolean;
//   proxyTargetUrl: string | null;
//   faultTypeApplied: FaultType | null;
```

---

## 7. i18n Keys

### English (`en.json`) - Add these keys:

```json
{
  "rules": {
    "enableTemplate": "Enable Template",
    "templateTooltip": "Use Handlebars syntax {{variable}} for dynamic responses",
    "previewTemplate": "Preview",
    "template": "Template",
    "templateVariables": "Template Variables",
    "templateVarMethod": "HTTP method (GET, POST...)",
    "templateVarPath": "Request path",
    "templateVarBody": "Raw request body",
    "templateVarHeaders": "Header value",
    "templateVarQuery": "Query parameter",
    "templateVarPathParams": "Path parameter",
    "templateHelperNow": "Current date/time",
    "templateHelperUuid": "Random UUID",
    "templateHelperRandomInt": "Random integer in range",
    "templateHelperJsonPath": "Extract from JSON body",
    "templateHelperMath": "Arithmetic operation",
    "mockRequest": "Mock Request",
    "renderedResult": "Rendered Result",

    "faultType": "Fault Injection",
    "faultNone": "None",
    "faultRandomDelay": "Random Delay",
    "faultConnectionReset": "Connection Reset",
    "faultEmptyResponse": "Empty Response",
    "faultMalformed": "Malformed Response",
    "faultTimeout": "Timeout",
    "faultMinDelay": "Min Delay",
    "faultMaxDelay": "Max Delay",
    "faultTimeoutDuration": "Timeout Duration",
    "faultByteCount": "Byte Count",
    "faultWarning": "This fault will override the normal response body",

    "logicMode": "Logic Mode",
    "logicAnd": "AND",
    "logicOr": "OR",
    "opNotEquals": "Not Equals",
    "opJsonSchema": "JSON Schema",
    "opIsEmpty": "Is Empty",
    "opNotExists": "Not Exists"
  },

  "proxy": {
    "title": "Proxy Config",
    "subtitle": "Configure proxy forwarding and traffic recording",
    "targetBaseUrl": "Target Base URL",
    "stripPathPrefix": "Strip Path Prefix",
    "forwardHeaders": "Forward Original Headers",
    "additionalHeaders": "Additional Headers",
    "timeout": "Timeout",
    "enableRecording": "Enable Recording",
    "recording": "Recording",
    "scope": "Scope",
    "scopeGlobal": "Global (all unmatched requests)",
    "scopeEndpoint": "Specific Endpoint",
    "selectEndpoint": "Select Endpoint",
    "proxied": "Proxied",
    "proxyTarget": "Proxy Target"
  },

  "scenarios": {
    "title": "Scenarios",
    "subtitle": "Define stateful response workflows",
    "name": "Scenario Name",
    "description": "Description",
    "initialState": "Initial State",
    "currentState": "Current State",
    "resetState": "Reset State",
    "resetConfirm": "Reset this scenario to its initial state?",
    "addStep": "Add Step",
    "stateName": "State Name",
    "selectEndpoint": "Select Endpoint",
    "nextState": "Next State",
    "nextStateHint": "Leave empty to stay in current state",
    "stateFlow": "State Flow",
    "steps": "Steps"
  }
}
```

### Traditional Chinese (`zh-TW.json`) - Add these keys:

```json
{
  "rules": {
    "enableTemplate": "啟用模板",
    "templateTooltip": "使用 Handlebars 語法 {{variable}} 產生動態回應",
    "previewTemplate": "預覽",
    "template": "模板",
    "templateVariables": "模板變數",
    "templateVarMethod": "HTTP 方法 (GET, POST...)",
    "templateVarPath": "請求路徑",
    "templateVarBody": "原始請求內容",
    "templateVarHeaders": "標頭值",
    "templateVarQuery": "查詢參數",
    "templateVarPathParams": "路徑參數",
    "templateHelperNow": "目前日期/時間",
    "templateHelperUuid": "隨機 UUID",
    "templateHelperRandomInt": "範圍內隨機整數",
    "templateHelperJsonPath": "從 JSON 主體擷取",
    "templateHelperMath": "算術運算",
    "mockRequest": "模擬請求",
    "renderedResult": "渲染結果",

    "faultType": "故障注入",
    "faultNone": "無",
    "faultRandomDelay": "隨機延遲",
    "faultConnectionReset": "連線重置",
    "faultEmptyResponse": "空回應",
    "faultMalformed": "畸形回應",
    "faultTimeout": "逾時",
    "faultMinDelay": "最小延遲",
    "faultMaxDelay": "最大延遲",
    "faultTimeoutDuration": "逾時時間",
    "faultByteCount": "位元組數",
    "faultWarning": "此故障將覆蓋正常的回應內容",

    "logicMode": "邏輯模式",
    "logicAnd": "且 (AND)",
    "logicOr": "或 (OR)",
    "opNotEquals": "不等於",
    "opJsonSchema": "JSON Schema",
    "opIsEmpty": "為空",
    "opNotExists": "不存在"
  },

  "proxy": {
    "title": "代理設定",
    "subtitle": "設定代理轉發與流量錄製",
    "targetBaseUrl": "目標基礎網址",
    "stripPathPrefix": "移除路徑前綴",
    "forwardHeaders": "轉發原始標頭",
    "additionalHeaders": "附加標頭",
    "timeout": "逾時時間",
    "enableRecording": "啟用錄製",
    "recording": "錄製中",
    "scope": "範圍",
    "scopeGlobal": "全域（所有未匹配請求）",
    "scopeEndpoint": "指定端點",
    "selectEndpoint": "選擇端點",
    "proxied": "已代理",
    "proxyTarget": "代理目標"
  },

  "scenarios": {
    "title": "場景管理",
    "subtitle": "定義有狀態的回應工作流程",
    "name": "場景名稱",
    "description": "描述",
    "initialState": "初始狀態",
    "currentState": "目前狀態",
    "resetState": "重置狀態",
    "resetConfirm": "確定要將此場景重置為初始狀態嗎？",
    "addStep": "新增步驟",
    "stateName": "狀態名稱",
    "selectEndpoint": "選擇端點",
    "nextState": "下一個狀態",
    "nextStateHint": "留空表示保持在目前狀態",
    "stateFlow": "狀態流程",
    "steps": "步驟"
  }
}
```

---

## 8. Route Changes

### `App.tsx` - Add new routes:

```tsx
<Routes>
  <Route element={<AppLayout />}>
    <Route path="/" element={<DashboardPage />} />
    <Route path="/endpoints" element={<EndpointListPage />} />
    <Route path="/endpoints/:id" element={<EndpointDetailPage />} />
    <Route path="/logs" element={<LogListPage />} />
    {/* NEW: P1 */}
    <Route path="/proxy" element={<ProxyConfigPage />} />
    {/* NEW: P2 */}
    <Route path="/scenarios" element={<ScenarioListPage />} />
    <Route path="/scenarios/:id" element={<ScenarioDetailPage />} />
    {/* Existing */}
    <Route path="/import-export" element={<ImportExportPage />} />
    <Route path="*" element={<NotFoundPage />} />
  </Route>
</Routes>
```

### `AppLayout.tsx` - Update menu items:

```tsx
const menuItems = [
  { key: '/', icon: <DashboardOutlined />, label: t('dashboard.title') },
  { key: '/endpoints', icon: <ApiOutlined />, label: t('endpoints.title') },
  { key: '/logs', icon: <FileTextOutlined />, label: t('logs.title') },
  { key: '/proxy', icon: <CloudServerOutlined />, label: t('proxy.title') },        // NEW P1
  { key: '/scenarios', icon: <BranchesOutlined />, label: t('scenarios.title') },    // NEW P2
  { key: '/import-export', icon: <SwapOutlined />, label: t('importExport.title') },
];
```

---

## Implementation Notes

1. **P0 (Template Engine)** only modifies existing components — no new pages or routes. Fastest to ship.
2. **P1 (Fault Injection)** also only modifies existing components. Can ship together with P0.
3. **P1 (Proxy)** is a new module. Follow the exact same pattern as `modules/endpoints/`.
4. **P2 features** are new modules. Can be developed independently and in parallel.
5. All new `pill-tag` badges should use CSS variables for theme compatibility.
6. All form validation messages should use i18n keys.
7. Use `Form.useForm<T>()` for type-safe form handling.
8. Use `useQuery` / `useMutation` with proper `queryKey` invalidation.

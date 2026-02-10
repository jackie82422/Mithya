# Mithya Mock Server - E2E Test Plan

> Scope: Full end-to-end testing covering UI interactions, API behavior, and mock engine correctness
> Stack: Playwright (browser E2E) + Backend Integration Tests (xUnit, existing)
> Environments: Docker Compose (full stack) or local dev (`frontend:3000` + `backend:5050`)

---

## Table of Contents

1. [Test Strategy](#1-test-strategy)
2. [Test Environment Setup](#2-test-environment-setup)
3. [Test Suite: Dashboard](#3-test-suite-dashboard)
4. [Test Suite: Endpoint Management](#4-test-suite-endpoint-management)
5. [Test Suite: Rule Management](#5-test-suite-rule-management)
6. [Test Suite: Mock Engine (Request Matching)](#6-test-suite-mock-engine-request-matching)
7. [Test Suite: Request Logs](#7-test-suite-request-logs)
8. [Test Suite: Import / Export](#8-test-suite-import--export)
9. [Test Suite: Theme & i18n](#9-test-suite-theme--i18n)
10. [Test Suite: Responsive / Mobile](#10-test-suite-responsive--mobile)
11. [Test Suite: P0 - Response Template Engine](#11-test-suite-p0---response-template-engine)
12. [Test Suite: P1 - Fault Injection](#12-test-suite-p1---fault-injection)
13. [Test Suite: P1 - Proxy / Record & Playback](#13-test-suite-p1---proxy--record--playback)
14. [Test Suite: P2 - Stateful Scenarios](#14-test-suite-p2---stateful-scenarios)
15. [Test Suite: P2 - Enhanced Request Matching](#15-test-suite-p2---enhanced-request-matching)
16. [Test Data & Fixtures](#16-test-data--fixtures)
17. [CI/CD Integration](#17-cicd-integration)

---

## 1. Test Strategy

### 1.1 Test Pyramid

```
        ┌──────────┐
        │  E2E UI  │  <- Playwright (this document)
        │  Tests   │     Browser-based, full workflow
       ┌┴──────────┴┐
       │ Integration │  <- xUnit (existing in backend/tests/)
       │   Tests     │     API-level, InMemory DB
      ┌┴─────────────┴┐
      │   Unit Tests    │  <- xUnit (existing, extend)
      │                 │     OperatorEvaluator, PathMatcher, etc.
      └─────────────────┘
```

### 1.2 E2E Test Scope

| Category | What We Test | How |
|----------|-------------|-----|
| **UI Workflow** | User can complete tasks end-to-end through the browser | Playwright browser automation |
| **API + Engine** | Mock engine returns correct responses for given rules | Playwright `request` API (HTTP calls) |
| **Data Integrity** | Created data appears correctly in UI after refresh | Playwright assertions on page content |
| **Cross-cutting** | Theme, i18n, responsive layout work correctly | Playwright viewport/DOM assertions |

### 1.3 Test Naming Convention

```
test('[Module] Action - Expected Result')

Examples:
test('[Endpoint] Create REST endpoint - appears in list')
test('[Rule] Match by body JSONPath - returns correct response')
test('[Theme] Toggle dark mode - all pages render correctly')
```

### 1.4 Test Independence

- Each test suite uses `beforeEach` / `beforeAll` to set up required data via API calls (not UI)
- Each test suite uses `afterAll` to clean up via `DELETE` API calls
- Tests should NOT depend on execution order within a suite
- Use unique names/paths per test to avoid collision (e.g., `/api/test-{testId}`)

---

## 2. Test Environment Setup

### 2.1 Playwright Configuration

```
e2e/
├── playwright.config.ts
├── fixtures/
│   ├── api-helper.ts         # Direct API call utilities
│   ├── test-data.ts           # Shared test data constants
│   └── openapi-sample.json    # Sample OpenAPI spec for import tests
├── tests/
│   ├── dashboard.spec.ts
│   ├── endpoints.spec.ts
│   ├── rules.spec.ts
│   ├── mock-engine.spec.ts
│   ├── logs.spec.ts
│   ├── import-export.spec.ts
│   ├── theme-i18n.spec.ts
│   ├── responsive.spec.ts
│   ├── template-engine.spec.ts    # P0
│   ├── fault-injection.spec.ts    # P1
│   ├── proxy.spec.ts              # P1
│   ├── scenarios.spec.ts          # P2
│   └── enhanced-matching.spec.ts  # P2
└── package.json
```

### 2.2 Playwright Config

```typescript
// e2e/playwright.config.ts
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  retries: 1,
  workers: 1,  // Sequential to avoid mock data conflicts
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:3000',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { browserName: 'chromium' } },
  ],
});
```

### 2.3 API Helper Fixture

```typescript
// e2e/fixtures/api-helper.ts
const ADMIN_API = process.env.ADMIN_API_URL || 'http://localhost:5050/admin/api';
const MOCK_API = process.env.MOCK_API_URL || 'http://localhost:5050';

export class ApiHelper {
  constructor(private request: APIRequestContext) {}

  // Endpoints
  async createEndpoint(data: object) { ... }
  async deleteEndpoint(id: string) { ... }
  async deleteAllEndpoints() { ... }

  // Rules
  async createRule(endpointId: string, data: object) { ... }

  // Logs
  async clearLogs() { ... }

  // Mock requests (hit the mock API, not admin API)
  async sendMockRequest(method: string, path: string, options?: object) { ... }
}
```

---

## 3. Test Suite: Dashboard

**File**: `tests/dashboard.spec.ts`

### Test Cases

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| D-01 | Dashboard loads with stats | Navigate to `/` | Shows 4 stat cards (endpoints, active, rules, requests) |
| D-02 | Stats reflect actual data | Create 2 endpoints via API, navigate to `/` | Endpoint count = 2, Active = 2 |
| D-03 | Mock Server URL displayed | Navigate to `/` | Shows `http://localhost:5050` with copy button |
| D-04 | Copy URL button works | Click copy button | Clipboard contains the mock server URL |
| D-05 | Endpoint overview table | Create 3 endpoints via API | Table shows all 3 with correct name, protocol, method, path, status |
| D-06 | Click endpoint row navigates | Click an endpoint row in overview table | Navigates to `/endpoints/{id}` |
| D-07 | Recent logs section | Send 2 mock requests via API, reload dashboard | Recent logs show the 2 requests with correct method, path, status |
| D-08 | Match rate calculation | Send 3 requests (2 matched, 1 unmatched) | Stats show "Match Rate 67%" |
| D-09 | Empty state | Clear all endpoints | Shows empty state with CTA button "Create your first endpoint" |
| D-10 | Stats auto-update on data change | Create endpoint, go to dashboard | Stats immediately reflect the new endpoint |

---

## 4. Test Suite: Endpoint Management

**File**: `tests/endpoints.spec.ts`

### CRUD Operations

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| E-01 | Create REST endpoint | Click "+ Create Endpoint", fill form (REST, GET, /api/users), submit | Endpoint appears in list with correct badges |
| E-02 | Create SOAP endpoint | Create with protocol=SOAP, method=POST | Shows SOAP protocol tag, POST method tag |
| E-03 | Create endpoint with path params | Create with path `/api/users/{id}` | Path displays correctly with `{id}` |
| E-04 | Validation - empty name | Submit form with name empty | Shows validation error, form does not submit |
| E-05 | Validation - empty path | Submit form with path empty | Shows validation error |
| E-06 | Validation - duplicate path+method | Create 2 endpoints with same path+method | Second creation shows conflict error |
| E-07 | Edit endpoint | Click edit on existing endpoint, change name, save | Name updates in list |
| E-08 | Delete endpoint | Click delete, confirm | Endpoint removed from list |
| E-09 | Delete endpoint with rules | Delete endpoint that has rules | All rules also deleted, no orphans |
| E-10 | Toggle active/inactive | Click toggle switch | Status badge changes, switch reflects new state |

### Search & Batch

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| E-11 | Search by name | Type endpoint name in search box | Only matching endpoints shown |
| E-12 | Search by path | Type path fragment in search box | Filters correctly |
| E-13 | Search by service name | Type service name | Filters correctly |
| E-14 | Search keyboard shortcut | Press Cmd+K (or Ctrl+K) | Search box receives focus |
| E-15 | Search no results | Type non-existent term | Shows empty state |
| E-16 | Batch mode - toggle | Click "Batch", select 2 endpoints, disable | Both endpoints become inactive |
| E-17 | Batch mode - delete | Click "Batch", select 2 endpoints, delete, confirm | Both endpoints removed |
| E-18 | Batch mode - cancel | Enter batch mode, click cancel | Returns to normal mode, no changes |

### Default Response

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| E-19 | Set default response | Click "Set Default Response", enter status 200 + body | Default response saved |
| E-20 | Default response works | Set default, send request that matches no rules | Returns the default response |
| E-21 | Clear default response | Set default then clear it | Unmatched requests return 404 again |

---

## 5. Test Suite: Rule Management

**File**: `tests/rules.spec.ts`

### CRUD Operations

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| R-01 | Create rule with body condition | Navigate to endpoint detail, click "+ Add Rule", fill form with Body/$.id/Equals/123 | Rule appears in list with condition badge |
| R-02 | Create rule with header condition | Add condition: Header/Authorization/Contains/Bearer | Condition displayed as "Header.Authorization Contains Bearer" |
| R-03 | Create rule with query condition | Add condition: Query/page/Equals/1 | Condition displayed correctly |
| R-04 | Create rule with path condition | Add condition: Path/id/Equals/123 | Condition displayed correctly |
| R-05 | Create rule with multiple conditions | Add 3 conditions (body + header + query) | All 3 conditions shown in rule card |
| R-06 | Create rule with response headers | Fill response headers JSON `{"X-Custom": "test"}` | Headers saved and displayed in detail |
| R-07 | Create rule with delay | Set delayMs = 500 | Rule card shows "Delay: 500ms" |
| R-08 | Edit rule | Click edit, change name and status code, save | Updated values displayed |
| R-09 | Delete rule | Click delete, confirm | Rule removed from list |
| R-10 | Toggle rule active | Click toggle on rule | Status changes |

### Rule Card UI

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| R-11 | Expand rule detail | Click rule card expand arrow | Shows response body and headers with syntax highlighting |
| R-12 | Collapse rule detail | Click expanded rule card arrow | Detail section hides |
| R-13 | Copy cURL command | Click cURL button on rule card | Clipboard contains valid cURL command |
| R-14 | Priority display | Create 2 rules with priority 1 and 100 | Priority badges show #1 and #100 |

### Validation

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| R-15 | Empty rule name rejected | Submit rule form with empty name | Validation error shown |
| R-16 | Invalid body field path | Enter body condition with path "id" (no $.) | Validation error: must start with $. |
| R-17 | Status code range validation | Enter status code 999 | Validation error |
| R-18 | Empty response body rejected | Submit with empty response body | Validation error shown |

---

## 6. Test Suite: Mock Engine (Request Matching)

**File**: `tests/mock-engine.spec.ts`

> These tests verify the core mock engine by sending actual HTTP requests to the mock API port and asserting the responses. Use Playwright's `request` API or direct `fetch`.

### Basic Matching

| # | Test Case | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| M-01 | Exact body match | Rule: Body.$.id Equals "1" -> 200 `{"found":true}` | `POST /api/test {"id":"1"}` | 200 `{"found":true}` |
| M-02 | Body match no match | Same rule as M-01 | `POST /api/test {"id":"2"}` | Default response or 404 |
| M-03 | Header match | Rule: Header.X-Api-Key Equals "secret" -> 200 | `GET /api/test` with `X-Api-Key: secret` | 200 |
| M-04 | Query match | Rule: Query.status Equals "active" -> 200 | `GET /api/test?status=active` | 200 |
| M-05 | Path param match | Endpoint: `/api/users/{id}`, Rule: Path.id Equals "42" -> 200 | `GET /api/users/42` | 200 |
| M-06 | Path param no match | Same as M-05 | `GET /api/users/99` | Default or 404 |

### Operator Tests

| # | Test Case | Operator | Condition | Request Value | Expected |
|---|-----------|----------|-----------|---------------|----------|
| M-07 | Contains operator | Contains | Body.$.name Contains "john" | `{"name":"john doe"}` | Match |
| M-08 | Regex operator | Regex | Body.$.email Regex `^[a-z]+@` | `{"email":"test@example.com"}` | Match |
| M-09 | StartsWith operator | StartsWith | Header.Accept StartsWith "application/" | `Accept: application/json` | Match |
| M-10 | EndsWith operator | EndsWith | Body.$.file EndsWith ".pdf" | `{"file":"report.pdf"}` | Match |
| M-11 | GreaterThan operator | GreaterThan | Body.$.age GreaterThan "18" | `{"age":"25"}` | Match |
| M-12 | LessThan operator | LessThan | Body.$.price LessThan "100" | `{"price":"50"}` | Match |
| M-13 | Exists operator | Exists | Header.Authorization Exists | Request with `Authorization: xxx` | Match |
| M-14 | Exists negative | Exists | Header.Authorization Exists | Request without Authorization | No match |

### Priority & Multiple Rules

| # | Test Case | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| M-15 | Higher priority wins | Rule A (priority 1): Body.$.type Equals "a" -> 200 `{"rule":"A"}`; Rule B (priority 100): Body.$.type Equals "a" -> 200 `{"rule":"B"}` | `POST {"type":"a"}` | Returns `{"rule":"A"}` (lower priority number = higher priority) |
| M-16 | First match by priority | Rule priority=1 matches, Rule priority=2 also matches | Any request | Rule 1 response returned |
| M-17 | Multiple conditions AND | Rule: Body.$.id Equals "1" AND Header.X-Key Equals "abc" | Request with both matching | Match |
| M-18 | Multiple conditions AND fail | Same rule as M-17 | Request with body match but wrong header | No match |

### Response Features

| # | Test Case | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| M-19 | Custom response headers | Rule with headers `{"X-Custom":"hello"}` | Any matching request | Response has `X-Custom: hello` |
| M-20 | Response delay | Rule with delayMs=500 | Matching request | Response time >= 500ms |
| M-21 | Default response fallback | Endpoint has default response, no rules match | Any request | Returns default response body + status |
| M-22 | Inactive endpoint ignored | Create endpoint, toggle inactive | Any request | Returns 404, not the endpoint |
| M-23 | Inactive rule ignored | Create rule, toggle inactive | Matching request | Rule skipped, falls to default or 404 |

### Edge Cases

| # | Test Case | Request | Expected |
|---|-----------|---------|----------|
| M-24 | Non-existent path | `GET /api/does-not-exist` | 404 JSON error |
| M-25 | Wrong HTTP method | Endpoint is POST only, send GET | 404 |
| M-26 | Empty body | POST with empty body to endpoint with body condition | No match (condition can't evaluate) |
| M-27 | Malformed JSON body | POST `{invalid json` | No match, 404 or default |
| M-28 | Path with special characters | `GET /api/users/john%20doe` | Path param correctly decoded |
| M-29 | Query string with leading ? | `GET /api/test?foo=bar` | Query param `foo` correctly parsed |
| M-30 | Case insensitive header | Header condition: "content-type" Equals "application/json" | Request with `Content-Type: application/json` | Match |

---

## 7. Test Suite: Request Logs

**File**: `tests/logs.spec.ts`

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| L-01 | Logs appear after request | Send mock request, navigate to `/logs` | New log entry appears with correct method, path, status |
| L-02 | Log detail drawer | Click a log row | Drawer opens with full request/response details |
| L-03 | Log detail shows headers | Open log detail | Request headers displayed with syntax highlighting |
| L-04 | Log detail shows body | Open log detail for POST request | Request body and response body shown |
| L-05 | Match status - matched | Send request that matches a rule | Log shows "Matched" badge |
| L-06 | Match status - unmatched | Send request that matches no rule (404) | Log shows "Unmatched" badge |
| L-07 | Response time recorded | Open log detail | Response time shown in ms |
| L-08 | Filter by method | Click method filter, select POST | Only POST logs shown |
| L-09 | Filter by match status | Click match filter, select Matched | Only matched logs shown |
| L-10 | Sort by time | Click timestamp header | Logs sorted by time |
| L-11 | Sort by response time | Click response time header | Logs sorted by response time |
| L-12 | Clear all logs | Click "Clear Logs", confirm | All logs removed, empty state shown |
| L-13 | Auto-refresh toggle | Enable auto-refresh, send request | New log appears without manual refresh |
| L-14 | Manual refresh | Disable auto-refresh, send request, click "Refresh" | New log appears after refresh |
| L-15 | Pagination | Send 25+ requests, check pagination | Second page accessible, correct page size |
| L-16 | Close drawer | Open detail drawer, click X | Drawer closes, table visible |

---

## 8. Test Suite: Import / Export

**File**: `tests/import-export.spec.ts`

### JSON Export

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| IE-01 | Export shows summary | Create 3 endpoints with 5 rules, go to Export tab | Shows "3 endpoints, 5 rules" |
| IE-02 | Export downloads file | Click "Export JSON" | File downloaded with name `mithya-export-{date}.json` |
| IE-03 | Exported JSON is valid | Export and read file | Valid JSON containing all endpoints and their rules |

### JSON Import

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| IE-04 | Import valid JSON | Export first, delete all, import the exported file | All endpoints and rules restored |
| IE-05 | Import detects duplicates | Import file with endpoints that already exist (same path+method) | Shows duplicate warning with count |
| IE-06 | Import partial success | Import file with 3 endpoints, 1 already exists | 2 imported successfully, 1 skipped or warned |

### OpenAPI Import

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| IE-07 | Import OpenAPI 3.0 JSON | Upload valid OpenAPI 3.0 JSON file | Preview table shows all paths with method, status |
| IE-08 | Import OpenAPI 3.0 YAML | Upload valid OpenAPI 3.0 YAML file | Same as IE-07 |
| IE-09 | Import Swagger 2.0 | Upload valid Swagger 2.0 spec | Preview table shows paths |
| IE-10 | OpenAPI preview selection | In preview, deselect some paths | Only selected paths imported |
| IE-11 | OpenAPI service name | Enter service name "PetStore" before import | All imported endpoints have serviceName="PetStore" |
| IE-12 | OpenAPI with examples | Import spec with example responses | Imported endpoints have example as default response body |
| IE-13 | Invalid spec file | Upload non-spec file (e.g., random JSON) | Shows error message |
| IE-14 | Empty spec | Upload OpenAPI with no paths | Shows warning "no endpoints found" |

---

## 9. Test Suite: Theme & i18n

**File**: `tests/theme-i18n.spec.ts`

### Dark Mode

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| T-01 | Default follows system | (Set browser to prefers dark) Navigate to `/` | Dark mode applied |
| T-02 | Toggle to dark | Click moon icon | Background changes to dark, icon becomes sun |
| T-03 | Toggle to light | In dark mode, click sun icon | Background changes to light, icon becomes moon |
| T-04 | Theme persists | Set dark mode, reload page | Dark mode preserved |
| T-05 | Dashboard in dark | Navigate all pages in dark mode | No visual artifacts, all text readable |
| T-06 | Code editor in dark | Open rule detail or log detail in dark mode | CodeMirror renders dark theme |
| T-07 | Sidebar blur in dark | Check sidebar in dark mode | Backdrop-filter blur visible |
| T-08 | Pill tags in dark | View endpoint cards in dark mode | Protocol/method/status badges use dark theme colors |
| T-09 | Modal in dark | Open any form modal in dark mode | Modal background and form fields render correctly |

### Internationalization

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| T-10 | Default language zh-TW | Fresh load | All labels in Traditional Chinese |
| T-11 | Switch to English | Click globe icon, select English | All labels switch to English |
| T-12 | Switch back to zh-TW | Click globe icon, select Traditional Chinese | All labels in zh-TW |
| T-13 | Language persists | Set English, reload page | English preserved |
| T-14 | Dashboard in English | Switch to English, check dashboard | "Dashboard", "Total Endpoints", etc. |
| T-15 | Endpoints in English | Switch to English, check endpoints page | "Endpoints", "Create Endpoint", "Batch", etc. |
| T-16 | Rules in English | Switch to English, check rule form | "Rule Name", "Priority", "Conditions", etc. |
| T-17 | Logs in English | Switch to English, check logs page | "Request Logs", "Auto-refresh", "Clear Logs", etc. |
| T-18 | Import/Export in English | Switch to English, check all tabs | "Export", "Import", "Import OpenAPI" |
| T-19 | Validation messages i18n | Switch to English, trigger form validation | Error messages in English |
| T-20 | Ant Design locale | Switch to English | Pagination shows "items/page", dates in English format |

---

## 10. Test Suite: Responsive / Mobile

**File**: `tests/responsive.spec.ts`

| # | Test Case | Viewport | Steps | Expected Result |
|---|-----------|----------|-------|-----------------|
| RS-01 | Mobile sidebar hidden | 375x812 (iPhone) | Navigate to `/` | Sidebar is hidden, hamburger menu visible |
| RS-02 | Mobile menu drawer | 375x812 | Click hamburger menu | Drawer opens with menu items |
| RS-03 | Mobile navigate | 375x812 | Open menu, click "Endpoints" | Navigates to endpoints, drawer closes |
| RS-04 | Tablet sidebar visible | 768x1024 (iPad) | Navigate to `/` | Sidebar visible |
| RS-05 | Mobile endpoint cards | 375x812 | View endpoints page | Cards stack vertically, fully readable |
| RS-06 | Mobile log table | 375x812 | View logs page | Table scrollable horizontally or responsive |
| RS-07 | Mobile form modal | 375x812 | Open create endpoint modal | Modal takes full width, form usable |

---

## 11. Test Suite: P0 - Response Template Engine

**File**: `tests/template-engine.spec.ts`

> Prerequisites: Template engine feature implemented per `backend-design.md` Section 1

### UI Tests

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| TP-01 | Template toggle visible | Open rule form | Template switch visible in response section |
| TP-02 | Template toggle default off | Open rule form | Template switch is OFF by default |
| TP-03 | Template variable ref shows | Toggle template ON | Variable reference panel appears |
| TP-04 | Template variable ref content | Toggle template ON | Shows `{{request.method}}`, `{{uuid}}`, etc. |
| TP-05 | Template badge on rule card | Create rule with template ON | Rule card shows "Template" badge |
| TP-06 | Preview button visible | Toggle template ON | "Preview" button appears |
| TP-07 | Preview drawer opens | Click "Preview" | Preview drawer opens with mock request editor |
| TP-08 | Preview renders template | Enter template `Hello {{request.pathParams.name}}`, fill mock request with pathParams.name="World", click Preview | Shows "Hello World" |
| TP-09 | Preview shows error | Enter invalid template `{{#if}}`, click Preview | Shows error message |

### Engine Tests (API)

| # | Test Case | Template | Mock Request | Expected Response |
|---|-----------|----------|-------------|-------------------|
| TP-10 | Path param template | `{"userId": "{{request.pathParams.id}}"}` | `GET /api/users/42` | `{"userId": "42"}` |
| TP-11 | Query param template | `{"page": "{{request.query.page}}"}` | `GET /api/test?page=3` | `{"page": "3"}` |
| TP-12 | Header template | `{"agent": "{{request.headers.User-Agent}}"}` | Request with `User-Agent: curl` | `{"agent": "curl"}` |
| TP-13 | Method template | `{"method": "{{request.method}}"}` | `POST /api/test` | `{"method": "POST"}` |
| TP-14 | UUID helper | `{"id": "{{uuid}}"}` | Any request | `{"id": "<valid-uuid-format>"}` |
| TP-15 | Now helper | `{"date": "{{now "yyyy-MM-dd"}}"}` | Any request | `{"date": "2026-02-08"}` (current date) |
| TP-16 | RandomInt helper | `{"num": {{randomInt 1 100}}}` | Any request | `{"num": <integer 1-100>}` |
| TP-17 | JsonPath helper | `{"name": "{{jsonPath request.body "$.user.name"}}"}` | `POST {"user":{"name":"John"}}` | `{"name": "John"}` |
| TP-18 | Template OFF = raw body | Rule with isTemplate=false, body `Hello {{request.method}}` | Any request | Literal `Hello {{request.method}}` |
| TP-19 | Template error fallback | Invalid template syntax | Any request | Returns raw template string, adds `X-Template-Error` header |
| TP-20 | Template with header rendering | isResponseHeadersTemplate=true, header value `req-{{request.pathParams.id}}` | `GET /api/test/5` | Response header value is `req-5` |

---

## 12. Test Suite: P1 - Fault Injection

**File**: `tests/fault-injection.spec.ts`

### UI Tests

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| FI-01 | Fault section visible in form | Open rule form | Fault injection section visible |
| FI-02 | Default fault type is None | Open rule form | Segmented control shows "None" selected |
| FI-03 | Select Random Delay | Select "Random Delay" | Min/Max delay inputs appear |
| FI-04 | Select Connection Reset | Select "Connection Reset" | Warning alert shown |
| FI-05 | Select Timeout | Select "Timeout" | Timeout duration input appears |
| FI-06 | Fault badge on rule card | Create rule with Timeout fault | Rule card shows "Timeout" badge in red |
| FI-07 | Response body disabled for reset | Select "Connection Reset" | Response body editor disabled/hidden |

### Engine Tests (API)

| # | Test Case | Fault Type | Config | Expected |
|---|-----------|-----------|--------|----------|
| FI-08 | Random delay | RandomDelay | `{minDelayMs: 200, maxDelayMs: 500}` | Response time between 200-500ms |
| FI-09 | Connection reset | ConnectionReset | - | Connection aborted (fetch throws error) |
| FI-10 | Empty response | EmptyResponse | `{statusCode: 503}` | 503 status, empty body |
| FI-11 | Malformed response | MalformedResponse | `{byteCount: 256}` | Response body is not valid JSON, ~256 bytes |
| FI-12 | Timeout | Timeout | `{timeoutMs: 3000}` | Request hangs for ~3s then connection drops |
| FI-13 | None fault = normal | None | - | Normal response returned |
| FI-14 | Fault logged | Any fault | - | Log entry records `faultTypeApplied` |

---

## 13. Test Suite: P1 - Proxy / Record & Playback

**File**: `tests/proxy.spec.ts`

> Prerequisites: Need a real upstream API for proxy tests. Use a secondary Mithya instance or a simple HTTP server as the upstream target.

### UI Tests

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| PX-01 | Proxy page accessible | Click "Proxy Config" in sidebar | Page loads with title and description |
| PX-02 | Create global proxy | Click "+ New", fill global proxy form, save | Proxy card appears with "Global" scope |
| PX-03 | Create endpoint proxy | Click "+ New", select specific endpoint, fill form, save | Proxy card shows linked endpoint name |
| PX-04 | Edit proxy config | Click edit on proxy card, change URL, save | URL updated in card |
| PX-05 | Delete proxy config | Click delete, confirm | Proxy card removed |
| PX-06 | Toggle proxy active | Click toggle switch | Status changes |
| PX-07 | Toggle recording | Click recording toggle | Recording indicator appears/disappears |
| PX-08 | Recording indicator in header | Enable recording on any proxy | Pulsing red dot with "Recording" in header |
| PX-09 | Validation - empty URL | Submit form with empty target URL | Validation error |

### Proxy Engine Tests (API)

| # | Test Case | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| PX-10 | Proxy forwards unmatched | Global proxy to upstream, no matching rule | `GET /api/upstream-data` | Response from upstream API |
| PX-11 | Mock takes priority | Rule matches + proxy configured | Matching request | Mock response (not proxied) |
| PX-12 | Proxy forwards headers | Proxy with forwardHeaders=true | Request with custom headers | Upstream receives the headers |
| PX-13 | Proxy adds headers | Proxy with additionalHeaders `{Auth: "token"}` | Any request | Upstream receives Auth header |
| PX-14 | Proxy strips path | Proxy with stripPathPrefix="/api/v1" | `GET /api/v1/users` | Upstream receives `GET /users` |
| PX-15 | Proxy timeout | Proxy with timeoutMs=1000, upstream delays 3s | Any request | Proxy returns error within ~1s |
| PX-16 | Recording creates rules | Proxy with recording ON | `GET /api/new-path` | New endpoint + rule created, verify in UI |
| PX-17 | Recorded rule replays | After PX-16, disable proxy | Same `GET /api/new-path` | Returns recorded response from rule |
| PX-18 | Log shows proxied | Send proxied request | Check logs | Log entry has "Proxied" indicator + proxy target URL |

---

## 14. Test Suite: P2 - Stateful Scenarios

**File**: `tests/scenarios.spec.ts`

### UI Tests

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| SC-01 | Scenarios page accessible | Click "Scenarios" in sidebar | Page loads |
| SC-02 | Create scenario | Click "+ New", enter name "Login Flow", initial state "logged_out" | Scenario card appears |
| SC-03 | Scenario detail page | Click scenario card | Detail page with state info and steps list |
| SC-04 | Add step | Click "+ Add Step", fill state/endpoint/response/nextState | Step appears in list under correct state |
| SC-05 | Edit step | Click edit on step | Form pre-filled, updates on save |
| SC-06 | Delete step | Click delete, confirm | Step removed |
| SC-07 | State flow diagram | Add 3 states with transitions | Diagram renders showing state machine |
| SC-08 | Current state display | Check scenario detail | Shows current state name |
| SC-09 | Reset state | Click "Reset State" | Current state returns to initial state |
| SC-10 | Toggle scenario active | Click toggle | Status changes |

### Scenario Engine Tests (API)

| # | Test Case | Scenario Setup | Requests (in order) | Expected |
|---|-----------|---------------|---------------------|----------|
| SC-11 | Basic state transition | State "A": POST /api/action -> 200 `{"state":"A"}` -> next: "B"; State "B": GET /api/status -> 200 `{"state":"B"}` | 1. `POST /api/action` 2. `GET /api/status` | 1. `{"state":"A"}` 2. `{"state":"B"}` |
| SC-12 | Stay in same state | State "A": GET /api/data -> 200 -> stay in "A" | 1. `GET /api/data` 2. `GET /api/data` | Both return same response |
| SC-13 | Conditional transition | State "init": POST /api/login with Body.$.pass Equals "ok" -> next "auth"; without match -> stay | 1. `POST {"pass":"wrong"}` 2. `POST {"pass":"ok"}` | 1. Stays in init (no match or default) 2. Transitions to auth |
| SC-14 | Reset mid-flow | Scenario in state "B", call reset API | `GET /api/status` | Returns state "A" response (initial state) |
| SC-15 | Inactive scenario ignored | Disable scenario | Matching request | Falls through to normal rule matching |
| SC-16 | Scenario priority over rules | Scenario active + normal rule for same endpoint | Matching request | Scenario response takes priority |

---

## 15. Test Suite: P2 - Enhanced Request Matching

**File**: `tests/enhanced-matching.spec.ts`

### New Operators (API)

| # | Test Case | Operator | Condition | Request | Expected |
|---|-----------|----------|-----------|---------|----------|
| EM-01 | NotEquals | NotEquals | Body.$.status NotEquals "deleted" | `{"status":"active"}` | Match |
| EM-02 | NotEquals negative | NotEquals | Body.$.status NotEquals "deleted" | `{"status":"deleted"}` | No match |
| EM-03 | JsonSchema | JsonSchema | Body.$ JsonSchema `{"type":"object","required":["name"]}` | `{"name":"John"}` | Match |
| EM-04 | JsonSchema fail | JsonSchema | Same schema | `{"age":25}` (missing "name") | No match |
| EM-05 | IsEmpty | IsEmpty | Body.$.notes IsEmpty | `{"notes":""}` | Match |
| EM-06 | IsEmpty negative | IsEmpty | Body.$.notes IsEmpty | `{"notes":"hello"}` | No match |
| EM-07 | NotExists | NotExists | Header.X-Debug NotExists | Request without X-Debug header | Match |
| EM-08 | NotExists negative | NotExists | Header.X-Debug NotExists | Request with `X-Debug: true` | No match |

### Logic Mode (API)

| # | Test Case | Logic | Conditions | Request | Expected |
|---|-----------|-------|-----------|---------|----------|
| EM-09 | AND mode (default) | AND | Body.$.a Equals "1" AND Body.$.b Equals "2" | `{"a":"1","b":"2"}` | Match |
| EM-10 | AND mode partial fail | AND | Same as EM-09 | `{"a":"1","b":"9"}` | No match |
| EM-11 | OR mode | OR | Body.$.a Equals "1" OR Body.$.b Equals "2" | `{"a":"1","b":"9"}` | Match (first condition is enough) |
| EM-12 | OR mode both fail | OR | Same as EM-11 | `{"a":"9","b":"9"}` | No match |

### UI Tests

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| EM-13 | Logic mode toggle visible | Open rule form | AND/OR segmented control visible |
| EM-14 | Default is AND | Open rule form | AND selected by default |
| EM-15 | Switch to OR | Click OR | Logic connector between conditions shows "OR" |
| EM-16 | New operators in dropdown | Open operator selector in condition builder | Shows NotEquals, JsonSchema, IsEmpty, NotExists options |
| EM-17 | Exists hides value | Select "Exists" operator | Value input field hidden |
| EM-18 | IsEmpty hides value | Select "IsEmpty" operator | Value input field hidden |
| EM-19 | JsonSchema shows editor | Select "JsonSchema" operator | Value field becomes CodeEditor for JSON schema |
| EM-20 | OR badge on rule card | Create rule with OR logic | Rule card shows "OR" badge |

---

## 16. Test Data & Fixtures

### 16.1 Seed Data Helper

```typescript
// e2e/fixtures/test-data.ts

export const SEED = {
  endpoint: {
    rest_get: {
      name: 'E2E Test - GET Users',
      serviceName: 'e2e-test',
      protocol: 1, // REST
      path: '/api/e2e/users',
      httpMethod: 'GET',
    },
    rest_post: {
      name: 'E2E Test - POST User',
      serviceName: 'e2e-test',
      protocol: 1,
      path: '/api/e2e/users/{id}',
      httpMethod: 'POST',
    },
    soap: {
      name: 'E2E Test - SOAP Service',
      serviceName: 'e2e-test',
      protocol: 2, // SOAP
      path: '/soap/e2e/service',
      httpMethod: 'POST',
    },
  },
  rule: {
    body_equals: {
      ruleName: 'E2E - Body Equals',
      priority: 1,
      conditions: [
        { sourceType: 1, fieldPath: '$.id', operator: 1, value: '123' }
      ],
      statusCode: 200,
      responseBody: '{"matched": true, "rule": "body_equals"}',
      delayMs: 0,
    },
    header_contains: {
      ruleName: 'E2E - Header Contains',
      priority: 2,
      conditions: [
        { sourceType: 2, fieldPath: 'Authorization', operator: 2, value: 'Bearer' }
      ],
      statusCode: 200,
      responseBody: '{"matched": true, "rule": "header_contains"}',
      delayMs: 0,
    },
  },
};
```

### 16.2 Sample OpenAPI Spec

```json
// e2e/fixtures/openapi-sample.json
{
  "openapi": "3.0.0",
  "info": { "title": "E2E Test API", "version": "1.0.0" },
  "paths": {
    "/api/pets": {
      "get": {
        "operationId": "listPets",
        "summary": "List all pets",
        "responses": {
          "200": {
            "description": "A list of pets",
            "content": {
              "application/json": {
                "example": [{"id": 1, "name": "Dog"}]
              }
            }
          }
        }
      },
      "post": {
        "operationId": "createPet",
        "summary": "Create a pet",
        "responses": {
          "201": {
            "description": "Pet created",
            "content": {
              "application/json": {
                "example": {"id": 1, "name": "Dog"}
              }
            }
          }
        }
      }
    },
    "/api/pets/{petId}": {
      "get": {
        "operationId": "getPet",
        "summary": "Get a pet by ID",
        "parameters": [
          { "name": "petId", "in": "path", "required": true, "schema": { "type": "string" } }
        ],
        "responses": {
          "200": {
            "description": "A pet",
            "content": {
              "application/json": {
                "example": {"id": 1, "name": "Dog"}
              }
            }
          }
        }
      }
    }
  }
}
```

---

## 17. CI/CD Integration

### 17.1 GitHub Actions Workflow

```yaml
# .github/workflows/e2e-tests.yml
name: E2E Tests

on:
  pull_request:
    branches: [main]
  push:
    branches: [main]

jobs:
  e2e:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Start services
        run: docker compose up -d --wait
        timeout-minutes: 5

      - name: Wait for services
        run: |
          # Wait for frontend
          timeout 60 bash -c 'until curl -s http://localhost:3000 > /dev/null; do sleep 2; done'
          # Wait for backend
          timeout 60 bash -c 'until curl -s http://localhost:5050/admin/api/config > /dev/null; do sleep 2; done'

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: 20

      - name: Install Playwright
        working-directory: e2e
        run: |
          npm ci
          npx playwright install --with-deps chromium

      - name: Run E2E tests
        working-directory: e2e
        run: npx playwright test
        env:
          BASE_URL: http://localhost:3000
          ADMIN_API_URL: http://localhost:5050/admin/api
          MOCK_API_URL: http://localhost:5050

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: e2e/playwright-report/
          retention-days: 7

      - name: Upload failure screenshots
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: test-screenshots
          path: e2e/test-results/
          retention-days: 7

      - name: Stop services
        if: always()
        run: docker compose down -v
```

### 17.2 Test Execution Summary

| Suite | Tests | Phase | Priority |
|-------|-------|-------|----------|
| Dashboard | 10 | Current | Run first |
| Endpoints | 21 | Current | Run first |
| Rules | 18 | Current | Run first |
| Mock Engine | 30 | Current | Run first |
| Logs | 16 | Current | Run first |
| Import/Export | 14 | Current | Run first |
| Theme & i18n | 20 | Current | Run first |
| Responsive | 7 | Current | Run first |
| **Subtotal (Current)** | **136** | | |
| Template Engine | 20 | P0 | After P0 ships |
| Fault Injection | 14 | P1 | After P1 ships |
| Proxy | 18 | P1 | After P1 ships |
| Scenarios | 16 | P2 | After P2 ships |
| Enhanced Matching | 20 | P2 | After P2 ships |
| **Subtotal (New Features)** | **88** | | |
| **Grand Total** | **224** | | |

### 17.3 Test Execution Order

```
1. Theme & i18n    (environment verification)
2. Dashboard       (basic page load)
3. Endpoints       (CRUD foundation)
4. Rules           (CRUD on top of endpoints)
5. Mock Engine     (core matching logic)
6. Logs            (verifies logging pipeline)
7. Import/Export   (bulk operations)
8. Responsive      (layout verification)
--- New feature tests (gated by feature flags) ---
9. Template Engine (P0)
10. Fault Injection (P1)
11. Proxy           (P1)
12. Enhanced Matching (P2)
13. Scenarios        (P2)
```

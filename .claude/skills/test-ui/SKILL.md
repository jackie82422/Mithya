---
name: test-ui
description: Run Playwright UI tests on localhost:3000
disable-model-invocation: true
argument-hint: "[module-name]"
---

# UI Testing with Playwright

Use Playwright MCP to perform UI testing on the running application at http://localhost:3000.

## Usage

- `/test-ui` — Full test of all modules
- `/test-ui dashboard` — Test dashboard only
- `/test-ui endpoints` — Test endpoint CRUD
- `/test-ui proxy` — Test proxy configuration
- `/test-ui logs` — Test request logs
- `/test-ui scenarios` — Test scenarios
- `/test-ui import-export` — Test import/export
- `/test-ui dark` — Test dark mode
- `/test-ui i18n` — Test language switching
- `/test-ui responsive` — Test responsive layouts (768px, 375px)

## Test Flow Per Module

### Dashboard
1. Navigate to `/`
2. Verify stats cards (Total Endpoints, Active, Rules, Requests)
3. Verify endpoint overview table loads
4. Verify recent logs section
5. Click an endpoint → verify navigation

### Endpoints
1. Navigate to `/endpoints`
2. Create endpoint (fill form, submit, verify in list)
3. Search/filter endpoints
4. Edit endpoint (modify fields, save)
5. Toggle active/inactive
6. Set default response
7. Delete endpoint (confirm dialog)
8. Batch operations

### Proxy
1. Navigate to `/proxy`
2. Create proxy config (fill target URL, settings)
3. Toggle active on/off
4. Toggle recording on/off
5. Edit proxy config
6. Delete proxy config

### Logs
1. Navigate to `/logs`
2. Verify log table loads
3. Filter by method / status
4. Click log row → verify detail modal
5. Clear logs

### Scenarios
1. Navigate to `/scenarios`
2. Create scenario (name, initial state)
3. View scenario detail
4. Add steps
5. Reset state
6. Toggle active

### Import/Export
1. Navigate to `/import-export`
2. Test export (download JSON)
3. Test import JSON tab
4. Test import OpenAPI tab

## Output

After testing, provide a summary table:

```
| Module | Test | Result |
|--------|------|--------|
| Dashboard | Stats cards load | PASS/FAIL |
| ...    | ...  | ...    |
```

Categorize bugs as **FE** (frontend) or **BE** (backend) with severity.

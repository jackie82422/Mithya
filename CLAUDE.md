# Mithya Mock Server

Mock API Server with .NET 8 backend + React/Vite frontend, supporting multi-protocol mock, rule matching, proxy forwarding, scenario state machine, fault injection, and import/export.

> Development Guides:
> - [Infrastructure Guide](./docs/dev/infrastructure.md)

---

## Development Workflow (MUST follow)

**Every feature/fix MUST use the following skills. These rules apply at all times, including after `/compact`.**

### Available Skills

| Skill | Usage | Purpose |
|-------|-------|---------|
| `/commit [scope]` | Every feature/fix done | Stage + commit with conventional format |
| `/dev [target]` | Start dev environment | Start DB / Backend / Frontend |
| `/build [target]` | Build project | Compile frontend / backend / docker |
| `/migrate <name>` | DB schema change | Create / apply EF Core migrations |
| `/test-ui [module]` | UI verification | Playwright test on localhost:3000 |

### Commit Rules (MANDATORY)

- **Format**: `type(scope): subject`
- **Types**: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`, `style`, `ci`
- **Scopes**: `frontend`, `backend`, `proxy`, `endpoints`, `logs`, `scenarios`, `import-export`, `infra`, `db`
- **Subject**: imperative mood, lowercase, no period, max 50 chars
- **NEVER** include AI co-author, attribution, or any AI-related text in commits
- **NEVER** use `git add -A` or `git add .` — only stage relevant files
- **NEVER** use `--no-verify` unless explicitly asked
- After completing a feature or fix, **always run `/commit`** before moving to next task

### Development Flow

1. Before writing code, read relevant files first
2. Follow existing patterns in the codebase (see conventions below)
3. After completing a feature/fix → **`/commit`**
4. If DB schema changed → **`/migrate`** before commit
5. If UI changed → **`/test-ui`** to verify
6. i18n: update both `en.json` and `zh-TW.json` for any new UI text

---

## Quick Start

```bash
# Docker Compose (recommended)
docker-compose up -d          # Start all services (Postgres + Backend + Frontend)
# Frontend: http://localhost:3000
# Backend API: http://localhost:5050/admin/api/
# Swagger: http://localhost:5050/swagger

# Local Development
docker-compose up -d postgres                              # Start DB only
cd backend/src/Mithya.Api && dotnet run                # Backend on :5000
cd frontend && npm install && npm run dev                  # Frontend on :5173
```

---

## Project Structure

```
mServer/
├── backend/
│   └── src/
│       ├── Mithya.Api/            # ASP.NET Core 8 web layer
│       │   ├── Program.cs             # DI, middleware pipeline, startup
│       │   ├── Endpoints/             # Minimal API endpoint groups
│       │   └── Middleware/            # DynamicMockMiddleware, GlobalExceptionHandler
│       ├── Mithya.Core/           # Domain layer (entities, enums, interfaces)
│       │   ├── Entities/              # MockEndpoint, MockRule, ServiceProxy, Scenario...
│       │   ├── Enums/                 # ProtocolType, MatchOperator, FaultType...
│       │   ├── Interfaces/            # Repository & engine contracts
│       │   └── ValueObjects/          # CachedEndpoint, CachedRule, MatchResult...
│       └── Mithya.Infrastructure/ # Data & business logic
│           ├── Data/                  # DbContext, EF configurations, migrations
│           ├── Repositories/          # EF Core repository implementations
│           ├── MockEngine/            # MatchEngine, ProxyEngine, ResponseRenderer...
│           └── ProtocolHandlers/      # REST, SOAP protocol validators
├── frontend/
│   └── src/
│       ├── App.tsx                    # Routes, QueryClient, ThemeProvider
│       ├── global.css                 # Apple-style CSS variables (light/dark)
│       ├── modules/                   # Feature modules
│       │   ├── dashboard/             # Stats, overview, recent logs
│       │   ├── endpoints/             # Endpoint CRUD
│       │   ├── rules/                 # Rule CRUD, condition builder
│       │   ├── logs/                  # Request log viewer
│       │   ├── proxy/                 # Service proxy management (per-service fallback)
│       │   ├── scenarios/             # Stateful workflows
│       │   └── import-export/         # JSON/OpenAPI import, export
│       └── shared/
│           ├── api/client.ts          # Axios instance (baseURL: /admin/api)
│           ├── components/            # StatusBadge, HttpMethodTag, CodeEditor...
│           ├── contexts/ThemeContext   # Light/dark mode
│           ├── i18n/                  # en.json, zh-TW.json
│           ├── layouts/AppLayout.tsx   # Sidebar + header + content
│           ├── types/index.ts         # All TypeScript interfaces & enums
│           └── utils/errorUtils.ts    # API error message extraction
└── docker-compose.yml                 # Postgres + Backend + Frontend (Nginx)
```

---

## Backend Architecture

### Tech Stack
- .NET 8, ASP.NET Core Minimal APIs, EF Core 8 + PostgreSQL, Handlebars.Net, NJsonSchema

### Middleware Pipeline (order matters)
```
GlobalExceptionHandler → CORS → DynamicMockMiddleware → Admin API Endpoints
```
- `/admin/api/*` and `/swagger/*` pass through to admin endpoints
- All other paths are intercepted by `DynamicMockMiddleware` for mock matching

### Request Processing Flow (DynamicMockMiddleware)
```
Request
 ├─ Match Endpoint (path + method)
 │   ├─ Scenario Match?       → Return scenario response
 │   ├─ Rule Match?           → Return rule response (with template/fault/delay)
 │   └─ No Rule Match
 │       ├─ Service Proxy?    → Forward to real API (fallback)
 │       └─ No Proxy          → Return default response
 └─ No Endpoint Match         → 404
```

### API Route Groups

| Route | File | Purpose |
|-------|------|---------|
| `/admin/api/endpoints` | `EndpointManagementApis.cs` | Endpoint CRUD + toggle + set default |
| `/admin/api/endpoints/{id}/rules` | `RuleManagementApis.cs` | Rule CRUD + toggle |
| `/admin/api/logs` | `LogApis.cs` | Log querying + clear |
| `/admin/api/proxy-configs` | `ProxyConfigApis.cs` | Legacy proxy CRUD + toggle |
| `/admin/api/service-proxies` | `ServiceProxyApis.cs` | Service-level proxy CRUD + toggle + fallback |
| `/admin/api/scenarios` | `ScenarioApis.cs` | Scenario CRUD + toggle + reset + steps |
| `/admin/api/templates` | `TemplateApis.cs` | Template preview |
| `/admin/api/protocols` | `ProtocolEndpoints.cs` | Protocol schema list |
| `/admin/api/config` | `ConfigEndpoints.cs` | Server URL config |
| `/admin/api/export`, `/admin/api/import/*` | `ImportExportApis.cs` | Export JSON, Import JSON/OpenAPI |

### Entities

| Entity | Table | Description |
|--------|-------|-------------|
| `MockEndpoint` | `mock_endpoints` | API endpoints with path, method, protocol, serviceName |
| `MockRule` | `mock_rules` | Match conditions + response (priority, template, fault) |
| `MockRequestLog` | `mock_request_logs` | Request audit trail |
| `ProxyConfig` | `proxy_configs` | Legacy proxy config (global or per-endpoint) |
| `ServiceProxy` | `service_proxies` | Service-level proxy with fallback (keyed by ServiceName) |
| `Scenario` | `scenarios` | State machine definition |
| `ScenarioStep` | `scenario_steps` | State transitions with conditional response |

### Key Engine Services (all singletons)

| Service | Role |
|---------|------|
| `MockRuleCache` | In-memory cache of active endpoints + rules |
| `MatchEngine` | Path matching + condition evaluation |
| `ResponseRenderer` | Render response (template, headers, fault, delay) |
| `ProxyEngine` | HTTP forwarding with header injection (accepts `IProxyTarget`) |
| `ProxyConfigCache` | In-memory cache of legacy proxy configs |
| `ServiceProxyCache` | In-memory cache of service proxies (keyed by ServiceName) |
| `RecordingService` | Auto-create endpoints/rules from proxied responses |
| `ScenarioEngine` | Stateful mock with state transitions |
| `HandlebarsTemplateEngine` | Dynamic response rendering |
| `FaultInjector` | Chaos engineering (delay, reset, timeout, malformed) |

### Template Helpers (Handlebars)
`{{jsonPath body "$.user.id"}}`, `{{now "yyyy-MM-dd"}}`, `{{uuid}}`, `{{randomInt 1 100}}`, `{{randomString 16}}`, `{{base64 text}}`, `{{base64Decode text}}`

---

## Frontend Architecture

### Tech Stack
- React 18, TypeScript 5, Vite 5, Ant Design 5, TanStack React Query 5, React Router 6, i18next, Axios, CodeMirror

### Routes

| Path | Page | Module |
|------|------|--------|
| `/` | DashboardPage | dashboard |
| `/endpoints` | EndpointListPage | endpoints |
| `/endpoints/:id` | EndpointDetailPage | endpoints |
| `/logs` | LogListPage | logs |
| `/proxy` | ProxyConfigPage | proxy |
| `/scenarios` | ScenarioListPage | scenarios |
| `/scenarios/:id` | ScenarioDetailPage | scenarios |
| `/import-export` | ImportExportPage | import-export |

### Module Convention
Each module under `modules/{name}/` follows:
```
{name}/
├── api.ts           # Axios API client functions
├── hooks.ts         # TanStack Query hooks (useQuery + useMutation)
├── index.ts         # Re-exports
├── pages/           # Page components
└── components/      # Module-specific components
```

### Theme System
- `ThemeContext` manages light/dark mode, persists to `localStorage`
- `global.css` defines CSS variables under `[data-theme='light']` and `[data-theme='dark']`
- Apple-style glass morphism: `.apple-sidebar`, `.apple-header` with `backdrop-filter: blur(20px)`
- Pill-shaped badges for status, methods, protocols using CSS custom properties

### i18n
- Languages: English (`en.json`), Traditional Chinese (`zh-TW.json`)
- `LanguageSwitcher` component in header
- All UI text uses `useTranslation()` hook

### TanStack Query Patterns
- Query keys are hierarchical: `['endpoints']`, `['endpoints', id]`, `['rules', endpointId]`, `['serviceProxies']`, `['serviceProxies', 'services']`
- Mutations invalidate relevant query keys on success
- Errors displayed via `message.error(getApiErrorMessage(err, fallback))`
- Server config uses `staleTime: Infinity`

---

## How to Add New Features

### Backend: Add a New Entity + CRUD

1. **Entity**: Create `Mithya.Core/Entities/NewEntity.cs`
2. **Enum** (if needed): Add to `Mithya.Core/Enums/`
3. **Repository interface**: `Mithya.Core/Interfaces/INewEntityRepository.cs`
4. **Repository impl**: `Mithya.Infrastructure/Repositories/NewEntityRepository.cs`
5. **EF Config**: `Mithya.Infrastructure/Data/Configurations/NewEntityConfiguration.cs`
6. **DbSet**: Add to `MithyaDbContext.cs`
7. **Migration**: `dotnet ef migrations add AddNewEntity --project src/Mithya.Infrastructure --startup-project src/Mithya.Api`
8. **API endpoints**: `Mithya.Api/Endpoints/NewEntityApis.cs` with `MapNewEntityApis()`
9. **Register DI** in `Program.cs`:
   - `builder.Services.AddScoped<INewEntityRepository, NewEntityRepository>()`
   - `app.MapNewEntityApis()`
10. **Cache** (if hot path): Create singleton cache, load on startup

### Backend: Add a Column to Existing Entity

1. Add property to entity class
2. Update EF configuration (Fluent API)
3. Create migration: `dotnet ef migrations add AddColumnToEntity ...`
4. Update API endpoints to handle new field
5. App auto-migrates on startup

### Frontend: Add a New Module

1. **Types**: Add interfaces in `shared/types/index.ts`
2. **API client**: Create `modules/{name}/api.ts`
3. **Hooks**: Create `modules/{name}/hooks.ts` with useQuery/useMutation
4. **Pages**: Create `modules/{name}/pages/{Name}Page.tsx`
5. **Components**: Create `modules/{name}/components/`
6. **Route**: Add to `App.tsx` routes
7. **Nav item**: Add to `shared/layouts/AppLayout.tsx` menuItems
8. **i18n**: Add keys to both `en.json` and `zh-TW.json`

### Frontend: Add a Shared Component

1. Create in `shared/components/`
2. Use CSS custom properties (`var(--color-*)`) for theme-aware styling
3. Use `useTranslation()` for all text
4. Follow existing pill-style pattern if it's a badge/tag

---

## Conventions

### Backend
- IDs: `Guid.NewGuid()` auto-set by repository
- Timestamps: `CreatedAt`/`UpdatedAt` auto-set UTC by repository
- Tables: `snake_case` names
- JSON columns: `jsonb` type in PostgreSQL
- Validation: Custom logic in endpoint handlers, return `Results.BadRequest(new { errors = [...] })`
- Cache invalidation: Call `cache.ReloadAsync()` after DB writes

### Frontend
- Styling: CSS variables only, no hardcoded colors
- State: TanStack Query for server state, `useState` for local UI state
- API errors: `getApiErrorMessage()` utility
- Forms: Ant Design `Form` with `useForm()` hook
- i18n: Never hardcode strings, always use `t('key')`
- Path alias: `@/` maps to `src/`

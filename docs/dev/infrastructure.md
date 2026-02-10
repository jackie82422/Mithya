# Infrastructure Guide

## Architecture Overview

```
┌─────────────┐     ┌──────────────────┐     ┌────────────────┐
│   Frontend   │────▶│     Backend      │────▶│   PostgreSQL   │
│  (Nginx/SPA) │     │  (ASP.NET Core)  │     │    16-Alpine   │
│   Port 3000  │     │   Port 5050      │     │   Port 5432    │
└─────────────┘     └──────────────────┘     └────────────────┘
```

- **Frontend**: React SPA served by Nginx, proxies `/admin/api/*` to backend
- **Backend**: .NET 8 Minimal API, handles admin API + mock request interception
- **Database**: PostgreSQL 16, auto-migrated by EF Core on startup

---

## Docker Compose

**File**: `docker-compose.yml`

### Services

| Service | Image | Container | Host Port | Container Port |
|---------|-------|-----------|-----------|----------------|
| `postgres` | `postgres:16-alpine` | `mockserver-db` | 5432 | 5432 |
| `backend` | Custom (Dockerfile) | `mockserver-backend` | 5050 | 8080 |
| `frontend` | Custom (Dockerfile) | `mockserver-frontend` | 3000 | 80 |

### Network
- Bridge network: `mockserver-network`
- Services communicate by container name (e.g., `http://backend:8080`, `Host=postgres`)

### Volumes
- `postgres-data`: Persists database data at `/var/lib/postgresql/data`

### Start Order
1. PostgreSQL starts first (health check: `pg_isready`)
2. Backend starts after Postgres is healthy (`depends_on: condition: service_healthy`)
3. Frontend starts after backend (`depends_on: backend`)

### Commands
```bash
docker-compose up -d                # Start all
docker-compose up -d postgres       # Database only
docker-compose logs -f backend      # Follow backend logs
docker-compose down                 # Stop all
docker-compose down -v              # Stop all + delete volumes (resets DB)
docker-compose build --no-cache     # Rebuild images
```

---

## Dockerfiles

### Backend (`backend/src/MockServer.Api/Dockerfile`)
Multi-stage build:
1. **base**: `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime only)
2. **build**: `mcr.microsoft.com/dotnet/sdk:8.0` (restore + build)
3. **publish**: Release publish (`/app/publish`)
4. **final**: Copy published output, entrypoint `dotnet MockServer.Api.dll`

### Frontend (`frontend/Dockerfile`)
Multi-stage build:
1. **build**: `node:20-alpine` (npm install + vite build)
2. **runtime**: `nginx:alpine` (serve static files)
3. Custom `nginx.conf` copied for SPA routing + API proxy

---

## Port Configuration

### Production (Docker Compose)

| Component | Host | Container |
|-----------|------|-----------|
| Frontend (Nginx) | `localhost:3000` | `:80` |
| Backend API | `localhost:5050` | `:8080` |
| PostgreSQL | `localhost:5432` | `:5432` |

### Local Development

| Component | Port | Command |
|-----------|------|---------|
| Frontend (Vite) | `localhost:5173` | `cd frontend && npm run dev` |
| Backend (.NET) | `localhost:5000` | `cd backend/src/MockServer.Api && dotnet run` |
| PostgreSQL | `localhost:5432` | Docker or local install |

### Proxy Chain

**Production** (Nginx `nginx.conf`):
```
Browser → :3000 (Nginx)
  ├─ /admin/api/* → proxy_pass http://backend:8080/admin/api/
  └─ /*           → SPA (index.html)
```

**Development** (Vite `vite.config.ts`):
```
Browser → :5173 (Vite)
  ├─ /admin/api/* → proxy to http://localhost:5000/admin/api/
  └─ /*           → Vite HMR
```

---

## Database

### PostgreSQL Configuration
```
Host:     localhost (dev) / postgres (docker)
Port:     5432
Database: mockserver
Username: mockserver
Password: mockserver123
```

### Connection String
- **appsettings.json**: `Host=localhost;Database=mockserver;Username=mockserver;Password=mockserver123`
- **Docker override**: `Host=postgres;...` via environment variable `ConnectionStrings__Default`

### EF Core Migrations

Migrations are at `backend/src/MockServer.Infrastructure/Data/Migrations/`.

**Auto-migration**: `Program.cs` runs `db.Database.Migrate()` on startup for relational DBs, `db.Database.EnsureCreated()` for in-memory (tests).

**Create new migration**:
```bash
cd backend
dotnet ef migrations add MigrationName \
  --project src/MockServer.Infrastructure \
  --startup-project src/MockServer.Api
```

**Apply manually** (usually not needed due to auto-migration):
```bash
cd backend
dotnet ef database update \
  --project src/MockServer.Infrastructure \
  --startup-project src/MockServer.Api
```

### Current Migrations (in order)
1. `InitialCreate` - endpoints, rules, logs tables
2. `AddEndpointPathMethodUniqueConstraint` - unique (path, httpMethod)
3. `AddTemplateFields` - isTemplate, isResponseHeadersTemplate
4. `AddFaultInjection` - faultType, faultConfig
5. `AddProxyConfigAndLogExtensions` - proxy_configs table, log proxy fields
6. `AddScenariosAndSteps` - scenarios, scenario_steps tables
7. `AddLogicModeToMockRules` - logicMode (AND/OR)

### Tables
```
mock_endpoints      - API endpoint definitions
mock_rules          - Match rules with conditions + response
mock_request_logs   - Request audit trail
proxy_configs       - Proxy forwarding configuration
scenarios           - State machine definitions
scenario_steps      - State transitions
```

---

## Configuration Files

### Backend

**`appsettings.json`**:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=mockserver;Username=mockserver;Password=mockserver123"
  },
  "AllowedHosts": "*"
}
```

**Environment variable overrides** (Docker):
```yaml
ASPNETCORE_ENVIRONMENT: Development
ASPNETCORE_URLS: http://+:8080
ConnectionStrings__Default: Host=postgres;Database=mockserver;...
MockServer__BaseUrl: http://localhost:5050
```

### Frontend

**`vite.config.ts`**: Dev server port (5173), API proxy target, path alias (`@`)

No `.env` files - configuration is build-time via Vite.

---

## Development Workflow

### Full Stack (Docker)
```bash
docker-compose up -d
# Access at http://localhost:3000
# API at http://localhost:5050/admin/api/
# Swagger at http://localhost:5050/swagger
```

### Local Development (Hot Reload)
```bash
# Terminal 1: Database
docker-compose up -d postgres

# Terminal 2: Backend
cd backend/src/MockServer.Api
dotnet run
# Listens on http://localhost:5000

# Terminal 3: Frontend
cd frontend
npm run dev
# Listens on http://localhost:5173, proxies API to :5000
```

### Frontend Build
```bash
cd frontend
npm run build         # TypeScript compile + Vite bundle → dist/
npm run preview       # Preview production build locally
```

### Backend Build
```bash
cd backend
dotnet build                    # Debug build
dotnet publish -c Release       # Release publish
```

---

## Key Infrastructure Notes

1. **CORS**: Backend allows all origins (`AllowAnyOrigin`) - appropriate for mock server use case
2. **Auto-migration**: Database schema is managed by code, no manual migration needed
3. **Cache warming**: MockRuleCache, ProxyConfigCache, ScenarioEngine all load on startup
4. **Swagger**: Available in Development environment at `/swagger`
5. **Health check**: Only PostgreSQL has a health check; backend and frontend don't
6. **Secrets**: Credentials are hardcoded for dev; use environment variables for production
7. **JSON serialization**: Backend uses `ReferenceHandler.IgnoreCycles` for circular reference handling
8. **HttpClient**: Proxy uses named `"ProxyClient"` with `AllowAutoRedirect = false`

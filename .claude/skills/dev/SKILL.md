---
name: dev
description: Start development environment services
disable-model-invocation: true
allowed-tools: Bash(docker-compose *), Bash(docker compose *), Bash(cd * && npm *), Bash(cd * && dotnet *), Bash(npm *), Bash(dotnet *), Bash(lsof *)
argument-hint: "[all|db|backend|frontend]"
---

# Start Development Environment

Start the Mithya development stack based on the argument.

## Targets

| Argument | Action |
|----------|--------|
| `all` or empty | Start DB + Backend + Frontend |
| `db` | Start PostgreSQL only via docker-compose |
| `backend` | Start .NET backend (assumes DB is running) |
| `frontend` | Start Vite dev server (assumes backend is running) |

## Commands

### Database
```bash
docker-compose up -d postgres
```
Wait until healthy, then confirm with `docker-compose ps`.

### Backend
```bash
cd backend/src/Mithya.Api && dotnet run
```
Runs on http://localhost:5000. Auto-applies EF migrations on startup.

### Frontend
```bash
cd frontend && npm run dev
```
Runs on http://localhost:5173. Proxies `/admin/api` to backend :5000.

## Full Stack (`all`)

1. Start DB: `docker-compose up -d postgres`
2. Wait for healthy: check `docker-compose ps`
3. Start Backend in background: `cd backend/src/Mithya.Api && dotnet run`
4. Start Frontend in background: `cd frontend && npm run dev`

## Troubleshooting

- Port conflict: check with `lsof -i :5000` or `lsof -i :5173`
- DB connection fail: verify postgres container is healthy
- Frontend proxy fail: ensure backend is running on :5000

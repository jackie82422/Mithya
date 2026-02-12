---
name: build
description: Build frontend, backend, or Docker images
disable-model-invocation: true
allowed-tools: Bash(cd * && npm *), Bash(npm *), Bash(cd * && dotnet *), Bash(dotnet *), Bash(docker-compose *), Bash(docker compose *), Bash(docker build *)
argument-hint: "[all|frontend|backend|docker]"
---

# Build Project

Build the Mithya project based on the argument.

## Targets

| Argument | Action |
|----------|--------|
| `all` or empty | Build backend + frontend |
| `frontend` | TypeScript compile + Vite bundle |
| `backend` | .NET build |
| `docker` | Docker Compose build all images |

## Commands

### Frontend
```bash
cd frontend && npm run build
```
Output: `frontend/dist/`
This runs `tsc -b && vite build`.

### Backend
```bash
cd backend && dotnet build
```
For release: `dotnet build -c Release`

### Docker
```bash
docker-compose build
```
For clean rebuild: `docker-compose build --no-cache`

## Post-Build Checks

- Frontend: verify `frontend/dist/index.html` exists
- Backend: verify no build errors in output
- Docker: verify images with `docker images | grep mithya`

## On Failure

- Frontend TypeScript errors: show the error, suggest fix
- Backend build errors: show the error, suggest fix
- Docker build errors: check Dockerfile syntax and base image availability

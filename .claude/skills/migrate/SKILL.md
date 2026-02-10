---
name: migrate
description: Create or apply EF Core database migrations
disable-model-invocation: true
allowed-tools: Bash(cd * && dotnet *), Bash(dotnet *)
argument-hint: "<migration-name>"
---

# Database Migration

Create or manage EF Core migrations for PostgreSQL.

## Usage

- `/migrate AddNewColumn` — Create a new migration named `AddNewColumn`
- `/migrate apply` — Apply pending migrations
- `/migrate status` — Show migration status
- `/migrate remove` — Remove the last unapplied migration

## Commands

### Create Migration
```bash
cd backend && dotnet ef migrations add $ARGUMENTS \
  --project src/MockServer.Infrastructure \
  --startup-project src/MockServer.Api
```

### Apply Migrations
```bash
cd backend && dotnet ef database update \
  --project src/MockServer.Infrastructure \
  --startup-project src/MockServer.Api
```

### Check Status
```bash
cd backend && dotnet ef migrations list \
  --project src/MockServer.Infrastructure \
  --startup-project src/MockServer.Api
```

### Remove Last Migration
```bash
cd backend && dotnet ef migrations remove \
  --project src/MockServer.Infrastructure \
  --startup-project src/MockServer.Api
```

## Migration Naming Convention

Use PascalCase describing the change:
- `AddServiceProxyTable`
- `AddFallbackEnabledToServiceProxy`
- `RemoveProxyConfigTable`
- `AddIndexOnLogsTimestamp`

## Notes

- App auto-migrates on startup (`Program.cs` calls `db.Database.Migrate()`)
- Migrations are at `backend/src/MockServer.Infrastructure/Data/Migrations/`
- Entity configurations are at `backend/src/MockServer.Infrastructure/Data/Configurations/`
- Always create the EF Configuration class (`IEntityTypeConfiguration<T>`) before running migration
- After creating migration, review the generated file to verify correctness

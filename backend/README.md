# Mithya Backend

A multi-protocol mock server with admin UI support, built with ASP.NET Core 8.

## Features

- ✅ Multi-protocol support (REST/JSON, SOAP/XML)
- ✅ JsonPath and XPath matching
- ✅ Body, Header, Query, Path parameter matchers
- ✅ Default response handling
- ✅ Real-time rule updates without restart
- ✅ PostgreSQL persistence
- ✅ Admin REST API
- ✅ Swagger documentation

## Quick Start

### Prerequisites

- Docker & Docker Compose
- .NET 8.0 SDK (for local development)

### Run with Docker Compose

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f backend

# Stop all services
docker-compose down
```

### Run Locally

```bash
# Start PostgreSQL
docker-compose up -d postgres

# Run migrations
cd src/Mithya.Api
export PATH="$PATH:/Users/chachalin/.dotnet/tools"
dotnet ef database update

# Run application
dotnet run
```

## API Endpoints

### Admin API (Port 5000)

- `GET /admin/api/protocols` - Get supported protocols
- `GET /admin/api/endpoints` - List all endpoints
- `POST /admin/api/endpoints` - Create endpoint
- `PUT /admin/api/endpoints/{id}/default` - Set default response
- `POST /admin/api/endpoints/{id}/rules` - Create rule
- `GET /admin/api/logs` - Query request logs

### Mock API (Port 5001)

All mocked endpoints run on port 5001 based on your configuration.

## Example Usage

### 1. Create a REST Endpoint

```bash
curl -X POST http://localhost:5000/admin/api/endpoints \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Credit Query",
    "serviceName": "Credit Service",
    "protocol": 1,
    "path": "/api/v1/credit/query",
    "httpMethod": "POST"
  }'
```

### 2. Add a Rule with Body Matching

```bash
curl -X POST http://localhost:5000/admin/api/endpoints/{endpoint-id}/rules \
  -H "Content-Type: application/json" \
  -d '{
    "ruleName": "Normal User",
    "priority": 1,
    "conditions": [
      {
        "sourceType": 1,
        "fieldPath": "$.idn",
        "operator": 1,
        "value": "P220395911"
      }
    ],
    "statusCode": 200,
    "responseBody": "{\"status\":\"success\",\"creditScore\":750}"
  }'
```

### 3. Test Mock Request

```bash
curl -X POST http://localhost:5001/api/v1/credit/query \
  -H "Content-Type: application/json" \
  -d '{"idn":"P220395911","accountId":123456}'
```

## Architecture

```
backend/
├── src/
│   ├── Mithya.Core/          # Domain layer
│   ├── Mithya.Infrastructure/ # Data + WireMock integration
│   └── Mithya.Api/            # REST API + Minimal APIs
├── docker-compose.yml
└── README.md
```

## Configuration

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=mithya;..."
  },
  "WireMock": {
    "Port": 5001
  }
}
```

## Development

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Create migration
dotnet ef migrations add MigrationName --project src/Mithya.Infrastructure

# Apply migration
dotnet ef database update
```

## Phase 2 Preview

- React Admin UI
- Response templating (Handlebars)
- Stateful mock
- Proxy/Recording mode

## License

MIT

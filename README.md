# Lesto

Lesto is a university project for managing last-mile deliveries through a
small distributed REST API.

## Architecture

The solution contains three .NET 10 services:

- **API Gateway** - the single public entry point, implemented with YARP.
- **Identity Service** - registration, login, JWT tokens and user roles.
- **Order Service** - delivery points, orders and delivery status changes.

The services use separate PostgreSQL databases. The Order Service also
integrates with OSRM to calculate route distance and duration.

```text
Client -> API Gateway -> Identity Service -> identitydb
                     -> Order Service   -> orderdb
                                           -> OSRM
```

## Technologies

- C# and ASP.NET Core on .NET 10
- Entity Framework Core and PostgreSQL
- JWT authentication and role-based authorization
- YARP API Gateway
- Docker Compose
- xUnit, Moq and EF Core InMemory for tests

## Run locally

1. Copy `.env.example` to `.env` and set the database, JWT and optional seed
   credentials.
2. Start the stack:

```powershell
docker compose -f deploy\docker\docker-compose.yml up --build -d
```

3. Use the API Gateway at `http://localhost:5000`.

The local service ports are:

| Service | Port |
|---|---:|
| API Gateway | 5000 |
| Identity Service | 5001 |
| Order Service instance 1 | 5002 |
| Order Service instance 2 | 5003 |
| PostgreSQL | 5432 |

Swagger is available directly on the Identity and Order services during local
development. Postman environments and the request collection are available in
the [postman](postman) folder.

## Run tests

```powershell
dotnet test Lesto.slnx
```

## Documentation

- [Architecture](docs/architecture.md)
- [Requirements](docs/requirements.md)
- [Technical contracts](docs/technical-contracts.md)
- [Data model](docs/data-model.md)
- [Security notes](docs/seguranca.md)
- [Cloud deployment commands](docs/cloud/comandos.md)

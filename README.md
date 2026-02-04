# Antital API

Investment platform backend (CQRS/MediatR, EF Core, Postgres, .NET 8).

## Tech Stack
- .NET 8.0
- PostgreSQL (Npgsql/EF Core)
- Docker & docker-compose
- Clean Architecture / CQRS

## Run with Docker (recommended)
```bash
docker compose up -d
```
Services:
- Postgres 15 on host **55432** → container **db:5432**
- Antital API on **http://localhost:18001**

Compose reads secrets from `.env`. Start by copying the template:
```bash
cp .env.example .env
# then edit .env with your local values (do not commit .env)
```

Apply migrations to the compose DB:
```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=55432;Database=antitaldb;Username=postgres;Password=postgres" \
dotnet ef database update --project Antital.Infrastructure --startup-project Antital.API
```

Stop / wipe:
```bash
docker compose down        # keep data
docker compose down -v     # drop pgdata volume
```

## Run locally (without Docker)
Reuse the compose Postgres from host:
```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=55432;Database=antitaldb;Username=postgres;Password=postgres" \
dotnet run --project Antital.API
```

## Integration tests
```bash
export TEST_DB_CONNECTION_STRING="Host=localhost;Port=55432;Database=antitaldb_test;Username=postgres;Password=postgres"
ConnectionStrings__DefaultConnection="Host=localhost;Port=55432;Database=antitaldb;Username=postgres;Password=postgres" \
dotnet test Antital.Test/Antital.Test.csproj -c Release
```
Tests auto-migrate the test DB.

## CI (GitHub Actions)
- Spins up Postgres service named `postgres`
- Uses connection string: `Host=localhost;Port=5432;Database=antitaldb_test;Username=postgres;Password=postgres`

## URLs
- Swagger: http://localhost:18001/swagger
- Health: http://localhost:18001/healthz

## Logs
```bash
docker logs antital-api-antital.api-1 -f    # API
docker logs antital-api-db-1 -f             # Postgres
```

## Notes
- Connection strings differ only by host/port: `localhost:55432` on host vs `db:5432` inside Docker.
- Migrations live in `Antital.Infrastructure`.

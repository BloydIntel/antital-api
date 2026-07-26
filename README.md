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

## Local secrets

Use one of these local-only approaches for secrets such as Dojah keys, Paystack keys, Cloudinary credentials, or a dev connection string.

### Option 1: `dotnet user-secrets`

The API project already has a `UserSecretsId`, so you can store secrets outside the repo:

```bash
dotnet user-secrets --project Antital.API set "Dojah:AppId" "your-app-id"
dotnet user-secrets --project Antital.API set "Dojah:PublicKey" "your-public-key"
dotnet user-secrets --project Antital.API set "Dojah:PrivateKey" "your-private-key"
dotnet user-secrets --project Antital.API set "Dojah:WidgetId" "your-widget-id"
dotnet user-secrets --project Antital.API set "Dojah:BaseUrl" "https://sandbox.dojah.io"
```

You can also set flat aliases because the API binds both nested `Dojah:*` keys and flat `Dojah_*` keys:

```bash
dotnet user-secrets --project Antital.API set "Dojah_AppId" "your-app-id"
dotnet user-secrets --project Antital.API set "Dojah_PublicKey" "your-public-key"
dotnet user-secrets --project Antital.API set "Dojah_PrivateKey" "your-private-key"
dotnet user-secrets --project Antital.API set "Dojah_WidgetId" "your-widget-id"
```

Useful commands:

```bash
dotnet user-secrets --project Antital.API list
dotnet user-secrets --project Antital.API remove "Dojah:PrivateKey"
```

### Option 2: `appsettings.Development.local.json`

You can also keep local secrets in:

`Antital.API/appsettings.Development.local.json`

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=55432;Database=antitaldb;Username=postgres;Password=postgres"
  },
  "Dojah": {
    "Enabled": true,
    "AppId": "your-app-id",
    "PublicKey": "your-public-key",
    "PrivateKey": "your-private-key",
    "WidgetId": "your-widget-id",
    "BaseUrl": "https://sandbox.dojah.io"
  }
}
```

Keep this file out of source control and use it only for your machine.

### Precedence

Recommended order for local development:

1. `appsettings.Development.json` for safe shared defaults.
2. `appsettings.Development.local.json` for machine-specific overrides.
3. `dotnet user-secrets` for sensitive values you do not want in any file.
4. Environment variables / shell overrides for one-off runs.

For example:

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=55432;Database=antitaldb;Username=postgres;Password=postgres" \
dotnet run --project Antital.API
```

## Run with .NET Aspire (AppHost)

This repository uses an Aspire AppHost project at `AntitalAPI.AppHost`.
Start it with:

```bash
dotnet restore
dotnet run --project AntitalAPI.AppHost
```

The AppHost wires `NEXT_PUBLIC_API_URL` for the UI and sets `Paystack:CallbackUrl` to the Aspire UI port automatically (e.g. `http://localhost:62388/marketplace/invest/callback`).

**Paystack local dev:** Paystack does not provide a separate webhook secret in the dashboard. Leave `Paystack:WebhookSecret` empty — the API validates webhook signatures using `Paystack:SecretKey`. For localhost, webhooks cannot reach your machine; after Paystack redirect the UI calls `POST /api/investments/orders/{orderId}/verify` to confirm payment via Paystack's verify API.

If you want Aspire workload/templates available locally:

```bash
dotnet workload install aspire
```

Note: `dotnet aspire` may not exist as a command unless the corresponding tool is installed; running the AppHost project is the standard startup path.

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

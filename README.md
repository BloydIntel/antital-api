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

## Secret Management for Local Development

Sensitive configuration values (e.g., API keys, connection strings) should not be committed to version control. For local development, there are two primary ways to manage these:

### 1. User Secrets

This is the recommended approach for development-specific secrets that don't need to be shared.

-   **Initialize User Secrets:**
    First, add a `UserSecretsId` to your `.csproj` file (e.g., `Antital.API/Antital.API.csproj`). If it's not already there, you can add it within a `<PropertyGroup>`:
    ```xml
    <PropertyGroup>
      <UserSecretsId>YOUR_UNIQUE_GUID_HERE</UserSecretsId>
    </PropertyGroup>
    ```
    You can generate a GUID using `uuidgen` in your terminal.

-   **Set a Secret:**
    Navigate to the project directory containing your `.csproj` file (e.g., `cd Antital.API`) or use the `--project` flag. Then, set your secret:
    ```bash
    dotnet user-secrets set "Section:Key" "YourSecretValue"
    # Example for a JWT key:
    dotnet user-secrets set "Jwt:Key" "q8cKZr8u9wH2v3F5Zz8pVd5pE5ZzZc4Qn1F0y9RZzF8="
    # Example for Dojah PrivateKey:
    dotnet user-secrets set "Dojah:PrivateKey" "sk_live_YOUR_PRIVATE_KEY"
    ```
    These secrets are stored in a JSON file on your machine (outside the repository) and automatically loaded by the application in the Development environment.

### 2. `appsettings.Development.local.json`

For local configuration overrides that are not sensitive but you want to keep out of `appsettings.Development.json` (e.g., changing a service endpoint URL just for your local setup), you can create `appsettings.Development.local.json`.

This file is typically ignored by Git (check `.gitignore`) and allows you to override any settings defined in `appsettings.json` or `appsettings.Development.json` without modifying the committed files.

Example `appsettings.Development.local.json`:
```json
{
  "Dojah": {
    "Enabled": true,
    "AppId": "your_local_app_id",
    "PublicKey": "your_local_public_key",
    "WidgetId": "your_local_widget_id",
    "BaseUrl": "https://sandbox.dojah.io"
  }
}
```

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

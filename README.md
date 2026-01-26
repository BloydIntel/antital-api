# Antital API

**Antital** is an investment platform for Nigeria - democratizing access to startup investments and alternative assets, similar to Republic.com but tailored for the Nigerian market.

## 🚀 What is Antital?

Antital enables Nigerians to invest in startups, real estate, and other alternative investment opportunities. Our platform provides a seamless way for investors to discover, evaluate, and invest in promising Nigerian businesses and assets.

## 🛠️ Tech Stack

- **.NET 10.0** - Backend API
- **SQL Server** - Database
- **Docker** - Containerization
- **Clean Architecture** - CQRS pattern with MediatR
- **Entity Framework Core** - ORM

## 📋 Prerequisites

- Docker Desktop installed and running
- .NET 10.0 SDK (for local development)
- SQL Server Management Studio (SSMS) - optional, for database management

## 🏃 Quick Start

### 1. Start the Services

```bash
docker-compose up -d
```

This will start:
- **SQL Server** on port `8600`
- **Antital API** on port `18001`

### 2. Access the API

- **Swagger UI**: http://localhost:18001/swagger
- **Health Check**: http://localhost:18001/healthz
- **API Base URL**: http://localhost:18001

### 3. Database Connection (SSMS)

- **Server**: `localhost,8600`
- **Authentication**: SQL Server Authentication
- **Username**: `sa`
- **Password**: `Admin1234!!`
- **Database**: `AntitalDB`

## 🛑 Stop Services

```bash
docker-compose down
```

To remove volumes (clears database data):
```bash
docker-compose down -v
```

## 📁 Project Structure

```
Antital.API/              # Web API layer
Antital.Application/      # Business logic (CQRS handlers)
Antital.Domain/           # Domain models and interfaces
Antital.Infrastructure/   # Data access and external services
Antital.Resources/        # Localization resources
BuildingBlocks/           # Shared infrastructure components
```

## 🔧 Development

### Running Locally (without Docker)

1. Ensure SQL Server LocalDB is installed
2. Update connection string in `Antital.API/appsettings.Development.json`
3. Run:
   ```bash
   dotnet run --project Antital.API
   ```

### Viewing Logs

```bash
# API logs
docker logs antital.api -f

# Database logs
docker logs antitaldb -f
```

## 📚 Additional Services

The project includes additional services that can be enabled as needed:

- **Redis** - Caching
- **RabbitMQ** - Message queuing
- **Prometheus + Grafana** - Monitoring
- **Worker APIs** - Background processing
- **Load Balancer** - Load distribution

See `DOCKER_SETUP.md` for details on enabling these services.

## 🤝 Contributing

1. Create a feature branch
2. Make your changes
3. Submit a pull request

## 📝 License

[Your License Here]

---

**Built with ❤️ for Nigeria**

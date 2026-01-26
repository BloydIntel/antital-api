# Docker Compose Setup Guide

## 🚀 Minimal Setup (For First-Time Development)

**What's Running:**
- ✅ `antitaldb` - SQL Server database
- ✅ `antital.api` - Main API application

**To start:**
```bash
docker-compose up
```

**Access:**
- API: http://localhost:18001/swagger
- SQL Server: localhost:8600 (sa/Admin1234!!)

---

## 📋 Gradually Enable Services

### Phase 1: Add Caching (Redis)
**When you need:** Caching, session storage

**Uncomment in both files:**
- `docker-compose.yml`: `redis` service
- `docker-compose.override.yml`: `redis` service
- `docker-compose.override.yml`: Uncomment `ConnectionStrings:RedisConnection` in `antital.api`

**Then:**
```bash
docker-compose up redis
```

---

### Phase 2: Add Messaging (RabbitMQ)
**When you need:** Message queues, async processing

**Uncomment in both files:**
- `docker-compose.yml`: `rabbitmq` service
- `docker-compose.override.yml`: `rabbitmq` service
- `docker-compose.override.yml`: Uncomment `RabbitMQSettings` in `antital.api`
- `docker-compose.override.yml`: Add `rabbitmq` to `depends_on` in `antital.api`

**Then:**
```bash
docker-compose up rabbitmq
```

---

### Phase 3: Add Worker Services
**When you need:** Background processing, load balancing

**Uncomment in both files:**
- `docker-compose.yml`: `antitalworkerdb`, `antital.worker.api1/2/3`, `antital.worker.loadbalancer`
- `docker-compose.override.yml`: All worker services

**Then:**
```bash
docker-compose up antitalworkerdb antital.worker.api1 antital.worker.api2 antital.worker.api3 antital.worker.loadbalancer
```

---

### Phase 4: Add Monitoring (Prometheus + Grafana)
**When you need:** Metrics, monitoring dashboards

**Uncomment in both files:**
- `docker-compose.yml`: `prometheus`, `grafana`
- `docker-compose.override.yml`: Both services

**Access:**
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3700 (admin/admin)

---

### Phase 5: Add Database Management Tools
**When you need:** GUI for PostgreSQL

**Uncomment in both files:**
- `docker-compose.yml`: `pgadmin`
- `docker-compose.override.yml`: `pgadmin` service

**Access:**
- PgAdmin: http://localhost:5050 (behzad@dara.com/admin1234)

---

### Phase 6: Add Docker Management (Portainer)
**When you need:** GUI for Docker management

**Uncomment in both files:**
- `docker-compose.yml`: `portainer`
- `docker-compose.override.yml`: `portainer` service

**Access:**
- Portainer: http://localhost:9000

---

## 🗑️ Should You Keep All Solutions?

### Keep These (Core):
- ✅ `Antital.API` - Your main API
- ✅ `Antital.Application` - Business logic
- ✅ `Antital.Domain` - Domain models
- ✅ `Antital.Infrastructure` - Data access
- ✅ `Antital.Resources` - Localization
- ✅ `BuildingBlocks.*` - Shared infrastructure

### Optional (Can Remove Later):
- ⚠️ `Antital.Worker.API` - Only if you need background workers
- ⚠️ `Antital.Worker.LoadBalancer` - Only if you need load balancing
- ⚠️ `Antital.Test` - Keep for testing (recommended)

### Recommendation:
1. **Start with minimal setup** (API + SQL Server)
2. **Keep Worker projects** for now (you might need them)
3. **Remove later** if you don't use them after a few weeks

---

## 💡 Tips

1. **Start minimal** - Only enable what you need
2. **Check logs** - `docker-compose logs antital.api` to debug
3. **Rebuild when needed** - `docker-compose up --build` after code changes
4. **Stop services** - `docker-compose down` to stop everything
5. **Remove volumes** - `docker-compose down -v` to clean up data (careful!)

---

## 🔧 Troubleshooting

**API won't start?**
- Check SQL Server is running: `docker ps`
- Check logs: `docker-compose logs antital.api`
- Verify connection string in `docker-compose.override.yml`

**Port already in use?**
- Change ports in `docker-compose.override.yml`
- Or stop conflicting services

**Database connection issues?**
- Wait 10-15 seconds after SQL Server starts
- Check password matches: `Admin1234!!`
- Verify trust certificate is enabled

Docker compose for local development

This repository includes a reference `docker-compose.yml` at the project root that starts a PostgreSQL 16 (image: `postgres:16-alpine`) and an optional pgAdmin service.

Default environment variables (you can create a `.env` file in the repo root to override):
- POSTGRES_USER (default: postgres)
- POSTGRES_PASSWORD (default: postgres)
- POSTGRES_DB (default: cavero)
- POSTGRES_PORT (default: 5432)
- PGADMIN_DEFAULT_EMAIL (default: admin@localhost)
- PGADMIN_DEFAULT_PASSWORD (default: admin)
- PGADMIN_PORT (default: 8080)

Example connection string for `appsettings.Development.json`:

"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=cavero;Username=postgres;Password=postgres"
}

(If you override `POSTGRES_PORT` or other vars, adapt the connection string accordingly, e.g. `Port=${POSTGRES_PORT}`.)

Usage:

```powershell
# Start services in background
docker compose up -d

# Stop
docker compose down
```

Notes:
- The Postgres image version `postgres:16-alpine` was chosen to match Testcontainers fixtures for integration tests, avoiding "works on my machine" differences.
- The `pgdata` volume persists the database between restarts.
- pgAdmin is optional — access it at `http://localhost:8080` (or the port you set).

# Quick Reference Guide

## Quick Start Commands

**First time setup:**
```bash
# Create .env file with secure credentials
cp .env.example .env
# Edit and set POSTGRES_PASSWORD to a strong password
nano .env
```

**Daily usage:**
```bash
# Start the application
docker compose up -d

# Stop the application
docker compose down

# View logs
docker compose logs -f

# Restart everything
docker compose restart
```

## Docker Compose Structure

```
wasthere/
├── docker-compose.yml       # Main orchestration file
├── Dockerfile.api          # Backend container definition
├── Dockerfile.web          # Frontend container definition
├── nginx.conf             # Nginx web server configuration
├── setup-docker.sh        # Docker installation script
├── .env.example          # Environment variables template
└── .dockerignore         # Files to exclude from builds
```

## Container Architecture

```
┌─────────────────────────────────────────┐
│         Docker Host (Linux VM)          │
│                                         │
│  ┌────────────┐  ┌──────────────┐     │
│  │  wasthere- │  │  wasthere-   │     │
│  │    web     │  │     api      │     │
│  │  (nginx)   │  │  (.NET 8.0)  │     │
│  │   :80      │  │    :5000     │     │
│  └─────┬──────┘  └──────┬───────┘     │
│        │                 │              │
│        └────────┬────────┘              │
│                 │                       │
│         ┌───────▼────────┐             │
│         │  wasthere-db   │             │
│         │  (PostgreSQL)  │             │
│         │     :5432      │             │
│         └────────────────┘             │
│                 │                       │
│         ┌───────▼────────┐             │
│         │  postgres_data │             │
│         │    (volume)    │             │
│         └────────────────┘             │
└─────────────────────────────────────────┘
```

## Service Ports

| Service | Internal Port | External Port | Description |
|---------|--------------|---------------|-------------|
| web     | 80           | 80            | Frontend application |
| api     | 5000         | 5000          | Backend API |
| db      | 5432         | 5432          | PostgreSQL database |

## Environment Variables

### Database
- `POSTGRES_DB`: Database name
- `POSTGRES_USER`: Database username
- `POSTGRES_PASSWORD`: Database password

### API
- `ASPNETCORE_ENVIRONMENT`: ASP.NET environment
- `ConnectionStrings__DefaultConnection`: Database connection string

### Frontend
- `VITE_API_URL`: API endpoint URL (build-time only)

## Common Tasks

### Access container shells
```bash
# API container
docker compose exec api bash

# Database container
docker compose exec db psql -U postgres -d wasthere

# Web container
docker compose exec web sh
```

### View specific logs
```bash
docker compose logs api      # API logs only
docker compose logs web      # Frontend logs only
docker compose logs db       # Database logs only
```

### Check container health
```bash
docker compose ps            # Status of all services
docker compose top          # Running processes in containers
docker stats                # Resource usage
```

### Rebuild specific service
```bash
docker compose build api    # Rebuild API only
docker compose build web    # Rebuild frontend only
docker compose up -d --no-deps api  # Restart API without dependencies
```

## Development Workflow

### Local changes
1. Make code changes
2. Rebuild the affected service: `docker compose build api` or `docker compose build web`
3. Restart the service: `docker compose up -d`
4. Test the changes

### Database changes
1. Update Entity Framework models
2. Create migration: `cd WasThere.Api && dotnet ef migrations add MigrationName`
3. Rebuild API: `docker compose build api`
4. Restart API: `docker compose up -d api`
5. Migration runs automatically on startup

## Troubleshooting

### Container crashes immediately
```bash
docker compose logs --tail=50 <service-name>
```

### Can't connect to database
```bash
# Check if database is ready
docker compose exec db pg_isready -U postgres

# Check connection from API
docker compose exec api ping db
```

### Port already in use
```bash
# Check what's using the port
sudo lsof -i :80
sudo lsof -i :5000
sudo lsof -i :5432

# Stop the conflicting service or change ports in docker-compose.yml
```

### Out of disk space
```bash
# Clean up Docker resources
docker system prune -a --volumes

# Check disk usage
df -h
docker system df
```

## URLs

When deployed:
- Frontend: `http://localhost` or `http://<your-vm-ip>`
- API: `http://localhost:5000` or `http://<your-vm-ip>:5000`
- Swagger: `http://localhost:5000/swagger` or `http://<your-vm-ip>:5000/swagger`

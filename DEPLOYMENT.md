# WasThere Docker Deployment Guide

This guide provides detailed instructions for deploying the WasThere application using Docker containers.

## Overview

The application consists of three Docker containers:
- **wasthere-db**: PostgreSQL 16 database with persistent storage
- **wasthere-api**: .NET 8.0 API backend
- **wasthere-web**: React frontend served by Nginx

## Prerequisites

- A Linux VM (Ubuntu, Debian, CentOS, RHEL, or Fedora)
- Root or sudo access
- At least 2GB RAM recommended
- 10GB free disk space

## Step 1: Install Docker

On a fresh Linux environment, run the provided setup script:

```bash
# Make the script executable (if not already)
chmod +x setup-docker.sh

# Run the installation script
sudo bash setup-docker.sh
```

The script will:
- Detect your Linux distribution
- Install Docker and Docker Compose
- Configure Docker to start on boot
- Add your user to the docker group

**Important**: After the script completes, log out and back in for group permissions to take effect.

## Step 2: Verify Docker Installation

```bash
docker --version
docker compose version
docker run hello-world
```

## Step 3: Configure the Application (Required)

**IMPORTANT**: You must create a `.env` file with secure credentials before starting the application.

```bash
# Copy the example environment file
cp .env.example .env

# Edit the file and set a strong password
nano .env
```

**Required configuration**:
- `POSTGRES_PASSWORD`: Set a strong password (minimum 16 characters recommended)

Available configuration options:
- `POSTGRES_DB`: Database name (default: wasthere)
- `POSTGRES_USER`: Database username (default: wasthere_user)
- `POSTGRES_PASSWORD`: Database password (**MUST BE SET** - no default)
- `VITE_API_URL`: Frontend API URL (default: http://localhost:5000/api)
- `DB_PORT`: Database port (default: 5432)
- `API_PORT`: API port (default: 5000)
- `WEB_PORT`: Web frontend port (default: 80)

**Security Note**: For production deployments:
- Use a strong, randomly generated password (e.g., `openssl rand -base64 32`)
- Never commit the `.env` file to version control
- Consider using Docker secrets for sensitive data in production
- Remove or change the DB_PORT mapping to not expose PostgreSQL externally

### Verify Configuration

Run the deployment checker to verify everything is configured correctly:

```bash
bash check-deployment.sh
```

This will check for:
- Required files and configurations
- Secure password settings
- Port availability
- Sufficient disk space

## Step 4: Deploy the Application

```bash
# Build and start all containers
docker compose up -d

# View logs
docker compose logs -f

# Check container status
docker compose ps
```

The application will be available at:
- **Frontend**: http://localhost (or http://your-vm-ip)
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger

## Step 5: Verify Deployment

1. Open your browser to http://localhost (or your VM's IP address)
2. You should see the WasThere application
3. Try adding some events, venues, and acts
4. Create a club night to verify full functionality

## Managing the Application

### Start the application
```bash
docker compose up -d
```

### Stop the application
```bash
docker compose down
```

### View logs
```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f api
docker compose logs -f web
docker compose logs -f db
```

### Restart a service
```bash
docker compose restart api
docker compose restart web
docker compose restart db
```

### Update the application
```bash
# Pull latest changes
git pull

# Rebuild and restart
docker compose down
docker compose build --no-cache
docker compose up -d
```

## Data Management

### Database Migrations

If you need to create new database migrations during development:

```bash
# Set the connection string for design-time operations
export DESIGN_TIME_CONNECTION_STRING='Host=localhost;Database=wasthere;Username=postgres;Password=yourpassword'

# Create a new migration
cd WasThere.Api
dotnet ef migrations add MigrationName

# The migration will be applied automatically when the container starts
```

### Backup the database
```bash
docker compose exec db pg_dump -U postgres wasthere > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Restore the database
```bash
docker compose exec -T db psql -U postgres wasthere < backup_file.sql
```

### Reset all data
```bash
# WARNING: This will delete all data!
docker compose down -v
docker compose up -d
```

## Troubleshooting

### Container won't start
```bash
# Check logs
docker compose logs -f

# Check container status
docker compose ps -a
```

### Database connection issues
```bash
# Check if database is healthy
docker compose ps

# Restart database
docker compose restart db
```

### Port conflicts
If ports 80, 5000, or 5432 are already in use, edit `docker-compose.yml`:

```yaml
ports:
  - "8080:80"    # Change host port for web
  - "5001:5000"  # Change host port for API
  - "5433:5432"  # Change host port for database
```

### Build failures
```bash
# Clean rebuild
docker compose down
docker compose build --no-cache
docker compose up -d
```

## Security Considerations

For production deployments:

1. **Change default passwords**: Edit `.env` and update `POSTGRES_PASSWORD`
2. **Use HTTPS**: Configure a reverse proxy (nginx/Apache) with SSL certificates
3. **Firewall rules**: Only expose necessary ports (80/443)
4. **Regular updates**: Keep Docker and application images updated
5. **Backup strategy**: Implement regular database backups

## Network Configuration

By default, the application uses:
- Port 80 for the web frontend
- Port 5000 for the API
- Port 5432 for PostgreSQL (internal only, exposed for development)

For production, consider:
- Using a reverse proxy with SSL termination
- Removing the database port exposure
- Setting up proper firewall rules

## Monitoring

### Check resource usage
```bash
docker stats
```

### Check disk usage
```bash
docker system df
```

### Clean up unused resources
```bash
docker system prune -a
```

## Support

For issues or questions:
1. Check the logs: `docker compose logs -f`
2. Verify all containers are running: `docker compose ps`
3. Review this guide for common troubleshooting steps
4. Check the main README.md for application-specific information

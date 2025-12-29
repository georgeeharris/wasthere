# WasThere - Club Events Archive

A simple, clean web application for maintaining an archive of club events. Keep track of club nights with events, venues, acts, and dates.

## Features

- **Events Management**: Add and manage club night series (e.g., "Bugged Out")
- **Venues Management**: Maintain a list of venues (e.g., "Sankey's Soap")
- **Acts Management**: Track performing artists
- **Club Nights**: Create club night instances that combine events, venues, dates, and lineup of acts

## Tech Stack

- **Backend**: .NET 8.0 Web API with Entity Framework Core
- **Frontend**: React with TypeScript (Vite)
- **Database**: PostgreSQL (when containerized) or In-Memory (for development)
- **Deployment**: Docker & Docker Compose

## Getting Started

### Option 1: Docker Deployment (Recommended for Production)

#### Prerequisites

- Docker and Docker Compose

#### First-Time Setup on a Fresh Linux VM

If you're starting with a brand new Linux environment without Docker:

```bash
# Run the setup script to install Docker
sudo bash setup-docker.sh

# Log out and back in for group permissions to take effect
```

#### Running with Docker

```bash
# First, create a .env file with secure credentials
cp .env.example .env
# Edit .env and set POSTGRES_PASSWORD to a strong password
nano .env

# Start all services (frontend, backend, and database)
docker compose up -d

# View logs
docker compose logs -f

# Stop all services
docker compose down

# Stop and remove all data
docker compose down -v
```

The application will be available at:
- **Frontend**: http://localhost
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger

**Important**: You must set a secure `POSTGRES_PASSWORD` in the `.env` file before running.

#### Configuration

You can customize the deployment by creating a `.env` file:

```bash
cp .env.example .env
# Edit .env with your preferred settings
```

### Option 2: Local Development

#### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18 or later)

#### Running the Application

##### 1. Start the Backend API

```bash
cd WasThere.Api
dotnet run
```

The API will start at `http://localhost:5000`

##### 2. Start the Frontend

In a new terminal:

```bash
cd wasthere-web
npm install  # First time only
npm run dev
```

The frontend will start at `http://localhost:5173`

### Using the Application

1. Open your browser to `http://localhost:5173` (development) or `http://localhost` (Docker)
2. Navigate to "Master Lists" to add Events, Venues, and Acts
3. Switch to "Club Nights" to create club night instances combining your master data with dates

## API Endpoints

- `GET/POST/PUT/DELETE /api/events` - Manage events
- `GET/POST/PUT/DELETE /api/venues` - Manage venues
- `GET/POST/PUT/DELETE /api/acts` - Manage acts
- `GET/POST/PUT/DELETE /api/clubnights` - Manage club night instances

## Development Notes

- In local development, the backend uses an in-memory database, so data is reset when the API is restarted
- In Docker deployment, data is persisted in a PostgreSQL database
- CORS is configured to allow requests from the frontend
- The API includes Swagger UI at `http://localhost:5000/swagger` for API exploration

## Docker Architecture

The application consists of three containers:

1. **wasthere-db**: PostgreSQL 16 database with persistent storage
2. **wasthere-api**: .NET 8.0 API backend
3. **wasthere-web**: React frontend served by Nginx

All containers are networked together and the database includes health checks to ensure proper startup order.



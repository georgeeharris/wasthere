# WasThere - Club Events Archive

A simple, clean web application for maintaining an archive of club events. Keep track of club nights with events, venues, acts, and dates. Hoping to be the discogs of techno club events in the late 90s / early 00s

## Features

- **Events Management**: Add and manage club night series (e.g., "Bugged Out")
- **Venues Management**: Maintain a list of venues (e.g., "Sankey's Soap")
- **Acts Management**: Track performing artists
- **Club Nights**: Create club night instances that combine events, venues, dates, and lineup of acts
- **Flyer Management**: Upload and manage flyer images organized by event, venue, and date
- **AI-Powered Auto-Population**: Automatically extract events, venues, acts, and club night dates from flyer images using Google Gemini AI

## Tech Stack

- **Backend**: .NET 8.0 Web API with Entity Framework Core
- **Frontend**: React with TypeScript (Vite)
- **Database**: PostgreSQL (when containerized) or In-Memory (for development)
- **Deployment**: Docker & Docker Compose
- **CI/CD**: GitHub Actions for automated testing and deployment

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
- **Frontend**: http://www.wasthere.co.uk (production) or http://localhost (local)
- **API**: http://www.wasthere.co.uk:5000 (production) or http://localhost:5000 (local)
- **Swagger UI**: http://www.wasthere.co.uk:5000/swagger (production) or http://localhost:5000/swagger (local)

**HTTPS**: To enable HTTPS/SSL for secure connections, see [HTTPS Setup Guide](HTTPS-SETUP.md) for detailed instructions on obtaining and configuring SSL certificates.

**Important**: You must set a secure `POSTGRES_PASSWORD` in the `.env` file before running.

#### Configuration

You can customize the deployment by creating a `.env` file:

```bash
cp .env.example .env
# Edit .env with your preferred settings
```

Key configuration options:
- `POSTGRES_PASSWORD`: Required - Set a strong database password
- `CORS_ORIGINS`: Comma-separated list of allowed frontend origins (default: www.wasthere.co.uk)
  - For production with a domain: `CORS_ORIGINS=http://www.wasthere.co.uk,https://www.wasthere.co.uk`
  - For local development: `CORS_ORIGINS=http://localhost,http://localhost:5173,http://localhost:3000`
- `VITE_API_URL`: Frontend API endpoint URL
  - **Important**: Always use domain names, not IP addresses (e.g., `http://www.wasthere.co.uk:5000/api` not `http://82.165.153.98:5000/api`) to avoid CORS errors
  - Use `https://` protocol when SSL/HTTPS is enabled
- Ports: `API_PORT`, `WEB_PORT`, `DB_PORT`
- HTTPS/SSL: `WEB_HTTPS_PORT` (default: 443), see [HTTPS Setup Guide](HTTPS-SETUP.md) for full SSL configuration

**Note**: After changing `.env`, restart the services with `docker compose restart`

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
- `GET/POST/DELETE /api/flyers` - Manage flyer images
  - `POST /api/flyers/upload` - Upload flyer image with Event, Venue, and earliest ClubNight date
  - `POST /api/flyers/{id}/auto-populate` - Automatically extract and populate events, venues, acts, and club nights from flyer using AI
  - Static files served at `/uploads/{event}/{venue}/{date}/`

## Development Notes

- In local development, the backend uses an in-memory database, so data is reset when the API is restarted
- In Docker deployment, data is persisted in a PostgreSQL database
- CORS is configured to allow requests from the frontend
- The API includes Swagger UI at `http://localhost:5000/swagger` for API exploration

### AI-Powered Auto-Population

The application uses Google's Gemini 1.5 Flash AI to automatically extract information from flyer images:

- **Configuration**: Set `GoogleGemini:ApiKey` in `appsettings.json` or via environment variable
- **Features**:
  - Extracts event names, venue names, dates, and performing acts from flyer images
  - Creates new entities or matches existing ones (case-insensitive)
  - Handles multiple dates on the same flyer (creates separate club nights)
  - Adds resident DJs to all club nights on the flyer
- **Usage**: Click the "Auto-populate" button on any uploaded flyer in the Flyers section
- **Notes**: 
  - API key in `appsettings.json` is for development only
  - For production, set via environment variable: `GoogleGemini__ApiKey`
  - The service will fail gracefully if no API key is configured

## Docker Architecture

The application consists of three containers:

1. **wasthere-db**: PostgreSQL 16 database with persistent storage
2. **wasthere-api**: .NET 8.0 API backend
3. **wasthere-web**: React frontend served by Nginx

All containers are networked together and the database includes health checks to ensure proper startup order.

## CI/CD Pipeline

This project includes automated CI/CD pipelines using GitHub Actions:

- **Continuous Integration**: Automatically tests and builds the application on every push and pull request
- **Continuous Deployment**: Automatically deploys to your Linux VPS when changes are pushed to the `main` branch

### Setting Up Automated Deployment

To set up automated deployment to your VPS:

1. Follow the [CI/CD Setup Guide](.github/CICD-SETUP.md) for detailed instructions
2. Configure GitHub Secrets as described in the [Secrets Reference](.github/SECRETS-REFERENCE.md)
3. Push to the `main` branch to trigger automatic deployment

**Quick Setup:**
- Required: `VPS_HOST`, `VPS_USERNAME`, `VPS_SSH_KEY`, `POSTGRES_PASSWORD`
- See [.github/SECRETS-REFERENCE.md](.github/SECRETS-REFERENCE.md) for the complete list


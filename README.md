# WasThere - Club Events Archive

A simple, clean web application for maintaining an archive of club events. Keep track of club nights with events, venues, acts, and dates.

## Features

- **Events Management**: Add and manage club night series (e.g., "Bugged Out")
- **Venues Management**: Maintain a list of venues (e.g., "Sankey's Soap")
- **Acts Management**: Track performing artists
- **Club Nights**: Create club night instances that combine events, venues, dates, and lineup of acts

## Tech Stack

- **Backend**: .NET 8.0 Web API with Entity Framework Core (In-Memory Database)
- **Frontend**: React with TypeScript (Vite)

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18 or later)

### Running the Application

#### 1. Start the Backend API

```bash
cd WasThere.Api
dotnet run
```

The API will start at `http://localhost:5000`

#### 2. Start the Frontend

In a new terminal:

```bash
cd wasthere-web
npm install  # First time only
npm run dev
```

The frontend will start at `http://localhost:5173`

### Using the Application

1. Open your browser to `http://localhost:5173`
2. Navigate to "Master Lists" to add Events, Venues, and Acts
3. Switch to "Club Nights" to create club night instances combining your master data with dates

## API Endpoints

- `GET/POST/PUT/DELETE /api/events` - Manage events
- `GET/POST/PUT/DELETE /api/venues` - Manage venues
- `GET/POST/PUT/DELETE /api/acts` - Manage acts
- `GET/POST/PUT/DELETE /api/clubnights` - Manage club night instances

## Development Notes

- The backend uses an in-memory database, so data is reset when the API is restarted
- CORS is configured to allow requests from the frontend development server
- The API includes Swagger UI at `http://localhost:5000/swagger` for API exploration


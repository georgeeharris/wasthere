# "Was There" Feature Implementation

## Overview

This feature allows users to mark club nights they attended, creating a personal record of their club-going history. The feature uses the phrase "was there" throughout, reflecting the site's name and purpose.

## Features

### Club Nights Page
- Each club night card now has a "Was there" checkbox
- Marked club nights are highlighted with a distinctive yellow border (3px solid #ffd700 with a subtle glow)
- Filter panel includes "Only show nights I was there" option to view only attended events

### Timeline Page
- Each timeline card includes a "Was there" checkbox
- Yellow border highlighting for attended nights (consistent with club nights page)
- Single "Only show nights I was there" checkbox filter at the top level (no filter panel needed)

### Visual Styling
- Yellow border: `3px solid #ffd700` with `box-shadow: 0 0 8px rgba(255, 215, 0, 0.3)`
- Enhanced hover effect for marked nights: `box-shadow: 0 4px 12px rgba(255, 215, 0, 0.4)`
- Checkboxes styled consistently across both pages

## Technical Implementation

### Database Schema

#### User Table
```sql
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Username TEXT NOT NULL UNIQUE
);
```

#### UserClubNightAttendance Table
```sql
CREATE TABLE UserClubNightAttendances (
    UserId INTEGER NOT NULL,
    ClubNightId INTEGER NOT NULL,
    MarkedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    PRIMARY KEY (UserId, ClubNightId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (ClubNightId) REFERENCES ClubNights(Id) ON DELETE CASCADE
);
```

The schema is designed to support multiple users in the future, even though currently only the "admin" user is used.

### API Endpoints

#### GET /api/clubnights
Returns all club nights with `wasThereByAdmin` field indicating if the admin user attended.

#### GET /api/clubnights/{id}
Returns a specific club night with `wasThereByAdmin` field.

#### POST /api/clubnights/{id}/was-there
Marks that the admin user was at the specified club night.

#### DELETE /api/clubnights/{id}/was-there
Removes the "was there" marking for the admin user at the specified club night.

### Frontend Components

#### ClubNightList Component
- Added `filterWasThere` state for filtering
- Added `handleWasThereToggle` function to mark/unmark attendance
- Updated filter logic to include "was there" filtering
- Added checkbox to each club night card
- Applied `was-there` CSS class for visual highlighting

#### Timeline Component
- Added `filterWasThere` state for filtering
- Added `handleWasThereToggle` function to mark/unmark attendance
- Updated `loadClubNights` to filter by "was there" status
- Added top-level filter checkbox
- Added checkbox to each timeline card
- Applied `was-there` CSS class for visual highlighting

## Current Limitations & Future Enhancements

### Current State
- Single "admin" user hardcoded (no authentication/user state yet)
- All "was there" markings are associated with this admin user

### Planned Enhancements
- Integration with Auth0 authentication
- User-specific "was there" markings
- Potentially: See which other users were at the same events
- Potentially: Statistics and analytics (e.g., "nights attended per year", "most visited venues")

## Testing

### Manual Testing Steps
1. Start the application with Docker Compose (see README.md)
2. Navigate to Club Nights page
3. Check a "Was there" checkbox on a club night - verify yellow border appears
4. Apply "Only show nights I was there" filter - verify only marked nights appear
5. Navigate to Timeline page
6. Select events to view timeline
7. Check "Was there" on timeline cards - verify yellow border appears
8. Enable "Only show nights I was there" filter - verify only marked nights appear
9. Refresh the page - verify "was there" status persists

### Database Migration
After deploying this feature, the database migration must be run to create the new tables and seed the admin user:

```bash
docker compose exec api dotnet ef database update
```

Or for local development:
```bash
cd WasThere.Api
export DESIGN_TIME_CONNECTION_STRING='Host=localhost;Database=wasthere;Username=postgres;Password=yourpassword'
dotnet ef database update
```

## Implementation Details

### Code Quality
- ✅ All code builds successfully (both .NET API and React frontend)
- ✅ No linting errors introduced
- ✅ Code review feedback addressed
- ✅ Security scan passed (CodeQL)
- ✅ Consistent with existing code patterns

### Files Changed
- **Backend (C#/.NET)**:
  - `WasThere.Api/Models/User.cs` (new)
  - `WasThere.Api/Models/UserClubNightAttendance.cs` (new)
  - `WasThere.Api/Models/ClubNight.cs` (updated)
  - `WasThere.Api/Data/ClubEventContext.cs` (updated)
  - `WasThere.Api/Controllers/ClubNightsController.cs` (updated)
  - `WasThere.Api/Migrations/20260108214407_AddUserAndAttendance.cs` (new)

- **Frontend (React/TypeScript)**:
  - `wasthere-web/src/types/index.ts` (updated)
  - `wasthere-web/src/services/api.ts` (updated)
  - `wasthere-web/src/components/ClubNightList.tsx` (updated)
  - `wasthere-web/src/components/Timeline.tsx` (updated)
  - `wasthere-web/src/App.css` (updated)

## Design Decisions

### Why "Was There" Not "Attended"?
The phrase "was there" is used throughout because it's the name of the site and creates a personal, conversational feel. It's more engaging than technical terms like "attended" or "checked in".

### Why Yellow Border?
- Yellow is a warm, positive color associated with highlighting and importance
- The border is thick enough (3px) to be immediately noticeable
- The subtle glow effect adds polish without being overwhelming
- The color maintains good contrast with the card background

### Why Single User for Now?
The requirement specified no authentication/user state is set up yet, so we use a single "admin" user. However, the database schema is designed to support multiple users, making the transition to multi-user support straightforward when authentication is implemented.

### Why Two Separate Filter Implementations?
- **Club Nights Page**: Has an existing filter panel with multiple filters, so "was there" is added as another option
- **Timeline Page**: Has no other filters, so a simple top-level checkbox is more appropriate and less cluttered

## Migration Path to Multi-User

When authentication is implemented:

1. **Backend Changes**:
   - Replace hardcoded "admin" user references with authenticated user ID
   - Update endpoints to use `userId` from JWT token
   - Add authorization checks

2. **Frontend Changes**:
   - Update API calls to include authentication token
   - Change field from `wasThereByAdmin` to `wasThere` or `wasThereByCurrentUser`
   - Update UI text if needed

3. **Database**:
   - No schema changes needed (already supports multiple users)
   - Keep admin user for backward compatibility

# Testing Auto-Populate Feature

This document describes how to test the AI-powered auto-populate feature for flyer images.

## Prerequisites

1. The application must be running (either via Docker or locally)
2. Google Gemini API key must be configured in `appsettings.json` or via environment variable
3. At least one Event and Venue should exist in the database (or will be created from flyer)

## Test Setup

### Option 1: Using Docker

```bash
# Ensure .env file has the API key (optional - already set in appsettings.json for testing)
cp .env.example .env

# Start the application
docker compose up -d

# View logs
docker compose logs -f wasthere-api
```

### Option 2: Local Development

```bash
# Start the backend
cd WasThere.Api
dotnet run

# In another terminal, start the frontend
cd wasthere-web
npm install
npm run dev
```

## Testing Steps

### 1. Upload a Flyer

1. Navigate to the application (http://localhost or http://localhost:5173)
2. Go to the "Flyers" section
3. Upload a club event flyer image with:
   - Select an existing Event (or create one first in Master Lists)
   - Select an existing Venue (or create one first in Master Lists)
   - Set the earliest club night date visible on the flyer
4. Click "Upload Flyer"

### 2. Auto-Populate from Flyer

1. Find the uploaded flyer in the flyers grid
2. Click the "Auto-populate" button
3. Wait for the AI analysis (button shows "Analyzing..." during processing)
4. A success message will appear showing:
   - Number of club nights created
   - Number of new events created
   - Number of new venues created
   - Number of new acts created

### 3. Verify Results

1. Go to "Club Nights" section to see the newly created club nights
2. Check "Master Lists" to see any newly created:
   - Events (if different from existing ones)
   - Venues (if different from existing ones)
   - Acts (all DJs/performers from the flyer)
3. Verify that:
   - Multiple dates from the flyer created separate club nights
   - Resident DJs are added to all club nights
   - Event and venue names match or are close to what's on the flyer

## Expected Behavior

### Success Case
- Green success message appears
- Statistics show counts of created entities
- Club nights appear in the Club Nights section
- Acts appear in the Master Lists section

### Error Cases

**No API Key Configured:**
- Error: "Google Gemini API key is not configured"
- Solution: Set `GoogleGemini:ApiKey` in appsettings.json or environment

**Flyer Not Found:**
- Error: "Flyer image file not found"
- Solution: Ensure the file was uploaded correctly

**AI Analysis Failed:**
- Error message from Gemini API
- Check logs for detailed error information

**No Information Extracted:**
- Error: "No club nights found in flyer"
- The flyer might not contain clear event information
- The AI might not be able to read the text

## Test Flyer Requirements

For best results, test flyers should have:
- Clear event name (recurring night name or specific event title)
- Venue name
- Date(s) - can be multiple dates
- List of performing DJs/artists
- Optional: "Residents" section (will be added to all dates)

## Known Limitations

1. **Timezone Handling**: Dates are treated as UTC. Future enhancement needed for proper timezone handling.
2. **Duplicate Acts**: If act names have different spellings, duplicates will be created (by design - will be reviewed in a future feature).
3. **Image Quality**: Poor quality or stylized flyers may not be read accurately by AI.
4. **Language**: Currently optimized for English text.

## Troubleshooting

### Button Does Nothing
- Check browser console for JavaScript errors
- Check API logs for backend errors
- Verify API is running and accessible

### Getting 404 Errors
- Ensure the API endpoint `/api/flyers/{id}/auto-populate` is accessible
- Check that CORS is properly configured

### AI Returns Empty Results
- The flyer image might be unclear
- Try with a different, clearer flyer image
- Check API logs to see the raw AI response

## Manual API Testing

You can also test the API directly:

```bash
# Get list of flyers
curl http://localhost:5000/api/flyers

# Auto-populate from a flyer (replace {id} with actual flyer ID)
curl -X POST http://localhost:5000/api/flyers/1/auto-populate

# Check created club nights
curl http://localhost:5000/api/clubnights
```

## Future Testing

Once sample flyers are added to the repository, automated tests can be added to verify:
- Correct extraction of event information
- Proper entity creation and matching
- Handling of edge cases (multiple dates, residents, etc.)

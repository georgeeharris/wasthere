# Testing the Flyer Conversion Logging

## Overview
This document provides instructions for testing the comprehensive flyer conversion logging feature.

## What Gets Logged

The logging system captures:
1. **Gemini API Inputs:**
   - Prompt text sent to Gemini
   - Image path and metadata (size, MIME type)

2. **Gemini API Outputs:**
   - Raw JSON response from Gemini
   - Success/failure status
   - Error messages if any

3. **Analysis Results:**
   - Parsed club nights with all details
   - Acts, venues, dates
   - Candidate years for date inference
   - Diagnostics information (steps, timing, metadata)

4. **User Inputs:**
   - Selected years for partial dates

5. **Database Operations:**
   - Creates for Events, Venues, Acts, ClubNights
   - Entity IDs for tracking

## Log File Format

Log files are created in `/app/logs/` with timestamp-based names:
- Format: `flyer-conversion-{YYYYMMDD-HHmmss-fff}.log`
- Example: `flyer-conversion-20260103-174500-123.log`

Each log contains:
- Session start/end timestamps
- All Gemini API interactions
- Database operations with entity details
- Error information with stack traces
- Summary with counts and duration

## Testing Steps

### Prerequisites
1. API must be running (docker-compose up or dotnet run)
2. Google Gemini API key must be configured

### Test 1: Upload a Flyer
```bash
# Upload a flyer image
curl -X POST http://localhost:5000/api/flyers/upload \
  -F "file=@path/to/flyer.jpg"
```

**Expected:**
- Log file created in `/app/logs/` (or `WasThere.Api/logs/` locally)
- Log contains:
  - Image path and size
  - Gemini prompt
  - Gemini raw response
  - Parsed club nights
  - Flyer creation in database

### Test 2: Complete Upload with Year Selection
```bash
# Get the flyer ID from Test 1 response
FLYER_ID=<id>

# Complete upload with year selection
curl -X POST http://localhost:5000/api/flyers/${FLYER_ID}/complete-upload \
  -H "Content-Type: application/json" \
  -d '{
    "selectedYears": [
      {"month": 5, "day": 27, "year": 2003}
    ]
  }'
```

**Expected:**
- New log file created
- Log contains:
  - Re-analysis of the flyer
  - User's year selections
  - Database operations for Events, Venues, Acts, ClubNights
  - Summary with entity counts

### Test 3: Verify Log Files
```bash
# If running with Docker
docker exec wasthere-api ls -la /app/logs/

# View a log file
docker exec wasthere-api cat /app/logs/flyer-conversion-*.log

# Or if running locally
ls -la WasThere.Api/logs/
cat WasThere.Api/logs/flyer-conversion-*.log
```

**Expected:**
- Log files with appropriate timestamps
- Complete, well-formatted logs with all sections
- No errors in log creation

### Test 4: Error Handling
Upload an invalid image or trigger an error to verify error logging works:

```bash
# Upload invalid file
curl -X POST http://localhost:5000/api/flyers/upload \
  -F "file=@invalid.txt"
```

**Expected:**
- Log file created even for failed operations
- Error messages and stack traces captured
- Log properly closed with failure status

## Accessing Logs on VPS

When deployed to VPS with Docker, logs are persisted in the `api_logs` volume:

```bash
# List log files
docker volume inspect wasthere_api_logs

# Find the mount point
docker volume inspect wasthere_api_logs | grep Mountpoint

# Access logs directly
sudo ls -la /var/lib/docker/volumes/wasthere_api_logs/_data/

# View a specific log
sudo cat /var/lib/docker/volumes/wasthere_api_logs/_data/flyer-conversion-*.log

# Or exec into container
docker exec -it wasthere-api bash
cd /app/logs
ls -la
cat flyer-conversion-*.log
```

## Log File Sections

A complete log file includes:

1. **Header:**
   - Log ID
   - Timestamp
   - Image path and filename

2. **Gemini API Request:**
   - Image metadata
   - Full prompt text

3. **Gemini API Response:**
   - Success status
   - Raw JSON response

4. **Analysis Result:**
   - Parsed club nights with details
   - Acts and venue information
   - Diagnostics (steps, timing, metadata)

5. **User Year Selection** (if applicable):
   - Selected years for partial dates

6. **Database Operations:**
   - Each CREATE operation with entity type and ID

7. **Summary:**
   - Success/failure status
   - Entity counts
   - Total duration
   - End timestamp

## Notes

- Logs are timestamped in UTC
- Log files are automatically closed when conversion completes
- Failed conversions also generate complete logs
- Logs are excluded from git via `.gitignore`
- Docker volume ensures logs persist across container restarts

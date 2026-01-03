# Flyer Conversion Logging Implementation

## Overview

This implementation adds comprehensive logging for every flyer conversion operation in the WasThere application. The logging system captures detailed information about:

- Gemini API inputs (prompts, image metadata)
- Gemini API outputs (raw responses, parsed data)
- User inputs (year selections)
- Database operations (creates, updates, deletes)
- Errors and diagnostics

## Architecture

### Components

1. **IFlyerConversionLogger** - Interface defining logging operations
2. **FlyerConversionLogger** - Service implementation that writes logs to disk
3. **Enhanced GoogleGeminiService** - Captures additional fields for logging
4. **Updated FlyersController** - Integrates logging into upload workflows

### Log Storage

Logs are stored in timestamped files:
- **Location:** `/app/logs/` (in Docker) or `WasThere.Api/logs/` (local)
- **Filename Format:** `flyer-conversion-{YYYYMMDD-HHmmss-fff}.log`
- **Example:** `flyer-conversion-20260103-174500-123.log`

### Docker Volume

The logs directory is mounted as a Docker volume (`api_logs`) in `docker-compose.yml`, ensuring:
- Logs persist across container restarts
- Logs can be accessed from the VPS terminal
- No data loss when updating the application

## Usage

### FlyersController Integration

The logging is integrated into two main endpoints:

#### 1. Upload Endpoint (`POST /api/flyers/upload`)

```csharp
// Start log
var logId = _conversionLogger.StartConversionLog(tempFilePath, file.FileName);

// Analyze flyer
var analysisResult = await _geminiService.AnalyzeFlyerImageAsync(tempFilePath);

// Log Gemini request/response
_conversionLogger.LogGeminiRequest(logId, ...);
_conversionLogger.LogGeminiResponse(logId, ...);
_conversionLogger.LogAnalysisResult(logId, analysisResult);

// Log database operations
_conversionLogger.LogDatabaseOperation(logId, "CREATE", "Flyer", fileName, flyerId);

// Complete log
_conversionLogger.CompleteConversionLog(logId, success, message);
```

#### 2. Complete Upload Endpoint (`POST /api/flyers/{id}/complete-upload`)

```csharp
// Start log for completion phase
var logId = _conversionLogger.StartConversionLog(imagePath, flyer.FileName);

// Re-analyze and log
var analysisResult = await _geminiService.AnalyzeFlyerImageAsync(imagePath);
_conversionLogger.LogAnalysisResult(logId, analysisResult);

// Log user selections
_conversionLogger.LogUserYearSelection(logId, request.SelectedYears);

// Process and log database operations
// ... creates for events, venues, acts, clubnights
_conversionLogger.LogDatabaseOperation(logId, "CREATE", entityType, name, id);

// Complete with summary
_conversionLogger.CompleteConversionLog(logId, success, summary, 
    eventsCreated, venuesCreated, actsCreated, clubNightsCreated);
```

## Log File Structure

Each log file follows this structure:

### 1. Header
```
=== FLYER CONVERSION LOG START ===
Log ID: 20260103-174500-123
Timestamp: 2026-01-03 17:45:00.123 UTC
Image Path: /app/uploads/temp/abc123.jpg
File Name: fabric-may-2003.jpg
```

### 2. Gemini API Request
```
--- GEMINI API REQUEST ---
Image Path: /app/uploads/temp/abc123.jpg
Image Size: 1234567 bytes (1205.63 KB)
MIME Type: image/jpeg

Prompt:
Analyze this club/event flyer image and extract...
[Full prompt text]
```

### 3. Gemini API Response
```
--- GEMINI API RESPONSE ---
Success: True

Raw Response:
{"clubNights": [{"eventName": "Fabric", ...}]}
```

### 4. Analysis Result
```
--- ANALYSIS RESULT ---
Success: True
Club Nights Found: 2

Club Night 1:
  Event Name: Fabric
  Venue Name: Fabric London
  Date: 2003-05-27
  Day of Week: Friday
  Acts Count: 3
  Acts:
    - Dave Clarke (Live Set: False)
    - Ben Sims (Live Set: False)

Diagnostics:
  Steps: 8
  - API Key Check: completed (0ms)
  - Read Image File: completed (5ms)
  ...
```

### 5. User Year Selection (if applicable)
```
--- USER YEAR SELECTION ---
Selected Years Count: 1
  5/27 -> Year: 2003
```

### 6. Database Operations
```
DB CREATE: Event - Fabric (ID: 123)
DB CREATE: Venue - Fabric London (ID: 456)
DB CREATE: ClubNight - Fabric at Fabric London on 2003-05-27 (ID: 789)
DB CREATE: Act - Dave Clarke (ID: 101)
DB CREATE: Act - Ben Sims (ID: 102)
```

### 7. Summary
```
--- CONVERSION SUMMARY ---
Success: True
Summary: Created 1 club nights, 1 events, 1 venues, 2 acts

Database Operations:
  Events Created: 1
  Venues Created: 1
  Acts Created: 2
  Club Nights Created: 1

Total Duration: 12.45 seconds
End Timestamp: 2026-01-03 17:45:12.573 UTC
=== FLYER CONVERSION LOG END ===
```

## Accessing Logs

### Local Development
```bash
# View logs
ls -la WasThere.Api/logs/
cat WasThere.Api/logs/flyer-conversion-*.log
```

### Docker (Local)
```bash
# List logs
docker exec wasthere-api ls -la /app/logs/

# View log
docker exec wasthere-api cat /app/logs/flyer-conversion-20260103-174500-123.log
```

### VPS Production
```bash
# Option 1: Via Docker exec
docker exec -it wasthere-api bash
cd /app/logs
ls -la
cat flyer-conversion-*.log

# Option 2: Via volume mount
sudo ls -la /var/lib/docker/volumes/wasthere_api_logs/_data/
sudo cat /var/lib/docker/volumes/wasthere_api_logs/_data/flyer-conversion-*.log

# Option 3: Copy to host
docker cp wasthere-api:/app/logs/. ./logs/
```

## Error Handling

The logging service includes robust error handling:

1. **Log Creation Failures:** Logged to ILogger, operation continues
2. **Write Failures:** Logged to ILogger, subsequent writes attempted
3. **Conversion Errors:** Captured in log with full stack traces
4. **API Failures:** Logged with error messages and diagnostics

Example error log:
```
!!! ERROR !!!
Error Message: Failed to parse Gemini response
Exception Type: JsonException
Exception Message: The JSON value could not be converted to...
Stack Trace:
  at System.Text.Json.JsonSerializer.Deserialize...
```

## Configuration

### Service Registration (Program.cs)
```csharp
builder.Services.AddScoped<IFlyerConversionLogger, FlyerConversionLogger>();
```

### Docker Volume (docker-compose.yml)
```yaml
services:
  api:
    volumes:
      - api_logs:/app/logs

volumes:
  api_logs:
```

### Git Ignore
Logs are excluded from version control via `.gitignore`:
```
[Ll]ogs/
```

## Benefits

1. **Debugging:** Full visibility into conversion process
2. **Auditing:** Complete record of all operations
3. **Monitoring:** Easy to identify issues and patterns
4. **Support:** Detailed information for troubleshooting user issues
5. **Analysis:** Data for improving conversion accuracy

## Future Enhancements

Potential improvements:
- Log rotation/cleanup for old logs
- Structured logging (JSON format)
- Log aggregation/search (ELK stack)
- Performance metrics collection
- User-specific log filtering
- Automated log analysis/alerts

## Testing

See `TESTING-FLYER-CONVERSION-LOGGING.md` for detailed testing instructions.

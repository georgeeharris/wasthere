# Error Diagnostics System

## Overview

The WasThere application now includes a comprehensive error diagnostics system designed to capture and display detailed information when flyer upload and analysis operations fail. This system helps developers and AI agents debug issues without needing IDE-based debugging.

## Architecture

### Backend Components

#### DiagnosticInfo Model (`WasThere.Api/Models/DiagnosticInfo.cs`)

The diagnostic information is structured as follows:

```csharp
public class DiagnosticInfo
{
    public List<DiagnosticStep> Steps { get; set; }        // Processing steps
    public Dictionary<string, string> Metadata { get; set; } // Key-value metadata
    public string? ErrorMessage { get; set; }              // Error summary
    public string? StackTrace { get; set; }                // Stack trace if available
}

public class DiagnosticStep
{
    public string Name { get; set; }           // Step name
    public string Status { get; set; }         // "started", "completed", "failed"
    public DateTime Timestamp { get; set; }    // When the step occurred
    public long? DurationMs { get; set; }      // How long the step took
    public string? Details { get; set; }       // Additional details
    public string? Error { get; set; }         // Error message if failed
}
```

#### GoogleGeminiService Enhancements

The service now tracks each step of the AI analysis process:

1. **API Key Check** - Verifies configuration
2. **Read Image File** - Loads the image from disk
3. **Prepare Gemini Request** - Constructs the API request
4. **Call Gemini API** - Makes the actual API call (tracks duration)
5. **Validate API Response** - Checks response structure
6. **Extract Text from Response** - Extracts text from API response
7. **Clean Response Text** - Removes markdown formatting
8. **Parse JSON Response** - Deserializes the JSON

Each step records:
- Start timestamp
- Completion status (started/completed/failed)
- Duration in milliseconds
- Additional details (e.g., file size, response length)
- Error information if the step fails

#### Metadata Captured

The system captures comprehensive metadata:
- `ImagePath` - Path to the uploaded file
- `Timestamp` - When the operation started
- `APIKeyConfigured` - Whether the API key is configured
- `ImageSizeBytes` - Size of the uploaded image
- `MimeType` - Image MIME type
- `GeminiModel` - AI model used
- `GeminiResponseReceived` - Whether AI responded
- `ResponseCandidatesCount` - Number of response candidates
- `ResponseTextLength` - Length of response text
- `ResponsePreview` - First 500 characters of response
- `ClubNightsFound` - Number of club nights extracted
- `TotalDurationMs` - Total operation duration

### Frontend Components

#### ErrorDiagnostics Component (`wasthere-web/src/components/ErrorDiagnostics.tsx`)

A modal popup that displays error information with the following features:

**Features:**
- Clear error message display
- Expandable diagnostics section
- Copy to clipboard functionality
- Formatted step-by-step processing timeline
- Metadata table view
- Stack trace display (when available)
- Browser information inclusion

**User Interface:**
- ❌ Error header with close button
- Show/Hide diagnostics toggle
- Copy Diagnostics button (with confirmation)
- Color-coded step status:
  - ✓ Green for completed steps
  - ✗ Red for failed steps
  - ○ Orange for started but not completed
- Helpful guidance on what to do next

#### Updated FlyerList Component

The component now:
- Captures `DiagnosticInfo` from API responses
- Displays `ErrorDiagnostics` modal when errors occur
- Passes diagnostics to the modal component
- Clears diagnostics when modal is closed

## How It Works

### Successful Upload Flow

1. User selects and uploads a flyer
2. Backend processes the file and calls AI
3. Diagnostics are captured at each step (all "completed")
4. Success message is shown to user
5. Diagnostics are available but not displayed (since no error)

### Failed Upload Flow

1. User selects and uploads a flyer
2. Backend encounters an error (e.g., API timeout, parsing error)
3. Error step is marked as "failed" with error details
4. All steps up to the failure point are recorded
5. API returns error response with diagnostics
6. Frontend displays ErrorDiagnostics modal
7. User can view detailed information and copy diagnostics

### Example Diagnostic Output

When user clicks "Copy Diagnostics", they get:

```
Error Report
============

Error Message: Failed to upload flyer

Detailed Error: Request timed out. The AI analysis is taking longer than expected.

Metadata:
---------
  ImagePath: /uploads/temp/abc123.jpg
  Timestamp: 2025-12-30T23:00:00.000Z
  APIKeyConfigured: true
  ImageSizeBytes: 2458376
  MimeType: image/jpeg
  GeminiModel: gemini-2.5-flash
  TotalDurationMs: 180234

Processing Steps:
-----------------

[COMPLETED] API Key Check
  Time: 2025-12-30T23:00:00.123Z
  Duration: 0ms

[COMPLETED] Read Image File
  Time: 2025-12-30T23:00:00.234Z
  Duration: 45ms
  Details: Read 2458376 bytes

[COMPLETED] Prepare Gemini Request
  Time: 2025-12-30T23:00:00.345Z
  Duration: 0ms

[STARTED] Call Gemini API
  Time: 2025-12-30T23:00:00.456Z
  Duration: 180000ms
  Error: Request timeout

Browser Information:
--------------------
User Agent: Mozilla/5.0...
Timestamp: 2025-12-30T23:03:00.789Z
```

## Benefits

### For Development
- **No IDE Required**: All debugging information available through UI
- **Complete Timeline**: See exactly where failures occur
- **Performance Metrics**: Track operation duration
- **AI Response Tracking**: Know if AI returned results

### For Users
- **Transparency**: Users see what went wrong
- **Easy Reporting**: One-click copy of diagnostic information
- **Clear Guidance**: Help text explains next steps

### For AI Agents
- **Structured Information**: Standardized diagnostic format
- **Complete Context**: All relevant details in one place
- **Easy to Parse**: Clear step-by-step breakdown
- **Metadata Access**: Key-value pairs for quick analysis

## Common Issues and Diagnostics

### Issue: "Failed to fetch" after ~20 seconds

**Diagnostic Indicators:**
- "Call Gemini API" step shows "started" but not "completed"
- Duration is close to 20,000ms (20 seconds)
- No "GeminiResponseReceived" metadata

**Likely Cause:** Browser timeout before AI completes processing

**Solution:** Backend timeout increased to 5 minutes, but very large images may still timeout

### Issue: "No club nights found in flyer"

**Diagnostic Indicators:**
- All steps complete successfully
- "ClubNightsFound: 0" in metadata
- "ResponsePreview" shows AI response

**Likely Cause:** AI couldn't extract event information from image

**Solution:** Image may be too low quality, wrong content, or AI misinterpreted

### Issue: "Failed to parse AI response as JSON"

**Diagnostic Indicators:**
- "Parse JSON Response" step fails
- "ResponsePreview" shows malformed JSON or unexpected format

**Likely Cause:** AI returned text that isn't valid JSON

**Solution:** May need to adjust AI prompt or handle AI responses better

### Issue: "Error calling Gemini API"

**Diagnostic Indicators:**
- "Call Gemini API" step fails immediately
- Error message in step details
- May include network or authentication errors

**Likely Cause:** API key issue, network problem, or API service down

**Solution:** Check API key configuration, network connectivity, and API service status

## Configuration

No additional configuration is required. The diagnostic system is automatically enabled for all flyer upload and analysis operations.

## API Response Format

### Success Response (with diagnostics)
```json
{
  "success": true,
  "message": "Flyer uploaded successfully",
  "flyer": { ... },
  "autoPopulateResult": { ... },
  "diagnostics": {
    "steps": [ ... ],
    "metadata": { ... }
  }
}
```

### Error Response (with diagnostics)
```json
{
  "success": false,
  "message": "Failed to analyze flyer",
  "diagnostics": {
    "steps": [ ... ],
    "metadata": { ... },
    "errorMessage": "Detailed error message",
    "stackTrace": "Stack trace if available"
  }
}
```

## Future Enhancements

Potential improvements to the diagnostic system:

1. **Persistence**: Save diagnostics to database for later analysis
2. **Analytics**: Track common failure patterns
3. **Alerts**: Notify developers of recurring issues
4. **Download**: Export diagnostics as JSON or text file
5. **Progress Indicators**: Show real-time progress during long operations
6. **Retry Logic**: Automatic retry with exponential backoff
7. **Performance Baselines**: Compare against expected durations
8. **User Feedback**: Allow users to add context to error reports

## Testing

To test the diagnostic system:

1. **Successful Upload**: Upload a valid flyer and verify no error appears
2. **Failed Upload**: Try uploading an invalid file type
3. **API Timeout**: Upload a very large image (>5MB) on slow connection
4. **Copy Diagnostics**: Click the copy button and paste to verify format
5. **Modal Interaction**: Open/close modal, expand/collapse sections

## Support

When reporting issues, always include the diagnostic output by:
1. Clicking "Copy Diagnostics" button
2. Pasting the output in your bug report or support request
3. The diagnostic information will help developers quickly identify the root cause

## Related Files

- `WasThere.Api/Models/DiagnosticInfo.cs` - Diagnostic data models
- `WasThere.Api/Services/GoogleGeminiService.cs` - Service with diagnostic capture
- `WasThere.Api/Services/IGoogleGeminiService.cs` - Service interface
- `WasThere.Api/Controllers/FlyersController.cs` - Controller passing diagnostics
- `wasthere-web/src/components/ErrorDiagnostics.tsx` - Diagnostic modal component
- `wasthere-web/src/components/FlyerList.tsx` - Component using diagnostics
- `wasthere-web/src/types/index.ts` - TypeScript type definitions
- `wasthere-web/src/services/api.ts` - API client with diagnostic types
- `wasthere-web/src/App.css` - Modal and diagnostic styling

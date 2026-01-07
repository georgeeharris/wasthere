# Testing Diagnostic Log Download Feature

This document describes how to manually test the new diagnostic log download feature for flyer upload errors.

## Overview

When a flyer upload fails, the error popup now includes a "Download Full Log" button that allows users to download the complete diagnostic log file. This enables users to share detailed error information without needing to log into the server.

## Prerequisites

1. Backend API running (WasThere.Api)
2. Frontend web app running (wasthere-web)
3. Google Gemini API key configured (or intentionally not configured to trigger errors)

## Test Scenarios

### Scenario 1: Upload Error with Missing API Key

**Steps:**
1. Ensure the Google Gemini API key is NOT configured in the backend
2. Navigate to the Contribute page
3. Select a flyer image and upload it
4. Wait for the upload to fail

**Expected Result:**
- Error popup appears with message about API key not configured
- "Copy Diagnostics" button is visible
- "Download Full Log" button is visible
- Click "Download Full Log" button
- A file named `flyer-diagnostic-{logId}.log` should be downloaded
- Open the log file and verify it contains:
  - Log start timestamp
  - Image path and filename
  - API key check failure details
  - Error message about missing API key

### Scenario 2: Upload Error with Invalid Image

**Steps:**
1. Ensure the Google Gemini API key IS configured
2. Navigate to the Contribute page
3. Create or select a corrupted/invalid image file
4. Upload the file

**Expected Result:**
- Error popup appears with appropriate error message
- "Download Full Log" button is visible (if logId is available)
- Click "Download Full Log" to download the diagnostic log
- Log file should contain detailed error information

### Scenario 3: Network/Timeout Error

**Steps:**
1. Configure the API but simulate a slow network or timeout
2. Upload a flyer image
3. Wait for timeout or error

**Expected Result:**
- Error popup appears
- Download button should be available if a log was created
- Downloaded log should show the error details

## Verification Checklist

- [ ] Download button appears in error popup when logId is available
- [ ] Download button shows loading state ("⏳ Downloading...") while downloading
- [ ] Downloaded file has correct naming format: `flyer-diagnostic-{logId}.log`
- [ ] Downloaded log file contains complete diagnostic information:
  - Log ID and timestamps
  - Image path and metadata
  - Gemini API request details (if applicable)
  - Gemini API response (if applicable)
  - Error messages and stack traces
  - Processing steps with status
- [ ] Download works on different browsers (Chrome, Firefox, Safari, Edge)
- [ ] Mobile devices can download the log file

## API Endpoint

The new API endpoint is:
```
GET /api/flyers/diagnostic-log/{logId}
```

**Security Features:**
- Validates logId format to prevent path traversal attacks
- Returns 400 Bad Request for invalid logId format
- Returns 404 Not Found if log file doesn't exist
- Returns 500 Internal Server Error if file read fails

## Integration with Agents

The downloaded log file can be sent to AI agents for automated error analysis and fixes, which was the primary goal of this feature. The log format is human-readable plain text.

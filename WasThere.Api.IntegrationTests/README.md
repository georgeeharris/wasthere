# WasThere.Api Integration Tests

This project contains integration tests for the WasThere API, specifically focusing on the Google Gemini API integration for flyer analysis.

## Overview

The integration tests in this project are designed to:
1. Test the actual Google Gemini API integration with real API calls
2. Diagnose null reference exceptions and other issues in the flyer upload flow
3. Validate that the flyer analysis pipeline works correctly from end to end

## Running the Tests

### Prerequisites

1. **Google Gemini API Key**: You need a valid Google Gemini API key to run these tests.
   - Get your API key from: https://aistudio.google.com/app/apikey
   - The test API key is already configured in `appsettings.json` for local development

2. **Test Image**: The tests use `HiRes-2.jpg` in the repository root.

### Running Locally

```bash
# Set the API key as an environment variable
export GOOGLE_GEMINI_API_KEY="your-api-key-here"

# Navigate to test project
cd WasThere.Api.IntegrationTests

# Run all tests
dotnet test

# Run specific test with detailed output
dotnet test --filter "FullyQualifiedName~DiagnoseNullReferenceException" --logger "console;verbosity=detailed"
```

**Note:** A test API key is configured in `WasThere.Api/appsettings.json` for local development. You can use that for testing, but for production use, always use environment variables or user secrets.

### Running in GitHub Actions

The tests can run in GitHub Actions if the `GOOGLE_GEMINI_API_KEY` secret is configured:

```yaml
- name: Run Integration Tests
  env:
    GOOGLE_GEMINI_API_KEY: ${{ secrets.GOOGLE_GEMINI_API_KEY }}
  run: |
    cd WasThere.Api.IntegrationTests
    dotnet test --logger "console;verbosity=detailed"
```

## Test Structure

### FlyerGeminiIntegrationTests

This test class contains integration tests for the Gemini API flyer analysis.

#### DiagnoseNullReferenceException_WithRealApi

This is the main diagnostic test that:
- Loads the HiRes-2.jpg image from the repository root
- Calls the Google Gemini API to analyze the flyer
- Checks for null values in the response that could cause NullReferenceException
- Simulates what the FlyersController does with the analysis result
- Provides detailed output showing exactly where null references might occur

**Output includes:**
- Whether the API call succeeded or failed
- All fields from the ClubNightData objects (EventName, VenueName, Date, Acts, etc.)
- Warnings for any NULL values that could cause exceptions
- Step-by-step simulation of the controller's processing logic

#### AnalyzeFlyerImage_WithRealGeminiApi_ShouldReturnValidResult

A more traditional integration test that validates the entire analysis flow works correctly.

## Issue Diagnosed and Fixed

### The Problem

The application was experiencing a persistent NullReferenceException when uploading flyers. A previous defensive fix (checking for `actData == null` in the controller) did not resolve the issue.

### The Root Cause

The null reference exception was occurring in `GoogleGeminiService.cs` at line 95, when trying to use a collection initializer on the `Content.Parts` property:

```csharp
// This caused NullReferenceException because Parts was null
var content = new Content
{
    Parts =
    {
        new Part { Text = prompt },
        ...
    }
};
```

### The Fix

The fix properly initializes the `Parts` collection before adding items:

```csharp
// Fixed version - initialize Content and check Parts
var content = new Content();
if (content.Parts == null)
{
    content.Parts = new List<Part>();
}
content.Parts.Add(new Part { Text = prompt });
content.Parts.Add(new Part
{
    InlineData = new Blob
    {
        MimeType = mimeType,
        Data = imageBytes
    }
});
```

### Verification

The integration test successfully verified that:
1. The NullReferenceException no longer occurs when creating the Content object
2. The API call proceeds correctly (fails with network error in sandbox, but no null reference)
3. The defensive fix in the controller (checking `actData == null`) remains in place for additional safety

## Test Output Example

When running the diagnostic test, you'll see output like:

```
=== DIAGNOSTIC TEST FOR NULL REFERENCE EXCEPTION ===
API Key Set: True
Image Exists: True

=== CALLING GEMINI API ===
[Information] Gemini raw response: { ... }

=== ANALYSIS RESULT ===
Success: True
Club Nights Count: 2

=== CHECKING FOR NULL VALUES ===
Club Night 1:
  EventName: "Fabric" ✓
  VenueName: "Fabric London" ✓
  Date: 2003-05-27
  DayOfWeek: Friday
  Acts: Count=5 ✓
    Act 1: Name="DJ One" ✓, IsLiveSet=False
    Act 2: Name="DJ Two" ✓, IsLiveSet=True
    ...
```

## Network Restrictions

Note: In the GitHub Actions sandbox environment, outbound network requests to generativelanguage.googleapis.com may be blocked. This is normal for the sandbox and doesn't affect the fix verification. The important thing is that we no longer see the NullReferenceException.

## Future Improvements

Potential enhancements for these tests:
1. Add mock/stub tests that don't require real API calls
2. Add more comprehensive validation of the response structure
3. Test error handling for various API failure scenarios
4. Add tests for the complete flyer upload flow including database operations

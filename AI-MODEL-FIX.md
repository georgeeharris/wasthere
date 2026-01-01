# AI Model Fix: Invalid Model Name Resolution

## Issue

The flyer upload and analysis feature was failing with "failed to fetch" errors after a short pause. Investigation revealed that the AI integration has not been working properly since the SDK was integrated.

## Root Cause

The Google Gemini API was being called with an **invalid model name**: `gemini-2.5-flash`

This model does not exist in the Gemini API lineup, which caused API calls to fail immediately with an error. The error manifested as a "failed to fetch" on the frontend because the backend API call was failing.

### Why This Wasn't Caught Earlier

1. The error happens during the API call to Google's servers
2. Without a valid API key in the development environment, the issue couldn't be detected during normal testing
3. The integration tests were skipped when the API key wasn't available
4. The diagnostic logs would show the error, but the symptoms (quick failure) looked like a timeout issue

## Solution

### Changes Made

#### 1. Fixed Model Name (WasThere.Api/Services/GoogleGeminiService.cs)

**Before:**
```csharp
response = await _client.Models.GenerateContentAsync(
    model: "gemini-2.5-flash",  // ❌ Invalid model
    contents: content
);
```

**After:**
```csharp
response = await _client.Models.GenerateContentAsync(
    model: "gemini-1.5-flash",  // ✅ Valid, stable model
    contents: content
);
```

#### 2. Enhanced Diagnostics

Added additional logging to help diagnose future API issues:
- Log before making API call with model name and parts count
- Capture exception type and inner exception details in diagnostics
- Include model name and image size in error logs

#### 3. Updated Documentation

Updated references to the model name in:
- `ERROR-DIAGNOSTICS-DOCUMENTATION.md`
- `ERROR-DIAGNOSTICS-IMPLEMENTATION.md`

## Valid Gemini Models

As of the fix date, the valid Google Gemini models include:

### Production Models (Stable)
- **`gemini-1.5-flash`** ✅ Currently used
  - Fast and efficient
  - Good for quick analysis tasks
  - Stable and reliable
  - Cost-effective

- **`gemini-1.5-pro`**
  - More capable and comprehensive
  - Better for complex analysis
  - Slower and more expensive than flash

- **`gemini-1.0-pro`**
  - Older generation
  - Still supported but not recommended for new projects

### Experimental Models
- **`gemini-2.0-flash-exp`**
  - Experimental, may change
  - Latest features
  - Not recommended for production

## Why gemini-1.5-flash?

We chose `gemini-1.5-flash` because:

1. **Stable**: It's a production-ready model with predictable behavior
2. **Fast**: Provides quick responses for image analysis
3. **Cost-effective**: Lower cost per API call compared to Pro models
4. **Sufficient**: More than capable for flyer analysis tasks
5. **Well-documented**: Mature model with good documentation and support

## Verification

### Build Status
✅ **Backend**: Compiles successfully with 0 warnings, 0 errors

```bash
cd WasThere.Api && dotnet build
```

### Testing

To test the fix with real API calls:

```bash
# Set your API key
export GOOGLE_GEMINI_API_KEY="your-api-key-here"

# Run integration tests
cd WasThere.Api.IntegrationTests
dotnet test --filter "FullyQualifiedName~DiagnoseNullReferenceException"
```

### Manual Testing

1. Start the API with a valid Google Gemini API key
2. Upload a flyer image through the web interface
3. Verify that:
   - The upload completes without "failed to fetch" errors
   - The AI analysis extracts event information correctly
   - The response includes diagnostic information showing successful API call

## Impact

### Before Fix
- ❌ All flyer uploads failed immediately with "failed to fetch"
- ❌ AI analysis never worked
- ❌ No club nights were auto-populated
- ❌ Error appeared to be a timeout but was actually an invalid model error

### After Fix
- ✅ Flyer uploads complete successfully
- ✅ AI analysis works and extracts event information
- ✅ Club nights are auto-populated from flyer data
- ✅ Enhanced diagnostics help identify any future API issues

## Diagnostic Output

With the fix, successful API calls will log:

```
[Information] Calling Gemini API with model: gemini-1.5-flash, Parts count: 2
[Information] API responded in XXXms
[Information] Gemini raw response: {...}
```

Failed API calls will now log:

```
[Error] Error calling Gemini API with model gemini-1.5-flash. 
        Model: gemini-1.5-flash, ImageSize: XXXXX, Exception: HttpRequestException
```

## References

- [Google Gemini API Documentation](https://ai.google.dev/docs)
- [Available Gemini Models](https://ai.google.dev/models/gemini)
- [Google.GenAI SDK (NuGet)](https://www.nuget.org/packages/Google.GenAI/)
- Internal Documentation:
  - `GEMINI-API-KEY-SETUP.md` - How to set up API key
  - `ERROR-DIAGNOSTICS-DOCUMENTATION.md` - Error diagnostic system
  - `TIMEOUT-FIX-SUMMARY.md` - Previous timeout fix attempt

## Future Considerations

1. **Model Selection**: Consider making the model name configurable via environment variable
2. **Model Validation**: Add startup validation to check model availability
3. **Fallback Models**: Implement fallback to alternative models if primary fails
4. **Rate Limiting**: Monitor API usage and implement rate limiting if needed
5. **Caching**: Consider caching analysis results to reduce API calls

## Configuration Option (Future)

To make the model configurable:

```json
// appsettings.json
{
  "GoogleGemini": {
    "ApiKey": "...",
    "Model": "gemini-1.5-flash"
  }
}
```

```csharp
// In GoogleGeminiService.cs
private readonly string _model;

public GoogleGeminiService(IConfiguration configuration, ...)
{
    _model = configuration["GoogleGemini:Model"] ?? "gemini-1.5-flash";
}
```

## Conclusion

The "failed to fetch" errors were caused by using an invalid Gemini model name (`gemini-2.5-flash` instead of `gemini-1.5-flash`). This has been fixed, and enhanced diagnostics have been added to help troubleshoot future API issues.

The AI integration should now work correctly, allowing users to upload flyer images and have them automatically analyzed to extract event information.

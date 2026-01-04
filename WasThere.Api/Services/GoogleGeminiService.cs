using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.GenAI;
using Google.GenAI.Types;
using WasThere.Api.Models;

namespace WasThere.Api.Services;

public class GoogleGeminiService : IGoogleGeminiService
{
    private readonly Client? _client;
    private readonly ILogger<GoogleGeminiService> _logger;
    private readonly string _apiKey;
    
    // Gemini model to use for flyer analysis
    // Using gemini-2.5-flash: stable, fast, and cost-effective
    // Note: gemini-1.5-flash is deprecated and no longer available in v1beta API
    private const string GeminiModel = "gemini-2.5-flash";

    public GoogleGeminiService(IConfiguration configuration, ILogger<GoogleGeminiService> logger)
    {
        _logger = logger;
        _apiKey = configuration["GoogleGemini:ApiKey"] ?? string.Empty;
        
        // Initialize the Google GenAI client
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _client = new Client(apiKey: _apiKey);
        }
    }

    public async Task<FlyerAnalysisResult> AnalyzeFlyerImageAsync(string imagePath)
    {
        var diagnostics = new DiagnosticInfo();
        var overallStopwatch = Stopwatch.StartNew();
        
        try
        {
            diagnostics.Metadata["ImagePath"] = imagePath;
            diagnostics.Metadata["Timestamp"] = DateTime.UtcNow.ToString("o");
            
            // Check if API key is configured
            var checkKeyStep = new DiagnosticStep 
            { 
                Name = "API Key Check",
                Timestamp = DateTime.UtcNow,
                Status = "started"
            };
            diagnostics.Steps.Add(checkKeyStep);
            
            if (string.IsNullOrEmpty(_apiKey) || _client == null)
            {
                checkKeyStep.Status = "failed";
                checkKeyStep.Error = "API key not configured";
                
                _logger.LogError("Google Gemini API key is not configured");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Google Gemini API key is not configured. Please set GoogleGemini:ApiKey in configuration.",
                    Diagnostics = diagnostics
                };
            }
            
            checkKeyStep.Status = "completed";
            diagnostics.Metadata["APIKeyConfigured"] = "true";

            // Read image bytes
            var readFileStep = new DiagnosticStep
            {
                Name = "Read Image File",
                Timestamp = DateTime.UtcNow,
                Status = "started"
            };
            diagnostics.Steps.Add(readFileStep);
            
            var readStopwatch = Stopwatch.StartNew();
            var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
            readStopwatch.Stop();
            
            readFileStep.Status = "completed";
            readFileStep.DurationMs = readStopwatch.ElapsedMilliseconds;
            readFileStep.Details = $"Read {imageBytes.Length} bytes";
            diagnostics.Metadata["ImageSizeBytes"] = imageBytes.Length.ToString();
            
            // Determine MIME type from file extension
            var mimeType = GetMimeType(imagePath);
            diagnostics.Metadata["MimeType"] = mimeType;

            // Construct the prompt for analyzing the flyer
            var prompt = @"Analyze this club/event flyer image and extract the following information in JSON format.

If the image contains MULTIPLE SEPARATE FLYERS (e.g., front and back of different flyers), return an array of flyer objects.
If the image contains a SINGLE FLYER (even with multiple dates), return an array with one flyer object.

{
  ""flyers"": [
    {
      ""clubNights"": [
        {
          ""eventName"": ""The event name (e.g., 'Fabric', 'Ministry of Sound') or null if unclear"",
          ""venueName"": ""The venue name"",
          ""date"": ""The FULL date in ISO format (YYYY-MM-DD) if year is visible, otherwise null"",
          ""dayOfWeek"": ""Day of week if visible (e.g., 'Friday', 'Saturday') - IMPORTANT for date inference"",
          ""month"": numeric month (1-12) if visible,
          ""day"": numeric day of month (1-31) if visible,
          ""acts"": [
            {
              ""name"": ""Act name without performance type indicators"",
              ""isLiveSet"": true or false
            }
          ]
        }
      ]
    }
  ]
}

Important instructions:
1. **MULTIPLE FLYERS**: If the image shows multiple distinct flyers (different events/venues), create a separate flyer object for each one in the array
2. **SINGLE FLYER**: If it's a single flyer (even with multiple dates), return one flyer object with all club nights in its clubNights array
3. Extract ALL dates shown on each flyer - create a separate club night entry for each date within that flyer
4. For 'Residents' or 'Resident DJs', add them as acts on EVERY club night date for that flyer
5. **EVENT NAME EXTRACTION - CRITICAL**:
   - ONLY extract a specific, identifiable event name if it is clearly visible (e.g., 'Fabric', 'Ministry of Sound', 'Bugged Out')
   - Set eventName to null or empty string if:
     * No clear event/club night name is visible
     * Only generic terms appear (e.g., 'Club Night', 'DJ Night', 'Party', etc.)
     * You are unsure what the actual event name is
   - DO NOT make up or infer event names
   - DO NOT use generic placeholder names like 'Club Night'
   - When in doubt, set eventName to null - the user will be prompted to select the correct event
6. Include all performing artists/DJs listed
7. **CRITICAL**: If a listing combines multiple acts with separators like '&', 'and', 'B2B', 'b2b', 'vs', 'VS', or similar, split them into separate act entries. For example:
   - 'DJ A & DJ B' should become two acts: 'DJ A' and 'DJ B'
   - 'Artist X B2B Artist Y' should become two acts: 'Artist X' and 'Artist Y'
   - 'DJ 1 vs DJ 2' should become two acts: 'DJ 1' and 'DJ 2'
8. For each act, determine if it's a live set:
   - Set isLiveSet to true if the act has indicators like '(live)', '(live set)', '(live PA)', 'live', or similar
   - Set isLiveSet to false if it has '(DJ set)', '(DJ)', or no indicator (default to DJ set)
   - Remove the performance type indicators from the name (e.g., 'Dave Clarke (live)' should be just 'Dave Clarke')
9. **DATE EXTRACTION**:
   - If the full date with year is visible (e.g., '27 May 2003'), provide it in the 'date' field as 'YYYY-MM-DD'
   - If only partial date is visible (e.g., 'Friday 27th May' without year), set 'date' to null and provide:
     * 'dayOfWeek': the day name if visible (very important for inferring year)
     * 'month': the numeric month (1-12)
     * 'day': the numeric day of month (1-31)
10. Only extract information that is clearly visible in the flyer
11. Return ONLY valid JSON, no additional text or markdown

Please analyze the flyer and return the JSON:";

            // Create the request content with text and image using the SDK
            var prepareRequestStep = new DiagnosticStep
            {
                Name = "Prepare Gemini Request",
                Timestamp = DateTime.UtcNow,
                Status = "started"
            };
            diagnostics.Steps.Add(prepareRequestStep);
            
            var content = new Content();
            content.Parts ??= new List<Part>();
            content.Parts.Add(new Part { Text = prompt });
            content.Parts.Add(new Part
            {
                InlineData = new Blob
                {
                    MimeType = mimeType,
                    Data = imageBytes
                }
            });
            
            prepareRequestStep.Status = "completed";
            diagnostics.Metadata["GeminiModel"] = GeminiModel;

            // Call the Gemini API using the SDK
            var apiCallStep = new DiagnosticStep
            {
                Name = "Call Gemini API",
                Timestamp = DateTime.UtcNow,
                Status = "started"
            };
            diagnostics.Steps.Add(apiCallStep);
            
            GenerateContentResponse response;
            var apiStopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Calling Gemini API with model: {Model}, Parts count: {PartsCount}", GeminiModel, content.Parts?.Count ?? 0);
            
            try
            {
                // Using gemini-2.5-flash: stable, fast, and cost-effective model
                // Note: gemini-1.5-flash is deprecated and no longer available in v1beta API
                // Valid alternatives: gemini-2.5-pro (more capable), gemini-2.5-flash-lite (faster)
                response = await _client.Models.GenerateContentAsync(
                    model: GeminiModel,
                    contents: content
                );
                apiStopwatch.Stop();
                
                apiCallStep.Status = "completed";
                apiCallStep.DurationMs = apiStopwatch.ElapsedMilliseconds;
                apiCallStep.Details = $"API responded in {apiCallStep.DurationMs}ms";
                diagnostics.Metadata["GeminiResponseReceived"] = "true";
            }
            catch (Exception ex)
            {
                apiStopwatch.Stop();
                apiCallStep.Status = "failed";
                apiCallStep.DurationMs = apiStopwatch.ElapsedMilliseconds;
                apiCallStep.Error = ex.Message;
                diagnostics.ErrorMessage = $"Error calling Gemini API: {ex.Message}";
                diagnostics.StackTrace = ex.StackTrace;
                diagnostics.Metadata["ExceptionType"] = ex.GetType().FullName ?? ex.GetType().Name;
                
                if (ex.InnerException != null)
                {
                    diagnostics.Metadata["InnerException"] = ex.InnerException.Message;
                    diagnostics.Metadata["InnerExceptionType"] = ex.InnerException.GetType().FullName ?? ex.InnerException.GetType().Name;
                }
                
                _logger.LogError(ex, "Error calling Gemini API with model {Model}. ImageSize: {ImageSize}, Exception: {ExceptionType}", 
                    GeminiModel, 
                    imageBytes.Length, 
                    ex.GetType().Name);
                    
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Error calling Gemini API: {ex.Message}",
                    Diagnostics = diagnostics
                };
            }

            // Check if we have a valid response
            var validateResponseStep = new DiagnosticStep
            {
                Name = "Validate API Response",
                Timestamp = DateTime.UtcNow,
                Status = "started"
            };
            diagnostics.Steps.Add(validateResponseStep);
            
            if (response?.Candidates == null || response.Candidates.Count == 0)
            {
                validateResponseStep.Status = "failed";
                validateResponseStep.Error = "No candidates in response";
                diagnostics.Metadata["ResponseHasCandidates"] = "false";
                
                _logger.LogWarning("No candidates in Gemini response");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "No analysis results returned from AI",
                    Diagnostics = diagnostics
                };
            }
            
            validateResponseStep.Status = "completed";
            diagnostics.Metadata["ResponseCandidatesCount"] = response.Candidates.Count.ToString();

            // Extract text from response
            var extractTextStep = new DiagnosticStep
            {
                Name = "Extract Text from Response",
                Timestamp = DateTime.UtcNow,
                Status = "started"
            };
            diagnostics.Steps.Add(extractTextStep);
            
            var textResponse = response.Candidates[0]?.Content?.Parts?.FirstOrDefault()?.Text;
            
            if (string.IsNullOrEmpty(textResponse))
            {
                extractTextStep.Status = "failed";
                extractTextStep.Error = "Empty text response";
                diagnostics.Metadata["ResponseTextEmpty"] = "true";
                
                _logger.LogWarning("Empty text response from Gemini");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Empty response from AI",
                    Diagnostics = diagnostics
                };
            }
            
            extractTextStep.Status = "completed";
            extractTextStep.Details = $"Extracted {textResponse.Length} characters";
            diagnostics.Metadata["ResponseTextLength"] = textResponse.Length.ToString();

            _logger.LogInformation("Gemini raw response: {Response}", textResponse);
            
            // Store first 500 chars of response for diagnostics
            diagnostics.Metadata["ResponsePreview"] = textResponse.Length > 500 
                ? textResponse.Substring(0, 500) + "..." 
                : textResponse;

            // Clean up the response - sometimes AI returns markdown code blocks
            var cleanResponseStep = new DiagnosticStep
            {
                Name = "Clean Response Text",
                Timestamp = DateTime.UtcNow,
                Status = "started"
            };
            diagnostics.Steps.Add(cleanResponseStep);
            
            textResponse = textResponse.Trim();
            if (textResponse.StartsWith("```json"))
            {
                textResponse = textResponse.Substring(7);
            }
            if (textResponse.StartsWith("```"))
            {
                textResponse = textResponse.Substring(3);
            }
            if (textResponse.EndsWith("```"))
            {
                textResponse = textResponse.Substring(0, textResponse.Length - 3);
            }
            textResponse = textResponse.Trim();
            
            cleanResponseStep.Status = "completed";

            // Parse the JSON response
            var parseJsonStep = new DiagnosticStep
            {
                Name = "Parse JSON Response",
                Timestamp = DateTime.UtcNow,
                Status = "started"
            };
            diagnostics.Steps.Add(parseJsonStep);
            
            FlyerAnalysisData? analysisData;
            try
            {
                analysisData = JsonSerializer.Deserialize<FlyerAnalysisData>(textResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                parseJsonStep.Status = "completed";
            }
            catch (Exception ex)
            {
                parseJsonStep.Status = "failed";
                parseJsonStep.Error = ex.Message;
                diagnostics.ErrorMessage = $"Failed to parse JSON: {ex.Message}";
                
                _logger.LogError(ex, "Failed to parse Gemini response as JSON");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to parse AI response as JSON: {ex.Message}",
                    Diagnostics = diagnostics
                };
            }

            if (analysisData?.Flyers == null || analysisData.Flyers.Count == 0)
            {
                // Check for old format (backward compatibility)
                if (analysisData?.ClubNights != null && analysisData.ClubNights.Count > 0)
                {
                    // Convert old format to new format
                    analysisData.Flyers = new List<FlyerData>
                    {
                        new FlyerData { ClubNights = analysisData.ClubNights }
                    };
                    _logger.LogInformation("Converted old format response to new format");
                }
                else
                {
                    diagnostics.Metadata["FlyersFound"] = "0";
                    
                    _logger.LogWarning("No flyers found in analysis");
                    return new FlyerAnalysisResult
                    {
                        Success = false,
                        ErrorMessage = "No flyers found in image",
                        Diagnostics = diagnostics
                    };
                }
            }
            
            // Count total club nights across all flyers
            var totalClubNights = analysisData.Flyers.Sum(f => f.ClubNights.Count);
            diagnostics.Metadata["FlyersFound"] = analysisData.Flyers.Count.ToString();
            diagnostics.Metadata["ClubNightsFound"] = totalClubNights.ToString();
            overallStopwatch.Stop();
            diagnostics.Metadata["TotalDurationMs"] = overallStopwatch.ElapsedMilliseconds.ToString();

            return new FlyerAnalysisResult
            {
                Success = true,
                Flyers = analysisData.Flyers,
                Diagnostics = diagnostics,
                GeminiPrompt = prompt,
                GeminiRawResponse = textResponse,
                ImageSizeBytes = imageBytes.Length,
                ImageMimeType = mimeType
            };
        }
        catch (Exception ex)
        {
            diagnostics.ErrorMessage = $"Error analyzing image: {ex.Message}";
            diagnostics.StackTrace = ex.StackTrace;
            diagnostics.Metadata["ExceptionType"] = ex.GetType().Name;
            
            _logger.LogError(ex, "Error analyzing flyer image");
            return new FlyerAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Error analyzing image: {ex.Message}",
                Diagnostics = diagnostics
            };
        }
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private class FlyerAnalysisData
    {
        [JsonPropertyName("flyers")]
        public List<FlyerData>? Flyers { get; set; }
        
        // For backward compatibility with old format
        [JsonPropertyName("clubNights")]
        public List<ClubNightData>? ClubNights { get; set; }
    }
}

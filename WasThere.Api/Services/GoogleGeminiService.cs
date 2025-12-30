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
                checkKeyStep.DurationMs = 0;
                
                _logger.LogError("Google Gemini API key is not configured");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Google Gemini API key is not configured. Please set GoogleGemini:ApiKey in configuration.",
                    Diagnostics = diagnostics
                };
            }
            
            checkKeyStep.Status = "completed";
            checkKeyStep.DurationMs = 0;
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
            var prompt = @"Analyze this club/event flyer image and extract the following information in JSON format:

{
  ""clubNights"": [
    {
      ""eventName"": ""The event name (e.g., 'Fabric', 'Ministry of Sound')"",
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

Important instructions:
1. Extract ALL dates shown on the flyer - create a separate club night entry for each date
2. For 'Residents' or 'Resident DJs', add them as acts on EVERY club night date
3. Include the event name (the recurring night name like 'Fabric' or the specific event title)
4. Include all performing artists/DJs listed
5. **CRITICAL**: If a listing combines multiple acts with separators like '&', 'and', 'B2B', 'b2b', 'vs', 'VS', or similar, split them into separate act entries. For example:
   - 'DJ A & DJ B' should become two acts: 'DJ A' and 'DJ B'
   - 'Artist X B2B Artist Y' should become two acts: 'Artist X' and 'Artist Y'
   - 'DJ 1 vs DJ 2' should become two acts: 'DJ 1' and 'DJ 2'
6. For each act, determine if it's a live set:
   - Set isLiveSet to true if the act has indicators like '(live)', '(live set)', '(live PA)', 'live', or similar
   - Set isLiveSet to false if it has '(DJ set)', '(DJ)', or no indicator (default to DJ set)
   - Remove the performance type indicators from the name (e.g., 'Dave Clarke (live)' should be just 'Dave Clarke')
7. **DATE EXTRACTION**:
   - If the full date with year is visible (e.g., '27 May 2003'), provide it in the 'date' field as 'YYYY-MM-DD'
   - If only partial date is visible (e.g., 'Friday 27th May' without year), set 'date' to null and provide:
     * 'dayOfWeek': the day name if visible (very important for inferring year)
     * 'month': the numeric month (1-12)
     * 'day': the numeric day of month (1-31)
8. If multiple dates are shown, create separate entries for each date
9. Only extract information that is clearly visible in the flyer
10. Return ONLY valid JSON, no additional text or markdown

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
            prepareRequestStep.DurationMs = 0;
            diagnostics.Metadata["GeminiModel"] = "gemini-2.5-flash";

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
            try
            {
                response = await _client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
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
                
                _logger.LogError(ex, "Error calling Gemini API");
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
                validateResponseStep.DurationMs = 0;
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
            validateResponseStep.DurationMs = 0;
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
                extractTextStep.DurationMs = 0;
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
            extractTextStep.DurationMs = 0;
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
            cleanResponseStep.DurationMs = 0;

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
                parseJsonStep.DurationMs = 0;
            }
            catch (Exception ex)
            {
                parseJsonStep.Status = "failed";
                parseJsonStep.Error = ex.Message;
                parseJsonStep.DurationMs = 0;
                diagnostics.ErrorMessage = $"Failed to parse JSON: {ex.Message}";
                
                _logger.LogError(ex, "Failed to parse Gemini response as JSON");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to parse AI response as JSON: {ex.Message}",
                    Diagnostics = diagnostics
                };
            }

            if (analysisData?.ClubNights == null || analysisData.ClubNights.Count == 0)
            {
                diagnostics.Metadata["ClubNightsFound"] = "0";
                
                _logger.LogWarning("No club nights found in analysis");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "No club nights found in flyer",
                    Diagnostics = diagnostics
                };
            }
            
            diagnostics.Metadata["ClubNightsFound"] = analysisData.ClubNights.Count.ToString();
            overallStopwatch.Stop();
            diagnostics.Metadata["TotalDurationMs"] = overallStopwatch.ElapsedMilliseconds.ToString();

            return new FlyerAnalysisResult
            {
                Success = true,
                ClubNights = analysisData.ClubNights,
                Diagnostics = diagnostics
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
        [JsonPropertyName("clubNights")]
        public List<ClubNightData> ClubNights { get; set; } = new();
    }
}

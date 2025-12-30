using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WasThere.Api.Services;

public class GoogleGeminiService : IGoogleGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleGeminiService> _logger;
    private readonly string _apiKey;
    private const string GeminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    public GoogleGeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleGeminiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["GoogleGemini:ApiKey"] ?? string.Empty;
    }

    public async Task<FlyerAnalysisResult> AnalyzeFlyerImageAsync(string imagePath)
    {
        try
        {
            // Check if API key is configured
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Google Gemini API key is not configured");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Google Gemini API key is not configured. Please set GoogleGemini:ApiKey in configuration."
                };
            }

            // Read and encode image to base64
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var base64Image = Convert.ToBase64String(imageBytes);
            
            // Determine MIME type from file extension
            var mimeType = GetMimeType(imagePath);

            // Construct the prompt for analyzing the flyer
            var prompt = @"Analyze this club/event flyer image and extract the following information in JSON format:

{
  ""clubNights"": [
    {
      ""eventName"": ""The event name (e.g., 'Fabric', 'Ministry of Sound')"",
      ""venueName"": ""The venue name"",
      ""date"": ""The date in ISO format (YYYY-MM-DD)"",
      ""acts"": [""Act 1"", ""Act 2"", ""Act 3""]
    }
  ]
}

Important instructions:
1. Extract ALL dates shown on the flyer - create a separate club night entry for each date
2. For 'Residents' or 'Resident DJs', add them as acts on EVERY club night date
3. Include the event name (the recurring night name like 'Fabric' or the specific event title)
4. Include all performing artists/DJs listed
5. If multiple dates are shown, create separate entries for each date
6. Only extract information that is clearly visible in the flyer
7. Return ONLY valid JSON, no additional text or markdown

Please analyze the flyer and return the JSON:";

            // Create the request payload
            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Image
                                }
                            }
                        }
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Add API key to URL
            var urlWithKey = $"{GeminiApiUrl}?key={_apiKey}";

            // Send request to Gemini API
            var response = await _httpClient.PostAsync(urlWithKey, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Gemini API error: {response.StatusCode}"
                };
            }

            // Parse response
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
            
            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
            {
                _logger.LogWarning("No candidates in Gemini response");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "No analysis results returned from AI"
                };
            }

            // Extract text from response
            var textResponse = geminiResponse.Candidates[0]?.Content?.Parts?.FirstOrDefault()?.Text;
            
            if (string.IsNullOrEmpty(textResponse))
            {
                _logger.LogWarning("Empty text response from Gemini");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Empty response from AI"
                };
            }

            _logger.LogInformation("Gemini raw response: {Response}", textResponse);

            // Clean up the response - sometimes AI returns markdown code blocks
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

            // Parse the JSON response
            var analysisData = JsonSerializer.Deserialize<FlyerAnalysisData>(textResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (analysisData?.ClubNights == null || analysisData.ClubNights.Count == 0)
            {
                _logger.LogWarning("No club nights found in analysis");
                return new FlyerAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "No club nights found in flyer"
                };
            }

            return new FlyerAnalysisResult
            {
                Success = true,
                ClubNights = analysisData.ClubNights
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing flyer image");
            return new FlyerAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Error analyzing image: {ex.Message}"
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

    // DTOs for Gemini API response
    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class FlyerAnalysisData
    {
        [JsonPropertyName("clubNights")]
        public List<ClubNightData> ClubNights { get; set; } = new();
    }
}

using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;

namespace WasThere.Api.Services;

public class ImageSplitterService : IImageSplitterService
{
    private readonly Client? _client;
    private readonly ILogger<ImageSplitterService> _logger;
    private const string GeminiModel = "gemini-2.5-flash";

    public ImageSplitterService(IConfiguration configuration, ILogger<ImageSplitterService> logger)
    {
        _logger = logger;
        var apiKey = configuration["GoogleGemini:ApiKey"] ?? string.Empty;
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            _client = new Client(apiKey: apiKey);
        }
    }

    public async Task<FlyerBoundingBoxResult> DetectFlyerBoundingBoxesAsync(string imagePath)
    {
        try
        {
            if (string.IsNullOrEmpty(imagePath) || _client == null)
            {
                return new FlyerBoundingBoxResult
                {
                    Success = false,
                    ErrorMessage = "Invalid configuration or image path"
                };
            }

            // Read image bytes
            var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
            var mimeType = GetMimeType(imagePath);

            // Construct prompt for bounding box detection
            var prompt = @"Analyze this image and determine if it contains multiple separate, distinct flyer images.

IMPORTANT: Only return multiple bounding boxes if the image shows physically distinct, separate flyers (e.g., a photo of 4 different flyers laid on a table, or multiple flyers in different sections of the image). Each separate flyer will typically have its own distinct design, logo, or visual boundary.

If this is a single flyer (even if it shows multiple dates/events on that one flyer), return a single bounding box for the entire image.

Return your response in JSON format:

{
  ""flyerCount"": <number of distinct flyers>,
  ""boundingBoxes"": [
    {
      ""index"": 0,
      ""x"": <normalized x coordinate 0-1 of top-left corner>,
      ""y"": <normalized y coordinate 0-1 of top-left corner>,
      ""width"": <normalized width 0-1>,
      ""height"": <normalized height 0-1>
    }
  ]
}

Guidelines:
- Use normalized coordinates where 0,0 is top-left and 1,1 is bottom-right
- If single flyer, return: {""index"": 0, ""x"": 0, ""y"": 0, ""width"": 1, ""height"": 1}
- For multiple flyers, provide tight bounding boxes around each distinct flyer
- Order flyers left-to-right, top-to-bottom
- Ensure bounding boxes don't overlap significantly

Return ONLY valid JSON, no additional text.";

            // Create the request
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

            _logger.LogInformation("Calling Gemini API for bounding box detection");

            // Call Gemini API
            var response = await _client.Models.GenerateContentAsync(
                model: GeminiModel,
                contents: content
            );

            if (response?.Candidates == null || response.Candidates.Count == 0)
            {
                return new FlyerBoundingBoxResult
                {
                    Success = false,
                    ErrorMessage = "No response from AI for bounding box detection"
                };
            }

            var textResponse = response.Candidates[0]?.Content?.Parts?.FirstOrDefault()?.Text;
            
            if (string.IsNullOrEmpty(textResponse))
            {
                return new FlyerBoundingBoxResult
                {
                    Success = false,
                    ErrorMessage = "Empty response from AI"
                };
            }

            _logger.LogInformation("Bounding box detection response: {Response}", textResponse);

            // Clean up response - remove markdown code blocks
            textResponse = CleanJsonResponse(textResponse);

            // Parse JSON response
            var boundingBoxData = JsonSerializer.Deserialize<BoundingBoxResponse>(textResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (boundingBoxData?.BoundingBoxes == null || boundingBoxData.BoundingBoxes.Count == 0)
            {
                // Default to single flyer if no boxes detected
                return new FlyerBoundingBoxResult
                {
                    Success = true,
                    BoundingBoxes = new List<BoundingBox>
                    {
                        new BoundingBox { Index = 0, X = 0, Y = 0, Width = 1, Height = 1 }
                    }
                };
            }

            return new FlyerBoundingBoxResult
            {
                Success = true,
                BoundingBoxes = boundingBoxData.BoundingBoxes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting flyer bounding boxes");
            return new FlyerBoundingBoxResult
            {
                Success = false,
                ErrorMessage = $"Error detecting bounding boxes: {ex.Message}"
            };
        }
    }

    public async Task<List<string>> SplitImageIntoFlyersAsync(string imagePath, List<BoundingBox> boundingBoxes, string outputDirectory)
    {
        var splitImagePaths = new List<string>();

        try
        {
            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            using var image = SixLabors.ImageSharp.Image.Load(imagePath);
            var imageWidth = image.Width;
            var imageHeight = image.Height;

            for (int i = 0; i < boundingBoxes.Count; i++)
            {
                var bbox = boundingBoxes[i];
                
                // Convert normalized coordinates to pixel coordinates
                var x = (int)(bbox.X * imageWidth);
                var y = (int)(bbox.Y * imageHeight);
                var width = (int)(bbox.Width * imageWidth);
                var height = (int)(bbox.Height * imageHeight);

                // Ensure coordinates are within bounds and dimensions are reasonable
                x = Math.Max(0, Math.Min(x, imageWidth - 10)); // Leave at least 10 pixels for crop
                y = Math.Max(0, Math.Min(y, imageHeight - 10));
                width = Math.Max(10, Math.Min(width, imageWidth - x)); // Minimum 10x10 crop
                height = Math.Max(10, Math.Min(height, imageHeight - y));

                // Clone the image and crop to bounding box
                using var croppedImage = image.Clone(ctx => 
                    ctx.Crop(new SixLabors.ImageSharp.Rectangle(x, y, width, height))
                );

                // Generate output filename
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                var originalFilename = Path.GetFileNameWithoutExtension(imagePath);
                var outputFilename = $"{originalFilename}_flyer_{i}{extension}";
                var outputPath = Path.Combine(outputDirectory, outputFilename);

                // Save cropped image to file
                using (var fileStream = new FileStream(outputPath, FileMode.Create))
                {
                    // Detect encoder based on extension
                    IImageEncoder encoder = extension switch
                    {
                        ".png" => new PngEncoder(),
                        ".jpg" or ".jpeg" => new JpegEncoder(),
                        ".gif" => new GifEncoder(),
                        ".webp" => new WebpEncoder(),
                        _ => new JpegEncoder()
                    };
                    await croppedImage.SaveAsync(fileStream, encoder);
                }
                splitImagePaths.Add(outputPath);

                _logger.LogInformation("Split flyer {Index}: saved to {Path}", i, outputPath);
            }

            return splitImagePaths;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting image into flyers");
            throw;
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

    private static string CleanJsonResponse(string response)
    {
        response = response.Trim();
        
        // Remove markdown code block markers
        if (response.StartsWith("```json"))
        {
            response = response.Substring(7);
        }
        else if (response.StartsWith("```"))
        {
            response = response.Substring(3);
        }
        
        if (response.EndsWith("```"))
        {
            response = response.Substring(0, response.Length - 3);
        }
        
        return response.Trim();
    }

    private class BoundingBoxResponse
    {
        public int FlyerCount { get; set; }
        public List<BoundingBox> BoundingBoxes { get; set; } = new();
    }
}

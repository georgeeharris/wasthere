using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;
using WasThere.Api.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.Authorization;

namespace WasThere.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlyersController : ControllerBase
{
    private readonly ClubEventContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FlyersController> _logger;
    private readonly IGoogleGeminiService _geminiService;
    private readonly IDateYearInferenceService _yearInferenceService;
    private readonly IFlyerConversionLogger _conversionLogger;
    private readonly IImageSplitterService _imageSplitterService;
    private const long MaxFileSize = 20 * 1024 * 1024; // 20MB
    private const string UploadsFolder = "uploads";
    private const int ThumbnailWidth = 300;
    private const int ThumbnailHeight = 400;
    private const string PlaceholderEventName = "Unknown Event (Pending Selection)";
    private const string PlaceholderVenueName = "Unknown Venue";
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] UncertainVenueIndicators = { "pending", "unknown", "unclear", "n/a", "tbd" };

    public FlyersController(
        ClubEventContext context, 
        IWebHostEnvironment environment,
        ILogger<FlyersController> logger,
        IGoogleGeminiService geminiService,
        IDateYearInferenceService yearInferenceService,
        IFlyerConversionLogger conversionLogger,
        IImageSplitterService imageSplitterService)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _geminiService = geminiService;
        _yearInferenceService = yearInferenceService;
        _conversionLogger = conversionLogger;
        _imageSplitterService = imageSplitterService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Flyer>>> GetFlyers()
    {
        return await _context.Flyers
            .Include(f => f.Event)
            .Include(f => f.Venue)
            .Include(f => f.ClubNights)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Flyer>> GetFlyer(int id)
    {
        var flyer = await _context.Flyers
            .Include(f => f.Event)
            .Include(f => f.Venue)
            .Include(f => f.ClubNights)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (flyer == null)
        {
            return NotFound();
        }

        return flyer;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<MultiFlyerUploadResponse>> UploadFlyer([FromForm] IFormFile file, [FromForm] bool skipImageSplitting = false)
    {
        // Validate file
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest($"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return BadRequest($"Invalid file type. Allowed types: {string.Join(", ", AllowedExtensions)}");
        }

        // Save file to temporary location first
        var uploadsPath = Path.Combine(_environment.ContentRootPath, UploadsFolder, "temp");
        Directory.CreateDirectory(uploadsPath);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var tempFilePath = Path.Combine(uploadsPath, uniqueFileName);

        // Start conversion log
        var logId = _conversionLogger.StartConversionLog(tempFilePath, file.FileName);

        try
        {
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file to disk");
            _conversionLogger.LogError(logId, "Error saving file to disk", ex);
            _conversionLogger.CompleteConversionLog(logId, false, "Failed to save uploaded file");
            return StatusCode(500, "Error saving file to disk.");
        }

        // NEW: Detect if image contains multiple flyers (skip if user indicates single flyer)
        int flyerCount = 1;
        FlyerBoundingBoxResult boundingBoxResult = new FlyerBoundingBoxResult
        {
            Success = true,
            BoundingBoxes = new List<BoundingBox>
            {
                new BoundingBox { Index = 0, X = 0, Y = 0, Width = 1, Height = 1 }
            }
        };
        
        if (!skipImageSplitting)
        {
            _logger.LogInformation("Detecting flyer bounding boxes in uploaded image");
            boundingBoxResult = await _imageSplitterService.DetectFlyerBoundingBoxesAsync(tempFilePath);
            
            if (!boundingBoxResult.Success)
            {
                _logger.LogWarning("Failed to detect bounding boxes, proceeding with single flyer assumption");
                // Fall back to treating as single flyer
                boundingBoxResult.BoundingBoxes = new List<BoundingBox>
                {
                    new BoundingBox { Index = 0, X = 0, Y = 0, Width = 1, Height = 1 }
                };
            }

            flyerCount = boundingBoxResult.FlyerCount;
            _logger.LogInformation("Detected {FlyerCount} flyer(s) in the uploaded image", flyerCount);
            _conversionLogger.LogDatabaseOperation(logId, "DETECT", "Flyers", $"Detected {flyerCount} flyer(s)", 0);
        }
        else
        {
            _logger.LogInformation("Skipping image splitting as requested by user");
            _conversionLogger.LogDatabaseOperation(logId, "SKIP", "Splitting", "User indicated single flyer upload", 0);
        }

        List<FlyerUploadResult> flyerResults = new();

        // If multiple flyers detected, split the image
        List<string> imagePaths;
        if (flyerCount > 1)
        {
            _logger.LogInformation("Splitting image into {Count} individual flyers", flyerCount);
            try
            {
                var splitDir = Path.Combine(uploadsPath, $"split_{Guid.NewGuid()}");
                imagePaths = await _imageSplitterService.SplitImageIntoFlyersAsync(
                    tempFilePath, 
                    boundingBoxResult.BoundingBoxes, 
                    splitDir
                );
                _conversionLogger.LogDatabaseOperation(logId, "SPLIT", "Image", $"Split into {imagePaths.Count} files", 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error splitting image");
                _conversionLogger.LogError(logId, "Error splitting image", ex);
                _conversionLogger.CompleteConversionLog(logId, false, "Failed to split image");
                try { System.IO.File.Delete(tempFilePath); } catch { }
                return StatusCode(500, "Error splitting image into individual flyers.");
            }
        }
        else
        {
            // Single flyer, use original temp file
            imagePaths = new List<string> { tempFilePath };
        }

        // Process each flyer image separately
        for (int i = 0; i < imagePaths.Count; i++)
        {
            var flyerImagePath = imagePaths[i];
            var flyerLogId = i == 0 ? logId : _conversionLogger.StartConversionLog(flyerImagePath, $"{file.FileName} (Flyer {i + 1})");
            
            _logger.LogInformation("Processing flyer {Index} of {Total}", i + 1, imagePaths.Count);
            
            try
            {
                var result = await ProcessSingleFlyerAsync(
                    flyerImagePath,
                    file.FileName,
                    extension,
                    flyerLogId,
                    i + 1,
                    imagePaths.Count
                );
                
                flyerResults.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing flyer {Index}", i + 1);
                _conversionLogger.LogError(flyerLogId, $"Error processing flyer {i + 1}", ex);
                flyerResults.Add(new FlyerUploadResult
                {
                    Success = false,
                    Message = $"Failed to process flyer {i + 1}: {ex.Message}",
                    FlyerIndex = i + 1
                });
            }
        }

        // Clean up split files and temp file
        try
        {
            foreach (var path in imagePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            
            // Clean up split directory if it was created
            if (flyerCount > 1 && imagePaths.Count > 0)
            {
                var splitDir = Path.GetDirectoryName(imagePaths[0]);
                if (!string.IsNullOrEmpty(splitDir) && Directory.Exists(splitDir))
                {
                    Directory.Delete(splitDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up temporary files");
        }

        // Build response
        var successfulFlyers = flyerResults.Count(r => r.Success);
        var response = new MultiFlyerUploadResponse
        {
            Success = successfulFlyers > 0,
            Message = successfulFlyers == flyerResults.Count
                ? $"Successfully processed all {flyerResults.Count} flyer(s)"
                : $"Processed {successfulFlyers} of {flyerResults.Count} flyer(s) successfully",
            TotalFlyers = flyerResults.Count,
            FlyerResults = flyerResults
        };

        return Ok(response);
    }

    private async Task<FlyerUploadResult> ProcessSingleFlyerAsync(
        string imagePath,
        string originalFileName,
        string extension,
        string logId,
        int flyerIndex,
        int totalFlyers)
    {
        try
        {
            // Analyze the flyer
            var analysisResult = await _geminiService.AnalyzeFlyerImageAsync(imagePath);
            
            // Log Gemini request and response
            if (!string.IsNullOrEmpty(analysisResult.GeminiPrompt))
            {
                _conversionLogger.LogGeminiRequest(logId, analysisResult.GeminiPrompt, imagePath, 
                    analysisResult.ImageSizeBytes, analysisResult.ImageMimeType ?? "unknown");
            }
            
            if (!string.IsNullOrEmpty(analysisResult.GeminiRawResponse))
            {
                _conversionLogger.LogGeminiResponse(logId, analysisResult.GeminiRawResponse, 
                    analysisResult.Success, analysisResult.ErrorMessage);
            }
            
            // Log analysis result
            _conversionLogger.LogAnalysisResult(logId, analysisResult);
            
            if (!analysisResult.Success || analysisResult.ClubNights.Count == 0)
            {
                _conversionLogger.CompleteConversionLog(logId, false, 
                    analysisResult.ErrorMessage ?? "Failed to analyze flyer");
                return new FlyerUploadResult
                {
                    Success = false,
                    Message = analysisResult.ErrorMessage ?? "Failed to analyze flyer. Could not extract event information.",
                    Diagnostics = analysisResult.Diagnostics,
                    FlyerIndex = flyerIndex
                };
            }

            // Populate candidate years for each club night
            foreach (var clubNightData in analysisResult.ClubNights)
            {
                if (!clubNightData.Date.HasValue && clubNightData.Month.HasValue && clubNightData.Day.HasValue)
                {
                    var candidateYears = _yearInferenceService.GetCandidateYears(
                        clubNightData.Month.Value,
                        clubNightData.Day.Value,
                        clubNightData.DayOfWeek
                    );
                    clubNightData.CandidateYears = candidateYears;
                }
            }

            // Use the first club night's data to determine event and venue
            var firstClubNight = analysisResult.ClubNights[0];
            
            // Check if event name was detected
            var eventName = firstClubNight.EventName?.Trim();
            var needsEventSelection = string.IsNullOrEmpty(eventName);
            
            // Create a placeholder event if event name is missing
            Event eventEntity;
            if (needsEventSelection)
            {
                eventName = PlaceholderEventName;
                var existingPlaceholder = await _context.Events
                    .FirstOrDefaultAsync(e => e.Name == eventName);
                eventEntity = existingPlaceholder ?? new Event { Name = eventName };
                if (existingPlaceholder == null)
                {
                    _context.Events.Add(eventEntity);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var existingEvent = await _context.Events
                    .FirstOrDefaultAsync(e => e.Name.ToLower() == eventName!.ToLower());
                eventEntity = existingEvent ?? new Event { Name = eventName! };
                if (existingEvent == null)
                {
                    _context.Events.Add(eventEntity);
                    await _context.SaveChangesAsync();
                }
            }

            // Find or create Venue
            var venueName = firstClubNight.VenueName?.Trim();
            Venue? venueEntity = null;
            
            if (IsUncertainVenueName(venueName))
            {
                venueName = PlaceholderVenueName;
                var existingVenue = await _context.Venues
                    .FirstOrDefaultAsync(v => v.Name == venueName);
                venueEntity = existingVenue ?? new Venue { Name = venueName };
                if (existingVenue == null)
                {
                    _context.Venues.Add(venueEntity);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var existingVenue = await _context.Venues
                    .FirstOrDefaultAsync(v => v.Name.ToLower() == venueName!.ToLower());
                venueEntity = existingVenue ?? new Venue { Name = venueName! };
                if (existingVenue == null)
                {
                    _context.Venues.Add(venueEntity);
                    await _context.SaveChangesAsync();
                }
            }

            // Determine earliest date
            DateTime? earliestDate = null;
            foreach (var clubNightData in analysisResult.ClubNights)
            {
                var inferredDate = InferDate(clubNightData);
                if (inferredDate.HasValue)
                {
                    if (!earliestDate.HasValue || inferredDate.Value < earliestDate.Value)
                    {
                        earliestDate = inferredDate.Value;
                    }
                }
            }

            var finalDate = earliestDate ?? new DateTime(2002, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Sanitize names for file paths
            var sanitizedEventName = SanitizeFileName(eventEntity.Name);
            var sanitizedVenueName = SanitizeFileName(venueEntity.Name);
            var dateFolder = finalDate.ToString("yyyy-MM-dd");

            // Move file to proper location
            var finalUploadsPath = Path.Combine(_environment.ContentRootPath, UploadsFolder, sanitizedEventName, sanitizedVenueName, dateFolder);
            Directory.CreateDirectory(finalUploadsPath);
            
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var finalFilePath = Path.Combine(finalUploadsPath, uniqueFileName);
            System.IO.File.Copy(imagePath, finalFilePath);

            // Generate thumbnail
            var thumbnailFileName = $"thumb_{uniqueFileName}";
            var thumbnailFilePath = Path.Combine(finalUploadsPath, thumbnailFileName);
            try
            {
                GenerateThumbnail(finalFilePath, thumbnailFilePath, ThumbnailWidth, ThumbnailHeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail");
            }

            // Create relative paths
            var relativePath = Path.Combine(UploadsFolder, sanitizedEventName, sanitizedVenueName, dateFolder, uniqueFileName);
            var thumbnailRelativePath = System.IO.File.Exists(thumbnailFilePath) 
                ? Path.Combine(UploadsFolder, sanitizedEventName, sanitizedVenueName, dateFolder, thumbnailFileName)
                : null;

            // Create Flyer entity
            var displayName = totalFlyers > 1 ? $"{originalFileName} (Flyer {flyerIndex})" : originalFileName;
            var flyer = new Flyer
            {
                FilePath = relativePath,
                ThumbnailPath = thumbnailRelativePath,
                FileName = displayName,
                UploadedAt = DateTime.UtcNow,
                EventId = eventEntity.Id,
                VenueId = venueEntity.Id,
                EarliestClubNightDate = finalDate
            };

            _context.Flyers.Add(flyer);
            await _context.SaveChangesAsync();
            
            _conversionLogger.LogDatabaseOperation(logId, "CREATE", "Flyer", displayName, flyer.Id);

            // Check if needs year selection
            var needsYearSelection = analysisResult.ClubNights.Any(cn => cn.CandidateYears.Count > 0);
            
            // If no user input is needed, automatically create club nights
            AutoPopulateResult? autoPopulateResult = null;
            if (!needsEventSelection && !needsYearSelection)
            {
                _logger.LogInformation("No user input needed for flyer {FlyerIndex}, automatically creating club nights", flyerIndex);
                autoPopulateResult = await ProcessAnalysisResult(flyer, analysisResult);
                _conversionLogger.LogDatabaseOperation(logId, "AUTO_PROCESS", "ClubNights", 
                    $"Automatically created {autoPopulateResult.ClubNightsCreated} club nights", 0);
            }
            
            // Build message
            string message;
            if (needsEventSelection && needsYearSelection)
            {
                message = "Flyer analyzed. Please select the event and years for the dates.";
            }
            else if (needsEventSelection)
            {
                message = "Flyer analyzed. Please select the event.";
            }
            else if (needsYearSelection)
            {
                message = "Flyer analyzed. Please select years for the dates.";
            }
            else
            {
                // When club nights were auto-created, include the count in the message
                if (autoPopulateResult != null)
                {
                    message = $"Flyer analyzed successfully. {autoPopulateResult.Message}";
                }
                else
                {
                    message = "Flyer analyzed successfully.";
                }
            }
            
            if (totalFlyers > 1)
            {
                message = $"Flyer {flyerIndex} of {totalFlyers}: " + message;
            }
            
            _conversionLogger.CompleteConversionLog(logId, true, 
                $"Upload complete. {(needsEventSelection || needsYearSelection ? "Awaiting user input." : "Club nights created automatically.")}");

            return new FlyerUploadResult
            {
                Success = true,
                NeedsEventSelection = needsEventSelection,
                Message = message,
                Flyer = await _context.Flyers
                    .Include(f => f.Event)
                    .Include(f => f.Venue)
                    .Include(f => f.ClubNights)
                        .ThenInclude(cn => cn.ClubNightActs)
                            .ThenInclude(cna => cna.Act)
                    .FirstOrDefaultAsync(f => f.Id == flyer.Id),
                AnalysisResult = analysisResult,
                FlyerIndex = flyerIndex
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessSingleFlyerAsync");
            return new FlyerUploadResult
            {
                Success = false,
                Message = $"Error processing flyer: {ex.Message}",
                FlyerIndex = flyerIndex
            };
        }
    }

    [HttpPost("{id}/complete-upload")]
    public async Task<ActionResult<AutoPopulateResult>> CompleteUpload(int id, [FromBody] CompleteUploadRequest request)
    {
        var flyer = await _context.Flyers.FindAsync(id);
        if (flyer == null)
        {
            return NotFound("Flyer not found");
        }

        // Get full path to the image
        var imagePath = Path.Combine(_environment.ContentRootPath, flyer.FilePath);
        if (!System.IO.File.Exists(imagePath))
        {
            return NotFound("Flyer image file not found");
        }

        // Start conversion log for completing the upload
        var logId = _conversionLogger.StartConversionLog(imagePath, flyer.FileName);

        // Re-analyze the flyer to get the club nights data
        var analysisResult = await _geminiService.AnalyzeFlyerImageAsync(imagePath);
        
        // Log Gemini request and response
        if (!string.IsNullOrEmpty(analysisResult.GeminiPrompt))
        {
            _conversionLogger.LogGeminiRequest(logId, analysisResult.GeminiPrompt, imagePath, 
                analysisResult.ImageSizeBytes, analysisResult.ImageMimeType ?? "unknown");
        }
        
        if (!string.IsNullOrEmpty(analysisResult.GeminiRawResponse))
        {
            _conversionLogger.LogGeminiResponse(logId, analysisResult.GeminiRawResponse, 
                analysisResult.Success, analysisResult.ErrorMessage);
        }
        
        _conversionLogger.LogAnalysisResult(logId, analysisResult);
        
        if (!analysisResult.Success)
        {
            _conversionLogger.CompleteConversionLog(logId, false, 
                analysisResult.ErrorMessage ?? "Failed to analyze flyer");
            return BadRequest(new AutoPopulateResult
            {
                Success = false,
                Message = analysisResult.ErrorMessage ?? "Failed to analyze flyer",
                Diagnostics = analysisResult.Diagnostics
            });
        }

        // Log user year selections
        _conversionLogger.LogUserYearSelection(logId, request.SelectedYears);

        // If user selected a different event, update the flyer and event name in club nights
        if (request.EventId.HasValue && request.EventId.Value != flyer.EventId)
        {
            var selectedEvent = await _context.Events.FindAsync(request.EventId.Value);
            if (selectedEvent == null)
            {
                _conversionLogger.CompleteConversionLog(logId, false, "Selected event not found");
                return BadRequest(new AutoPopulateResult
                {
                    Success = false,
                    Message = "Selected event not found"
                });
            }
            
            // Update flyer's event
            flyer.EventId = selectedEvent.Id;
            _context.Flyers.Update(flyer);
            await _context.SaveChangesAsync();
            
            // Update event name in all club nights in the analysis result
            foreach (var clubNight in analysisResult.ClubNights)
            {
                clubNight.EventName = selectedEvent.Name;
            }
            
            _conversionLogger.LogDatabaseOperation(logId, "UPDATE", "Flyer", 
                $"Event changed to {selectedEvent.Name}", flyer.Id);
        }

        // Process club nights with the selected years
        var result = await ProcessAnalysisResultWithSelectedYears(flyer, analysisResult, request.SelectedYears, logId);
        
        // Update the flyer's EarliestClubNightDate based on the created club nights
        if (result.Success && result.ClubNightsCreated > 0)
        {
            var earliestClubNight = await _context.ClubNights
                .Where(cn => cn.FlyerId == flyer.Id)
                .OrderBy(cn => cn.Date)
                .FirstOrDefaultAsync();
            
            if (earliestClubNight != null)
            {
                flyer.EarliestClubNightDate = earliestClubNight.Date;
                _context.Flyers.Update(flyer);
                await _context.SaveChangesAsync();
                _conversionLogger.LogDatabaseOperation(logId, "UPDATE", "Flyer", 
                    $"Updated EarliestClubNightDate to {earliestClubNight.Date:yyyy-MM-dd}", flyer.Id);
            }
        }
        
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFlyer(int id)
    {
        var flyer = await _context.Flyers.FindAsync(id);
        if (flyer == null)
        {
            return NotFound();
        }

        // Delete the physical file
        var fullPath = Path.Combine(_environment.ContentRootPath, flyer.FilePath);
        if (System.IO.File.Exists(fullPath))
        {
            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from disk");
                // Continue with database deletion even if file deletion fails
            }
        }

        // Delete the thumbnail file
        if (!string.IsNullOrEmpty(flyer.ThumbnailPath))
        {
            var thumbnailPath = Path.Combine(_environment.ContentRootPath, flyer.ThumbnailPath);
            if (System.IO.File.Exists(thumbnailPath))
            {
                try
                {
                    System.IO.File.Delete(thumbnailPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting thumbnail from disk");
                }
            }
        }

        _context.Flyers.Remove(flyer);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/auto-populate")]
    public async Task<ActionResult<AutoPopulateResult>> AutoPopulateFromFlyer(int id)
    {
        var flyer = await _context.Flyers.FindAsync(id);
        if (flyer == null)
        {
            return NotFound("Flyer not found");
        }

        // Get full path to the image
        var imagePath = Path.Combine(_environment.ContentRootPath, flyer.FilePath);
        if (!System.IO.File.Exists(imagePath))
        {
            return NotFound("Flyer image file not found");
        }

        // Analyze the flyer using Google Gemini
        var analysisResult = await _geminiService.AnalyzeFlyerImageAsync(imagePath);
        
        if (!analysisResult.Success)
        {
            return BadRequest(new AutoPopulateResult
            {
                Success = false,
                Message = analysisResult.ErrorMessage ?? "Failed to analyze flyer",
                Diagnostics = analysisResult.Diagnostics
            });
        }

        var result = new AutoPopulateResult
        {
            Success = true,
            Diagnostics = analysisResult.Diagnostics,
            Message = "Successfully analyzed flyer"
        };

        // Process each club night from the analysis
        foreach (var clubNightData in analysisResult.ClubNights)
        {
            try
            {
                // Find or create Event
                var eventName = clubNightData.EventName?.Trim();
                if (string.IsNullOrEmpty(eventName))
                {
                    _logger.LogWarning("Skipping club night with empty event name");
                    continue;
                }

                var existingEvent = await _context.Events
                    .FirstOrDefaultAsync(e => e.Name.ToLower() == eventName.ToLower());
                
                var eventEntity = existingEvent ?? new Event { Name = eventName };
                if (existingEvent == null)
                {
                    _context.Events.Add(eventEntity);
                    await _context.SaveChangesAsync();
                    result.EventsCreated++;
                }

                // Find or create Venue
                var venueName = clubNightData.VenueName?.Trim();
                Venue? venueEntity = null;
                
                if (!string.IsNullOrEmpty(venueName))
                {
                    var existingVenue = await _context.Venues
                        .FirstOrDefaultAsync(v => v.Name.ToLower() == venueName.ToLower());
                    
                    venueEntity = existingVenue ?? new Venue { Name = venueName };
                    if (existingVenue == null)
                    {
                        _context.Venues.Add(venueEntity);
                        await _context.SaveChangesAsync();
                        result.VenuesCreated++;
                    }
                }
                else
                {
                    // Use venue from flyer if not in analysis
                    venueEntity = await _context.Venues.FindAsync(flyer.VenueId);
                }

                if (venueEntity == null)
                {
                    _logger.LogWarning("No venue found for club night");
                    continue;
                }

                // Create ClubNight if date is provided or can be inferred
                var inferredDate = InferDate(clubNightData);
                if (inferredDate.HasValue)
                {
                    var clubNight = new ClubNight
                    {
                        Date = DateTime.SpecifyKind(inferredDate.Value, DateTimeKind.Utc),
                        EventId = eventEntity.Id,
                        VenueId = venueEntity.Id,
                        FlyerId = flyer.Id
                    };

                    _context.ClubNights.Add(clubNight);
                    await _context.SaveChangesAsync();
                    result.ClubNightsCreated++;

                    // Add acts
                    if (clubNightData.Acts != null)
                    {
                        foreach (var actData in clubNightData.Acts)
                        {
                            if (actData == null)
                            {
                                continue;
                            }
                            
                            var trimmedActName = actData.Name?.Trim();
                            if (string.IsNullOrEmpty(trimmedActName))
                            {
                                continue;
                            }

                            // Find or create Act
                            var existingAct = await _context.Acts
                                .FirstOrDefaultAsync(a => a.Name.ToLower() == trimmedActName.ToLower());
                            
                            var actEntity = existingAct ?? new Act { Name = trimmedActName };
                            if (existingAct == null)
                            {
                                _context.Acts.Add(actEntity);
                                await _context.SaveChangesAsync();
                                result.ActsCreated++;
                            }

                            // Link act to club night
                            var clubNightAct = new ClubNightAct
                            {
                                ClubNightId = clubNight.Id,
                                ActId = actEntity.Id,
                                IsLiveSet = actData.IsLiveSet
                            };
                            _context.ClubNightActs.Add(clubNightAct);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing club night data");
                result.Errors.Add($"Error processing club night: {ex.Message}");
            }
        }

        result.Message = $"Created {result.ClubNightsCreated} club nights, {result.EventsCreated} events, {result.VenuesCreated} venues, {result.ActsCreated} acts";
        
        return Ok(result);
    }

    private DateTime? InferDate(ClubNightData clubNightData)
    {
        // If full date is available, use it
        if (clubNightData.Date.HasValue)
        {
            // Ensure the DateTime is marked as UTC
            return DateTime.SpecifyKind(clubNightData.Date.Value, DateTimeKind.Utc);
        }

        // If we have month and day, try to infer the year
        if (clubNightData.Month.HasValue && clubNightData.Day.HasValue)
        {
            var inferredYear = _yearInferenceService.InferYear(
                clubNightData.Month.Value,
                clubNightData.Day.Value,
                clubNightData.DayOfWeek
            );

            if (inferredYear.HasValue)
            {
                try
                {
                    return new DateTime(inferredYear.Value, clubNightData.Month.Value, clubNightData.Day.Value, 0, 0, 0, DateTimeKind.Utc);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create date from inferred year {Year}, month {Month}, day {Day}",
                        inferredYear.Value, clubNightData.Month.Value, clubNightData.Day.Value);
                }
            }
        }

        return null;
    }

    private async Task<AutoPopulateResult> ProcessAnalysisResult(Flyer flyer, FlyerAnalysisResult analysisResult)
    {
        var result = new AutoPopulateResult
        {
            Success = true,
            Message = "Successfully processed flyer"
        };

        // Process each club night from the analysis
        foreach (var clubNightData in analysisResult.ClubNights)
        {
            try
            {
                // Find or create Event
                var eventName = clubNightData.EventName?.Trim();
                if (string.IsNullOrEmpty(eventName))
                {
                    _logger.LogWarning("Skipping club night with empty event name");
                    continue;
                }

                var existingEvent = await _context.Events
                    .FirstOrDefaultAsync(e => e.Name.ToLower() == eventName.ToLower());
                
                var eventEntity = existingEvent ?? new Event { Name = eventName };
                if (existingEvent == null)
                {
                    _context.Events.Add(eventEntity);
                    await _context.SaveChangesAsync();
                    result.EventsCreated++;
                }

                // Find or create Venue
                var venueName = clubNightData.VenueName?.Trim();
                Venue? venueEntity = null;
                
                // Check if venue name is uncertain or missing
                if (IsUncertainVenueName(venueName))
                {
                    // Use venue from flyer if venue name is uncertain or missing
                    venueEntity = await _context.Venues.FindAsync(flyer.VenueId);
                }
                else
                {
                    // Use detected venue name
                    var existingVenue = await _context.Venues
                        .FirstOrDefaultAsync(v => v.Name.ToLower() == venueName!.ToLower());
                    
                    venueEntity = existingVenue ?? new Venue { Name = venueName! };
                    if (existingVenue == null)
                    {
                        _context.Venues.Add(venueEntity);
                        await _context.SaveChangesAsync();
                        result.VenuesCreated++;
                    }
                }

                if (venueEntity == null)
                {
                    _logger.LogWarning("No venue found for club night");
                    continue;
                }

                // Create ClubNight if date is provided or can be inferred
                var inferredDate = InferDate(clubNightData);
                if (inferredDate.HasValue)
                {
                    var clubNight = new ClubNight
                    {
                        Date = DateTime.SpecifyKind(inferredDate.Value, DateTimeKind.Utc),
                        EventId = eventEntity.Id,
                        VenueId = venueEntity.Id,
                        FlyerId = flyer.Id
                    };

                    _context.ClubNights.Add(clubNight);
                    await _context.SaveChangesAsync();
                    result.ClubNightsCreated++;

                    // Add acts
                    if (clubNightData.Acts != null)
                    {
                        foreach (var actData in clubNightData.Acts)
                        {
                            if (actData == null)
                            {
                                continue;
                            }
                            
                            var trimmedActName = actData.Name?.Trim();
                            if (string.IsNullOrEmpty(trimmedActName))
                            {
                                continue;
                            }

                            // Find or create Act
                            var existingAct = await _context.Acts
                                .FirstOrDefaultAsync(a => a.Name.ToLower() == trimmedActName.ToLower());
                            
                            var actEntity = existingAct ?? new Act { Name = trimmedActName };
                            if (existingAct == null)
                            {
                                _context.Acts.Add(actEntity);
                                await _context.SaveChangesAsync();
                                result.ActsCreated++;
                            }

                            // Link act to club night
                            var clubNightAct = new ClubNightAct
                            {
                                ClubNightId = clubNight.Id,
                                ActId = actEntity.Id,
                                IsLiveSet = actData.IsLiveSet
                            };
                            _context.ClubNightActs.Add(clubNightAct);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing club night data");
                result.Errors.Add($"Error processing club night: {ex.Message}");
            }
        }

        result.Message = $"Created {result.ClubNightsCreated} club nights, {result.EventsCreated} events, {result.VenuesCreated} venues, {result.ActsCreated} acts";
        
        return result;
    }

    private async Task<AutoPopulateResult> ProcessAnalysisResultWithSelectedYears(Flyer flyer, FlyerAnalysisResult analysisResult, List<YearSelection> selectedYears, string logId)
    {
        var result = new AutoPopulateResult
        {
            Success = true,
            Message = "Successfully processed flyer"
        };

        // Create a dictionary for quick lookup of selected years by month/day
        var yearLookup = selectedYears.ToDictionary(
            y => (y.Month, y.Day),
            y => y.Year
        );

        // Process each club night from the analysis
        foreach (var clubNightData in analysisResult.ClubNights)
        {
            try
            {
                // Find or create Event
                var eventName = clubNightData.EventName?.Trim();
                if (string.IsNullOrEmpty(eventName))
                {
                    _logger.LogWarning("Skipping club night with empty event name");
                    continue;
                }

                var existingEvent = await _context.Events
                    .FirstOrDefaultAsync(e => e.Name.ToLower() == eventName.ToLower());
                
                var eventEntity = existingEvent ?? new Event { Name = eventName };
                if (existingEvent == null)
                {
                    _context.Events.Add(eventEntity);
                    await _context.SaveChangesAsync();
                    result.EventsCreated++;
                    _conversionLogger.LogDatabaseOperation(logId, "CREATE", "Event", eventName, eventEntity.Id);
                }

                // Find or create Venue
                var venueName = clubNightData.VenueName?.Trim();
                Venue? venueEntity = null;
                
                // Check if venue name is uncertain or missing
                if (IsUncertainVenueName(venueName))
                {
                    // Use venue from flyer if venue name is uncertain or missing
                    venueEntity = await _context.Venues.FindAsync(flyer.VenueId);
                }
                else
                {
                    // Use detected venue name
                    var existingVenue = await _context.Venues
                        .FirstOrDefaultAsync(v => v.Name.ToLower() == venueName!.ToLower());
                    
                    venueEntity = existingVenue ?? new Venue { Name = venueName! };
                    if (existingVenue == null)
                    {
                        _context.Venues.Add(venueEntity);
                        await _context.SaveChangesAsync();
                        result.VenuesCreated++;
                        _conversionLogger.LogDatabaseOperation(logId, "CREATE", "Venue", venueName!, venueEntity.Id);
                    }
                }

                if (venueEntity == null)
                {
                    _logger.LogWarning("No venue found for club night");
                    continue;
                }

                // Create ClubNight using selected year or inferred date
                DateTime? dateToUse = null;
                
                if (clubNightData.Date.HasValue)
                {
                    // Full date already provided
                    dateToUse = clubNightData.Date.Value;
                }
                else if (clubNightData.Month.HasValue && clubNightData.Day.HasValue)
                {
                    // Check if user selected a year for this date
                    if (yearLookup.TryGetValue((clubNightData.Month.Value, clubNightData.Day.Value), out var selectedYear))
                    {
                        try
                        {
                            dateToUse = new DateTime(selectedYear, clubNightData.Month.Value, clubNightData.Day.Value, 0, 0, 0, DateTimeKind.Utc);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to create date from selected year {Year}, month {Month}, day {Day}",
                                selectedYear, clubNightData.Month.Value, clubNightData.Day.Value);
                        }
                    }
                    else
                    {
                        // Fall back to inference if no year was selected
                        dateToUse = InferDate(clubNightData);
                    }
                }

                if (dateToUse.HasValue)
                {
                    var clubNight = new ClubNight
                    {
                        Date = DateTime.SpecifyKind(dateToUse.Value, DateTimeKind.Utc),
                        EventId = eventEntity.Id,
                        VenueId = venueEntity.Id,
                        FlyerId = flyer.Id
                    };

                    _context.ClubNights.Add(clubNight);
                    await _context.SaveChangesAsync();
                    result.ClubNightsCreated++;
                    _conversionLogger.LogDatabaseOperation(logId, "CREATE", "ClubNight", 
                        $"{eventName} at {venueEntity.Name} on {dateToUse.Value:yyyy-MM-dd}", clubNight.Id);

                    // Add acts
                    if (clubNightData.Acts != null)
                    {
                        foreach (var actData in clubNightData.Acts)
                        {
                            if (actData == null)
                            {
                                continue;
                            }
                            
                            var trimmedActName = actData.Name?.Trim();
                            if (string.IsNullOrEmpty(trimmedActName))
                            {
                                continue;
                            }

                            // Find or create Act
                            var existingAct = await _context.Acts
                                .FirstOrDefaultAsync(a => a.Name.ToLower() == trimmedActName.ToLower());
                            
                            var actEntity = existingAct ?? new Act { Name = trimmedActName };
                            if (existingAct == null)
                            {
                                _context.Acts.Add(actEntity);
                                await _context.SaveChangesAsync();
                                result.ActsCreated++;
                            }
                                _conversionLogger.LogDatabaseOperation(logId, "CREATE", "Act", trimmedActName, actEntity.Id);

                            // Link act to club night
                            var clubNightAct = new ClubNightAct
                            {
                                ClubNightId = clubNight.Id,
                                ActId = actEntity.Id,
                                IsLiveSet = actData.IsLiveSet
                            };
                            _context.ClubNightActs.Add(clubNightAct);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing club night data");
                _conversionLogger.LogError(logId, "Error processing club night data", ex);
                result.Errors.Add($"Error processing club night: {ex.Message}");
            }
        }

        result.Message = $"Created {result.ClubNightsCreated} club nights, {result.EventsCreated} events, {result.VenuesCreated} venues, {result.ActsCreated} acts";
        
        // Complete the conversion log
        _conversionLogger.CompleteConversionLog(logId, result.Success, result.Message, 
            result.EventsCreated, result.VenuesCreated, result.ActsCreated, result.ClubNightsCreated);
        
        return result;
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid characters and replace spaces with underscores
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(ch => !invalidChars.Contains(ch))
            .ToArray());
        return sanitized.Replace(" ", "_");
    }

    private static bool IsUncertainVenueName(string? venueName)
    {
        if (string.IsNullOrWhiteSpace(venueName))
        {
            return true;
        }
        
        return UncertainVenueIndicators.Any(indicator => 
            venueName.Contains(indicator, StringComparison.OrdinalIgnoreCase));
    }

    private void GenerateThumbnail(string sourcePath, string thumbnailPath, int width, int height)
    {
        using var image = Image.Load(sourcePath);
        
        // Calculate aspect ratio
        var aspectRatio = (float)image.Width / image.Height;
        var targetAspectRatio = (float)width / height;
        
        int resizeWidth, resizeHeight;
        
        if (aspectRatio > targetAspectRatio)
        {
            // Image is wider than target, fit by height
            resizeHeight = height;
            resizeWidth = (int)(height * aspectRatio);
        }
        else
        {
            // Image is taller than target, fit by width
            resizeWidth = width;
            resizeHeight = (int)(width / aspectRatio);
        }
        
        image.Mutate(x => x.Resize(resizeWidth, resizeHeight));
        
        image.Save(thumbnailPath);
    }
}

public class CompleteUploadRequest
{
    public List<YearSelection> SelectedYears { get; set; } = new();
    public int? EventId { get; set; }
}

public class AutoPopulateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ClubNightsCreated { get; set; }
    public int EventsCreated { get; set; }
    public int VenuesCreated { get; set; }
    public int ActsCreated { get; set; }
    public List<string> Errors { get; set; } = new();
    public DiagnosticInfo? Diagnostics { get; set; }
}

public class FlyerUploadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Flyer? Flyer { get; set; }
    public AutoPopulateResult? AutoPopulateResult { get; set; }
    public DiagnosticInfo? Diagnostics { get; set; }
    public FlyerAnalysisResult? AnalysisResult { get; set; }
    public bool NeedsEventSelection { get; set; }
}

public class FlyerUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Flyer? Flyer { get; set; }
    public DiagnosticInfo? Diagnostics { get; set; }
    public FlyerAnalysisResult? AnalysisResult { get; set; }
    public bool NeedsEventSelection { get; set; }
    public int FlyerIndex { get; set; }
}

public class MultiFlyerUploadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalFlyers { get; set; }
    public List<FlyerUploadResult> FlyerResults { get; set; } = new();
}

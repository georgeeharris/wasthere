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
    private const long MaxFileSize = 20 * 1024 * 1024; // 20MB
    private const string UploadsFolder = "uploads";
    private const int ThumbnailWidth = 300;
    private const int ThumbnailHeight = 400;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public FlyersController(
        ClubEventContext context, 
        IWebHostEnvironment environment,
        ILogger<FlyersController> logger,
        IGoogleGeminiService geminiService,
        IDateYearInferenceService yearInferenceService,
        IFlyerConversionLogger conversionLogger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _geminiService = geminiService;
        _yearInferenceService = yearInferenceService;
        _conversionLogger = conversionLogger;
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
    public async Task<ActionResult<FlyerUploadResponse>> UploadFlyer([FromForm] IFormFile file)
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

        // Analyze the flyer immediately
        var analysisResult = await _geminiService.AnalyzeFlyerImageAsync(tempFilePath);
        
        // Log Gemini request and response
        if (!string.IsNullOrEmpty(analysisResult.GeminiPrompt))
        {
            _conversionLogger.LogGeminiRequest(logId, analysisResult.GeminiPrompt, tempFilePath, 
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
            // If analysis failed, clean up and return error
            _conversionLogger.CompleteConversionLog(logId, false, 
                analysisResult.ErrorMessage ?? "Failed to analyze flyer");
            try { System.IO.File.Delete(tempFilePath); } catch { }
            return BadRequest(new FlyerUploadResponse
            {
                Success = false,
                Message = analysisResult.ErrorMessage ?? "Failed to analyze flyer. Could not extract event information.",
                Diagnostics = analysisResult.Diagnostics
            });
        }

        // Populate candidate years for each club night
        foreach (var clubNightData in analysisResult.ClubNights)
        {
            // Only generate candidate years if full date is not present
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
            // Use a placeholder event name that will be replaced when user selects an event
            eventName = "Unknown Event (Pending Selection)";
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
            // Find or create Event with the detected name
            // eventName is guaranteed to be non-null here because needsEventSelection is false
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
        
        if (!string.IsNullOrEmpty(venueName))
        {
            var existingVenue = await _context.Venues
                .FirstOrDefaultAsync(v => v.Name.ToLower() == venueName.ToLower());
            venueEntity = existingVenue ?? new Venue { Name = venueName };
            if (existingVenue == null)
            {
                _context.Venues.Add(venueEntity);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            // Create a placeholder venue if not detected
            venueName = "Unknown Venue";
            var existingVenue = await _context.Venues
                .FirstOrDefaultAsync(v => v.Name == venueName);
            venueEntity = existingVenue ?? new Venue { Name = venueName };
            if (existingVenue == null)
            {
                _context.Venues.Add(venueEntity);
                await _context.SaveChangesAsync();
            }
        }

        // Determine earliest date from analysis results
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

        // If no valid date found, use a default date in the middle of our target range
        var finalDate = earliestDate ?? new DateTime(2002, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Sanitize names for use in file paths
        var sanitizedEventName = SanitizeFileName(eventEntity.Name);
        var sanitizedVenueName = SanitizeFileName(venueEntity.Name);
        var dateFolder = finalDate.ToString("yyyy-MM-dd");

        // Move file to proper location: uploads/{event}/{venue}/{date}/
        var finalUploadsPath = Path.Combine(_environment.ContentRootPath, UploadsFolder, sanitizedEventName, sanitizedVenueName, dateFolder);
        Directory.CreateDirectory(finalUploadsPath);
        
        var finalFilePath = Path.Combine(finalUploadsPath, uniqueFileName);
        try
        {
            System.IO.File.Move(tempFilePath, finalFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving file to final location");
            try { System.IO.File.Delete(tempFilePath); } catch { }
            return StatusCode(500, "Error organizing uploaded file.");
        }

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
            // Continue even if thumbnail generation fails
        }

        // Create relative paths for storage in database
        var relativePath = Path.Combine(UploadsFolder, sanitizedEventName, sanitizedVenueName, dateFolder, uniqueFileName);
        var thumbnailRelativePath = System.IO.File.Exists(thumbnailFilePath) 
            ? Path.Combine(UploadsFolder, sanitizedEventName, sanitizedVenueName, dateFolder, thumbnailFileName)
            : null;

        // Create Flyer entity
        var flyer = new Flyer
        {
            FilePath = relativePath,
            ThumbnailPath = thumbnailRelativePath,
            FileName = file.FileName,
            UploadedAt = DateTime.UtcNow,
            EventId = eventEntity.Id,
            VenueId = venueEntity.Id,
            EarliestClubNightDate = finalDate
        };

        _context.Flyers.Add(flyer);
        await _context.SaveChangesAsync();
        
        // Log flyer creation
        _conversionLogger.LogDatabaseOperation(logId, "CREATE", "Flyer", file.FileName, flyer.Id);

        // Check if any club nights have candidate years that need selection
        var needsYearSelection = analysisResult.ClubNights.Any(cn => cn.CandidateYears.Count > 0);
        
        // Build the message based on what user input is needed
        string message;
        if (needsEventSelection && needsYearSelection)
        {
            message = "Flyer uploaded and analyzed successfully. Please select the event and years for the dates.";
        }
        else if (needsEventSelection)
        {
            message = "Flyer uploaded and analyzed successfully. Please select the event.";
        }
        else if (needsYearSelection)
        {
            message = "Flyer uploaded and analyzed successfully. Please select years for the dates.";
        }
        else
        {
            message = "Flyer uploaded and analyzed successfully.";
        }
        
        // Complete the log (upload phase)
        _conversionLogger.CompleteConversionLog(logId, true, 
            $"Upload complete. {(needsEventSelection || needsYearSelection ? "Awaiting user input." : "Ready for processing.")}");

        // Return response with analysis result containing candidate years
        // Do NOT create club nights yet - user needs to select event/years first if needed
        var response = new FlyerUploadResponse
        {
            Success = true,
            NeedsEventSelection = needsEventSelection,
            Message = message,
            Flyer = await _context.Flyers
                .Include(f => f.Event)
                .Include(f => f.Venue)
                .Include(f => f.ClubNights)
                .FirstOrDefaultAsync(f => f.Id == flyer.Id),
            AnalysisResult = analysisResult
        };

        return CreatedAtAction(nameof(GetFlyer), new { id = flyer.Id }, response);
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
                        _conversionLogger.LogDatabaseOperation(logId, "CREATE", "Venue", venueName, venueEntity.Id);
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;
using WasThere.Api.Services;

namespace WasThere.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlyersController : ControllerBase
{
    private readonly ClubEventContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FlyersController> _logger;
    private readonly IGoogleGeminiService _geminiService;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private const string UploadsFolder = "uploads";
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public FlyersController(
        ClubEventContext context, 
        IWebHostEnvironment environment,
        ILogger<FlyersController> logger,
        IGoogleGeminiService geminiService)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _geminiService = geminiService;
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
    public async Task<ActionResult<Flyer>> UploadFlyer(
        [FromForm] IFormFile file,
        [FromForm] int eventId,
        [FromForm] int venueId,
        [FromForm] DateTime earliestClubNightDate)
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

        // Validate that Event and Venue exist
        var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId);
        if (!eventExists)
        {
            return BadRequest($"Event with ID {eventId} does not exist.");
        }

        var venueExists = await _context.Venues.AnyAsync(v => v.Id == venueId);
        if (!venueExists)
        {
            return BadRequest($"Venue with ID {venueId} does not exist.");
        }

        // Get Event and Venue names for folder structure
        var eventItem = await _context.Events.FindAsync(eventId);
        var venue = await _context.Venues.FindAsync(venueId);

        // Sanitize names for use in file paths
        var sanitizedEventName = SanitizeFileName(eventItem!.Name);
        var sanitizedVenueName = SanitizeFileName(venue!.Name);
        var dateFolder = earliestClubNightDate.ToString("yyyy-MM-dd");

        // Create folder structure: uploads/{event}/{venue}/{date}/
        var uploadsPath = Path.Combine(_environment.ContentRootPath, UploadsFolder, sanitizedEventName, sanitizedVenueName, dateFolder);
        Directory.CreateDirectory(uploadsPath);

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        // Save file to disk
        try
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file to disk");
            return StatusCode(500, "Error saving file to disk.");
        }

        // Create relative path for storage in database
        var relativePath = Path.Combine(UploadsFolder, sanitizedEventName, sanitizedVenueName, dateFolder, uniqueFileName);

        // Create Flyer entity
        var flyer = new Flyer
        {
            FilePath = relativePath,
            FileName = file.FileName,
            UploadedAt = DateTime.UtcNow,
            EventId = eventId,
            VenueId = venueId,
            EarliestClubNightDate = DateTime.SpecifyKind(earliestClubNightDate, DateTimeKind.Utc)
        };

        _context.Flyers.Add(flyer);
        await _context.SaveChangesAsync();

        // Return the created flyer with related entities
        var createdFlyer = await _context.Flyers
            .Include(f => f.Event)
            .Include(f => f.Venue)
            .FirstOrDefaultAsync(f => f.Id == flyer.Id);

        return CreatedAtAction(nameof(GetFlyer), new { id = flyer.Id }, createdFlyer);
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
                Message = analysisResult.ErrorMessage ?? "Failed to analyze flyer"
            });
        }

        var result = new AutoPopulateResult
        {
            Success = true,
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

                // Create ClubNight if date is provided
                if (clubNightData.Date.HasValue)
                {
                    // Note: AI returns dates without timezone info, treating as UTC
                    // For production, consider enhancing prompt to request timezone or infer from venue location
                    var clubNight = new ClubNight
                    {
                        Date = DateTime.SpecifyKind(clubNightData.Date.Value, DateTimeKind.Utc),
                        EventId = eventEntity.Id,
                        VenueId = venueEntity.Id,
                        FlyerId = flyer.Id
                    };

                    _context.ClubNights.Add(clubNight);
                    await _context.SaveChangesAsync();
                    result.ClubNightsCreated++;

                    // Add acts
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

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid characters and replace spaces with underscores
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(ch => !invalidChars.Contains(ch))
            .ToArray());
        return sanitized.Replace(" ", "_");
    }
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
}

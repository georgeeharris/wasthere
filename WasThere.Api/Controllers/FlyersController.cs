using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;

namespace WasThere.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlyersController : ControllerBase
{
    private readonly ClubEventContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FlyersController> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private const string UploadsFolder = "uploads";
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public FlyersController(
        ClubEventContext context, 
        IWebHostEnvironment environment,
        ILogger<FlyersController> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
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

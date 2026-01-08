using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace WasThere.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClubNightsController : ControllerBase
{
    private readonly ClubEventContext _context;

    public ClubNightsController(ClubEventContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetClubNights()
    {
        // Get admin user ID (hardcoded for now)
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        var adminUserId = adminUser?.Id ?? 0;
        
        var clubNights = await _context.ClubNights
            .Include(cn => cn.Event)
            .Include(cn => cn.Venue)
            .Include(cn => cn.Flyer)
            .Include(cn => cn.ClubNightActs)
                .ThenInclude(cna => cna.Act)
            .Include(cn => cn.Attendances)
            .Select(cn => new
            {
                cn.Id,
                cn.Date,
                EventId = cn.EventId,
                EventName = cn.Event!.Name,
                VenueId = cn.VenueId,
                VenueName = cn.Venue!.Name,
                FlyerId = cn.FlyerId,
                FlyerThumbnailPath = cn.Flyer != null ? cn.Flyer.ThumbnailPath : null,
                Acts = cn.ClubNightActs.Select(cna => new
                {
                    cna.ActId,
                    ActName = cna.Act!.Name,
                    cna.IsLiveSet
                }).ToList(),
                WasThereByAdmin = cn.Attendances.Any(a => a.UserId == adminUserId)
            })
            .ToListAsync();

        return Ok(clubNights);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetClubNight(int id)
    {
        // Get admin user ID (hardcoded for now)
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        var adminUserId = adminUser?.Id ?? 0;
        
        var clubNight = await _context.ClubNights
            .Include(cn => cn.Event)
            .Include(cn => cn.Venue)
            .Include(cn => cn.Flyer)
            .Include(cn => cn.ClubNightActs)
                .ThenInclude(cna => cna.Act)
            .Include(cn => cn.Attendances)
            .Where(cn => cn.Id == id)
            .Select(cn => new
            {
                cn.Id,
                cn.Date,
                EventId = cn.EventId,
                EventName = cn.Event!.Name,
                VenueId = cn.VenueId,
                VenueName = cn.Venue!.Name,
                FlyerId = cn.FlyerId,
                FlyerFilePath = cn.Flyer != null ? cn.Flyer.FilePath : null,
                FlyerThumbnailPath = cn.Flyer != null ? cn.Flyer.ThumbnailPath : null,
                Acts = cn.ClubNightActs.Select(cna => new
                {
                    cna.ActId,
                    ActName = cna.Act!.Name,
                    cna.IsLiveSet
                }).ToList(),
                WasThereByAdmin = cn.Attendances.Any(a => a.UserId == adminUserId)
            })
            .FirstOrDefaultAsync();

        if (clubNight == null)
        {
            return NotFound();
        }

        return Ok(clubNight);
    }

    [HttpPost]
    public async Task<ActionResult<ClubNight>> PostClubNight(ClubNightDto dto)
    {
        var clubNight = new ClubNight
        {
            Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
            EventId = dto.EventId,
            VenueId = dto.VenueId
        };

        _context.ClubNights.Add(clubNight);
        await _context.SaveChangesAsync();

        // Add acts
        if (dto.Acts != null && dto.Acts.Any())
        {
            foreach (var act in dto.Acts)
            {
                _context.ClubNightActs.Add(new ClubNightAct
                {
                    ClubNightId = clubNight.Id,
                    ActId = act.ActId,
                    IsLiveSet = act.IsLiveSet
                });
            }
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetClubNight), new { id = clubNight.Id }, clubNight);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutClubNight(int id, ClubNightDto dto)
    {
        var clubNight = await _context.ClubNights
            .Include(cn => cn.ClubNightActs)
            .FirstOrDefaultAsync(cn => cn.Id == id);

        if (clubNight == null)
        {
            return NotFound();
        }

        clubNight.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        clubNight.EventId = dto.EventId;
        clubNight.VenueId = dto.VenueId;

        // Update acts
        _context.ClubNightActs.RemoveRange(clubNight.ClubNightActs);
        
        if (dto.Acts != null && dto.Acts.Any())
        {
            foreach (var act in dto.Acts)
            {
                _context.ClubNightActs.Add(new ClubNightAct
                {
                    ClubNightId = clubNight.Id,
                    ActId = act.ActId,
                    IsLiveSet = act.IsLiveSet
                });
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClubNight(int id)
    {
        var clubNight = await _context.ClubNights.FindAsync(id);
        if (clubNight == null)
        {
            return NotFound();
        }

        _context.ClubNights.Remove(clubNight);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpPost("{id}/was-there")]
    [AllowAnonymous]
    public async Task<IActionResult> MarkWasThere(int id)
    {
        // Get admin user (hardcoded for now)
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser == null)
        {
            return BadRequest("Admin user not found. Please run migrations.");
        }
        
        var clubNight = await _context.ClubNights.FindAsync(id);
        if (clubNight == null)
        {
            return NotFound();
        }
        
        // Check if already marked
        var existing = await _context.UserClubNightAttendances
            .FirstOrDefaultAsync(a => a.UserId == adminUser.Id && a.ClubNightId == id);
        
        if (existing == null)
        {
            _context.UserClubNightAttendances.Add(new UserClubNightAttendance
            {
                UserId = adminUser.Id,
                ClubNightId = id,
                MarkedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        
        return Ok();
    }
    
    [HttpDelete("{id}/was-there")]
    [AllowAnonymous]
    public async Task<IActionResult> UnmarkWasThere(int id)
    {
        // Get admin user (hardcoded for now)
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser == null)
        {
            return BadRequest("Admin user not found. Please run migrations.");
        }
        
        var attendance = await _context.UserClubNightAttendances
            .FirstOrDefaultAsync(a => a.UserId == adminUser.Id && a.ClubNightId == id);
        
        if (attendance != null)
        {
            _context.UserClubNightAttendances.Remove(attendance);
            await _context.SaveChangesAsync();
        }
        
        return Ok();
    }
}

public class ClubNightDto
{
    public DateTime Date { get; set; }
    public int EventId { get; set; }
    public int VenueId { get; set; }
    public List<ClubNightActDto>? Acts { get; set; }
}

public class ClubNightActDto
{
    public int ActId { get; set; }
    public bool IsLiveSet { get; set; }
}

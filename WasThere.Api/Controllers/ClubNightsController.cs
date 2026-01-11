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
    
    private async Task<User?> GetOrCreateCurrentUserAsync()
    {
        // Try to get Auth0 user ID from claims
        var auth0UserId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        
        if (string.IsNullOrEmpty(auth0UserId))
        {
            // Fall back to admin user for unauthenticated requests (backward compatibility)
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        }
        
        // Try to find existing user by Auth0 ID
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId);
        
        if (user == null)
        {
            // Create new user without username - they must set it via profile page
            user = new User
            {
                Username = null,
                Auth0UserId = auth0UserId
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        
        return user;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetClubNights()
    {
        // Get current user (authenticated or fall back to admin)
        var currentUser = await GetOrCreateCurrentUserAsync();
        var userId = currentUser?.Id ?? 0;
        
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
                WasThereByAdmin = cn.Attendances.Any(a => a.UserId == userId)
            })
            .ToListAsync();

        return Ok(clubNights);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetClubNight(int id)
    {
        // Get current user (authenticated or fall back to admin)
        var currentUser = await GetOrCreateCurrentUserAsync();
        var userId = currentUser?.Id ?? 0;
        
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
                WasThereByAdmin = cn.Attendances.Any(a => a.UserId == userId)
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
    public async Task<IActionResult> MarkWasThere(int id)
    {
        // Get current authenticated user
        var currentUser = await GetOrCreateCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized("User must be authenticated to mark attendance.");
        }
        
        var clubNight = await _context.ClubNights.FindAsync(id);
        if (clubNight == null)
        {
            return NotFound();
        }
        
        // Check if already marked
        var existing = await _context.UserClubNightAttendances
            .FirstOrDefaultAsync(a => a.UserId == currentUser.Id && a.ClubNightId == id);
        
        if (existing == null)
        {
            _context.UserClubNightAttendances.Add(new UserClubNightAttendance
            {
                UserId = currentUser.Id,
                ClubNightId = id,
                MarkedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        
        return Ok();
    }
    
    [HttpDelete("{id}/was-there")]
    public async Task<IActionResult> UnmarkWasThere(int id)
    {
        // Get current authenticated user
        var currentUser = await GetOrCreateCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized("User must be authenticated to modify attendance.");
        }
        
        var attendance = await _context.UserClubNightAttendances
            .FirstOrDefaultAsync(a => a.UserId == currentUser.Id && a.ClubNightId == id);
        
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

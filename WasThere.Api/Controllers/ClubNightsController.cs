using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;

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
    public async Task<ActionResult<IEnumerable<object>>> GetClubNights()
    {
        var clubNights = await _context.ClubNights
            .Include(cn => cn.Event)
            .Include(cn => cn.Venue)
            .Include(cn => cn.ClubNightActs)
                .ThenInclude(cna => cna.Act)
            .Select(cn => new
            {
                cn.Id,
                cn.Date,
                EventId = cn.EventId,
                EventName = cn.Event!.Name,
                VenueId = cn.VenueId,
                VenueName = cn.Venue!.Name,
                Acts = cn.ClubNightActs.Select(cna => new
                {
                    cna.ActId,
                    ActName = cna.Act!.Name
                }).ToList()
            })
            .ToListAsync();

        return Ok(clubNights);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetClubNight(int id)
    {
        var clubNight = await _context.ClubNights
            .Include(cn => cn.Event)
            .Include(cn => cn.Venue)
            .Include(cn => cn.ClubNightActs)
                .ThenInclude(cna => cna.Act)
            .Where(cn => cn.Id == id)
            .Select(cn => new
            {
                cn.Id,
                cn.Date,
                EventId = cn.EventId,
                EventName = cn.Event!.Name,
                VenueId = cn.VenueId,
                VenueName = cn.Venue!.Name,
                Acts = cn.ClubNightActs.Select(cna => new
                {
                    cna.ActId,
                    ActName = cna.Act!.Name
                }).ToList()
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
        if (dto.ActIds != null && dto.ActIds.Any())
        {
            foreach (var actId in dto.ActIds)
            {
                _context.ClubNightActs.Add(new ClubNightAct
                {
                    ClubNightId = clubNight.Id,
                    ActId = actId
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
        
        if (dto.ActIds != null && dto.ActIds.Any())
        {
            foreach (var actId in dto.ActIds)
            {
                _context.ClubNightActs.Add(new ClubNightAct
                {
                    ClubNightId = clubNight.Id,
                    ActId = actId
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
}

public class ClubNightDto
{
    public DateTime Date { get; set; }
    public int EventId { get; set; }
    public int VenueId { get; set; }
    public List<int>? ActIds { get; set; }
}

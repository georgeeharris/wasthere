using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace WasThere.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VenuesController : ControllerBase
{
    private readonly ClubEventContext _context;

    public VenuesController(ClubEventContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Venue>>> GetVenues()
    {
        return await _context.Venues.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Venue>> GetVenue(int id)
    {
        var venue = await _context.Venues.FindAsync(id);

        if (venue == null)
        {
            return NotFound();
        }

        return venue;
    }

    [HttpPost]
    public async Task<ActionResult<Venue>> PostVenue(Venue venue)
    {
        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVenue), new { id = venue.Id }, venue);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutVenue(int id, Venue venue)
    {
        if (id != venue.Id)
        {
            return BadRequest();
        }

        _context.Entry(venue).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!VenueExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVenue(int id)
    {
        var venue = await _context.Venues.FindAsync(id);
        if (venue == null)
        {
            return NotFound();
        }

        _context.Venues.Remove(venue);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool VenueExists(int id)
    {
        return _context.Venues.Any(e => e.Id == id);
    }
}

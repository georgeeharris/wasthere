using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace WasThere.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ClubEventContext _context;

    public EventsController(ClubEventContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        return await _context.Events.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEvent(int id)
    {
        var eventItem = await _context.Events.FindAsync(id);

        if (eventItem == null)
        {
            return NotFound();
        }

        return eventItem;
    }

    [HttpPost]
    public async Task<ActionResult<Event>> PostEvent(Event eventItem)
    {
        _context.Events.Add(eventItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = eventItem.Id }, eventItem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutEvent(int id, Event eventItem)
    {
        if (id != eventItem.Id)
        {
            return BadRequest();
        }

        _context.Entry(eventItem).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EventExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpGet("{id}/delete-impact")]
    public async Task<ActionResult<object>> GetDeleteImpact(int id)
    {
        var eventItem = await _context.Events.FindAsync(id);
        if (eventItem == null)
        {
            return NotFound();
        }

        var clubNightsCount = await _context.ClubNights.CountAsync(cn => cn.EventId == id);
        var flyersCount = await _context.Flyers.CountAsync(f => f.EventId == id);

        return Ok(new
        {
            clubNightsCount,
            flyersCount
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var eventItem = await _context.Events.FindAsync(id);
        if (eventItem == null)
        {
            return NotFound();
        }

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool EventExists(int id)
    {
        return _context.Events.Any(e => e.Id == id);
    }
}

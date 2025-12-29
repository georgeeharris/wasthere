using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;

namespace WasThere.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActsController : ControllerBase
{
    private readonly ClubEventContext _context;

    public ActsController(ClubEventContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Act>>> GetActs()
    {
        return await _context.Acts.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Act>> GetAct(int id)
    {
        var act = await _context.Acts.FindAsync(id);

        if (act == null)
        {
            return NotFound();
        }

        return act;
    }

    [HttpPost]
    public async Task<ActionResult<Act>> PostAct(Act act)
    {
        _context.Acts.Add(act);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAct), new { id = act.Id }, act);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAct(int id, Act act)
    {
        if (id != act.Id)
        {
            return BadRequest();
        }

        _context.Entry(act).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ActExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAct(int id)
    {
        var act = await _context.Acts.FindAsync(id);
        if (act == null)
        {
            return NotFound();
        }

        _context.Acts.Remove(act);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ActExists(int id)
    {
        return _context.Acts.Any(e => e.Id == id);
    }
}

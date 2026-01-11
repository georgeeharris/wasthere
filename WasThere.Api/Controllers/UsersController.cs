using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;

namespace WasThere.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ClubEventContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ClubEventContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string? GetAuth0UserId()
    {
        return User.FindFirst("sub")?.Value ?? 
               User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var auth0UserId = GetAuth0UserId();
        
        if (string.IsNullOrEmpty(auth0UserId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId);
        
        if (user == null)
        {
            // Create a new user without a username
            user = new User
            {
                Auth0UserId = auth0UserId,
                Username = null
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Auth0UserId = user.Auth0UserId
        });
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var auth0UserId = GetAuth0UserId();
        
        if (string.IsNullOrEmpty(auth0UserId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Validate username
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        // Username validation rules
        if (dto.Username.Length < 3 || dto.Username.Length > 20)
        {
            return BadRequest(new { message = "Username must be between 3 and 20 characters" });
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Username, @"^[a-zA-Z0-9_-]+$"))
        {
            return BadRequest(new { message = "Username can only contain letters, numbers, hyphens, and underscores" });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Check if username is already taken by another user
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username && u.Id != user.Id);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Username is already taken" });
        }

        user.Username = dto.Username;
        await _context.SaveChangesAsync();

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Auth0UserId = user.Auth0UserId
        });
    }

    [HttpGet("check-username/{username}")]
    public async Task<ActionResult<UsernameAvailabilityDto>> CheckUsername(string username)
    {
        // Validate username format
        if (string.IsNullOrWhiteSpace(username))
        {
            return Ok(new UsernameAvailabilityDto { Available = false, Message = "Username cannot be empty" });
        }

        if (username.Length < 3 || username.Length > 20)
        {
            return Ok(new UsernameAvailabilityDto { Available = false, Message = "Username must be between 3 and 20 characters" });
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$"))
        {
            return Ok(new UsernameAvailabilityDto { Available = false, Message = "Username can only contain letters, numbers, hyphens, and underscores" });
        }

        var exists = await _context.Users.AnyAsync(u => u.Username == username);
        
        return Ok(new UsernameAvailabilityDto 
        { 
            Available = !exists,
            Message = exists ? "Username is already taken" : "Username is available"
        });
    }
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Auth0UserId { get; set; }
}

public class UpdateProfileDto
{
    public required string Username { get; set; }
}

public class UsernameAvailabilityDto
{
    public bool Available { get; set; }
    public string? Message { get; set; }
}

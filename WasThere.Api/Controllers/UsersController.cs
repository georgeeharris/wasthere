using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Models;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace WasThere.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public partial class UsersController : ControllerBase
{
    private readonly ClubEventContext _context;
    private readonly ILogger<UsersController> _logger;
    
    // Compiled regex for username validation
    [GeneratedRegex(@"^[a-zA-Z0-9_-]+$")]
    private static partial Regex UsernameValidationRegex();

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

    private (bool IsValid, string? ErrorMessage) ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return (false, "Username cannot be empty");
        }

        if (username.Length < 3 || username.Length > 20)
        {
            return (false, "Username must be between 3 and 20 characters");
        }

        if (!UsernameValidationRegex().IsMatch(username))
        {
            return (false, "Username can only contain letters, numbers, hyphens, and underscores");
        }

        return (true, null);
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
        var (isValid, errorMessage) = ValidateUsername(dto.Username);
        if (!isValid)
        {
            return BadRequest(new { message = errorMessage });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Check if username is already taken by another user (case-insensitive)
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => 
            u.Username != null && 
            u.Username.ToLower() == dto.Username.ToLower() && 
            u.Id != user.Id);
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
        var (isValid, errorMessage) = ValidateUsername(username);
        if (!isValid)
        {
            return Ok(new UsernameAvailabilityDto { Available = false, Message = errorMessage });
        }

        // Check if username exists (case-insensitive)
        var exists = await _context.Users.AnyAsync(u => u.Username != null && u.Username.ToLower() == username.ToLower());
        
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
    [Required(ErrorMessage = "Username is required")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 20 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, hyphens, and underscores")]
    public required string Username { get; set; }
}

public class UsernameAvailabilityDto
{
    public bool Available { get; set; }
    public string? Message { get; set; }
}

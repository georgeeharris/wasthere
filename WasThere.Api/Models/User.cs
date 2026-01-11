namespace WasThere.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public string? Auth0UserId { get; set; }  // Auth0 "sub" claim (e.g., "auth0|...")
    
    public ICollection<UserClubNightAttendance> Attendances { get; set; } = new List<UserClubNightAttendance>();
}

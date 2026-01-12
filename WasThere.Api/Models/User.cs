namespace WasThere.Api.Models;

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }  // Nullable to allow users to set it after account creation
    public string? Auth0UserId { get; set; }  // Auth0 "sub" claim (e.g., "auth0|...")
    
    public ICollection<UserClubNightAttendance> Attendances { get; set; } = new List<UserClubNightAttendance>();
    public ICollection<ClubNightPost> Posts { get; set; } = new List<ClubNightPost>();
}

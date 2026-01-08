namespace WasThere.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    
    public ICollection<UserClubNightAttendance> Attendances { get; set; } = new List<UserClubNightAttendance>();
}

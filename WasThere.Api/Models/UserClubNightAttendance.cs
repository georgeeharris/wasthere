namespace WasThere.Api.Models;

public class UserClubNightAttendance
{
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int ClubNightId { get; set; }
    public ClubNight? ClubNight { get; set; }
    
    public DateTime MarkedAt { get; set; }
}

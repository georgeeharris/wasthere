namespace WasThere.Api.Models;

public class ClubNight
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    
    public int EventId { get; set; }
    public Event? Event { get; set; }
    
    public int VenueId { get; set; }
    public Venue? Venue { get; set; }
    
    public int? FlyerId { get; set; }
    public Flyer? Flyer { get; set; }
    
    public ICollection<ClubNightAct> ClubNightActs { get; set; } = new List<ClubNightAct>();
    public ICollection<UserClubNightAttendance> Attendances { get; set; } = new List<UserClubNightAttendance>();
}

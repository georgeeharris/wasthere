namespace WasThere.Api.Models;

public class ClubNight
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    
    public int EventId { get; set; }
    public Event? Event { get; set; }
    
    public int VenueId { get; set; }
    public Venue? Venue { get; set; }
    
    public ICollection<ClubNightAct> ClubNightActs { get; set; } = new List<ClubNightAct>();
}

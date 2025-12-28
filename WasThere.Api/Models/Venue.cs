namespace WasThere.Api.Models;

public class Venue
{
    public int Id { get; set; }
    public required string Name { get; set; }
    
    public ICollection<ClubNight> ClubNights { get; set; } = new List<ClubNight>();
}

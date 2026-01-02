namespace WasThere.Api.Models;

public class Flyer
{
    public int Id { get; set; }
    public required string FilePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public required string FileName { get; set; }
    public DateTime UploadedAt { get; set; }
    
    public int EventId { get; set; }
    public Event? Event { get; set; }
    
    public int VenueId { get; set; }
    public Venue? Venue { get; set; }
    
    public DateTime EarliestClubNightDate { get; set; }
    
    public ICollection<ClubNight> ClubNights { get; set; } = new List<ClubNight>();
}

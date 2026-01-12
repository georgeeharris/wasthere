namespace WasThere.Api.Models;

public class ClubNightPost
{
    public int Id { get; set; }
    public int ClubNightId { get; set; }
    public ClubNight? ClubNight { get; set; }
    
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    // For quoted replies
    public int? QuotedPostId { get; set; }
    public ClubNightPost? QuotedPost { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

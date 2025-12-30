namespace WasThere.Api.Models;

public class ClubNightAct
{
    public int ClubNightId { get; set; }
    public ClubNight? ClubNight { get; set; }
    
    public int ActId { get; set; }
    public Act? Act { get; set; }
    
    public bool IsLiveSet { get; set; }
}

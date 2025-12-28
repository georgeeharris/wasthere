namespace WasThere.Api.Models;

public class Act
{
    public int Id { get; set; }
    public required string Name { get; set; }
    
    public ICollection<ClubNightAct> ClubNightActs { get; set; } = new List<ClubNightAct>();
}

namespace WasThere.Api.Services;

public interface IGoogleGeminiService
{
    Task<FlyerAnalysisResult> AnalyzeFlyerImageAsync(string imagePath);
}

public class FlyerAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ClubNightData> ClubNights { get; set; } = new();
}

public class ClubNightData
{
    public string? EventName { get; set; }
    public string? VenueName { get; set; }
    public DateTime? Date { get; set; }
    public List<string> Acts { get; set; } = new();
}

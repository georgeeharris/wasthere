using System.Text.Json.Serialization;
using WasThere.Api.Models;

namespace WasThere.Api.Services;

public interface IGoogleGeminiService
{
    Task<FlyerAnalysisResult> AnalyzeFlyerImageAsync(string imagePath);
}

public class FlyerAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FlyerData> Flyers { get; set; } = new();
    public DiagnosticInfo Diagnostics { get; set; } = new();
    
    // For logging purposes
    public string? GeminiPrompt { get; set; }
    public string? GeminiRawResponse { get; set; }
    public long ImageSizeBytes { get; set; }
    public string? ImageMimeType { get; set; }
    
    // Legacy property for backward compatibility - returns all club nights from all flyers
    public List<ClubNightData> ClubNights => Flyers.SelectMany(f => f.ClubNights).ToList();
}

public class FlyerData
{
    [JsonPropertyName("clubNights")]
    public List<ClubNightData> ClubNights { get; set; } = new();
}

public class ClubNightData
{
    public string? EventName { get; set; }
    public string? VenueName { get; set; }
    public DateTime? Date { get; set; }
    
    // For partial dates when year is not on flyer
    public string? DayOfWeek { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
    
    // Candidate years for user selection (populated after analysis)
    public List<int> CandidateYears { get; set; } = new();
    
    public List<ActData> Acts { get; set; } = new();
}

public class ActData
{
    public string Name { get; set; } = string.Empty;
    public bool IsLiveSet { get; set; }
}

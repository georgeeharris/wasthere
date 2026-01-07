namespace WasThere.Api.Models;

public class DiagnosticInfo
{
    public string? LogId { get; set; }
    public List<DiagnosticStep> Steps { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
}

public class DiagnosticStep
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "started", "completed", "failed"
    public DateTime Timestamp { get; set; }
    public long? DurationMs { get; set; }
    public string? Details { get; set; }
    public string? Error { get; set; }
}

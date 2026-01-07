using System.Text;
using System.Text.Json;
using WasThere.Api.Models;

namespace WasThere.Api.Services;

/// <summary>
/// Service for logging detailed flyer conversion operations to timestamped files
/// </summary>
public class FlyerConversionLogger : IFlyerConversionLogger
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FlyerConversionLogger> _logger;
    private const string LogsFolder = "logs";
    private readonly Dictionary<string, StreamWriter> _activeLoggers = new();
    private readonly Dictionary<string, DateTime> _startTimes = new();
    private readonly object _lock = new();

    public FlyerConversionLogger(IWebHostEnvironment environment, ILogger<FlyerConversionLogger> logger)
    {
        _environment = environment;
        _logger = logger;
        
        // Ensure logs directory exists
        var logsPath = Path.Combine(_environment.ContentRootPath, LogsFolder);
        Directory.CreateDirectory(logsPath);
    }

    public string StartConversionLog(string imagePath, string fileName)
    {
        var timestamp = DateTime.UtcNow;
        var logId = $"{timestamp:yyyyMMdd-HHmmss-fff}";
        var logFileName = $"flyer-conversion-{logId}.log";
        var logsPath = Path.Combine(_environment.ContentRootPath, LogsFolder);
        var logFilePath = Path.Combine(logsPath, logFileName);

        lock (_lock)
        {
            _startTimes[logId] = timestamp;
            var writer = new StreamWriter(logFilePath, append: false, Encoding.UTF8)
            {
                AutoFlush = true
            };
            _activeLoggers[logId] = writer;

            WriteLogEntry(logId, "=== FLYER CONVERSION LOG START ===");
            WriteLogEntry(logId, $"Log ID: {logId}");
            WriteLogEntry(logId, $"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC");
            WriteLogEntry(logId, $"Image Path: {imagePath}");
            WriteLogEntry(logId, $"File Name: {fileName}");
            WriteLogEntry(logId, "");
        }

        return logId;
    }

    public string? GetLogFilePath(string logId)
    {
        var logFileName = $"flyer-conversion-{logId}.log";
        var logsPath = Path.Combine(_environment.ContentRootPath, LogsFolder);
        var logFilePath = Path.Combine(logsPath, logFileName);
        
        return System.IO.File.Exists(logFilePath) ? logFilePath : null;
    }

    public void LogGeminiRequest(string logId, string prompt, string imagePath, long imageSizeBytes, string mimeType)
    {
        lock (_lock)
        {
            WriteLogEntry(logId, "--- GEMINI API REQUEST ---");
            WriteLogEntry(logId, $"Image Path: {imagePath}");
            WriteLogEntry(logId, $"Image Size: {imageSizeBytes} bytes ({imageSizeBytes / 1024.0:F2} KB)");
            WriteLogEntry(logId, $"MIME Type: {mimeType}");
            WriteLogEntry(logId, "");
            WriteLogEntry(logId, "Prompt:");
            WriteLogEntry(logId, prompt);
            WriteLogEntry(logId, "");
        }
    }

    public void LogGeminiResponse(string logId, string rawResponse, bool success, string? errorMessage)
    {
        lock (_lock)
        {
            WriteLogEntry(logId, "--- GEMINI API RESPONSE ---");
            WriteLogEntry(logId, $"Success: {success}");
            
            if (!success && !string.IsNullOrEmpty(errorMessage))
            {
                WriteLogEntry(logId, $"Error: {errorMessage}");
            }
            
            WriteLogEntry(logId, "");
            WriteLogEntry(logId, "Raw Response:");
            WriteLogEntry(logId, rawResponse);
            WriteLogEntry(logId, "");
        }
    }

    public void LogAnalysisResult(string logId, FlyerAnalysisResult analysisResult)
    {
        lock (_lock)
        {
            WriteLogEntry(logId, "--- ANALYSIS RESULT ---");
            WriteLogEntry(logId, $"Success: {analysisResult.Success}");
            
            if (!analysisResult.Success)
            {
                WriteLogEntry(logId, $"Error Message: {analysisResult.ErrorMessage}");
            }
            
            WriteLogEntry(logId, $"Club Nights Found: {analysisResult.ClubNights.Count}");
            WriteLogEntry(logId, "");
            
            // Log parsed club nights
            for (int i = 0; i < analysisResult.ClubNights.Count; i++)
            {
                var clubNight = analysisResult.ClubNights[i];
                WriteLogEntry(logId, $"Club Night {i + 1}:");
                WriteLogEntry(logId, $"  Event Name: {clubNight.EventName}");
                WriteLogEntry(logId, $"  Venue Name: {clubNight.VenueName}");
                WriteLogEntry(logId, $"  Date: {clubNight.Date?.ToString("yyyy-MM-dd") ?? "null"}");
                WriteLogEntry(logId, $"  Day of Week: {clubNight.DayOfWeek ?? "null"}");
                WriteLogEntry(logId, $"  Month: {clubNight.Month?.ToString() ?? "null"}");
                WriteLogEntry(logId, $"  Day: {clubNight.Day?.ToString() ?? "null"}");
                WriteLogEntry(logId, $"  Candidate Years: {string.Join(", ", clubNight.CandidateYears)}");
                WriteLogEntry(logId, $"  Acts Count: {clubNight.Acts.Count}");
                
                if (clubNight.Acts.Any())
                {
                    WriteLogEntry(logId, "  Acts:");
                    foreach (var act in clubNight.Acts)
                    {
                        WriteLogEntry(logId, $"    - {act.Name} (Live Set: {act.IsLiveSet})");
                    }
                }
                WriteLogEntry(logId, "");
            }
            
            // Log diagnostics if available
            if (analysisResult.Diagnostics != null)
            {
                WriteLogEntry(logId, "Diagnostics:");
                WriteLogEntry(logId, $"  Steps: {analysisResult.Diagnostics.Steps.Count}");
                
                foreach (var step in analysisResult.Diagnostics.Steps)
                {
                    WriteLogEntry(logId, $"  - {step.Name}: {step.Status} ({step.DurationMs}ms)");
                    if (!string.IsNullOrEmpty(step.Details))
                    {
                        WriteLogEntry(logId, $"    Details: {step.Details}");
                    }
                    if (!string.IsNullOrEmpty(step.Error))
                    {
                        WriteLogEntry(logId, $"    Error: {step.Error}");
                    }
                }
                
                WriteLogEntry(logId, "");
                WriteLogEntry(logId, "Metadata:");
                foreach (var metadata in analysisResult.Diagnostics.Metadata)
                {
                    WriteLogEntry(logId, $"  {metadata.Key}: {metadata.Value}");
                }
            }
            WriteLogEntry(logId, "");
        }
    }

    public void LogUserYearSelection(string logId, List<YearSelection> selectedYears)
    {
        lock (_lock)
        {
            WriteLogEntry(logId, "--- USER YEAR SELECTION ---");
            WriteLogEntry(logId, $"Selected Years Count: {selectedYears.Count}");
            
            foreach (var selection in selectedYears)
            {
                WriteLogEntry(logId, $"  {selection.Month}/{selection.Day} -> Year: {selection.Year}");
            }
            WriteLogEntry(logId, "");
        }
    }

    public void LogDatabaseOperation(string logId, string operationType, string entityType, string entityName, int? entityId = null)
    {
        lock (_lock)
        {
            var idInfo = entityId.HasValue ? $" (ID: {entityId.Value})" : "";
            WriteLogEntry(logId, $"DB {operationType}: {entityType} - {entityName}{idInfo}");
        }
    }

    public void CompleteConversionLog(string logId, bool success, string summary, 
        int eventsCreated = 0, int venuesCreated = 0, int actsCreated = 0, int clubNightsCreated = 0)
    {
        lock (_lock)
        {
            WriteLogEntry(logId, "");
            WriteLogEntry(logId, "--- CONVERSION SUMMARY ---");
            WriteLogEntry(logId, $"Success: {success}");
            WriteLogEntry(logId, $"Summary: {summary}");
            WriteLogEntry(logId, "");
            WriteLogEntry(logId, "Database Operations:");
            WriteLogEntry(logId, $"  Events Created: {eventsCreated}");
            WriteLogEntry(logId, $"  Venues Created: {venuesCreated}");
            WriteLogEntry(logId, $"  Acts Created: {actsCreated}");
            WriteLogEntry(logId, $"  Club Nights Created: {clubNightsCreated}");
            WriteLogEntry(logId, "");
            
            if (_startTimes.TryGetValue(logId, out var startTime))
            {
                var duration = DateTime.UtcNow - startTime;
                WriteLogEntry(logId, $"Total Duration: {duration.TotalSeconds:F2} seconds");
            }
            
            WriteLogEntry(logId, $"End Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
            WriteLogEntry(logId, "=== FLYER CONVERSION LOG END ===");
            
            // Close the log file
            if (_activeLoggers.TryGetValue(logId, out var writer))
            {
                writer.Dispose();
                _activeLoggers.Remove(logId);
            }
            
            _startTimes.Remove(logId);
        }
    }

    public void LogError(string logId, string errorMessage, Exception? exception = null)
    {
        lock (_lock)
        {
            WriteLogEntry(logId, "!!! ERROR !!!");
            WriteLogEntry(logId, $"Error Message: {errorMessage}");
            
            if (exception != null)
            {
                WriteLogEntry(logId, $"Exception Type: {exception.GetType().Name}");
                WriteLogEntry(logId, $"Exception Message: {exception.Message}");
                
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    WriteLogEntry(logId, "Stack Trace:");
                    WriteLogEntry(logId, exception.StackTrace);
                }
                
                if (exception.InnerException != null)
                {
                    WriteLogEntry(logId, $"Inner Exception: {exception.InnerException.Message}");
                }
            }
            WriteLogEntry(logId, "");
        }
    }

    private void WriteLogEntry(string logId, string message)
    {
        if (_activeLoggers.TryGetValue(logId, out var writer))
        {
            try
            {
                writer.WriteLine(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to log file for log ID: {LogId}", logId);
            }
        }
    }
}

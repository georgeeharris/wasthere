using WasThere.Api.Models;

namespace WasThere.Api.Services;

/// <summary>
/// Interface for logging detailed flyer conversion operations
/// </summary>
public interface IFlyerConversionLogger
{
    /// <summary>
    /// Start a new conversion log session and return the log ID
    /// </summary>
    string StartConversionLog(string imagePath, string fileName);
    
    /// <summary>
    /// Get the log file path for a given log ID
    /// </summary>
    string? GetLogFilePath(string logId);
    
    /// <summary>
    /// Log the Gemini API request details
    /// </summary>
    void LogGeminiRequest(string logId, string prompt, string imagePath, long imageSizeBytes, string mimeType);
    
    /// <summary>
    /// Log the Gemini API response details
    /// </summary>
    void LogGeminiResponse(string logId, string rawResponse, bool success, string? errorMessage);
    
    /// <summary>
    /// Log the parsed analysis result
    /// </summary>
    void LogAnalysisResult(string logId, FlyerAnalysisResult analysisResult);
    
    /// <summary>
    /// Log user-selected years for partial dates
    /// </summary>
    void LogUserYearSelection(string logId, List<YearSelection> selectedYears);
    
    /// <summary>
    /// Log database operations (creates, updates, deletes)
    /// </summary>
    void LogDatabaseOperation(string logId, string operationType, string entityType, string entityName, int? entityId = null);
    
    /// <summary>
    /// Complete the conversion log with a summary
    /// </summary>
    void CompleteConversionLog(string logId, bool success, string summary, 
        int eventsCreated = 0, int venuesCreated = 0, int actsCreated = 0, int clubNightsCreated = 0);
    
    /// <summary>
    /// Log an error in the conversion process
    /// </summary>
    void LogError(string logId, string errorMessage, Exception? exception = null);
}

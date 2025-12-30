using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WasThere.Api.Services;
using Xunit;
using Xunit.Abstractions;

namespace WasThere.Api.IntegrationTests;

/// <summary>
/// Integration tests that call the real Google Gemini API to analyze flyer images.
/// These tests help diagnose null reference exceptions and validate the Gemini integration.
/// </summary>
public class FlyerGeminiIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<GoogleGeminiService> _logger;
    private readonly string _apiKey;
    private readonly string _flyerImagePath;

    public FlyerGeminiIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Setup logger to output to test results
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        _logger = loggerFactory.CreateLogger<GoogleGeminiService>();

        // Get API key from environment variable or configuration
        _apiKey = Environment.GetEnvironmentVariable("GOOGLE_GEMINI_API_KEY") 
                  ?? Environment.GetEnvironmentVariable("GoogleGemini__ApiKey")
                  ?? string.Empty;

        // Path to the test flyer image
        var repoRoot = FindRepositoryRoot();
        _flyerImagePath = Path.Combine(repoRoot, "HiRes-2.jpg");

        _output.WriteLine($"Repository root: {repoRoot}");
        _output.WriteLine($"Flyer image path: {_flyerImagePath}");
        _output.WriteLine($"API key configured: {!string.IsNullOrEmpty(_apiKey)}");
        _output.WriteLine($"Image file exists: {File.Exists(_flyerImagePath)}");
    }

    private string FindRepositoryRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "HiRes-2.jpg")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        return currentDir ?? Directory.GetCurrentDirectory();
    }

    [Fact(Skip = "Integration test that requires GOOGLE_GEMINI_API_KEY environment variable")]
    public async Task AnalyzeFlyerImage_WithRealGeminiApi_ShouldReturnValidResult()
    {
        // Arrange
        if (string.IsNullOrEmpty(_apiKey))
        {
            _output.WriteLine("SKIPPED: GOOGLE_GEMINI_API_KEY environment variable not set");
            return;
        }

        if (!File.Exists(_flyerImagePath))
        {
            _output.WriteLine($"SKIPPED: Flyer image not found at {_flyerImagePath}");
            return;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "GoogleGemini:ApiKey", _apiKey }
            })
            .Build();

        var geminiService = new GoogleGeminiService(configuration, _logger);

        // Act
        _output.WriteLine($"Analyzing flyer image: {_flyerImagePath}");
        var result = await geminiService.AnalyzeFlyerImageAsync(_flyerImagePath);

        // Assert and Log
        _output.WriteLine($"Analysis Success: {result.Success}");
        
        if (!result.Success)
        {
            _output.WriteLine($"Error Message: {result.ErrorMessage}");
            Assert.Fail($"Analysis failed: {result.ErrorMessage}");
        }

        Assert.True(result.Success, "Analysis should succeed");
        Assert.NotNull(result.ClubNights);
        _output.WriteLine($"Number of club nights found: {result.ClubNights.Count}");

        foreach (var clubNight in result.ClubNights)
        {
            _output.WriteLine($"\nClub Night:");
            _output.WriteLine($"  Event Name: {clubNight.EventName ?? "NULL"}");
            _output.WriteLine($"  Venue Name: {clubNight.VenueName ?? "NULL"}");
            _output.WriteLine($"  Date: {clubNight.Date?.ToString("yyyy-MM-dd") ?? "NULL"}");
            _output.WriteLine($"  Day of Week: {clubNight.DayOfWeek ?? "NULL"}");
            _output.WriteLine($"  Month: {clubNight.Month?.ToString() ?? "NULL"}");
            _output.WriteLine($"  Day: {clubNight.Day?.ToString() ?? "NULL"}");
            
            // Check for null values that could cause NullReferenceException
            if (clubNight.EventName == null)
            {
                _output.WriteLine("  WARNING: EventName is NULL - could cause NullReferenceException");
            }
            if (clubNight.VenueName == null)
            {
                _output.WriteLine("  WARNING: VenueName is NULL - could cause NullReferenceException");
            }

            if (clubNight.Acts != null)
            {
                _output.WriteLine($"  Number of acts: {clubNight.Acts.Count}");
                foreach (var act in clubNight.Acts)
                {
                    if (act == null)
                    {
                        _output.WriteLine("  WARNING: Act is NULL - this WILL cause NullReferenceException");
                        continue;
                    }
                    
                    _output.WriteLine($"    - {act.Name ?? "NULL"} (Live Set: {act.IsLiveSet})");
                    
                    if (act.Name == null)
                    {
                        _output.WriteLine("      WARNING: Act.Name is NULL - could cause NullReferenceException");
                    }
                }
            }
            else
            {
                _output.WriteLine("  Acts collection is NULL");
            }
        }

        // Validate that we have at least some data
        Assert.True(result.ClubNights.Count > 0, "Should have at least one club night");
    }

    /// <summary>
    /// This test can be manually run by removing the Skip attribute when GOOGLE_GEMINI_API_KEY is set.
    /// It helps diagnose the exact null reference exception that occurs in production.
    /// </summary>
    [Fact]
    public async Task DiagnoseNullReferenceException_WithRealApi()
    {
        // Arrange
        var hasApiKey = !string.IsNullOrEmpty(_apiKey);
        var imageExists = File.Exists(_flyerImagePath);

        _output.WriteLine("=== DIAGNOSTIC TEST FOR NULL REFERENCE EXCEPTION ===");
        _output.WriteLine($"API Key Set: {hasApiKey}");
        _output.WriteLine($"Image Exists: {imageExists}");

        if (!hasApiKey)
        {
            _output.WriteLine("\nTo run this test, set the GOOGLE_GEMINI_API_KEY environment variable:");
            _output.WriteLine("  Linux/Mac: export GOOGLE_GEMINI_API_KEY=\"your-key\"");
            _output.WriteLine("  Windows: set GOOGLE_GEMINI_API_KEY=your-key");
            _output.WriteLine("  GitHub Actions: Already configured as repository secret");
            
            // Skip but don't fail
            return;
        }

        if (!imageExists)
        {
            _output.WriteLine($"\nImage not found at: {_flyerImagePath}");
            _output.WriteLine("Expected image: HiRes-2.jpg in repository root");
            return;
        }

        // Act - Try to reproduce the production scenario
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "GoogleGemini:ApiKey", _apiKey }
                })
                .Build();

            var geminiService = new GoogleGeminiService(configuration, _logger);
            
            _output.WriteLine("\n=== CALLING GEMINI API ===");
            var result = await geminiService.AnalyzeFlyerImageAsync(_flyerImagePath);

            _output.WriteLine($"\n=== ANALYSIS RESULT ===");
            _output.WriteLine($"Success: {result.Success}");
            _output.WriteLine($"Error Message: {result.ErrorMessage ?? "None"}");
            _output.WriteLine($"Club Nights Count: {result.ClubNights?.Count ?? 0}");

            if (result.Success && result.ClubNights != null)
            {
                _output.WriteLine("\n=== CHECKING FOR NULL VALUES ===");
                
                for (int i = 0; i < result.ClubNights.Count; i++)
                {
                    var clubNight = result.ClubNights[i];
                    _output.WriteLine($"\nClub Night {i + 1}:");
                    
                    // Check all fields that could be null
                    _output.WriteLine($"  EventName: {(clubNight.EventName == null ? "NULL ❌" : $"\"{clubNight.EventName}\" ✓")}");
                    _output.WriteLine($"  VenueName: {(clubNight.VenueName == null ? "NULL ❌" : $"\"{clubNight.VenueName}\" ✓")}");
                    _output.WriteLine($"  Date: {(clubNight.Date == null ? "NULL" : clubNight.Date.Value.ToString("yyyy-MM-dd"))}");
                    _output.WriteLine($"  DayOfWeek: {(clubNight.DayOfWeek == null ? "NULL" : clubNight.DayOfWeek)}");
                    _output.WriteLine($"  Month: {(clubNight.Month == null ? "NULL" : clubNight.Month.ToString())}");
                    _output.WriteLine($"  Day: {(clubNight.Day == null ? "NULL" : clubNight.Day.ToString())}");
                    _output.WriteLine($"  Acts: {(clubNight.Acts == null ? "NULL ❌" : $"Count={clubNight.Acts.Count} ✓")}");
                    
                    if (clubNight.Acts != null)
                    {
                        for (int j = 0; j < clubNight.Acts.Count; j++)
                        {
                            var act = clubNight.Acts[j];
                            if (act == null)
                            {
                                _output.WriteLine($"    Act {j + 1}: NULL ❌ - THIS WILL CAUSE NullReferenceException!");
                            }
                            else
                            {
                                _output.WriteLine($"    Act {j + 1}: Name={(act.Name == null ? "NULL ❌" : $"\"{act.Name}\" ✓")}, IsLiveSet={act.IsLiveSet}");
                            }
                        }
                    }
                }

                // Simulate what the controller does
                _output.WriteLine("\n=== SIMULATING CONTROLLER CODE ===");
                var firstClubNight = result.ClubNights[0];
                
                _output.WriteLine("Attempting: firstClubNight.EventName?.Trim()");
                var eventName = firstClubNight.EventName?.Trim();
                _output.WriteLine($"Result: {(eventName == null ? "NULL" : $"\"{eventName}\"")}");
                
                _output.WriteLine("\nAttempting: string.IsNullOrEmpty(eventName)");
                var isNullOrEmpty = string.IsNullOrEmpty(eventName);
                _output.WriteLine($"Result: {isNullOrEmpty}");

                if (firstClubNight.Acts != null)
                {
                    _output.WriteLine($"\nIterating through {firstClubNight.Acts.Count} acts:");
                    foreach (var act in firstClubNight.Acts)
                    {
                        if (act == null)
                        {
                            _output.WriteLine("  Found NULL act - this causes NullReferenceException!");
                        }
                        else
                        {
                            _output.WriteLine($"  Attempting: act.Name?.Trim()");
                            var actName = act.Name?.Trim();
                            _output.WriteLine($"  Result: {(actName == null ? "NULL" : $"\"{actName}\"")}");
                        }
                    }
                }

                _output.WriteLine("\n=== TEST COMPLETED SUCCESSFULLY ===");
                _output.WriteLine("If you see NULL values above, those are the likely cause of NullReferenceException");
            }
            else
            {
                _output.WriteLine("\n=== ANALYSIS FAILED ===");
                _output.WriteLine($"This might be why the production code is failing");
                _output.WriteLine($"Error: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"\n=== EXCEPTION CAUGHT ===");
            _output.WriteLine($"Type: {ex.GetType().Name}");
            _output.WriteLine($"Message: {ex.Message}");
            _output.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                _output.WriteLine($"\nInner Exception:");
                _output.WriteLine($"Type: {ex.InnerException.GetType().Name}");
                _output.WriteLine($"Message: {ex.InnerException.Message}");
            }
            
            throw; // Re-throw to fail the test
        }
    }
}

/// <summary>
/// Logger provider that writes to xUnit test output
/// </summary>
public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XunitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(_output, categoryName);
    }

    public void Dispose() { }
}

/// <summary>
/// Logger that writes to xUnit test output
/// </summary>
public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            var message = formatter(state, exception);
            _output.WriteLine($"[{logLevel}] {_categoryName}: {message}");
            if (exception != null)
            {
                _output.WriteLine($"Exception: {exception}");
            }
        }
        catch
        {
            // Ignore errors writing to output
        }
    }
}

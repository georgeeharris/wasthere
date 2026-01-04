using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TechTalk.SpecFlow;
using WasThere.Api.Models;
using WasThere.Api.Services;
using Microsoft.AspNetCore.Hosting;

namespace WasThere.Api.BDD.Tests.StepDefinitions;

[Binding]
public class FlyerConversionLoggerStepDefinitions : IDisposable
{
    private IFlyerConversionLogger _logger = null!;
    private string _logId = string.Empty;
    private string _logsPath = string.Empty;
    private string _logFilePath = string.Empty;

    [Given(@"I have a FlyerConversionLogger")]
    public void GivenIHaveAFlyerConversionLogger()
    {
        // Create a temporary directory for logs
        _logsPath = Path.Combine(Path.GetTempPath(), $"wasthere-test-logs-{Guid.NewGuid()}");
        Directory.CreateDirectory(_logsPath);

        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.Setup(e => e.ContentRootPath).Returns(_logsPath);

        var mockLogger = new Mock<ILogger<FlyerConversionLogger>>();

        _logger = new FlyerConversionLogger(mockEnvironment.Object, mockLogger.Object);
    }

    [Given(@"I have started a conversion log")]
    public void GivenIHaveStartedAConversionLog()
    {
        GivenIHaveAFlyerConversionLogger();
        _logId = _logger.StartConversionLog("test-image.jpg", "test.jpg");
        _logFilePath = Path.Combine(_logsPath, "logs", $"flyer-conversion-{_logId}.log");
    }

    [When(@"I start a conversion log for image ""(.*)"" with filename ""(.*)""")]
    public void WhenIStartAConversionLogForImageWithFilename(string imagePath, string fileName)
    {
        _logId = _logger.StartConversionLog(imagePath, fileName);
        _logFilePath = Path.Combine(_logsPath, "logs", $"flyer-conversion-{_logId}.log");
    }

    [When(@"I log a Gemini request with prompt ""(.*)"" for image ""(.*)"" with size (.*) bytes and mime type ""(.*)""")]
    public void WhenILogAGeminiRequestWithPromptForImageWithSizeBytesAndMimeType(string prompt, string imagePath, long size, string mimeType)
    {
        _logger.LogGeminiRequest(_logId, prompt, imagePath, size, mimeType);
    }

    [When(@"I log a Gemini response with success (.*) and raw response ""(.*)""")]
    public void WhenILogAGeminiResponseWithSuccessAndRawResponse(bool success, string rawResponse)
    {
        _logger.LogGeminiResponse(_logId, rawResponse, success, null);
    }

    [When(@"I log an analysis result with (.*) club nights")]
    public void WhenILogAnAnalysisResultWithClubNights(int count)
    {
        var result = new FlyerAnalysisResult
        {
            Success = true,
            Flyers = new List<FlyerData>
            {
                new FlyerData
                {
                    ClubNights = Enumerable.Range(1, count).Select(i => new ClubNightData
                    {
                        EventName = $"Event {i}",
                        VenueName = $"Venue {i}",
                        Acts = new List<ActData>()
                    }).ToList()
                }
            }
        };

        _logger.LogAnalysisResult(_logId, result);
    }

    [When(@"I log user year selections for (.*) dates")]
    public void WhenILogUserYearSelectionsForDates(int count)
    {
        var selections = Enumerable.Range(1, count).Select(i => new YearSelection
        {
            Month = i,
            Day = i,
            Year = 2000 + i
        }).ToList();

        _logger.LogUserYearSelection(_logId, selections);
    }

    [When(@"I log a database (.*) operation for (.*) ""(.*)"" with ID (.*)")]
    public void WhenILogADatabaseOperationForWithID(string operationType, string entityType, string entityName, int entityId)
    {
        _logger.LogDatabaseOperation(_logId, operationType, entityType, entityName, entityId);
    }

    [When(@"I complete the conversion log with success and summary ""(.*)""")]
    public void WhenICompleteTheConversionLogWithSuccessAndSummary(string summary)
    {
        _logger.CompleteConversionLog(_logId, true, summary, eventsCreated: 1, venuesCreated: 1, actsCreated: 5, clubNightsCreated: 3);
    }

    [When(@"I log an error ""(.*)"" with an exception")]
    public void WhenILogAnErrorWithAnException(string errorMessage)
    {
        var exception = new Exception("Test exception");
        _logger.LogError(_logId, errorMessage, exception);
    }

    [Then(@"a log ID should be generated")]
    public void ThenALogIDShouldBeGenerated()
    {
        _logId.Should().NotBeNullOrEmpty();
    }

    [Then(@"a log file should be created")]
    public void ThenALogFileShouldBeCreated()
    {
        File.Exists(_logFilePath).Should().BeTrue();
    }

    [Then(@"the log should contain the request details")]
    public void ThenTheLogShouldContainTheRequestDetails()
    {
        var logContent = File.ReadAllText(_logFilePath);
        logContent.Should().Contain("GEMINI API REQUEST");
        logContent.Should().Contain("test.jpg");
        logContent.Should().Contain("1024 bytes");
        logContent.Should().Contain("image/jpeg");
    }

    [Then(@"the log should contain the response details")]
    public void ThenTheLogShouldContainTheResponseDetails()
    {
        var logContent = File.ReadAllText(_logFilePath);
        logContent.Should().Contain("GEMINI API RESPONSE");
        logContent.Should().Contain("Success: True");
        logContent.Should().Contain("analysis result");
    }

    [Then(@"the log should contain the club night details")]
    public void ThenTheLogShouldContainTheClubNightDetails()
    {
        var logContent = File.ReadAllText(_logFilePath);
        logContent.Should().Contain("ANALYSIS RESULT");
        logContent.Should().Contain("Club Nights Found: 2");
        logContent.Should().Contain("Event 1");
        logContent.Should().Contain("Event 2");
    }

    [Then(@"the log should contain the year selections")]
    public void ThenTheLogShouldContainTheYearSelections()
    {
        var logContent = File.ReadAllText(_logFilePath);
        logContent.Should().Contain("USER YEAR SELECTION");
        logContent.Should().Contain("Selected Years Count: 2");
    }

    [Then(@"the log should contain the database operation")]
    public void ThenTheLogShouldContainTheDatabaseOperation()
    {
        var logContent = File.ReadAllText(_logFilePath);
        logContent.Should().Contain("DB CREATE: Event - Fabric (ID: 123)");
    }

    [Then(@"the log should contain the summary")]
    public void ThenTheLogShouldContainTheSummary()
    {
        var logContent = File.ReadAllText(_logFilePath);
        logContent.Should().Contain("CONVERSION SUMMARY");
        logContent.Should().Contain("Success: True");
        logContent.Should().Contain("Conversion completed");
    }

    [Then(@"the log file should be closed")]
    public void ThenTheLogFileShouldBeClosed()
    {
        // If the log file is closed, we should be able to open it for reading without issues
        using var fileStream = File.Open(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        fileStream.Should().NotBeNull();
    }

    [Then(@"the log should contain the error details")]
    public void ThenTheLogShouldContainTheErrorDetails()
    {
        var logContent = File.ReadAllText(_logFilePath);
        logContent.Should().Contain("ERROR");
        logContent.Should().Contain("Something went wrong");
        logContent.Should().Contain("Test exception");
    }

    public void Dispose()
    {
        // Clean up test logs directory
        if (Directory.Exists(_logsPath))
        {
            try
            {
                Directory.Delete(_logsPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

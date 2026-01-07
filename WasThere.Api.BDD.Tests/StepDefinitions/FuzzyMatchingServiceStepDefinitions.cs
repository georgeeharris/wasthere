using Microsoft.Extensions.Logging;
using Moq;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;
using WasThere.Api.Services;
using Xunit;

namespace WasThere.Api.BDD.Tests.StepDefinitions;

[Binding]
public class FuzzyMatchingServiceStepDefinitions
{
    private IFuzzyMatchingService? _service;
    private List<string> _candidates = new();
    private string? _searchInput;
    private double _minSimilarity;
    private string? _matchResult;
    private double _similarityScore;
    private string? _str1;
    private string? _str2;

    [Given(@"I have a FuzzyMatchingService")]
    public void GivenIHaveAFuzzyMatchingService()
    {
        var mockLogger = new Mock<ILogger<FuzzyMatchingService>>();
        _service = new FuzzyMatchingService(mockLogger.Object);
    }

    [Given(@"I have a list of candidates:")]
    public void GivenIHaveAListOfCandidates(Table table)
    {
        _candidates = table.Rows.Select(row => row["Candidate"]).ToList();
    }

    [When(@"I search for ""(.*)"" with minimum similarity (.*)")]
    public void WhenISearchForWithMinimumSimilarity(string input, double minSimilarity)
    {
        _searchInput = input;
        _minSimilarity = minSimilarity;
        _matchResult = _service!.FindBestMatch(input, _candidates, minSimilarity);
    }

    [When(@"I calculate similarity between ""(.*)"" and ""(.*)""")]
    public void WhenICalculateSimilarityBetween(string str1, string str2)
    {
        _str1 = str1;
        _str2 = str2;
        _similarityScore = _service!.CalculateSimilarity(str1, str2);
    }

    [Then(@"the best match should be ""(.*)""")]
    public void ThenTheBestMatchShouldBe(string expected)
    {
        Assert.Equal(expected, _matchResult);
    }

    [Then(@"the best match should be null")]
    public void ThenTheBestMatchShouldBeNull()
    {
        Assert.Null(_matchResult);
    }

    [Then(@"the similarity score should be (.*)")]
    public void ThenTheSimilarityScoreShouldBe(double expected)
    {
        Assert.Equal(expected, _similarityScore, precision: 1);
    }

    [Then(@"the similarity score should be less than (.*)")]
    public void ThenTheSimilarityScoreShouldBeLessThan(double threshold)
    {
        Assert.True(_similarityScore < threshold, 
            $"Expected similarity score to be less than {threshold}, but was {_similarityScore}");
    }

    [Then(@"the similarity score should be between (.*) and (.*)")]
    public void ThenTheSimilarityScoreShouldBeBetweenAnd(double min, double max)
    {
        Assert.True(_similarityScore >= min && _similarityScore <= max,
            $"Expected similarity score to be between {min} and {max}, but was {_similarityScore}");
    }
}

using FluentAssertions;
using TechTalk.SpecFlow;
using WasThere.Api.Services;

namespace WasThere.Api.BDD.Tests.StepDefinitions;

[Binding]
public class DateYearInferenceServiceStepDefinitions
{
    private IDateYearInferenceService _service = null!;
    private int? _inferredYear;
    private List<int> _candidateYears = new();
    private int _month;
    private int _day;
    private string? _dayOfWeek;

    [Given(@"I have a DateYearInferenceService")]
    public void GivenIHaveADateYearInferenceService()
    {
        _service = new DateYearInferenceService();
    }

    [When(@"I infer the year for month (.*) and day (.*)")]
    public void WhenIInferTheYearForMonthAndDay(int month, int day)
    {
        _month = month;
        _day = day;
        _inferredYear = _service.InferYear(month, day);
    }

    [When(@"I infer the year for month (.*) and day (.*) with day of week ""(.*)""")]
    public void WhenIInferTheYearForMonthAndDayWithDayOfWeek(int month, int day, string dayOfWeek)
    {
        _month = month;
        _day = day;
        _dayOfWeek = dayOfWeek;
        _inferredYear = _service.InferYear(month, day, dayOfWeek);
    }

    [When(@"I get candidate years for month (.*) and day (.*)")]
    public void WhenIGetCandidateYearsForMonthAndDay(int month, int day)
    {
        _month = month;
        _day = day;
        _candidateYears = _service.GetCandidateYears(month, day);
    }

    [When(@"I get candidate years for month (.*) and day (.*) with day of week ""(.*)""")]
    public void WhenIGetCandidateYearsForMonthAndDayWithDayOfWeek(int month, int day, string dayOfWeek)
    {
        _month = month;
        _day = day;
        _dayOfWeek = dayOfWeek;
        _candidateYears = _service.GetCandidateYears(month, day, dayOfWeek);
    }

    [Then(@"the inferred year should be between (.*) and (.*)")]
    public void ThenTheInferredYearShouldBeBetweenAnd(int startYear, int endYear)
    {
        _inferredYear.Should().NotBeNull();
        _inferredYear.Should().BeGreaterOrEqualTo(startYear);
        _inferredYear.Should().BeLessOrEqualTo(endYear);
    }

    [Then(@"the inferred year should not be null")]
    public void ThenTheInferredYearShouldNotBeNull()
    {
        _inferredYear.Should().NotBeNull();
    }

    [Then(@"the inferred year should be null")]
    public void ThenTheInferredYearShouldBeNull()
    {
        _inferredYear.Should().BeNull();
    }

    [Then(@"the date should be a Friday")]
    public void ThenTheDateShouldBeAFriday()
    {
        _inferredYear.Should().NotBeNull();
        var date = new DateTime(_inferredYear!.Value, _month, _day);
        date.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    [Then(@"I should receive a list of candidate years")]
    public void ThenIShouldReceiveAListOfCandidateYears()
    {
        _candidateYears.Should().NotBeNull();
        _candidateYears.Should().NotBeEmpty();
    }

    [Then(@"all candidate years should be valid dates for May (.*)")]
    public void ThenAllCandidateYearsShouldBeValidDatesForMay(int day)
    {
        foreach (var year in _candidateYears)
        {
            // Should not throw
            var date = new DateTime(year, 5, day);
            date.Month.Should().Be(5);
            date.Day.Should().Be(day);
        }
    }

    [Then(@"all candidate years should be Fridays on May (.*)")]
    public void ThenAllCandidateYearsShouldBeFridaysOnMay(int day)
    {
        foreach (var year in _candidateYears)
        {
            var date = new DateTime(year, 5, day);
            date.DayOfWeek.Should().Be(DayOfWeek.Friday);
        }
    }

    [Then(@"the candidate list should include years within 1995-2005")]
    public void ThenTheCandidateListShouldIncludeYearsWithin()
    {
        var yearsInRange = _candidateYears.Where(y => y >= 1995 && y <= 2005).ToList();
        yearsInRange.Should().NotBeEmpty("there should be at least one valid year in the 1995-2005 range");
    }

    [Then(@"the candidate list should include one year before 1995 if available")]
    public void ThenTheCandidateListShouldIncludeOneYearBefore()
    {
        var yearsBefore1995 = _candidateYears.Where(y => y < 1995).ToList();
        yearsBefore1995.Should().HaveCountLessOrEqualTo(1, "should only include the closest year before 1995");
    }

    [Then(@"the candidate list should include one year after 2005 if available")]
    public void ThenTheCandidateListShouldIncludeOneYearAfter()
    {
        var yearsAfter2005 = _candidateYears.Where(y => y > 2005).ToList();
        yearsAfter2005.Should().HaveCountLessOrEqualTo(1, "should only include the closest year after 2005");
    }

    [Then(@"the inferred year should be closer to 2002 than the edges of the range")]
    public void ThenTheInferredYearShouldBeCloserToThanTheEdgesOfTheRange()
    {
        _inferredYear.Should().NotBeNull();
        var distanceTo2002 = Math.Abs(_inferredYear!.Value - 2002);
        var distanceToStart = Math.Abs(_inferredYear.Value - 1995);
        var distanceToEnd = Math.Abs(_inferredYear.Value - 2010);
        
        // The inferred year should be closer to the middle (2002) than to the edges
        distanceTo2002.Should().BeLessOrEqualTo(Math.Min(distanceToStart, distanceToEnd));
    }
}

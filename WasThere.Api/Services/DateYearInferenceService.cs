namespace WasThere.Api.Services;

public interface IDateYearInferenceService
{
    /// <summary>
    /// Infers the most likely year for a date based on month, day, and optional day of week.
    /// Favors years in the range 1995-2010.
    /// </summary>
    /// <param name="month">Month (1-12)</param>
    /// <param name="day">Day of month (1-31)</param>
    /// <param name="dayOfWeek">Optional day of week (e.g., "Friday", "Monday")</param>
    /// <returns>The inferred year, or null if no valid year can be determined</returns>
    int? InferYear(int month, int day, string? dayOfWeek = null);
}

public class DateYearInferenceService : IDateYearInferenceService
{
    // Preferred year range for club flyers
    private const int PreferredStartYear = 1995;
    private const int PreferredEndYear = 2010;
    
    // Extended search range
    private const int SearchStartYear = 1990;
    private const int SearchEndYear = 2025;

    public int? InferYear(int month, int day, string? dayOfWeek = null)
    {
        // Validate input
        if (month < 1 || month > 12 || day < 1 || day > 31)
        {
            return null;
        }

        DayOfWeek? targetDayOfWeek = null;
        if (!string.IsNullOrWhiteSpace(dayOfWeek))
        {
            targetDayOfWeek = ParseDayOfWeek(dayOfWeek);
        }

        var candidateYears = new List<int>();

        // First, search in the preferred range
        for (int year = PreferredStartYear; year <= PreferredEndYear; year++)
        {
            if (IsValidDate(year, month, day, targetDayOfWeek))
            {
                candidateYears.Add(year);
            }
        }

        // If we found matches in the preferred range, return the one closest to the middle of the range
        if (candidateYears.Count > 0)
        {
            // Prefer years closest to the middle of the 1995-2010 range (around 2002-2003)
            int targetYear = (PreferredStartYear + PreferredEndYear) / 2; // 2002
            return candidateYears.OrderBy(y => Math.Abs(y - targetYear)).First();
        }

        // If no matches in preferred range, search the extended range
        for (int year = SearchStartYear; year < PreferredStartYear; year++)
        {
            if (IsValidDate(year, month, day, targetDayOfWeek))
            {
                candidateYears.Add(year);
            }
        }
        
        for (int year = PreferredEndYear + 1; year <= SearchEndYear; year++)
        {
            if (IsValidDate(year, month, day, targetDayOfWeek))
            {
                candidateYears.Add(year);
            }
        }

        // Return the candidate closest to the preferred range
        if (candidateYears.Count > 0)
        {
            // Prefer years closest to the middle of the preferred range (around 2002)
            int targetYear = (PreferredStartYear + PreferredEndYear) / 2; // 2002
            return candidateYears.OrderBy(y => Math.Abs(y - targetYear)).First();
        }

        return null;
    }

    private bool IsValidDate(int year, int month, int day, DayOfWeek? targetDayOfWeek)
    {
        try
        {
            var date = new DateTime(year, month, day);
            
            // If day of week is specified, it must match
            if (targetDayOfWeek.HasValue && date.DayOfWeek != targetDayOfWeek.Value)
            {
                return false;
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private DayOfWeek? ParseDayOfWeek(string dayOfWeekStr)
    {
        var normalized = dayOfWeekStr.Trim().ToLowerInvariant();
        
        return normalized switch
        {
            "monday" or "mon" => DayOfWeek.Monday,
            "tuesday" or "tue" or "tues" => DayOfWeek.Tuesday,
            "wednesday" or "wed" => DayOfWeek.Wednesday,
            "thursday" or "thu" or "thur" or "thurs" => DayOfWeek.Thursday,
            "friday" or "fri" => DayOfWeek.Friday,
            "saturday" or "sat" => DayOfWeek.Saturday,
            "sunday" or "sun" => DayOfWeek.Sunday,
            _ => null
        };
    }
}

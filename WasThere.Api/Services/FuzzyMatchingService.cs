namespace WasThere.Api.Services;

public interface IFuzzyMatchingService
{
    /// <summary>
    /// Finds the best matching string from a list of candidates based on fuzzy string matching.
    /// Returns null if no match meets the minimum similarity threshold.
    /// </summary>
    string? FindBestMatch(string input, IEnumerable<string> candidates, double minSimilarity = 0.8);
    
    /// <summary>
    /// Calculates the similarity score between two strings (0.0 to 1.0).
    /// Higher scores indicate greater similarity.
    /// </summary>
    double CalculateSimilarity(string str1, string str2);
}

public class FuzzyMatchingService : IFuzzyMatchingService
{
    private readonly ILogger<FuzzyMatchingService> _logger;

    public FuzzyMatchingService(ILogger<FuzzyMatchingService> logger)
    {
        _logger = logger;
    }

    public string? FindBestMatch(string input, IEnumerable<string> candidates, double minSimilarity = 0.8)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var candidateList = candidates.ToList();
        if (candidateList.Count == 0)
        {
            return null;
        }

        // Normalize input for comparison
        var normalizedInput = NormalizeString(input);

        string? bestMatch = null;
        double bestScore = 0;

        foreach (var candidate in candidateList)
        {
            var normalizedCandidate = NormalizeString(candidate);
            var score = CalculateSimilarity(normalizedInput, normalizedCandidate);

            _logger.LogDebug("Comparing '{Input}' with '{Candidate}': similarity = {Score:F3}", 
                input, candidate, score);

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = candidate;
            }
        }

        if (bestScore >= minSimilarity)
        {
            _logger.LogInformation("Found fuzzy match for '{Input}': '{BestMatch}' with score {Score:F3}",
                input, bestMatch, bestScore);
            return bestMatch;
        }

        _logger.LogInformation("No fuzzy match found for '{Input}' (best score: {Score:F3}, threshold: {Threshold})",
            input, bestScore, minSimilarity);
        return null;
    }

    public double CalculateSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) && string.IsNullOrEmpty(str2))
        {
            return 1.0;
        }

        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
        {
            return 0.0;
        }

        var normalized1 = NormalizeString(str1);
        var normalized2 = NormalizeString(str2);

        // Use Levenshtein distance for similarity calculation
        var distance = LevenshteinDistance(normalized1, normalized2);
        var maxLength = Math.Max(normalized1.Length, normalized2.Length);
        
        // Convert distance to similarity score (0.0 to 1.0)
        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Normalizes a string for comparison by:
    /// - Converting to lowercase
    /// - Removing common punctuation (apostrophes, hyphens, etc.)
    /// - Trimming whitespace
    /// - Removing address-like suffixes
    /// </summary>
    private static string NormalizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.ToLowerInvariant().Trim();

        // Remove address-like patterns (e.g., ", corporation Street, Birmingham B1 5QS")
        // Match patterns starting with comma followed by potential address components
        var addressPatterns = new[]
        {
            @",\s*\d+\s+[a-z\s]+,\s*[a-z\s]+\s+[a-z0-9\s]+$", // ", 123 Street Name, City Postcode"
            @",\s*[a-z\s]+,\s*[a-z\s]+\s+[a-z0-9\s]+$", // ", Street Name, City Postcode"
            @",\s*[a-z\s]+street[a-z\s,]*$", // ", something street..."
            @",\s*[a-z\s]+road[a-z\s,]*$", // ", something road..."
            @",\s*[a-z\s]+avenue[a-z\s,]*$", // ", something avenue..."
        };

        foreach (var pattern in addressPatterns)
        {
            normalized = System.Text.RegularExpressions.Regex.Replace(
                normalized, 
                pattern, 
                string.Empty, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        // Remove common punctuation and special characters
        normalized = normalized
            .Replace("'", "")
            .Replace("'", "")
            .Replace("-", " ")
            .Replace("_", " ")
            .Replace(".", "")
            .Replace(",", "");

        // Normalize whitespace
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// This represents the minimum number of single-character edits required to change one string into the other.
    /// </summary>
    private static int LevenshteinDistance(string str1, string str2)
    {
        var len1 = str1.Length;
        var len2 = str2.Length;
        var matrix = new int[len1 + 1, len2 + 1];

        // Initialize the matrix
        for (var i = 0; i <= len1; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= len2; j++)
        {
            matrix[0, j] = j;
        }

        // Calculate distances
        for (var i = 1; i <= len1; i++)
        {
            for (var j = 1; j <= len2; j++)
            {
                var cost = str1[i - 1] == str2[j - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1,      // deletion
                        matrix[i, j - 1] + 1),     // insertion
                    matrix[i - 1, j - 1] + cost);  // substitution
            }
        }

        return matrix[len1, len2];
    }
}

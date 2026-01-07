# Fuzzy Matching for Flyer Data Consistency

## Problem Statement

When uploading flyers, the AI can extract venue and event names with variance due to:
1. **Punctuation differences**: "Sankey's Soap" vs "Sankeys Soap"
2. **Address information**: "The Que Club" vs "The Que Club, Corporation Street, Birmingham B1 5QS"
3. **OCR errors**: "Bugged Out" vs "Busted Out" (poor image quality)

This leads to duplicate entries in the database for the same venue or event.

## Solution

Implemented a fuzzy matching system with two complementary approaches:

### 1. AI Prompt Updates

Updated the GoogleGeminiService prompt to instruct the AI to:
- Extract ONLY the venue name, without address details
- Focus on the establishment name, not its location
- Remove trailing address information

**Example prompt instruction:**
```
6. **VENUE NAME EXTRACTION - CRITICAL**:
   - Extract ONLY the venue name itself (e.g., 'Sankey's Soap', 'The Que Club', 'Ministry of Sound')
   - DO NOT include any address information (street, city, postcode, etc.)
   - If the flyer shows 'The Que Club, Corporation Street, Birmingham B1 5QS', extract only 'The Que Club'
```

### 2. Code-Based Fuzzy Matching

Implemented `FuzzyMatchingService` that:
- Uses Levenshtein distance algorithm for similarity calculation
- Normalizes strings by:
  - Converting to lowercase
  - Removing punctuation (apostrophes, hyphens, etc.)
  - Removing address patterns (e.g., ", street name, city postcode")
  - Normalizing whitespace
- Compares extracted names against existing database entries
- Returns the best match if similarity ≥ threshold (80% for venues/events, 85% for acts)
- Prefers existing "canonical" database entries maintained by admins

### Why Code-Based vs AI-Based Matching?

**Code-based fuzzy matching** was chosen because:
1. **Deterministic**: Consistent results for the same inputs
2. **Testable**: Easy to write unit tests and verify behavior
3. **Fast**: No additional API calls required
4. **Lower cost**: No extra AI API usage
5. **Controllable**: Precise threshold tuning
6. **Small dataset**: < 100 venues and < 100 events (perfect for in-memory comparison)

## Implementation Details

### New Service: FuzzyMatchingService

Located in: `WasThere.Api/Services/FuzzyMatchingService.cs`

**Key Methods:**
- `FindBestMatch(string input, IEnumerable<string> candidates, double minSimilarity)`: Finds best matching candidate
- `CalculateSimilarity(string str1, string str2)`: Returns similarity score 0.0-1.0
- `NormalizeString(string input)`: Normalizes strings for comparison

### Updated Methods in FlyersController

Created helper methods:
- `FindOrCreateEventAsync(string eventName)`: Finds existing event via fuzzy matching or creates new
- `FindOrCreateVenueAsync(string venueName)`: Finds existing venue via fuzzy matching or creates new  
- `FindOrCreateActAsync(string actName)`: Finds existing act via fuzzy matching or creates new

These methods are now used in:
- `ProcessSingleFlyerAsync`: Initial flyer upload processing
- `ProcessAnalysisResult`: Auto-population of club nights
- `ProcessAnalysisResultWithSelectedYears`: Completion with year selection
- `AutoPopulateFromFlyer`: Manual re-analysis

## Testing

Created comprehensive BDD tests in `WasThere.Api.BDD.Tests/Features/FuzzyMatchingService.feature`:

✅ Exact match returns correct candidate
✅ Case insensitive matching with punctuation variation
✅ Ignores address suffixes
✅ Handles OCR errors (similar spelling)
✅ Returns null when similarity too low
✅ Handles punctuation and spacing variations
✅ Similarity calculation tests (identical, different, similar strings)

**All 30 tests passing** (9 new fuzzy matching tests + 21 existing tests)

## Examples

### Venue Matching
```
AI extracts: "Sankeys Soap"
Database has: "Sankey's Soap"
Result: Uses "Sankey's Soap" (admin-maintained canonical version)

AI extracts: "The Que Club, Corporation Street, Birmingham B1 5QS"
Database has: "The Que Club"
Result: Uses "The Que Club" (normalized and matched)
```

### Event Matching
```
AI extracts: "Busted Out" (OCR error)
Database has: "Bugged Out"
Result: Uses "Bugged Out" (similarity 82%, above 80% threshold)

AI extracts: "BUGGED OUT" (case difference)
Database has: "Bugged Out"
Result: Uses "Bugged Out" (case-insensitive match)
```

## Configuration

Similarity thresholds:
- **Events & Venues**: 0.8 (80% similarity required)
- **Acts**: 0.85 (85% similarity required - stricter to avoid false positives with short names)

These can be adjusted in the helper methods if needed.

## Benefits

1. **Maintains Data Quality**: Admins can set the "correct" version (e.g., with apostrophes)
2. **Reduces Duplicates**: Similar names automatically matched to existing entries
3. **Handles OCR Errors**: AI misreadings still match correct database entries
4. **Transparent**: Logs all fuzzy matches with similarity scores for debugging
5. **Scalable**: Efficient for current dataset size (< 100 venues/events)

## Future Enhancements

If the dataset grows significantly (> 1000 entries):
- Consider indexing/caching strategies
- Implement database-level fuzzy matching (e.g., PostgreSQL pg_trgm extension)
- Add UI for reviewing and confirming fuzzy matches

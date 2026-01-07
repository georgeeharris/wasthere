Feature: FuzzyMatchingService
  As a flyer processing system
  I want to use fuzzy matching to find similar venue and event names
  So that I can maintain data consistency and avoid duplicates

  Background:
    Given I have a FuzzyMatchingService

  Scenario: Exact match returns the candidate string
    Given I have a list of candidates:
      | Candidate      |
      | Sankey's Soap  |
      | The Que Club   |
      | Bugged Out     |
    When I search for "Sankey's Soap" with minimum similarity 0.8
    Then the best match should be "Sankey's Soap"

  Scenario: Case insensitive match with punctuation variation
    Given I have a list of candidates:
      | Candidate      |
      | Sankey's Soap  |
    When I search for "Sankeys Soap" with minimum similarity 0.8
    Then the best match should be "Sankey's Soap"

  Scenario: Match ignores address suffix
    Given I have a list of candidates:
      | Candidate      |
      | The Que Club   |
    When I search for "The Que Club, Corporation Street, Birmingham B1 5QS" with minimum similarity 0.8
    Then the best match should be "The Que Club"

  Scenario: OCR error with similar spelling
    Given I have a list of candidates:
      | Candidate      |
      | Bugged Out     |
    When I search for "Busted Out" with minimum similarity 0.8
    Then the best match should be "Bugged Out"

  Scenario: No match when similarity is too low
    Given I have a list of candidates:
      | Candidate      |
      | Sankey's Soap  |
      | The Que Club   |
    When I search for "Fabric London" with minimum similarity 0.8
    Then the best match should be null

  Scenario: Punctuation and spacing variations match
    Given I have a list of candidates:
      | Candidate          |
      | Ministry of Sound  |
    When I search for "Ministry-of-Sound" with minimum similarity 0.8
    Then the best match should be "Ministry of Sound"

  Scenario: Similarity calculation for identical strings
    When I calculate similarity between "Bugged Out" and "Bugged Out"
    Then the similarity score should be 1.0

  Scenario: Similarity calculation for completely different strings
    When I calculate similarity between "Fabric" and "Ministry of Sound"
    Then the similarity score should be less than 0.5

  Scenario: Similarity calculation for similar strings
    When I calculate similarity between "Bugged Out" and "Busted Out"
    Then the similarity score should be between 0.7 and 0.9

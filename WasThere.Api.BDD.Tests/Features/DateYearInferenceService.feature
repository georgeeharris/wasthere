Feature: DateYearInferenceService
  As a club flyer analyzer
  I want to infer years from partial dates
  So that I can accurately date events from old flyers

  Background:
    Given I have a DateYearInferenceService

  Scenario: Infer year from valid date without day of week
    When I infer the year for month 5 and day 27
    Then the inferred year should be between 1995 and 2010
    And the inferred year should not be null

  Scenario: Infer year from date with matching day of week
    When I infer the year for month 5 and day 27 with day of week "Friday"
    Then the inferred year should be between 1995 and 2010
    And the date should be a Friday

  Scenario: Handle invalid month
    When I infer the year for month 13 and day 15
    Then the inferred year should be null

  Scenario: Handle invalid day
    When I infer the year for month 6 and day 32
    Then the inferred year should be null

  Scenario: Get candidate years for valid date
    When I get candidate years for month 5 and day 27
    Then I should receive a list of candidate years
    And all candidate years should be valid dates for May 27

  Scenario: Get candidate years with day of week constraint
    When I get candidate years for month 5 and day 27 with day of week "Friday"
    Then I should receive a list of candidate years
    And all candidate years should be Fridays on May 27

  Scenario: Candidate years should include range and edges
    When I get candidate years for month 1 and day 1 with day of week "Saturday"
    Then the candidate list should include years within 1995-2005
    And the candidate list should include one year before 1995 if available
    And the candidate list should include one year after 2005 if available

  Scenario: Prefer middle of range for ambiguous dates
    When I infer the year for month 3 and day 15
    Then the inferred year should be closer to 2002 than the edges of the range

Feature: FlyerConversionLogger
  As a developer debugging flyer conversion
  I want to log conversion operations to files
  So that I can diagnose issues and track the conversion process

  Background:
    Given I have a FlyerConversionLogger

  Scenario: Start conversion log
    When I start a conversion log for image "test-flyer.jpg" with filename "test.jpg"
    Then a log ID should be generated
    And a log file should be created

  Scenario: Log Gemini request details
    Given I have started a conversion log
    When I log a Gemini request with prompt "Analyze this flyer" for image "test.jpg" with size 1024 bytes and mime type "image/jpeg"
    Then the log should contain the request details

  Scenario: Log Gemini response
    Given I have started a conversion log
    When I log a Gemini response with success true and raw response "analysis result"
    Then the log should contain the response details

  Scenario: Log analysis result with club nights
    Given I have started a conversion log
    When I log an analysis result with 2 club nights
    Then the log should contain the club night details

  Scenario: Log user year selection
    Given I have started a conversion log
    When I log user year selections for 2 dates
    Then the log should contain the year selections

  Scenario: Log database operation
    Given I have started a conversion log
    When I log a database CREATE operation for Event "Fabric" with ID 123
    Then the log should contain the database operation

  Scenario: Complete conversion log
    Given I have started a conversion log
    When I complete the conversion log with success and summary "Conversion completed"
    Then the log should contain the summary
    And the log file should be closed

  Scenario: Log error with exception
    Given I have started a conversion log
    When I log an error "Something went wrong" with an exception
    Then the log should contain the error details

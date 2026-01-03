Feature: GoogleGeminiService
  As a flyer analyzer
  I want to extract event information from flyer images using AI
  So that I can automatically populate event data

  Background:
    Given I have a GoogleGeminiService

  Scenario: Service initialization without API key
    Given the API key is not configured
    When I attempt to analyze a flyer image
    Then the result should indicate failure
    And the error message should mention "API key is not configured"

  Scenario: Service detects missing image file
    Given the API key is configured
    When I attempt to analyze a non-existent image file
    Then the result should indicate failure
    And the error message should mention file not found

  Scenario: Service determines correct MIME type for JPEG
    Given I have an image file with extension ".jpg"
    Then the MIME type should be "image/jpeg"

  Scenario: Service determines correct MIME type for PNG
    Given I have an image file with extension ".png"
    Then the MIME type should be "image/png"

  Scenario: Service includes diagnostics information in error responses
    Given the API key is not configured
    When I attempt to analyze a flyer image
    Then the result should contain diagnostics
    And the diagnostics should include step information

# WasThere API BDD Tests

This project contains Behavior-Driven Development (BDD) tests for the WasThere API service layer using SpecFlow.

## Overview

These tests focus on the business logic within the service classes, specifically:

- **DateYearInferenceService**: Tests for inferring years from partial dates (9 scenarios)
- **FlyerConversionLogger**: Tests for logging flyer conversion operations (7 scenarios)
- **GoogleGeminiService**: Tests for AI-powered flyer analysis (5 scenarios)

## Running the Tests

### Local Development

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### CI/CD

These tests are automatically run as part of the CI pipeline in the `test-bdd` job.

## Test Structure

```
WasThere.Api.BDD.Tests/
├── Features/                           # Gherkin feature files
│   ├── DateYearInferenceService.feature
│   ├── FlyerConversionLogger.feature
│   └── GoogleGeminiService.feature
├── StepDefinitions/                    # Step definition implementations
│   ├── DateYearInferenceServiceStepDefinitions.cs
│   ├── FlyerConversionLoggerStepDefinitions.cs
│   └── GoogleGeminiServiceStepDefinitions.cs
├── specflow.json                       # SpecFlow configuration
└── WasThere.Api.BDD.Tests.csproj      # Project file
```

## Key Features

### DateYearInferenceService Tests
- Valid date inference with and without day of week
- Invalid input handling (month/day out of range)
- Candidate year generation
- Preference for middle of range dates

### FlyerConversionLogger Tests
- Log file creation and management
- Logging various conversion stages (Gemini requests/responses, analysis results, etc.)
- Database operation logging
- Error logging with exceptions

### GoogleGeminiService Tests
- API key validation
- File existence checking
- MIME type detection
- Diagnostics information generation

## Technologies Used

- **SpecFlow 3.9**: BDD framework for .NET
- **xUnit**: Test runner
- **FluentAssertions**: Assertion library for readable test assertions
- **Moq**: Mocking framework for dependencies

## Test Coverage

All tests focus on:
1. **Business logic validation**: Ensuring the core functionality works as expected
2. **Error handling**: Verifying proper handling of invalid inputs and error conditions
3. **Edge cases**: Testing boundary conditions and special cases

## Notes

- Tests for GoogleGeminiService that would require calling the actual Google Gemini API have been scoped to test what can be verified without external dependencies (e.g., configuration, file handling, MIME type detection).
- Integration tests that called the real Google API have been removed as they were not practical for CI/CD.

# Year Selection Feature for Flyer Upload

## Overview
This feature implements user-driven year selection when uploading flyers with ambiguous dates. When a flyer contains dates without explicit years (e.g., "Friday 7th March"), the system generates candidate years and prompts the user to select the correct one.

## Implementation Details

### Backend Changes

#### 1. DateYearInferenceService
- **New Method**: `GetCandidateYears(int month, int day, string? dayOfWeek)`
  - Returns all possible years for a given date within the range 1995-2005
  - Includes the closest year before 1995
  - Includes the closest year after 2005
  - Validates day of week if provided

#### 2. FlyersController
- **Modified Endpoint**: `POST /api/flyers/upload`
  - Now analyzes the flyer and populates candidate years
  - Does NOT create club nights automatically
  - Returns analysis result with candidate years to frontend
  
- **New Endpoint**: `POST /api/flyers/{id}/complete-upload`
  - Accepts user-selected years
  - Creates club nights with the selected years
  - Request body format:
    ```json
    {
      "selectedYears": [
        { "month": 3, "day": 7, "year": 2003 },
        { "month": 3, "day": 14, "year": 2003 }
      ]
    }
    ```

#### 3. Models
- **ClubNightData**: Added `CandidateYears` property (List<int>)
- **FlyerUploadResponse**: Added `AnalysisResult` property
- **New Classes**: `CompleteUploadRequest`, `YearSelection`

### Frontend Changes

#### 1. YearSelectionModal Component
- New modal component that displays when multiple candidate years are found
- Shows each date with its possible years in a dropdown
- Displays event and venue information for context
- User can select the correct year for each date

#### 2. FlyerList Component
- Updated to handle two-phase upload process:
  1. Upload and analyze flyer
  2. If multiple candidate years exist, show YearSelectionModal
  3. User selects years
  4. Complete upload with selected years
- Automatically proceeds if only one candidate year exists

#### 3. Type Definitions
- Added `ClubNightData`, `ActData`, `FlyerAnalysisResult` types
- Added `YearSelection` type

## User Flow

1. User uploads a flyer
2. System analyzes the flyer using AI
3. System generates candidate years for any dates without explicit years
4. If multiple candidates exist for any date:
   - Modal appears showing all dates needing year selection
   - User selects the correct year for each date from dropdowns
   - User confirms selections
5. System creates club nights with selected years
6. Success message shows created entities

## Example

For a flyer showing "Friday 7th March":
- System finds candidate years: 1997, 2003, 2008
- Modal displays: "Friday 7 March" with dropdown showing [1997, 2003, 2008]
- User selects 2003
- System creates club night for March 7, 2003

## Benefits

1. **Accuracy**: User validates the year instead of system guessing
2. **Transparency**: User sees all possible years and makes informed decision
3. **Flexibility**: Works with any date range configuration
4. **Context**: Shows event/venue info to help user decide
5. **User Control**: Prevents incorrect data from being created automatically

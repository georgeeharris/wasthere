# Testing Guide: Live Set Feature

## Overview
This document describes how to test the new live set feature that allows tracking whether an act performs a DJ set or a live set at a club night.

## What Changed

### Database
- Added `IsLiveSet` boolean column to `ClubNightActs` table
- Default value: `false` (represents DJ set)
- Migration file: `20251230132314_AddIsLiveSetToClubNightAct.cs`

### Backend API
- `ClubNightDto` now accepts `acts: ClubNightActDto[]` instead of `actIds: number[]`
- Each `ClubNightActDto` contains: `{ actId: number, isLiveSet: boolean }`
- GET endpoints return acts with `isLiveSet` property
- POST/PUT endpoints accept and save the `isLiveSet` flag

### Frontend UI
- New `ActSelector` component with checkboxes for live sets
- Live acts display a red "LIVE" badge in the selector
- Club night cards show "(live)" indicator for live performances
- Default behavior: unchecked = DJ set, checked = live set

### AI Auto-Population
- Gemini prompt updated to detect live set indicators in flyer text
- Patterns detected: "(live)", "(live set)", "(live PA)", "live", etc.
- Act names are cleaned (e.g., "Dave Clarke (live)" → "Dave Clarke")
- The `isLiveSet` flag is automatically set based on detection

## Testing Steps

### 1. Database Migration Testing

Run the migration to add the new column:

```bash
cd WasThere.Api
dotnet ef database update
```

Expected result: Migration should apply successfully without errors.

### 2. Manual Entry Testing

#### Test Case 1: Create a new club night with mixed performances

1. Start the application (see README.md for instructions)
2. Navigate to "Master Lists" tab
3. Create test data:
   - Event: "Fabric"
   - Venue: "Fabric London"
   - Acts: "Dave Clarke", "Laurent Garnier", "Richie Hawtin"
4. Navigate to "Club Nights" tab
5. Click "Add Club Night"
6. Fill in the form:
   - Date: Select any date
   - Event: "Fabric"
   - Venue: "Fabric London"
   - Acts: 
     - Select "Dave Clarke" (leave unchecked = DJ set)
     - Select "Laurent Garnier" and check "Live" box
     - Select "Richie Hawtin" (leave unchecked = DJ set)
7. Click "Create"

Expected result:
- Club night is created successfully
- "Laurent Garnier" appears with "(live)" indicator in red
- "Dave Clarke" and "Richie Hawtin" appear without indicators

#### Test Case 2: Edit existing club night to change performance type

1. Click "Edit" on the club night created above
2. Change "Dave Clarke" to live performance (check the "Live" box)
3. Change "Laurent Garnier" back to DJ set (uncheck the "Live" box)
4. Click "Update"

Expected result:
- Club night updates successfully
- "Dave Clarke" now shows "(live)" indicator
- "Laurent Garnier" no longer has the indicator

### 3. API Testing

#### Test Case 3: API endpoints return correct data

Using curl, Postman, or the Swagger UI at `http://localhost:5000/swagger`:

1. **GET /api/clubnights/{id}**
   ```json
   // Response should include:
   {
     "acts": [
       {
         "actId": 1,
         "actName": "Dave Clarke",
         "isLiveSet": true
       },
       {
         "actId": 2,
         "actName": "Laurent Garnier",
         "isLiveSet": false
       }
     ]
   }
   ```

2. **POST /api/clubnights**
   ```json
   // Request body:
   {
     "date": "2005-01-07",
     "eventId": 1,
     "venueId": 1,
     "acts": [
       { "actId": 1, "isLiveSet": false },
       { "actId": 2, "isLiveSet": true }
     ]
   }
   ```

Expected result: Club night is created with correct live set flags.

### 4. AI Auto-Population Testing

#### Test Case 4: Upload flyer with live acts

1. Navigate to "Flyers" tab
2. Upload a flyer image that contains text like:
   - "Dave Clarke (live)"
   - "Laurent Garnier (live set)"
   - "Richie Hawtin (DJ set)"
3. Click "Auto-populate" on the uploaded flyer

Expected result:
- Acts are created/matched correctly
- "Dave Clarke" and "Laurent Garnier" are marked as live sets
- "Richie Hawtin" is marked as DJ set
- Act names do not include "(live)" or "(DJ set)" suffixes
- Club nights are created with correct live set flags

#### Test Case 5: Flyer with no performance type indicators

1. Upload a flyer with just act names (no "(live)" or "(DJ set)" text)
2. Click "Auto-populate"

Expected result:
- All acts default to DJ set (isLiveSet = false)
- This is the expected behavior for backward compatibility

### 5. UI Component Testing

#### Test Case 6: ActSelector functionality

1. Open the "Add Club Night" form
2. Click on the Acts selector
3. Test the following:
   - Search for acts using the search box
   - Select multiple acts
   - Check/uncheck "Live" boxes for different acts
   - Remove acts using the "×" button
   - Verify live acts show red "LIVE" badge

Expected result:
- All interactions work smoothly
- Live set state is maintained correctly
- Visual feedback is clear and consistent

### 6. Edge Cases

#### Test Case 7: Empty acts list
1. Create a club night with no acts
2. Save successfully

Expected result: Club night is created with empty acts array.

#### Test Case 8: All acts as live sets
1. Create a club night with 3+ acts
2. Mark all as live sets
3. Save and verify

Expected result: All acts display with "(live)" indicator.

#### Test Case 9: Backward compatibility
1. If you have existing data, verify:
   - Existing club nights still load correctly
   - Existing acts default to DJ set (isLiveSet = false)
   - No data loss or corruption

## Known Behaviors

1. **Default value**: When migrating existing data, all existing `ClubNightAct` records will have `IsLiveSet = false` (DJ set), which is the most common case for club events.

2. **Act name cleaning**: The AI will automatically remove performance type indicators from act names to ensure consistency in the master acts list.

3. **Visual indicators**: 
   - In the selector: Red "LIVE" badge
   - In club night cards: Red italic "(live)" text

## Troubleshooting

### Migration Issues
If migration fails:
```bash
# Check migration status
dotnet ef migrations list

# If needed, remove and recreate
dotnet ef migrations remove
dotnet ef migrations add AddIsLiveSetToClubNightAct
```

### API Errors
- Check that the API is using the updated database schema
- Verify CORS settings if testing from frontend
- Check API logs for detailed error messages

### Frontend Issues
- Clear browser cache if styles don't load
- Check browser console for JavaScript errors
- Verify API URL is correctly configured

## Success Criteria

All tests pass when:
- ✅ Database migration applies successfully
- ✅ Club nights can be created with mixed DJ/live performances
- ✅ Club nights can be edited to change performance types
- ✅ API endpoints return and accept isLiveSet flag correctly
- ✅ AI auto-population detects and sets live flags correctly
- ✅ UI clearly shows live vs DJ set indicators
- ✅ Existing data remains functional
- ✅ No security vulnerabilities introduced (CodeQL passed)
- ✅ Code builds successfully (CI passed)

# Multi-Flyer Image Splitting - Testing Guide

## Overview
This feature allows users to upload a single image containing multiple flyers. The system automatically detects, splits, and processes each flyer independently.

## How It Works

### 1. Detection Phase
- User uploads a single image
- Backend calls Gemini AI to detect bounding boxes for individual flyers
- AI returns normalized coordinates (0-1) for each flyer's location

### 2. Splitting Phase
- If multiple flyers detected, image is split using ImageSharp
- Each flyer is saved as a separate image file in a temp directory
- Original aspect ratios and quality are preserved

### 3. Processing Phase
- Each split flyer image is analyzed separately by Gemini AI
- Separate database `Flyer` records are created for each
- Each flyer can have different events, venues, and dates
- User is shown progress messages: "Flyer 1 of 3: Flyer analyzed..."

### 4. User Input Phase (if needed)
- For flyers needing event or year selection, the wizard flow is triggered
- Currently processes sequentially (one flyer at a time)
- Each flyer maintains its own analysis result

## Test Scenarios

### Scenario 1: Single Flyer Upload (Regression Test)
**Purpose**: Verify existing functionality still works

**Steps**:
1. Upload a single flyer image
2. Verify analysis completes successfully
3. Check that only one `Flyer` record is created
4. Confirm event/year selection works as before

**Expected Result**:
- Single flyer detected and processed
- No image splitting occurs
- Existing wizard flow functions normally

### Scenario 2: Multiple Flyers - Clear Separation
**Purpose**: Test optimal case with well-separated flyers

**Test Image**: Photo of 3-4 distinct flyers laid out on a table with clear spacing

**Steps**:
1. Upload the multi-flyer image
2. Observe progress messages during upload
3. Check database for multiple `Flyer` records
4. Verify each flyer has its own analysis result

**Expected Result**:
- Multiple flyers detected (e.g., "Detected 4 flyer(s)")
- Each flyer split into separate image
- Multiple database records created
- Each record shows correct filename (e.g., "test_flyer_0.jpg", "test_flyer_1.jpg")
- Progress messages shown: "Flyer 1 of 4", "Flyer 2 of 4", etc.

### Scenario 3: Multiple Flyers - Overlapping
**Purpose**: Test edge case with slight overlap

**Test Image**: Photo where flyers partially overlap

**Steps**:
1. Upload the image
2. Check how AI handles bounding boxes
3. Verify cropped images include all content

**Expected Result**:
- AI should still detect multiple flyers
- Bounding boxes may overlap slightly
- Each cropped image should be at least 10x10 pixels
- No crashes or errors

### Scenario 4: Multiple Flyers - Different Events
**Purpose**: Verify each flyer can have different metadata

**Test Image**: Multiple flyers for completely different events

**Steps**:
1. Upload image with 2+ flyers for different events
2. Complete event/year selection for each (if needed)
3. Verify each flyer has correct event association

**Expected Result**:
- Each flyer correctly identified with its own event
- Different venues recognized per flyer
- Different dates extracted per flyer

### Scenario 5: False Positive - Single Flyer Misdetected
**Purpose**: Handle case where AI mistakenly detects multiple flyers

**Test Image**: Single large flyer with complex layout

**Steps**:
1. Upload single flyer image
2. Observe AI detection result

**Expected Result**:
- Ideally detects as single flyer (x=0, y=0, width=1, height=1)
- If falsely splits, both results should still process
- No data loss or corruption

### Scenario 6: Large File with Many Flyers
**Purpose**: Test performance with many flyers

**Test Image**: Photo of 8-10 flyers

**Steps**:
1. Upload image
2. Monitor processing time
3. Check all flyers are processed

**Expected Result**:
- All flyers detected and processed
- Each gets separate AI analysis call (may take 2-3 minutes)
- Progress messages keep user informed
- All database records created successfully

### Scenario 7: Error Handling - Invalid Image
**Purpose**: Test graceful error handling

**Steps**:
1. Upload corrupted or invalid image file
2. Observe error message

**Expected Result**:
- Clear error message shown to user
- No partial records created in database
- System remains stable

### Scenario 8: Event/Year Selection Workflow
**Purpose**: Verify wizard still works with split flyers

**Test Image**: Multiple flyers with partial dates

**Steps**:
1. Upload multi-flyer image where some need event selection
2. Work through event selection wizard for each
3. Work through year selection wizard for each
4. Complete the upload

**Expected Result**:
- Wizard triggered for each flyer needing input
- Progress indicators show which flyer (e.g., "Flyer 2 of 3")
- All selections saved correctly
- Club nights created with correct associations

## API Testing

### Test Endpoint: POST /api/flyers/upload

**Request**:
```
POST /api/flyers/upload
Content-Type: multipart/form-data

file: [binary image data]
```

**Response Format**:
```json
{
  "success": true,
  "message": "Successfully processed all 3 flyer(s)",
  "totalFlyers": 3,
  "flyerResults": [
    {
      "success": true,
      "message": "Flyer 1 of 3: Flyer analyzed...",
      "flyer": { "id": 1, "fileName": "upload_flyer_0.jpg", ... },
      "analysisResult": { ... },
      "needsEventSelection": false,
      "flyerIndex": 1
    },
    {
      "success": true,
      "message": "Flyer 2 of 3: Flyer analyzed. Please select years for the dates.",
      "flyer": { "id": 2, "fileName": "upload_flyer_1.jpg", ... },
      "analysisResult": { ... },
      "needsEventSelection": false,
      "flyerIndex": 2
    },
    {
      "success": true,
      "message": "Flyer 3 of 3: Flyer analyzed successfully.",
      "flyer": { "id": 3, "fileName": "upload_flyer_2.jpg", ... },
      "analysisResult": { ... },
      "needsEventSelection": false,
      "flyerIndex": 3
    }
  ]
}
```

### Manual API Test with cURL:

```bash
# Upload a test image
curl -X POST http://localhost:5000/api/flyers/upload \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@path/to/multi-flyer-image.jpg" \
  -v
```

## Performance Considerations

### Expected Processing Times:
- **Single flyer**: 10-30 seconds (1 AI call)
- **2 flyers**: 20-60 seconds (3 AI calls: 1 detection + 2 analysis)
- **4 flyers**: 40-120 seconds (5 AI calls: 1 detection + 4 analysis)

### Timeouts:
- Frontend has 5-minute timeout
- Backend Kestrel configured for long-running operations
- Each Gemini AI call can take 10-30 seconds

## Database Verification

After upload, check database:

```sql
-- View all flyers
SELECT id, fileName, eventId, venueId, earliestClubNightDate, uploadedAt
FROM "Flyers"
ORDER BY uploadedAt DESC
LIMIT 10;

-- View associated club nights
SELECT cn.id, cn.date, e.name as event, v.name as venue, f.fileName
FROM "ClubNights" cn
JOIN "Events" e ON cn.eventId = e.id
JOIN "Venues" v ON cn.venueId = v.id
JOIN "Flyers" f ON cn.flyerId = f.id
ORDER BY f.uploadedAt DESC, cn.date
LIMIT 20;
```

## Troubleshooting

### Issue: All flyers shown as single flyer
**Cause**: AI detection prompt may need tuning
**Fix**: Check Gemini API response in logs for detection phase

### Issue: Cropped images are too small
**Cause**: Bounding box coordinates may be inaccurate
**Check**: Minimum crop size is 10x10 pixels (see validation code)

### Issue: Processing takes too long
**Cause**: Many AI calls being made
**Expected**: Each flyer requires a separate analysis call
**Mitigation**: Keep users informed with progress messages

### Issue: Some flyers fail to process
**Check**: Individual flyer results in response array
**Note**: Other flyers should still succeed independently

## Success Criteria

✅ Single flyer uploads work as before (regression test)
✅ Multiple flyers detected and split correctly
✅ Each flyer analyzed independently
✅ Separate database records created per flyer
✅ Progress messages shown to user
✅ Event/year selection works for each flyer
✅ No data loss or corruption
✅ Error handling is graceful

## Future Enhancements

- **Parallel Processing**: Currently processes flyers sequentially; could parallelize AI calls
- **Preview**: Show user the detected/split flyers before processing
- **Manual Adjustment**: Let user adjust bounding boxes if detection is wrong
- **Batch Progress Bar**: Real-time progress indicator instead of just messages
- **Smart Grouping**: If multiple flyers are for same event, batch process them together

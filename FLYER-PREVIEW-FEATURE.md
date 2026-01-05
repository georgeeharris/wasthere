# Flyer Upload Preview Feature

## Overview

This feature allows users to preview the club nights that will be created from an uploaded flyer before they are actually created in the database. The preview shows all details including the flyer image, event name, venue, date, and lineup, formatted similarly to the club night detail page.

## User Flow

1. **Upload Flyer**: User selects and uploads a flyer image
2. **AI Analysis**: The backend analyzes the flyer using Google Gemini AI
3. **Preview Modal**: Before any club nights are created, a modal appears showing:
   - The uploaded/split flyer image
   - All detected club night details (event, venue, date, acts)
   - Navigation to view multiple club nights if more than one was detected
   - Warnings about required user input (event selection, year selection)
4. **User Decision**:
   - **Confirm**: Proceed to event/year selection (if needed) or create club nights
   - **Cancel**: Delete the uploaded flyer(s) and abort the process

## Components

### FlyerPreviewModal

Located at: `wasthere-web/src/components/FlyerPreviewModal.tsx`

**Purpose**: Displays a preview of club nights that will be created from uploaded flyer(s).

**Props**:
- `flyerResults`: Array of successfully analyzed flyer upload results
- `onConfirm`: Callback when user confirms and wants to proceed
- `onCancel`: Callback when user cancels the upload

**Features**:
- Displays flyer image alongside club night details
- Navigation between multiple club nights (Previous/Next buttons)
- Counter showing current position (e.g., "Club Night 2 of 5")
- Notices when user input will be required (event selection, year selection)
- Responsive layout (side-by-side on desktop, stacked on mobile)

### FlyerList Updates

Located at: `wasthere-web/src/components/FlyerList.tsx`

**Key Changes**:

1. Added state for preview modal:
   ```typescript
   const [showPreview, setShowPreview] = useState(false);
   const [pendingFlyerResults, setPendingFlyerResults] = useState<FlyerUploadResult[]>([]);
   ```

2. Modified `handleUpload` function:
   - After successful upload and analysis, shows preview modal instead of immediately proceeding
   - Only shows preview for flyers with successful analysis results
   
3. Added `handlePreviewConfirm`:
   - Closes preview modal
   - Proceeds to existing event/year selection flow if needed
   - Reloads flyer list to show created club nights if no user input needed

4. Added `handlePreviewCancel`:
   - Closes preview modal
   - Deletes all uploaded flyers from the database
   - Shows error message that upload was cancelled

## CSS Styling

Located at: `wasthere-web/src/App.css`

**New Styles**:
- `.preview-modal-content`: Large modal container (max-width: 1200px)
- `.modal-subtitle`: Helper text below modal title
- `.preview-navigation`: Navigation controls with Previous/Next buttons
- `.club-night-preview-layout`: Two-column grid (flyer image + details)
- `.club-night-preview-flyer`: Sticky container for flyer image
- `.preview-info-card`: Card container for club night details
- `.preview-notice`: Warning box for required user input
- Responsive breakpoints for mobile devices

## Backend Changes

**No backend changes required** - The feature uses existing API endpoints:
- `POST /api/flyers/upload` - Already returns analysis results
- `DELETE /api/flyers/{id}` - Used when user cancels
- `POST /api/flyers/{id}/complete-upload` - Used after confirmation

## How Split Flyers Work

When a flyer image contains multiple flyers:

1. Backend detects bounding boxes and splits the image
2. Each split flyer is saved separately with unique file paths
3. Backend analyzes each split flyer individually
4. Frontend receives multiple `FlyerUploadResult` objects
5. Preview modal shows all club nights from all split flyers
6. User can navigate through all detected club nights
7. Each club night displays its corresponding split flyer image

## Testing Scenarios

### Scenario 1: Single Flyer, Single Club Night
- Upload a flyer with one event
- Preview shows one club night
- No navigation buttons (only 1 of 1)
- Confirm proceeds to completion

### Scenario 2: Single Flyer, Multiple Club Nights
- Upload a flyer advertising multiple dates
- Preview shows all club nights
- Navigation allows viewing each one
- All club nights show the same flyer image

### Scenario 3: Split Flyer (Multiple Flyers in One Image)
- Upload an image with multiple distinct flyers
- Backend splits into individual images
- Preview shows club nights from all split flyers
- Each club night shows its specific split flyer image

### Scenario 4: Missing Event Name
- Upload a flyer where AI can't detect event name
- Preview shows "Unknown Event"
- Notice indicates event selection will be required
- Confirm proceeds to event selection modal

### Scenario 5: Ambiguous Year
- Upload a flyer with date but no year (e.g., "Saturday 15 June")
- Preview shows candidate years (e.g., "Year: 2002 or 2003")
- Notice indicates year selection will be required
- Confirm proceeds to year selection modal

### Scenario 6: User Cancels
- Upload a flyer
- View preview
- Click "Cancel Upload"
- Flyer is deleted from database
- Upload form is reset
- Error message shown: "Upload cancelled. Flyers have been deleted."

## Future Enhancements

Potential improvements for future iterations:

1. **Edit in Preview**: Allow editing club night details directly in preview
2. **Batch Operations**: Allow selecting which club nights to create
3. **Image Cropping**: Preview and adjust split flyer boundaries
4. **Confidence Indicators**: Show AI confidence scores for detected information
5. **Preview History**: Keep a history of recently previewed uploads

## Compatibility

- Works with all existing flyer upload features
- Compatible with event selection modal
- Compatible with year selection modal  
- Works with single and multiple flyers
- Works with split flyers
- Responsive design for mobile and desktop

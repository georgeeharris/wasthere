# Testing the Error Diagnostics System

## Prerequisites

- Backend API running (WasThere.Api)
- Frontend running (wasthere-web)
- Google Gemini API key configured

## Test Scenarios

### Test 1: Successful Upload (No Error Display)

**Purpose:** Verify that diagnostics are captured but not shown when upload succeeds

**Steps:**
1. Navigate to the Flyers tab
2. Select a valid flyer image (JPEG/PNG, < 10MB)
3. Click "Upload and Analyze Flyer"
4. Wait for processing

**Expected Result:**
- Green success message appears
- No error popup is shown
- Flyer appears in the list with extracted information
- Browser console should NOT show any errors

**Diagnostics Verification:**
- Backend logs should show successful processing steps
- All steps should be "completed" status

---

### Test 2: Invalid File Type

**Purpose:** Test error diagnostics for invalid file uploads

**Steps:**
1. Navigate to the Flyers tab
2. Try to select a non-image file (e.g., .txt, .pdf)
3. Click "Upload and Analyze Flyer"

**Expected Result:**
- Error popup appears with message about invalid file type
- Popup shows diagnostic information
- Can expand "Show Diagnostics" section
- No flyer is added to the list

**Diagnostics to Verify:**
- Error message clearly states invalid file type
- Metadata shows file information
- Steps show where validation failed

---

### Test 3: API Configuration Error

**Purpose:** Test diagnostics when Gemini API key is missing

**Setup:** Temporarily remove or rename the GOOGLE_GEMINI_API_KEY environment variable

**Steps:**
1. Restart the backend API
2. Navigate to Flyers tab
3. Select a valid image file
4. Click "Upload and Analyze Flyer"

**Expected Result:**
- Error popup appears
- Message indicates API key is not configured
- Diagnostics show "API Key Check" step failed
- Copy Diagnostics button works

**Diagnostics to Verify:**
- "API Key Check" step shows "failed" status
- Error clearly mentions API key configuration
- No subsequent processing steps appear

**Cleanup:** Restore the API key configuration

---

### Test 4: Large File (Potential Timeout)

**Purpose:** Test timeout handling and diagnostics

**Steps:**
1. Select a very large image file (> 5MB, ideally close to 10MB limit)
2. Click "Upload and Analyze Flyer"
3. Wait up to 5 minutes for processing

**Expected Result:**
- If successful: Success message with longer duration in diagnostics
- If timeout: Error popup with timeout message
- Diagnostics show how long API call took before timeout

**Diagnostics to Verify:**
- "Call Gemini API" step shows duration
- If timeout occurs, duration should be close to 300,000ms (5 minutes)
- Metadata shows image size in bytes

---

### Test 5: Copy Diagnostics Functionality

**Purpose:** Test the copy-to-clipboard feature

**Steps:**
1. Trigger any error (easiest: invalid file type)
2. When error popup appears, click "Show Diagnostics"
3. Click "Copy Diagnostics" button
4. Open a text editor and paste (Ctrl+V / Cmd+V)

**Expected Result:**
- Button shows "✓ Copied!" for 3 seconds
- Pasted text contains:
  - Error Report header
  - Error Message section
  - Metadata section with key-value pairs
  - Processing Steps with status indicators
  - Browser Information
- Text is well-formatted and readable

---

### Test 6: Diagnostic Details Expansion

**Purpose:** Test UI interaction with diagnostic details

**Steps:**
1. Trigger any error
2. Click "▶ Show Diagnostics" button
3. Verify all sections appear
4. Click "▼ Hide Diagnostics" button
5. Verify sections collapse

**Expected Result:**
- Toggle button text changes appropriately
- Details smoothly show/hide
- All diagnostic sections render correctly:
  - Metadata table
  - Processing steps with status icons
  - Error messages
  - Stack traces (if available)

---

### Test 7: Modal Close Behavior

**Purpose:** Test modal interaction and cleanup

**Steps:**
1. Trigger an error
2. Click outside the modal (on overlay)
3. Verify modal closes
4. Trigger another error
5. Click the X button in modal header
6. Verify modal closes

**Expected Result:**
- Modal closes when clicking outside
- Modal closes when clicking X button
- Error state is cleared
- Can trigger new operations after closing

---

### Test 8: Network Error Simulation

**Purpose:** Test diagnostics for network failures

**Steps:**
1. Start uploading a file
2. Immediately disconnect network (or use browser dev tools to throttle to offline)
3. Wait for timeout

**Expected Result:**
- Error popup appears
- Error message indicates network or timeout issue
- Diagnostics show which step was in progress
- May show "started" status for API call step without "completed"

---

### Test 9: Backend Processing Error

**Purpose:** Test error handling in auto-populate

**Steps:**
1. Upload a flyer successfully
2. Click "Analyze" button on an existing flyer
3. Observe results

**Expected Result:**
- If successful: Success message
- If error: Error popup with diagnostics
- Diagnostics show separate from upload diagnostics

---

### Test 10: Multiple Errors in Sequence

**Purpose:** Verify error popup handles multiple sequential errors

**Steps:**
1. Trigger error #1 (e.g., invalid file)
2. Close error popup
3. Immediately trigger error #2 (e.g., another invalid file)
4. Verify new error displays correctly

**Expected Result:**
- Each error shows independently
- Previous error diagnostics don't interfere with new ones
- Modal state resets between errors
- No memory leaks or state corruption

---

## Verification Checklist

After running all tests, verify:

- [ ] Error messages are clear and user-friendly
- [ ] Diagnostics are detailed and accurate
- [ ] Copy functionality works on all platforms
- [ ] Modal is responsive and accessible
- [ ] Performance is acceptable (no lag)
- [ ] Browser console shows no errors
- [ ] Backend logs match frontend diagnostics
- [ ] All UI interactions work smoothly

## Browser Compatibility

Test on multiple browsers:

- [ ] Chrome/Edge (Chromium)
- [ ] Firefox
- [ ] Safari (if available)
- [ ] Mobile browsers (responsive view)

## Known Limitations

- Copy to clipboard requires HTTPS or localhost (browser security)
- Very large diagnostic outputs may be truncated in some browsers
- Stack traces are only available for server-side errors

## Troubleshooting

If tests fail:

1. Check browser console for JavaScript errors
2. Check backend logs for exceptions
3. Verify API key configuration
4. Ensure all dependencies are installed
5. Try clearing browser cache
6. Verify network connectivity

## Reporting Issues

When reporting issues with the diagnostic system:

1. Use the "Copy Diagnostics" feature
2. Include browser version and OS
3. Describe steps to reproduce
4. Include screenshot of error popup
5. Note any browser console errors

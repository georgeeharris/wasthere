# Testing the 30-Second Timeout Fix

## What Was Fixed
The 30-second timeout during flyer uploads was caused by Kestrel's `MinResponseDataRate` limit. This has been fixed by disabling the rate limit in `WasThere.Api/Program.cs`.

## Quick Test Guide

### Prerequisites
- Docker or local development environment set up
- Valid Google Gemini API key
- A test flyer image (the included `HiRes-2.jpg` works well)

### Test Procedure

#### 1. Start the Application

**Using Docker:**
```bash
# Make sure your .env file has the API key
echo "GOOGLE_GEMINI_API_KEY=your-key-here" >> .env
echo "POSTGRES_PASSWORD=your-secure-password" >> .env

# Start the application
docker compose up -d

# Watch the logs
docker compose logs -f
```

**Using Local Development:**
```bash
# Terminal 1: Start the API
cd WasThere.Api
dotnet run

# Terminal 2: Start the frontend
cd wasthere-web
npm run dev
```

#### 2. Upload a Flyer

1. Open your browser:
   - Docker: `http://localhost`
   - Local dev: `http://localhost:5173`

2. Navigate to the **Flyers** tab

3. Click **Upload New Flyer**

4. Select a test image:
   - Use the included `HiRes-2.jpg` file
   - Or use your own flyer image (2-10 MB recommended for testing)

5. Click **Upload and Analyze Flyer**

#### 3. Monitor the Upload

**What to watch for:**

**Time: 0-5 seconds**
- File uploads to server
- "Uploading and Analyzing..." message appears

**Time: 5-30 seconds**
- ⚠️ **CRITICAL WINDOW** - This is where the old bug occurred
- Server is calling Gemini API
- No data being sent to client
- **VERIFY**: No "Failed to fetch" error appears at 30 seconds

**Time: 30-60+ seconds**
- Server still waiting for AI response
- Should continue without timeout
- **VERIFY**: Still showing "Uploading and Analyzing..."

**Time: Until completion (typically 30-90 seconds total)**
- ✅ **SUCCESS**: Analysis completes
- Success message appears with extracted data
- Flyer appears in the list

#### 4. Expected Results

✅ **Success Indicators:**
- No timeout at 30 seconds
- AI analysis completes
- Event, venue, and acts are extracted
- Flyer appears in the list with data
- No error messages

❌ **Failure Indicators:**
- "Failed to fetch" error at ~30 seconds → Fix didn't apply
- Timeout after 5 minutes → Expected (frontend timeout working)
- Immediate errors → Different issue (API key, network, etc.)

### Detailed Testing Scenarios

#### Scenario 1: Fast AI Response (< 20 seconds)
**Purpose**: Verify normal operation isn't affected

1. Upload a small, simple flyer (< 1 MB)
2. Should complete in 10-20 seconds
3. ✅ Should work without issues

**Expected**: Quick success

#### Scenario 2: Medium AI Response (20-40 seconds)
**Purpose**: Test the critical 30-second window

1. Upload a medium-sized flyer (2-5 MB)
2. Watch carefully between 25-35 seconds
3. ✅ Should NOT timeout at 30 seconds
4. Should complete around 30-40 seconds

**Expected**: Crosses 30-second mark without timeout

#### Scenario 3: Slow AI Response (40-90 seconds)
**Purpose**: Verify extended processing works

1. Upload a large, complex flyer (5-10 MB)
2. AI processing may take 60+ seconds
3. ✅ Should complete without timeout
4. May take 60-90 seconds

**Expected**: Completes successfully even after 60+ seconds

#### Scenario 4: Very Slow Response (> 5 minutes)
**Purpose**: Verify frontend timeout still works

1. Upload a very large file (if available)
2. If API is extremely slow or hangs
3. ✅ Should timeout at 5 minutes with message:
   "Request timed out. The AI analysis is taking longer than expected."

**Expected**: Clean timeout message at 5 minutes

### Monitoring Server Logs

**Docker:**
```bash
# Watch API logs in real-time
docker compose logs -f wasthere-api

# Search for specific patterns
docker logs wasthere-api 2>&1 | grep -i "gemini"
docker logs wasthere-api 2>&1 | grep -i "timeout"
docker logs wasthere-api 2>&1 | grep -i "abort"
```

**Local Development:**
Check the console where you ran `dotnet run`

**What to look for:**

✅ **Good signs:**
```
info: GoogleGeminiService[0]
      Gemini raw response: {"clubNights":[...]}
```
- Indicates API call succeeded
- Response was received and processed

❌ **Bad signs:**
```
Connection ID "...", Request ID "...": the application completed without reading the entire request body.
```
- May indicate Kestrel aborted connection

```
System.Threading.Tasks.TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout
```
- Indicates API timeout (might need to check Gemini API status)

### Browser Developer Tools

Open browser DevTools (F12) and watch the Network tab:

**What to monitor:**
1. POST request to `/api/flyers/upload`
2. Watch the "Time" column
3. Should see request pending for 30-60+ seconds
4. Should complete with 201 status code

**If timeout occurs at 30 seconds:**
- Check if response shows "Failed to fetch"
- Check Console tab for JavaScript errors
- Verify the fix was applied and server restarted

### Quick Validation Checklist

Before testing:
- [ ] Code changes applied to `WasThere.Api/Program.cs`
- [ ] Application rebuilt (or Docker image rebuilt)
- [ ] Application restarted
- [ ] Google Gemini API key is configured
- [ ] Test flyer image is ready

During testing:
- [ ] Upload starts successfully
- [ ] "Uploading and Analyzing..." message shows
- [ ] No timeout at 30 seconds ⭐ **MOST IMPORTANT**
- [ ] Processing continues past 30 seconds
- [ ] Analysis completes successfully
- [ ] Extracted data is shown

After testing:
- [ ] Flyer appears in the list
- [ ] Event and venue names extracted
- [ ] Acts/DJs extracted
- [ ] No errors in browser console
- [ ] No errors in server logs

## Troubleshooting

### Still Getting 30-Second Timeout

**Check #1: Is the fix applied?**
```bash
# Docker
docker exec wasthere-api cat /app/Program.dll 2>/dev/null || echo "Check source files"

# Or rebuild and restart
docker compose down
docker compose build --no-cache api
docker compose up -d
```

**Check #2: Is old container still running?**
```bash
docker ps
# Make sure you see the wasthere-api container
# Note the "Created" time - should be recent
```

**Check #3: Check the source code**
```bash
# Verify the fix is in Program.cs
grep -A 5 "MinResponseDataRate" WasThere.Api/Program.cs
# Should show: serverOptions.Limits.MinResponseDataRate = null;
```

### Getting Different Errors

**"No API key configured"**
- Check `.env` file has `GOOGLE_GEMINI_API_KEY=...`
- Restart the application after adding the key

**"Failed to analyze flyer"**
- Check server logs for detailed error
- May be a Gemini API issue, not timeout

**Timeout after 5 minutes**
- This is expected - frontend timeout
- AI processing is genuinely taking too long
- Try a smaller image

### Performance Notes

Typical Gemini API response times:
- Small flyer (< 1 MB): 10-30 seconds
- Medium flyer (1-3 MB): 20-45 seconds
- Large flyer (3-10 MB): 30-90 seconds
- Very large flyer (> 10 MB): 60-120+ seconds

The fix allows all of these to complete without timeout (up to 5 minutes).

## Success Criteria

✅ **Fix is confirmed working when:**

1. **Primary test**: Upload completes successfully without timeout at 30 seconds
2. **Critical test**: AI processing takes > 30 seconds and still completes
3. **Extended test**: Processing can take 60+ seconds without timeout
4. **Safety test**: Frontend timeout (5 min) still triggers if needed
5. **Logs test**: Server logs show "Gemini raw response:" (API completed)
6. **No errors**: No connection abort or timeout errors in logs

## Additional Testing

### Test with Multiple Concurrent Uploads

1. Open multiple browser tabs
2. Upload flyers in each tab simultaneously
3. ✅ All should complete without timeout
4. Server handles multiple long-running requests

### Test with Different Network Conditions

1. Test on slow network connection
2. ✅ Slow upload shouldn't cause timeout
3. ✅ MinRequestBodyDataRate = null helps here too

### Test with Error Conditions

1. Upload invalid file type
2. Upload very large file (> 10 MB)
3. ✅ Should get proper error messages, not timeouts

## Documentation

After successful testing:
1. Results confirm fix works
2. Update issue with test results
3. Close the issue as resolved
4. Document any observed performance characteristics

## Need Help?

If issues persist:
1. Copy the diagnostic output (if upload fails)
2. Copy server logs from the time of upload
3. Note the exact time when timeout occurs
4. Report in the issue with all details

## Related Documentation

- `TIMEOUT-FIX-30SEC-SUMMARY.md` - Technical explanation of the fix
- `TIMEOUT-FIX-SUMMARY.md` - Previous timeout fix (5 minutes)
- `ERROR-DIAGNOSTICS-DOCUMENTATION.md` - Error diagnostic system
- `TESTING-ERROR-DIAGNOSTICS.md` - Testing error diagnostics

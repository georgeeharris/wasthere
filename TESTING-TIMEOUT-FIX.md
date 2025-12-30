# Quick Reference: Testing the Timeout Fix

## What Was Fixed
The "Failed to fetch" error that occurred after ~20 seconds when uploading flyers has been resolved. The timeout has been increased from the browser default (~20-30 seconds) to 5 minutes (300 seconds) to accommodate the Google Gemini API processing time.

## How to Test

### 1. Start the Application

#### Using Docker (Recommended)
```bash
# Make sure you have the Google Gemini API key in your .env file
echo "GOOGLE_GEMINI_API_KEY=your-api-key-here" >> .env

# Start the application
docker compose up -d

# Check logs to verify everything is running
docker compose logs -f
```

#### Using Local Development
```bash
# Terminal 1: Start the backend
cd WasThere.Api
dotnet run

# Terminal 2: Start the frontend
cd wasthere-web
npm run dev
```

### 2. Test Flyer Upload

1. Open your browser to the frontend URL:
   - Docker: `http://localhost` or `http://your-server-ip`
   - Local dev: `http://localhost:5173`

2. Navigate to the **Flyers** tab

3. Click on **Upload New Flyer**

4. Select a high-resolution flyer image (the larger and more complex, the better for testing)
   - Test image included in repo: `HiRes-2.jpg`
   - Or use your own flyer image (2-10 MB recommended)

5. Click **Upload and Analyze Flyer**

6. Observe the behavior:
   - ✅ **Success**: The upload should complete without timing out, even if it takes 30-60+ seconds
   - ✅ **Progress**: You should see "Uploading and Analyzing..." during the process
   - ✅ **Result**: Event, venue, acts, and dates should be extracted from the flyer
   - ❌ **Timeout (5+ min)**: You'll see a user-friendly message: "Request timed out. The AI analysis is taking longer than expected. Please try again."

### 3. What to Look For

#### Expected Behavior
- Upload takes 20-60+ seconds (depending on image size and complexity)
- No "Failed to fetch" error before completion
- Success message showing extracted data
- Flyer appears in the list with extracted information

#### If It Still Times Out
If you still get a timeout after 5 minutes:
1. Check the backend logs for Google Gemini API errors
2. Verify your API key is valid: `docker exec wasthere-api env | grep GoogleGemini`
3. Try a smaller or simpler flyer image
4. Check your internet connection to Google's servers

### 4. Verify Error Handling

To test the improved error messages, you can:
1. Disconnect your internet temporarily
2. Try to upload a flyer
3. Verify you get a clear error message (not just "Failed to fetch")

### 5. Check the Logs

#### Docker
```bash
# Backend logs
docker compose logs wasthere-api -f

# Look for:
# - "Gemini raw response:" (successful API call)
# - Any timeout errors from the API
```

#### Local Development
Check the console output in the terminal where you ran `dotnet run`

## Troubleshooting

### "Failed to fetch" Still Occurs
- **After 5 minutes**: This is expected if the API is extremely slow. Consider:
  - Smaller image file
  - Retry the upload
  - Check Google Gemini API status

- **Before 5 minutes**: 
  - Check browser console for actual error
  - Verify backend is running and accessible
  - Check CORS configuration if using custom URLs

### "Request timed out" Message
This is the new, improved error message. It means:
- The Google Gemini API took longer than 5 minutes to respond
- This is rare but can happen with very large/complex images
- Solution: Try again with a smaller image or retry

### API Key Issues
```bash
# Verify API key is set (Docker)
docker exec wasthere-api env | grep GoogleGemini

# Should show: GoogleGemini__ApiKey=your-key-here
```

## Performance Notes

Typical Google Gemini API response times:
- Small flyer (< 1 MB): 10-30 seconds
- Medium flyer (1-3 MB): 20-45 seconds  
- Large flyer (3-10 MB): 30-90 seconds
- Very large flyer (> 10 MB): 60-120+ seconds

The 5-minute timeout provides a comfortable buffer for all these scenarios.

## What Changed Technically

### Backend
- Kestrel server timeouts increased from default (~60s) to 5 minutes
- No code changes to business logic
- No database changes

### Frontend
- Fetch calls now have explicit 5-minute timeout
- Better error messages
- Proper cleanup (no memory leaks)

### No Breaking Changes
- Existing functionality unchanged
- All features work as before
- Only change is increased timeout limit

## Success Criteria

✅ The fix is working if:
1. Flyers upload successfully without "Failed to fetch" errors
2. AI analysis completes and extracts event information
3. Upload can take 30-60+ seconds without timing out
4. If timeout occurs (rare), error message is clear and helpful

## Next Steps

After testing confirms the fix works:
1. ✅ Close this issue
2. Consider monitoring Google Gemini API response times in production
3. Consider adding progress indicators for long uploads
4. Consider implementing retry logic for failed uploads

## Questions?

If you encounter any issues or have questions:
1. Check `TIMEOUT-FIX-SUMMARY.md` for detailed technical explanation
2. Review backend logs for specific error messages
3. Verify Google Gemini API key is correctly configured
4. Ensure your API key has sufficient quota/credits

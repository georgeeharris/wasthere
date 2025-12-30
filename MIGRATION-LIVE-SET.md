# Migration Guide: Upgrading to Live Set Feature

## Overview
This guide explains how to upgrade your existing WasThere installation to include the new live set tracking feature.

## For Docker Deployments

### Step 1: Pull Latest Changes
```bash
cd /path/to/wasthere
git pull origin main
```

### Step 2: Rebuild and Restart Containers
```bash
# Stop current containers
docker compose down

# Rebuild with new code
docker compose build

# Start updated containers
docker compose up -d
```

The database migration will run automatically on startup.

### Step 3: Verify Migration
```bash
# Check API logs to confirm migration ran successfully
docker compose logs wasthere-api | grep Migration
```

You should see: `Applying migration '20251230132314_AddIsLiveSetToClubNightAct'`

## For Local Development

### Step 1: Update Database
```bash
cd WasThere.Api
dotnet ef database update
```

### Step 2: Restart API
```bash
# Stop the API (Ctrl+C)
# Start it again
dotnet run
```

### Step 3: Rebuild Frontend
```bash
cd wasthere-web
npm install
npm run build
```

## What Happens to Existing Data?

### Existing ClubNightAct Records
All existing `ClubNightAct` records will automatically have `IsLiveSet = false` set. This means:
- All existing performances are treated as DJ sets
- This is the correct default for the vast majority of club events
- No manual data cleanup is required

### Existing Act Names
If you have acts in your database like:
- "Dave Clarke (live)"
- "Dave Clarke (DJ set)"
- "Dave Clarke"

**These will NOT be automatically merged.** They remain as separate acts. However:
1. Going forward, new entries will use just "Dave Clarke" with the live flag
2. You can manually merge duplicate acts if needed:
   - Edit club nights to use the canonical act name
   - Delete the duplicate acts
   - Set the live flag appropriately on each club night

### Future Flyer Auto-Population
When you auto-populate from flyers after the upgrade:
- Act names will be cleaned automatically (e.g., "Dave Clarke (live)" → "Dave Clarke")
- The live set flag will be detected and set correctly
- New acts will follow the new pattern

## Rolling Back (If Needed)

If you need to roll back:

### Docker Deployment
```bash
# Stop containers
docker compose down

# Checkout previous version
git checkout <previous-commit-hash>

# Rebuild and restart
docker compose build
docker compose up -d
```

### Local Development
```bash
# Remove the migration
cd WasThere.Api
dotnet ef migrations remove

# Checkout previous version
git checkout <previous-commit-hash>
```

**Note:** Rolling back will lose the `IsLiveSet` data for any club nights created after the upgrade.

## Manual Data Cleanup (Optional)

If you want to clean up duplicate acts in your existing data:

### Step 1: Identify Duplicates
Query your database to find acts with similar names:
```sql
SELECT * FROM "Acts" 
WHERE "Name" LIKE '%live%' OR "Name" LIKE '%DJ%'
ORDER BY "Name";
```

### Step 2: Choose Canonical Names
Decide which act name to keep (usually the one without indicators):
- Keep: "Dave Clarke"
- Merge: "Dave Clarke (live)", "Dave Clarke (DJ set)"

### Step 3: Update Club Nights
For each club night using the duplicate act:
1. Edit the club night in the UI
2. Remove the duplicate act (e.g., "Dave Clarke (live)")
3. Add the canonical act (e.g., "Dave Clarke")
4. Check the "Live" box if it was a live performance
5. Save

### Step 4: Delete Unused Acts
Once no club nights reference the duplicate acts, you can delete them:
1. Go to "Master Lists" tab
2. Find the duplicate act
3. Click "Delete"

## Verifying the Upgrade

After upgrading, verify these features work:

1. **View existing club nights**
   - All should load correctly
   - Acts appear without "(live)" or "(DJ set)" unless it's part of the actual name

2. **Create new club night**
   - ActSelector component appears
   - Can mark acts as live sets
   - Live acts show with red "LIVE" badge

3. **Auto-populate from flyer**
   - Upload a test flyer
   - Click "Auto-populate"
   - Verify live sets are detected and marked correctly

4. **API endpoints**
   - GET /api/clubnights returns `isLiveSet` in acts array
   - POST /api/clubnights accepts `acts: [{ actId, isLiveSet }]`

## Support

If you encounter issues:

1. **Check API logs:**
   - Docker: `docker compose logs wasthere-api`
   - Local: Check console output

2. **Check browser console:**
   - Open browser DevTools (F12)
   - Look for JavaScript errors

3. **Verify database schema:**
   ```bash
   # Connect to database and check table
   docker compose exec wasthere-db psql -U postgres -d wasthere -c "\d \"ClubNightActs\""
   ```
   Should show `IsLiveSet` boolean column.

4. **Review migration status:**
   ```bash
   cd WasThere.Api
   dotnet ef migrations list
   ```
   Should show `20251230132314_AddIsLiveSetToClubNightAct` as applied.

## Summary

The upgrade is straightforward and non-breaking:
- ✅ Existing data remains intact
- ✅ Existing functionality continues to work
- ✅ New live set tracking is available immediately
- ✅ No manual data migration required
- ✅ Optional cleanup can be done at your pace

The migration adds value without disrupting existing workflows!

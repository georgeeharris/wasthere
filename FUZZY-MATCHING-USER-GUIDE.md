# Data Consistency Improvements for Flyer Uploads

## Overview

This update improves how the system handles venue and event names when processing flyers, reducing duplicates and maintaining cleaner data.

## What's Changed

### For Users

When you upload flyers, the system now:

1. **Ignores addresses in venue names** - The AI will extract just "The Que Club" instead of "The Que Club, Corporation Street, Birmingham B1 5QS"

2. **Matches similar names automatically** - If you already have "Sankey's Soap" in the database, uploading a flyer with "Sankeys Soap" will use your existing entry instead of creating a duplicate

3. **Handles OCR errors** - If the AI misreads text (e.g., "Busted Out" instead of "Bugged Out"), it will still match to the correct existing event

### For Administrators

**You control the "correct" version:**
- When you create venues and events manually, those become the canonical (official) versions
- Future flyer uploads with similar names will automatically use your versions
- This means you decide the proper formatting (apostrophes, capitalization, etc.)

**Examples:**
- Admin creates: "Sankey's Soap" (with apostrophe)
- Flyer contains: "Sankeys Soap" or "SANKEYS SOAP"
- System uses: "Sankey's Soap" (your version)

## How It Works

The system uses "fuzzy matching" to compare names:
- Ignores case differences (upper/lower case)
- Ignores punctuation (apostrophes, hyphens)
- Removes address information
- Calculates similarity score
- Uses existing database entry if similarity ≥ 80%

## Benefits

✅ Fewer duplicate venues and events
✅ Consistent naming across the database
✅ Admins maintain control over the canonical versions
✅ Handles common OCR/scanning errors
✅ Less manual cleanup needed

## What You Might Notice

- When uploading flyers, you may see log messages about "fuzzy matching" - this is normal
- The system will favor existing entries over creating new ones
- If a match isn't found (too different), a new entry is created as before

## Technical Details

For developers and technical users, see:
- [FUZZY-MATCHING-IMPLEMENTATION.md](FUZZY-MATCHING-IMPLEMENTATION.md) - Full technical documentation
- Similarity thresholds: 80% for venues/events, 85% for acts
- Algorithm: Levenshtein distance with string normalization

---

**Questions or Issues?**
If you notice incorrect matching (false positives), please report it with specific examples so thresholds can be adjusted.

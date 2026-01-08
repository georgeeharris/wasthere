# "Was There" Feature - Visual Guide

## Club Nights Page

### Regular Club Night Card (Not Attended)
```
┌────────────────────────────────────────┐
│  Bugged Out                            │
│  Sankey's Soap                         │
│                           15 Jan 2005  │
│                                        │
│  ☐ Was there                           │
│                                        │
│  Line-up:                              │
│  • Erol Alkan                          │
│  • Boys Noize                          │
│                                        │
│  [View Details] [Edit] [Delete]        │
└────────────────────────────────────────┘
```

### Club Night Card with "Was There" Marked
```
╔════════════════════════════════════════╗  ← Yellow border (3px #ffd700)
║  Bugged Out                            ║     with glow effect
║  Sankey's Soap                         ║
║                           15 Jan 2005  ║
║                                        ║
║  ☑ Was there                           ║  ← Checked checkbox
║                                        ║
║  Line-up:                              ║
║  • Erol Alkan                          ║
║  • Boys Noize                          ║
║                                        ║
║  [View Details] [Edit] [Delete]        ║
╚════════════════════════════════════════╝
```

### Filter Panel
```
┌────────────────────────────────────────┐
│  ▼ Filters                   [Clear All]│
│                                        │
│  Event:      [All events ▼]           │
│  Venue:      [All venues ▼]           │
│  Date From:  [________]                │
│  Date To:    [________]                │
│                                        │
│  Acts (select one or more):            │
│  ☐ Erol Alkan                          │
│  ☐ Boys Noize                          │
│  ☐ Laurent Garnier                     │
│                                        │
│  ☑ Only show nights I was there        │  ← New filter option
└────────────────────────────────────────┘
```

## Timeline Page

### Timeline Filter (Top Level)
```
┌────────────────────────────────────────┐
│  Timeline                              │
│                                        │
│  Select up to 3 events:                │
│  [Bugged Out, Fabric, Tribal Gathering]│
│                                        │
│  ☑ Only show nights I was there        │  ← Simple checkbox filter
└────────────────────────────────────────┘
```

### Timeline Cards - Side by Side View

#### Without "Was There"
```
┌──────────────────┐  ┌──────────────────┐
│  15 Jan 2005     │  │  22 Jan 2005     │
│  Sankey's Soap   │  │  Fabric          │
│  Erol Alkan,     │  │  Laurent Garnier │
│  Boys Noize      │  │                  │
│                  │  │                  │
│  ☐ Was there     │  │  ☐ Was there     │
└──────────────────┘  └──────────────────┘
```

#### With "Was There" Marked
```
╔══════════════════╗  ┌──────────────────┐
║  15 Jan 2005     ║  │  22 Jan 2005     │  ← Only left one is marked
║  Sankey's Soap   ║  │  Fabric          │     with yellow border
║  Erol Alkan,     ║  │  Laurent Garnier │
║  Boys Noize      ║  │                  │
║                  ║  │                  │
║  ☑ Was there     ║  │  ☐ Was there     │
╚══════════════════╝  └──────────────────┘
```

## CSS Classes Applied

### Yellow Border Styling
```css
.club-night-card.was-there,
.timeline-card.was-there {
  border: 3px solid #ffd700;
  box-shadow: 0 0 8px rgba(255, 215, 0, 0.3);
}

.club-night-card.was-there:hover,
.timeline-card.was-there:hover {
  box-shadow: 0 4px 12px rgba(255, 215, 0, 0.4);
}
```

### Checkbox Styling
```css
.was-there-checkbox .checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 500;
  color: #646cff;
  cursor: pointer;
}

.was-there-checkbox input[type="checkbox"] {
  cursor: pointer;
  width: 1.1rem;
  height: 1.1rem;
}
```

## User Interaction Flow

### Marking "Was There"
1. User clicks checkbox on a club night card
2. Frontend calls `POST /api/clubnights/{id}/was-there`
3. Backend creates UserClubNightAttendance record
4. Frontend refreshes club nights list
5. Card now shows with yellow border and checked checkbox

### Unmarking "Was There"
1. User unchecks checkbox on a marked club night card
2. Frontend calls `DELETE /api/clubnights/{id}/was-there`
3. Backend deletes UserClubNightAttendance record
4. Frontend refreshes club nights list
5. Card returns to normal appearance

### Using Filters
1. User checks "Only show nights I was there"
2. Frontend filters local club nights array
3. Only cards with `wasThereByAdmin: true` are displayed
4. Filter can be combined with other filters (event, venue, date, acts)

## Color Reference

- **Yellow Border**: `#ffd700` (gold)
- **Glow Effect**: `rgba(255, 215, 0, 0.3)` (30% opacity gold)
- **Hover Glow**: `rgba(255, 215, 0, 0.4)` (40% opacity gold)
- **Checkbox Color**: `#646cff` (site's primary blue)

## Responsive Behavior

The feature works consistently across different screen sizes:
- Mobile: Checkboxes remain clickable and visible
- Tablet: Cards stack vertically with full yellow borders visible
- Desktop: Timeline cards appear side-by-side with borders clearly distinguishing attended events

## Accessibility

- Checkboxes are properly labeled with "Was there" text
- Color is not the only indicator (checkbox state also conveys information)
- Yellow/gold provides good contrast against white/light gray backgrounds
- Keyboard navigation supported (standard checkbox behavior)

import { useState, useEffect, useCallback } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import type { ClubNight, Event } from '../types';
import { clubNightsApi, eventsApi, flyersApi } from '../services/api';
import { SearchableMultiSelect } from './SearchableMultiSelect';

const MAX_EVENTS = 3;

type GroupedClubNights = Record<number, Record<number, Record<string, ClubNight[]>>>;

export function Timeline() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const [events, setEvents] = useState<Event[]>([]);
  const [selectedEventIds, setSelectedEventIds] = useState<number[]>([]);
  const [clubNights, setClubNights] = useState<ClubNight[]>([]);
  const [loading, setLoading] = useState(false);
  const [filterWasThere, setFilterWasThere] = useState<boolean>(false);

  useEffect(() => {
    loadEvents();
  }, []);

  // Read eventIds from URL on load
  useEffect(() => {
    const eventIdsParam = searchParams.get('eventIds');
    if (eventIdsParam) {
      const eventIds = eventIdsParam.split(',')
        .map(id => parseInt(id, 10))
        .filter(id => !isNaN(id));
      if (eventIds.length > 0) {
        setSelectedEventIds(eventIds.slice(0, MAX_EVENTS));
      }
    }
  }, [searchParams]);

  const loadClubNights = useCallback(async () => {
    setLoading(true);
    try {
      const allClubNights = await clubNightsApi.getAll();
      let filtered = allClubNights.filter(cn => selectedEventIds.includes(cn.eventId));
      
      // Apply "was there" filter
      if (filterWasThere) {
        filtered = filtered.filter(cn => cn.wasThereByAdmin);
      }
      
      // Sort by date ascending (oldest first)
      filtered.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
      setClubNights(filtered);
    } catch (error) {
      console.error('Failed to load club nights:', error);
    } finally {
      setLoading(false);
    }
  }, [selectedEventIds, filterWasThere]);

  useEffect(() => {
    if (selectedEventIds.length > 0) {
      loadClubNights();
    } else {
      setClubNights([]);
    }
  }, [selectedEventIds, loadClubNights]);

  const loadEvents = async () => {
    try {
      const eventsData = await eventsApi.getAll();
      setEvents(eventsData);
    } catch (error) {
      console.error('Failed to load events:', error);
    }
  };
  
  const handleWasThereToggle = async (clubNightId: number, currentStatus: boolean) => {
    try {
      if (currentStatus) {
        await clubNightsApi.unmarkWasThere(clubNightId);
      } else {
        await clubNightsApi.markWasThere(clubNightId);
      }
      await loadClubNights();
    } catch (error) {
      console.error('Failed to update was there status:', error);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  };

  const formatActs = (acts: ClubNight['acts']) => {
    if (acts.length === 0) return 'No acts listed';
    
    const actNames = acts.map(act => {
      if (act.isLiveSet) {
        return `${act.actName} (live)`;
      }
      return act.actName;
    });

    if (actNames.length === 1) return actNames[0];
    if (actNames.length === 2) return `${actNames[0]} and ${actNames[1]}`;
    
    const lastAct = actNames[actNames.length - 1];
    const otherActs = actNames.slice(0, -1).join(', ');
    return `${otherActs}, and ${lastAct}`;
  };

  // Group club nights by year, month, and date for multi-column display
  const groupedClubNights: GroupedClubNights = clubNights.reduce((acc, clubNight) => {
    const date = new Date(clubNight.date);
    const year = date.getFullYear();
    const month = date.getMonth();
    const dateKey = date.toISOString().split('T')[0]; // YYYY-MM-DD
    
    if (!acc[year]) {
      acc[year] = {};
    }
    if (!acc[year][month]) {
      acc[year][month] = {};
    }
    if (!acc[year][month][dateKey]) {
      acc[year][month][dateKey] = [];
    }
    acc[year][month][dateKey].push(clubNight);
    
    return acc;
  }, {} as GroupedClubNights);

  const getDateRange = () => {
    if (clubNights.length === 0) return null;
    
    const dates = clubNights.map(cn => new Date(cn.date));
    const minDate = new Date(Math.min(...dates.map(d => d.getTime())));
    const maxDate = new Date(Math.max(...dates.map(d => d.getTime())));
    
    return { minDate, maxDate };
  };

  const generateTimelineStructure = () => {
    const dateRange = getDateRange();
    if (!dateRange) return [];

    const { minDate, maxDate } = dateRange;
    const startYear = minDate.getFullYear();
    const endYear = maxDate.getFullYear();
    
    const structure: Array<{ year: number; month: number; dates: Array<{ dateKey: string; clubNights: ClubNight[] }> }> = [];
    
    for (let year = startYear; year <= endYear; year++) {
      const startMonth = year === startYear ? minDate.getMonth() : 0;
      const endMonth = year === endYear ? maxDate.getMonth() : 11;
      
      for (let month = startMonth; month <= endMonth; month++) {
        const datesInMonth = groupedClubNights[year]?.[month] || {};
        const dateEntries = Object.entries(datesInMonth)
          .sort(([dateKeyA], [dateKeyB]) => dateKeyA.localeCompare(dateKeyB))
          .map(([dateKey, nights]) => ({ dateKey, clubNights: nights }));
        
        structure.push({ year, month, dates: dateEntries });
      }
    }
    
    return structure;
  };

  const timelineStructure = generateTimelineStructure();

  const getMonthName = (month: number) => {
    return new Date(2000, month, 1).toLocaleDateString('en-GB', { month: 'long' });
  };

  // Update URL when event selection changes
  const handleEventChange = (eventIds: number[]) => {
    // Limit to MAX_EVENTS
    const limitedEventIds = eventIds.slice(0, MAX_EVENTS);
    setSelectedEventIds(limitedEventIds);
    if (limitedEventIds.length > 0) {
      setSearchParams({ eventIds: limitedEventIds.join(',') });
    } else {
      setSearchParams({});
    }
  };

  return (
    <div className="card">
      <div className="card-header">
        <h2>Timeline</h2>
      </div>

      <div className="timeline-selector">
        <label htmlFor="event-select">Select up to {MAX_EVENTS} events to view their timeline:</label>
        <SearchableMultiSelect
          options={events}
          selectedIds={selectedEventIds}
          onChange={handleEventChange}
          placeholder="Choose events..."
          maxSelections={MAX_EVENTS}
        />
        {selectedEventIds.length >= MAX_EVENTS && (
          <p className="timeline-limit-notice">Maximum of {MAX_EVENTS} events selected</p>
        )}
      </div>
      
      {selectedEventIds.length > 0 && (
        <div className="timeline-filter">
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={filterWasThere}
              onChange={(e) => setFilterWasThere(e.target.checked)}
            />
            Only show nights I was there
          </label>
        </div>
      )}

      {loading && <p className="loading-state">Loading club nights...</p>}

      {!loading && selectedEventIds.length === 0 && (
        <p className="empty-state">Please select at least one event to view its timeline.</p>
      )}

      {!loading && selectedEventIds.length > 0 && clubNights.length === 0 && (
        <p className="empty-state">No club nights found for the selected event(s).</p>
      )}

      {!loading && clubNights.length > 0 && (
        <div className="timeline-container">
          {timelineStructure.map((item, index) => {
            const isNewYear = index === 0 || item.year !== timelineStructure[index - 1].year;
            
            return (
              <div key={`${item.year}-${item.month}`} className="timeline-month">
                {isNewYear && (
                  <div className="timeline-year-marker">
                    <h3>{item.year}</h3>
                  </div>
                )}
                
                <div className="timeline-month-marker">
                  <h4>{getMonthName(item.month)}</h4>
                </div>
                
                <div className="timeline-events">
                  {item.dates.length === 0 ? (
                    <div className="timeline-empty">No events this month</div>
                  ) : (
                    item.dates.map((dateEntry) => (
                      <div 
                        key={dateEntry.dateKey}
                        className={`timeline-date-row ${selectedEventIds.length > 1 ? 'multi-event' : ''}`}
                        style={{ 
                          display: 'grid',
                          gridTemplateColumns: `repeat(${selectedEventIds.length}, 1fr)`,
                          gap: '1rem'
                        }}
                      >
                        {selectedEventIds.map((eventId) => {
                          const clubNight = dateEntry.clubNights.find(cn => cn.eventId === eventId);
                          
                          if (!clubNight) {
                            return (
                              <div 
                                key={`${dateEntry.dateKey}-${eventId}`}
                                className="timeline-card-placeholder"
                              />
                            );
                          }
                          
                          return (
                            <div 
                              key={clubNight.id} 
                              className={['timeline-card', 'timeline-card-clickable', clubNight.wasThereByAdmin && 'was-there'].filter(Boolean).join(' ')}
                            >
                              <div onClick={() => navigate(`/nights/${clubNight.id}`)}>
                                {clubNight.flyerThumbnailPath && (
                                  <div className="timeline-card-thumbnail">
                                    <img
                                      src={flyersApi.getImageUrl(clubNight.flyerThumbnailPath)}
                                      alt="Flyer thumbnail"
                                      className="timeline-thumbnail-image"
                                    />
                                  </div>
                                )}
                                <div className="timeline-card-content">
                                  <div className="timeline-card-date">{formatDate(clubNight.date)}</div>
                                  <div className="timeline-card-venue">{clubNight.venueName}</div>
                                  <div className="timeline-card-acts">{formatActs(clubNight.acts)}</div>
                                </div>
                              </div>
                              <div 
                                className="was-there-checkbox"
                                onClick={(e) => e.stopPropagation()}
                              >
                                <label className="checkbox-label">
                                  <input
                                    type="checkbox"
                                    checked={clubNight.wasThereByAdmin || false}
                                    onChange={() => handleWasThereToggle(clubNight.id, clubNight.wasThereByAdmin || false)}
                                  />
                                  Was there
                                </label>
                              </div>
                            </div>
                          );
                        })}
                      </div>
                    ))
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

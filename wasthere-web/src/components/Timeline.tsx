import { useState, useEffect } from 'react';
import type { ClubNight, Event } from '../types';
import { clubNightsApi, eventsApi } from '../services/api';

export function Timeline() {
  const [events, setEvents] = useState<Event[]>([]);
  const [selectedEventId, setSelectedEventId] = useState<number>(0);
  const [clubNights, setClubNights] = useState<ClubNight[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadEvents();
  }, []);

  useEffect(() => {
    if (selectedEventId > 0) {
      loadClubNights();
    } else {
      setClubNights([]);
    }
  }, [selectedEventId]);

  const loadEvents = async () => {
    try {
      const eventsData = await eventsApi.getAll();
      setEvents(eventsData);
    } catch (error) {
      console.error('Failed to load events:', error);
    }
  };

  const loadClubNights = async () => {
    setLoading(true);
    try {
      const allClubNights = await clubNightsApi.getAll();
      const filtered = allClubNights.filter(cn => cn.eventId === selectedEventId);
      // Sort by date ascending (oldest first)
      filtered.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
      setClubNights(filtered);
    } catch (error) {
      console.error('Failed to load club nights:', error);
    } finally {
      setLoading(false);
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

  // Group club nights by year and month
  const groupedClubNights = clubNights.reduce((acc, clubNight) => {
    const date = new Date(clubNight.date);
    const year = date.getFullYear();
    const month = date.getMonth();
    
    if (!acc[year]) {
      acc[year] = {};
    }
    if (!acc[year][month]) {
      acc[year][month] = [];
    }
    acc[year][month].push(clubNight);
    
    return acc;
  }, {} as Record<number, Record<number, ClubNight[]>>);

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
    
    const structure: Array<{ year: number; month: number; clubNights: ClubNight[] }> = [];
    
    for (let year = startYear; year <= endYear; year++) {
      const startMonth = year === startYear ? minDate.getMonth() : 0;
      const endMonth = year === endYear ? maxDate.getMonth() : 11;
      
      for (let month = startMonth; month <= endMonth; month++) {
        const nights = groupedClubNights[year]?.[month] || [];
        structure.push({ year, month, clubNights: nights });
      }
    }
    
    return structure;
  };

  const timelineStructure = generateTimelineStructure();

  const getMonthName = (month: number) => {
    return new Date(2000, month, 1).toLocaleDateString('en-GB', { month: 'long' });
  };

  return (
    <div className="card">
      <div className="card-header">
        <h2>Timeline</h2>
      </div>

      <div className="timeline-selector">
        <label htmlFor="event-select">Select an event to view its timeline:</label>
        <select
          id="event-select"
          value={selectedEventId}
          onChange={(e) => setSelectedEventId(Number(e.target.value))}
          className="input"
        >
          <option value={0}>Choose an event...</option>
          {events.map((event) => (
            <option key={event.id} value={event.id}>
              {event.name}
            </option>
          ))}
        </select>
      </div>

      {loading && <p className="loading-state">Loading club nights...</p>}

      {!loading && selectedEventId === 0 && (
        <p className="empty-state">Please select an event to view its timeline.</p>
      )}

      {!loading && selectedEventId > 0 && clubNights.length === 0 && (
        <p className="empty-state">No club nights found for this event.</p>
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
                
                <div className="timeline-row">
                  <div className="timeline-date-label">
                    <span className="month-name">{getMonthName(item.month)}</span>
                  </div>
                  
                  <div className="timeline-line-container">
                    <div className="timeline-line"></div>
                    <div className="timeline-dot"></div>
                  </div>
                  
                  <div className="timeline-events">
                    {item.clubNights.length === 0 ? (
                      <div className="timeline-empty">No events this month</div>
                    ) : (
                      item.clubNights.map((clubNight) => (
                        <div key={clubNight.id} className="timeline-card">
                          <div className="timeline-card-date">{formatDate(clubNight.date)}</div>
                          <div className="timeline-card-venue">{clubNight.venueName}</div>
                          <div className="timeline-card-acts">{formatActs(clubNight.acts)}</div>
                        </div>
                      ))
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

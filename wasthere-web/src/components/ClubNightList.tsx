import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import type { ClubNight, Event, Venue, Act, ClubNightActDto } from '../types';
import { clubNightsApi, eventsApi, venuesApi, actsApi } from '../services/api';
import { ActSelector } from './ActSelector';
import { usePagination } from '../hooks/usePagination';
import { Pagination } from './Pagination';

export function ClubNightList() {
  const navigate = useNavigate();
  const [clubNights, setClubNights] = useState<ClubNight[]>([]);
  const [events, setEvents] = useState<Event[]>([]);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [acts, setActs] = useState<Act[]>([]);
  
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState({
    date: '2005-01-07', // First Friday in 2005
    eventId: 0,
    venueId: 0,
    acts: [] as ClubNightActDto[],
  });

  // Filter and sort state
  const [showFilters, setShowFilters] = useState(false);
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [filterEventId, setFilterEventId] = useState<number>(0);
  const [filterVenueId, setFilterVenueId] = useState<number>(0);
  const [filterActIds, setFilterActIds] = useState<number[]>([]);
  const [filterDateFrom, setFilterDateFrom] = useState<string>('');
  const [filterDateTo, setFilterDateTo] = useState<string>('');

  // Apply filters and sorting to club nights
  const filteredAndSortedClubNights = clubNights
    .filter(cn => {
      if (filterEventId > 0 && cn.eventId !== filterEventId) return false;
      if (filterVenueId > 0 && cn.venueId !== filterVenueId) return false;
      if (filterActIds.length > 0) {
        const hasAct = cn.acts.some(act => filterActIds.includes(act.actId));
        if (!hasAct) return false;
      }
      if (filterDateFrom && new Date(cn.date) < new Date(filterDateFrom)) return false;
      if (filterDateTo && new Date(cn.date) > new Date(filterDateTo)) return false;
      return true;
    })
    .sort((a, b) => {
      const comparison = new Date(a.date).getTime() - new Date(b.date).getTime();
      return sortOrder === 'asc' ? comparison : -comparison;
    });

  const {
    currentPage,
    pageSize,
    paginatedItems,
    totalPages,
    totalItems,
    setPage,
    setPageSize,
  } = usePagination({ items: filteredAndSortedClubNights, initialPageSize: 10 });

  useEffect(() => {
    loadAll();
  }, []);

  const loadAll = async () => {
    try {
      const [clubNightsData, eventsData, venuesData, actsData] = await Promise.all([
        clubNightsApi.getAll(),
        eventsApi.getAll(),
        venuesApi.getAll(),
        actsApi.getAll(),
      ]);
      setClubNights(clubNightsData);
      setEvents(eventsData);
      setVenues(venuesData);
      setActs(actsData);
    } catch (error) {
      console.error('Failed to load data:', error);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.date || !formData.eventId || !formData.venueId) return;

    try {
      if (editingId) {
        await clubNightsApi.update(editingId, formData);
      } else {
        await clubNightsApi.create(formData);
      }
      resetForm();
      await loadAll();
    } catch (error) {
      console.error('Failed to save club night:', error);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure you want to delete this club night?')) return;

    try {
      await clubNightsApi.delete(id);
      await loadAll();
    } catch (error) {
      console.error('Failed to delete club night:', error);
    }
  };

  const startEdit = (clubNight: ClubNight) => {
    setEditingId(clubNight.id);
    setFormData({
      date: clubNight.date.split('T')[0],
      eventId: clubNight.eventId,
      venueId: clubNight.venueId,
      acts: clubNight.acts.map(a => ({ actId: a.actId, isLiveSet: a.isLiveSet })),
    });
    setShowForm(true);
  };

  const resetForm = () => {
    setShowForm(false);
    setEditingId(null);
    setFormData({
      date: '2005-01-07', // First Friday in 2005
      eventId: 0,
      venueId: 0,
      acts: [],
    });
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  };

  return (
    <div className="card">
      <div className="card-header">
        <h2>Club Nights</h2>
        {!showForm && (
          <button onClick={() => setShowForm(true)} className="btn btn-primary">
            Add Club Night
          </button>
        )}
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="club-night-form">
          <h3>{editingId ? 'Edit Club Night' : 'New Club Night'}</h3>
          
          <div className="form-group">
            <label>Date</label>
            <input
              type="date"
              value={formData.date}
              onChange={(e) => setFormData({ ...formData, date: e.target.value })}
              className="input"
              required
            />
          </div>

          <div className="form-group">
            <label>Event</label>
            <select
              value={formData.eventId}
              onChange={(e) => setFormData({ ...formData, eventId: Number(e.target.value) })}
              className="input"
              required
            >
              <option value={0}>Select an event...</option>
              {events.map((event) => (
                <option key={event.id} value={event.id}>
                  {event.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Venue</label>
            <select
              value={formData.venueId}
              onChange={(e) => setFormData({ ...formData, venueId: Number(e.target.value) })}
              className="input"
              required
            >
              <option value={0}>Select a venue...</option>
              {venues.map((venue) => (
                <option key={venue.id} value={venue.id}>
                  {venue.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Acts</label>
            <ActSelector
              availableActs={acts}
              selectedActs={formData.acts}
              onChange={(acts) => setFormData({ ...formData, acts })}
              placeholder="Select acts..."
            />
          </div>

          <div className="form-actions">
            <button type="submit" className="btn btn-primary">
              {editingId ? 'Update' : 'Create'}
            </button>
            <button type="button" onClick={resetForm} className="btn">
              Cancel
            </button>
          </div>
        </form>
      )}

      <div className="sort-control" style={{ marginBottom: '1rem' }}>
        <label>Sort by date:</label>
        <select
          value={sortOrder}
          onChange={(e) => setSortOrder(e.target.value as 'asc' | 'desc')}
          className="input"
        >
          <option value="desc">Newest first</option>
          <option value="asc">Oldest first</option>
        </select>
      </div>

      <div className="filter-sort-controls">
        <button 
          className="btn btn-small"
          onClick={() => setShowFilters(!showFilters)}
        >
          {showFilters ? '▼' : '▶'} Filters
        </button>
      </div>

      {showFilters && (
        <div className="filter-panel">
          <div className="filter-panel-header">
            <h4>Filter Club Nights</h4>
            <button 
              className="btn btn-small"
              onClick={() => {
                setFilterEventId(0);
                setFilterVenueId(0);
                setFilterActIds([]);
                setFilterDateFrom('');
                setFilterDateTo('');
              }}
            >
              Clear All
            </button>
          </div>
          
          <div className="filter-grid">
            <div className="filter-group">
              <label>Event</label>
              <select
                value={filterEventId}
                onChange={(e) => setFilterEventId(Number(e.target.value))}
                className="input"
              >
                <option value={0}>All events</option>
                {events.map((event) => (
                  <option key={event.id} value={event.id}>
                    {event.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="filter-group">
              <label>Venue</label>
              <select
                value={filterVenueId}
                onChange={(e) => setFilterVenueId(Number(e.target.value))}
                className="input"
              >
                <option value={0}>All venues</option>
                {venues.map((venue) => (
                  <option key={venue.id} value={venue.id}>
                    {venue.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="filter-group">
              <label>Date From</label>
              <input
                type="date"
                value={filterDateFrom}
                onChange={(e) => setFilterDateFrom(e.target.value)}
                className="input"
              />
            </div>

            <div className="filter-group">
              <label>Date To</label>
              <input
                type="date"
                value={filterDateTo}
                onChange={(e) => setFilterDateTo(e.target.value)}
                className="input"
              />
            </div>

            <div className="filter-group filter-group-full">
              <label>Acts (select one or more)</label>
              <div className="checkbox-group">
                {acts.map((act) => (
                  <label key={act.id} className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={filterActIds.includes(act.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setFilterActIds([...filterActIds, act.id]);
                        } else {
                          setFilterActIds(filterActIds.filter(id => id !== act.id));
                        }
                      }}
                    />
                    {act.name}
                  </label>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      <div className="club-nights-list">
        {filteredAndSortedClubNights.length === 0 ? (
          <p className="empty-state">
            {clubNights.length === 0 
              ? 'No club nights yet. Add one above!' 
              : 'No club nights match the current filters.'}
          </p>
        ) : (
          paginatedItems.map((clubNight) => (
            <div key={clubNight.id} className="club-night-card">
              <div className="club-night-header">
                <div>
                  <h3>{clubNight.eventName}</h3>
                  <p className="venue-name">{clubNight.venueName}</p>
                </div>
                <div className="club-night-date">{formatDate(clubNight.date)}</div>
              </div>
              
              {clubNight.acts.length > 0 && (
                <div className="acts-section">
                  <h4>Line-up:</h4>
                  <ul className="acts-list">
                    {clubNight.acts.map((act) => (
                      <li key={act.actId}>
                        {act.actName}
                        {act.isLiveSet && <span className="live-indicator"> (live)</span>}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              
              <div className="club-night-actions">
                <button onClick={() => navigate(`/nights/${clubNight.id}`)} className="btn btn-small btn-primary">
                  View Details
                </button>
                <button onClick={() => startEdit(clubNight)} className="btn btn-small">
                  Edit
                </button>
                <button onClick={() => handleDelete(clubNight.id)} className="btn btn-small btn-danger">
                  Delete
                </button>
              </div>
            </div>
          ))
        )}
      </div>

      {filteredAndSortedClubNights.length > 0 && (
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          pageSize={pageSize}
          totalItems={totalItems}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
        />
      )}
    </div>
  );
}

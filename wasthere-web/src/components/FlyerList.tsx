import { useState, useEffect } from 'react';
import type { Flyer, Event, Venue } from '../types';
import { flyersApi, eventsApi, venuesApi, type AutoPopulateResult } from '../services/api';
import { usePagination } from '../hooks/usePagination';
import { Pagination } from './Pagination';

export function FlyerList() {
  const [flyers, setFlyers] = useState<Flyer[]>([]);
  const [autoPopulating, setAutoPopulating] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Filter and sort state
  const [showFilters, setShowFilters] = useState(false);
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [filterEventId, setFilterEventId] = useState<number>(0);
  const [filterVenueId, setFilterVenueId] = useState<number>(0);
  const [filterDateFrom, setFilterDateFrom] = useState<string>('');
  const [filterDateTo, setFilterDateTo] = useState<string>('');

  // Get all events and venues for filtering
  const [events, setEvents] = useState<Event[]>([]);
  const [venues, setVenues] = useState<Venue[]>([]);

  // Apply filters and sorting to flyers
  const filteredAndSortedFlyers = flyers
    .filter(flyer => {
      if (filterEventId > 0 && flyer.eventId !== filterEventId) return false;
      if (filterVenueId > 0 && flyer.venueId !== filterVenueId) return false;
      if (filterDateFrom && new Date(flyer.earliestClubNightDate) < new Date(filterDateFrom)) return false;
      if (filterDateTo && new Date(flyer.earliestClubNightDate) > new Date(filterDateTo)) return false;
      return true;
    })
    .sort((a, b) => {
      const comparison = new Date(a.earliestClubNightDate).getTime() - new Date(b.earliestClubNightDate).getTime();
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
  } = usePagination({ items: filteredAndSortedFlyers, initialPageSize: 12 });

  useEffect(() => {
    loadFlyers();
    loadFiltersData();
  }, []);

  const loadFiltersData = async () => {
    try {
      const [eventsData, venuesData] = await Promise.all([
        eventsApi.getAll(),
        venuesApi.getAll(),
      ]);
      setEvents(eventsData);
      setVenues(venuesData);
    } catch (error) {
      console.error('Failed to load filter data:', error);
    }
  };

  const loadFlyers = async () => {
    try {
      const data = await flyersApi.getAll();
      setFlyers(data);
    } catch (error) {
      console.error('Failed to load flyers:', error);
      setError('Failed to load flyers');
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure you want to delete this flyer?')) return;

    try {
      await flyersApi.delete(id);
      await loadFlyers();
    } catch (error) {
      console.error('Failed to delete flyer:', error);
      setError('Failed to delete flyer');
    }
  };

  const handleAutoPopulate = async (id: number) => {
    setAutoPopulating(id);
    setError(null);

    try {
      const result: AutoPopulateResult = await flyersApi.autoPopulate(id);
      
      if (result.success) {
        await loadFlyers();
      } else {
        setError(result.message || 'Failed to auto-populate from flyer');
      }
    } catch (error) {
      console.error('Failed to auto-populate:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to auto-populate from flyer';
      setError(errorMessage);
    } finally {
      setAutoPopulating(null);
    }
  };

  return (
    <div className="card">
      <h2>Flyers</h2>
      
      {error && (
        <div className="error-message" style={{ color: 'red', marginBottom: '1rem', padding: '1rem', backgroundColor: '#ffebee', borderRadius: '4px' }}>
          {error}
          <button 
            onClick={() => setError(null)} 
            style={{ marginLeft: '1rem', padding: '0.25rem 0.5rem' }}
          >
            Dismiss
          </button>
        </div>
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
            <h4>Filter Flyers</h4>
            <button 
              className="btn btn-small"
              onClick={() => {
                setFilterEventId(0);
                setFilterVenueId(0);
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
          </div>
        </div>
      )}

      <div className="flyers-grid">
        {filteredAndSortedFlyers.length === 0 ? (
          <div className="empty-state">
            {flyers.length === 0 
              ? 'No flyers uploaded yet.' 
              : 'No flyers match the current filters.'}
          </div>
        ) : (
          paginatedItems.map((flyer) => (
            <div key={flyer.id} className="flyer-card">
              <div className="flyer-image-container">
                <img
                  src={flyersApi.getImageUrl(flyer.filePath)}
                  alt={flyer.fileName}
                  className="flyer-image"
                />
              </div>
              <div className="flyer-info">
                <div className="flyer-details">
                  <div><strong>Event:</strong> {flyer.event?.name || 'N/A'}</div>
                  <div><strong>Venue:</strong> {flyer.venue?.name || 'N/A'}</div>
                  <div><strong>Date:</strong> {new Date(flyer.earliestClubNightDate).toLocaleDateString()}</div>
                  <div className="flyer-filename">{flyer.fileName}</div>
                </div>
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button 
                    onClick={() => handleAutoPopulate(flyer.id)} 
                    className="btn btn-small btn-primary"
                    disabled={autoPopulating === flyer.id}
                    title="Re-analyze flyer to extract additional information"
                  >
                    {autoPopulating === flyer.id ? 'Analyzing...' : 'Analyze'}
                  </button>
                  <button 
                    onClick={() => handleDelete(flyer.id)} 
                    className="btn btn-small btn-danger"
                  >
                    Delete
                  </button>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {filteredAndSortedFlyers.length > 0 && (
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          pageSize={pageSize}
          totalItems={totalItems}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
          pageSizeOptions={[12, 24, 48, 96]}
        />
      )}
    </div>
  );
}

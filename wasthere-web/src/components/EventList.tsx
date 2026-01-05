import { useState, useEffect } from 'react';
import type { Event } from '../types';
import { eventsApi } from '../services/api';
import { usePagination } from '../hooks/usePagination';
import { Pagination } from './Pagination';

export function EventList() {
  const [events, setEvents] = useState<Event[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [newEventName, setNewEventName] = useState('');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editingName, setEditingName] = useState('');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

  // Filter and sort events based on search query and sort order
  const filteredEvents = events
    .filter(event =>
      event.name.toLowerCase().includes(searchQuery.toLowerCase())
    )
    .sort((a, b) => {
      const comparison = a.name.localeCompare(b.name);
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
  } = usePagination({ items: filteredEvents, initialPageSize: 10 });

  useEffect(() => {
    loadEvents();
  }, []);

  const loadEvents = async () => {
    try {
      const data = await eventsApi.getAll();
      setEvents(data);
    } catch (error) {
      console.error('Failed to load events:', error);
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newEventName.trim()) return;

    try {
      await eventsApi.create(newEventName);
      setNewEventName('');
      await loadEvents();
    } catch (error) {
      console.error('Failed to create event:', error);
    }
  };

  const handleUpdate = async (id: number) => {
    if (!editingName.trim()) return;

    try {
      await eventsApi.update(id, editingName);
      setEditingId(null);
      setEditingName('');
      await loadEvents();
    } catch (error) {
      console.error('Failed to update event:', error);
    }
  };

  const handleDelete = async (id: number) => {
    try {
      // Get delete impact first
      const impact = await eventsApi.getDeleteImpact(id);
      
      // Build warning message
      let message = 'Are you sure you want to delete this event?';
      if (impact.clubNightsCount > 0 || impact.flyersCount > 0) {
        message += '\n\nThis will also delete:';
        if (impact.clubNightsCount > 0) {
          message += `\n- ${impact.clubNightsCount} club night${impact.clubNightsCount !== 1 ? 's' : ''}`;
        }
        if (impact.flyersCount > 0) {
          message += `\n- ${impact.flyersCount} flyer${impact.flyersCount !== 1 ? 's' : ''}`;
        }
      }
      
      if (!confirm(message)) return;

      await eventsApi.delete(id);
      await loadEvents();
    } catch (error) {
      console.error('Failed to delete event:', error);
      alert('Failed to delete event. It may be referenced by other records.');
    }
  };

  const startEdit = (event: Event) => {
    setEditingId(event.id);
    setEditingName(event.name);
  };

  return (
    <div className="card">
      <h2>Events</h2>

      <div className="search-box">
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="Search events..."
          className="input"
        />
      </div>

      <div className="sort-header">
        <button 
          className="sort-button"
          onClick={() => setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')}
          title={sortOrder === 'asc' ? 'Sort Z-A' : 'Sort A-Z'}
        >
          Name {sortOrder === 'asc' ? '↑' : '↓'}
        </button>
      </div>
      
      <form onSubmit={handleCreate} className="form">
        <input
          type="text"
          value={newEventName}
          onChange={(e) => setNewEventName(e.target.value)}
          placeholder="New event name (e.g., Bugged Out)"
          className="input"
        />
        <button type="submit" className="btn btn-primary">Add Event</button>
      </form>

      <ul className="list">
        {paginatedItems.map((event) => (
          <li key={event.id} className="list-item">
            {editingId === event.id ? (
              <div className="edit-form">
                <input
                  type="text"
                  value={editingName}
                  onChange={(e) => setEditingName(e.target.value)}
                  className="input"
                />
                <button onClick={() => handleUpdate(event.id)} className="btn btn-small">
                  Save
                </button>
                <button onClick={() => setEditingId(null)} className="btn btn-small">
                  Cancel
                </button>
              </div>
            ) : (
              <div className="list-item-content">
                <span className="list-item-text">{event.name}</span>
                <div className="list-item-actions">
                  <button onClick={() => startEdit(event)} className="btn btn-small">
                    Edit
                  </button>
                  <button onClick={() => handleDelete(event.id)} className="btn btn-small btn-danger">
                    Delete
                  </button>
                </div>
              </div>
            )}
          </li>
        ))}
      </ul>

      <Pagination
        currentPage={currentPage}
        totalPages={totalPages}
        pageSize={pageSize}
        totalItems={totalItems}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
      />
    </div>
  );
}

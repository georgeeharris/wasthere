import { useState, useEffect } from 'react';
import type { Event } from '../types';
import { eventsApi } from '../services/api';
import { usePagination } from '../hooks/usePagination';
import { Pagination } from './Pagination';

interface EventListProps {
  onEventSelect?: (event: Event) => void;
}

export function EventList(_props: EventListProps) {
  const [events, setEvents] = useState<Event[]>([]);
  const [newEventName, setNewEventName] = useState('');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editingName, setEditingName] = useState('');

  const {
    currentPage,
    pageSize,
    paginatedItems,
    totalPages,
    totalItems,
    setPage,
    setPageSize,
  } = usePagination({ items: events, initialPageSize: 10 });

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
    if (!confirm('Are you sure you want to delete this event?')) return;

    try {
      await eventsApi.delete(id);
      await loadEvents();
    } catch (error) {
      console.error('Failed to delete event:', error);
    }
  };

  const startEdit = (event: Event) => {
    setEditingId(event.id);
    setEditingName(event.name);
  };

  return (
    <div className="card">
      <h2>Events</h2>
      
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

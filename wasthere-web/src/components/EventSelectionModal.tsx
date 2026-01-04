import { useState, useEffect } from 'react';
import type { Event } from '../types';
import { eventsApi } from '../services/api';

interface EventSelectionModalProps {
  onConfirm: (eventId: number) => void;
  onCancel: () => void;
}

export function EventSelectionModal({ onConfirm, onCancel }: EventSelectionModalProps) {
  const [events, setEvents] = useState<Event[]>([]);
  const [selectedEventId, setSelectedEventId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadEvents();
  }, []);

  const loadEvents = async () => {
    try {
      setLoading(true);
      const data = await eventsApi.getAll();
      setEvents(data);
      if (data.length > 0) {
        setSelectedEventId(data[0].id);
      }
    } catch (err) {
      console.error('Failed to load events:', err);
      setError('Failed to load events. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleConfirm = () => {
    if (selectedEventId !== null) {
      onConfirm(selectedEventId);
    }
  };

  if (loading) {
    return (
      <div className="modal-overlay" onClick={onCancel}>
        <div className="modal-content" onClick={(e) => e.stopPropagation()}>
          <h3>Loading Events...</h3>
          <div className="spinner"></div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="modal-overlay" onClick={onCancel}>
        <div className="modal-content" onClick={(e) => e.stopPropagation()}>
          <h3>Error</h3>
          <p style={{ color: 'red' }}>{error}</p>
          <div className="modal-actions">
            <button onClick={onCancel} className="btn btn-secondary">
              Cancel
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (events.length === 0) {
    return (
      <div className="modal-overlay" onClick={onCancel}>
        <div className="modal-content" onClick={(e) => e.stopPropagation()}>
          <h3>No Events Available</h3>
          <p style={{ marginBottom: '1rem', color: '#666' }}>
            No events are available in the system. Please create an event first before uploading a flyer.
          </p>
          <div className="modal-actions">
            <button onClick={onCancel} className="btn btn-secondary">
              Cancel
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="modal-overlay" onClick={onCancel}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <h3>Select Event</h3>
        <p style={{ marginBottom: '1rem', color: '#666' }}>
          The event name could not be determined from the flyer. 
          Please select the event from the list below.
        </p>
        
        <div className="event-selection-container">
          <label htmlFor="event-select" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
            Event:
          </label>
          <select
            id="event-select"
            value={selectedEventId || ''}
            onChange={(e) => setSelectedEventId(parseInt(e.target.value))}
            className="input"
            style={{ width: '100%', marginBottom: '1.5rem' }}
          >
            {events.map((event) => (
              <option key={event.id} value={event.id}>
                {event.name}
              </option>
            ))}
          </select>
        </div>
        
        <div className="modal-actions">
          <button onClick={handleConfirm} className="btn btn-primary" disabled={selectedEventId === null}>
            Continue
          </button>
          <button onClick={onCancel} className="btn btn-secondary">
            Cancel
          </button>
        </div>
      </div>
      
      <style>{`
        .modal-overlay {
          position: fixed;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background: rgba(0, 0, 0, 0.5);
          display: flex;
          align-items: center;
          justify-content: center;
          z-index: 1000;
        }
        
        .modal-content {
          background: white;
          padding: 2rem;
          border-radius: 8px;
          max-width: 500px;
          width: 90%;
          max-height: 80vh;
          overflow-y: auto;
        }
        
        .event-selection-container {
          margin-bottom: 1rem;
        }
        
        .modal-actions {
          display: flex;
          gap: 1rem;
          justify-content: flex-end;
        }
      `}</style>
    </div>
  );
}

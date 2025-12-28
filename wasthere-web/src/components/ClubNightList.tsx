import { useState, useEffect } from 'react';
import { ClubNight, Event, Venue, Act } from '../types';
import { clubNightsApi, eventsApi, venuesApi, actsApi } from '../services/api';

export function ClubNightList() {
  const [clubNights, setClubNights] = useState<ClubNight[]>([]);
  const [events, setEvents] = useState<Event[]>([]);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [acts, setActs] = useState<Act[]>([]);
  
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState({
    date: '',
    eventId: 0,
    venueId: 0,
    actIds: [] as number[],
  });

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
      actIds: clubNight.acts.map(a => a.actId),
    });
    setShowForm(true);
  };

  const resetForm = () => {
    setShowForm(false);
    setEditingId(null);
    setFormData({
      date: '',
      eventId: 0,
      venueId: 0,
      actIds: [],
    });
  };

  const toggleAct = (actId: number) => {
    setFormData((prev) => ({
      ...prev,
      actIds: prev.actIds.includes(actId)
        ? prev.actIds.filter((id) => id !== actId)
        : [...prev.actIds, actId],
    }));
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
            <div className="checkbox-group">
              {acts.map((act) => (
                <label key={act.id} className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={formData.actIds.includes(act.id)}
                    onChange={() => toggleAct(act.id)}
                  />
                  <span>{act.name}</span>
                </label>
              ))}
            </div>
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

      <div className="club-nights-list">
        {clubNights.length === 0 ? (
          <p className="empty-state">No club nights yet. Add one above!</p>
        ) : (
          clubNights.map((clubNight) => (
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
                      <li key={act.actId}>{act.actName}</li>
                    ))}
                  </ul>
                </div>
              )}
              
              <div className="club-night-actions">
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
    </div>
  );
}

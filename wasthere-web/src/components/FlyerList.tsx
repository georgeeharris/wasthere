import { useState, useEffect } from 'react';
import type { Flyer, Event, Venue } from '../types';
import { flyersApi, eventsApi, venuesApi } from '../services/api';

export function FlyerList() {
  const [flyers, setFlyers] = useState<Flyer[]>([]);
  const [events, setEvents] = useState<Event[]>([]);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [selectedEventId, setSelectedEventId] = useState<number | ''>('');
  const [selectedVenueId, setSelectedVenueId] = useState<number | ''>('');
  const [earliestDate, setEarliestDate] = useState<string>('');
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadFlyers();
    loadEvents();
    loadVenues();
  }, []);

  const loadFlyers = async () => {
    try {
      const data = await flyersApi.getAll();
      // Shuffle array for random ordering
      const shuffled = [...data].sort(() => Math.random() - 0.5);
      setFlyers(shuffled);
    } catch (error) {
      console.error('Failed to load flyers:', error);
      setError('Failed to load flyers');
    }
  };

  const loadEvents = async () => {
    try {
      const data = await eventsApi.getAll();
      setEvents(data);
    } catch (error) {
      console.error('Failed to load events:', error);
    }
  };

  const loadVenues = async () => {
    try {
      const data = await venuesApi.getAll();
      setVenues(data);
    } catch (error) {
      console.error('Failed to load venues:', error);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setSelectedFile(e.target.files[0]);
      setError(null);
    }
  };

  const handleUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!selectedFile) {
      setError('Please select a file');
      return;
    }
    
    if (selectedEventId === '' || selectedVenueId === '' || !earliestDate) {
      setError('Please fill in all fields');
      return;
    }

    setUploading(true);
    setError(null);

    try {
      await flyersApi.upload(
        selectedFile,
        Number(selectedEventId),
        Number(selectedVenueId),
        earliestDate
      );
      
      // Reset form
      setSelectedFile(null);
      setSelectedEventId('');
      setSelectedVenueId('');
      setEarliestDate('');
      
      // Reset file input
      const fileInput = document.getElementById('flyer-file') as HTMLInputElement;
      if (fileInput) fileInput.value = '';
      
      // Reload flyers
      await loadFlyers();
    } catch (error) {
      console.error('Failed to upload flyer:', error);
      setError(error instanceof Error ? error.message : 'Failed to upload flyer');
    } finally {
      setUploading(false);
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

  return (
    <div className="card">
      <h2>Flyers</h2>
      
      <div className="club-night-form">
        <h3>Upload New Flyer</h3>
        {error && <div className="error-message" style={{ color: 'red', marginBottom: '1rem' }}>{error}</div>}
        
        <form onSubmit={handleUpload}>
          <div className="form-group">
            <label htmlFor="flyer-file">Select Image File</label>
            <input
              id="flyer-file"
              type="file"
              accept="image/jpeg,image/jpg,image/png,image/gif,image/webp"
              onChange={handleFileChange}
              className="input"
            />
            {selectedFile && (
              <div style={{ marginTop: '0.5rem', fontSize: '0.9rem', color: '#666' }}>
                Selected: {selectedFile.name} ({(selectedFile.size / 1024 / 1024).toFixed(2)} MB)
              </div>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="flyer-event">Event</label>
            <select
              id="flyer-event"
              value={selectedEventId}
              onChange={(e) => setSelectedEventId(e.target.value ? Number(e.target.value) : '')}
              className="input"
              required
            >
              <option value="">Select an event...</option>
              {events.map((event) => (
                <option key={event.id} value={event.id}>
                  {event.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="flyer-venue">Venue</label>
            <select
              id="flyer-venue"
              value={selectedVenueId}
              onChange={(e) => setSelectedVenueId(e.target.value ? Number(e.target.value) : '')}
              className="input"
              required
            >
              <option value="">Select a venue...</option>
              {venues.map((venue) => (
                <option key={venue.id} value={venue.id}>
                  {venue.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="flyer-date">Earliest Club Night Date</label>
            <input
              id="flyer-date"
              type="date"
              value={earliestDate}
              onChange={(e) => setEarliestDate(e.target.value)}
              className="input"
              required
            />
          </div>

          <div className="form-actions">
            <button 
              type="submit" 
              className="btn btn-primary"
              disabled={uploading || !selectedFile}
            >
              {uploading ? 'Uploading...' : 'Upload Flyer'}
            </button>
          </div>
        </form>
      </div>

      <div className="flyers-grid">
        {flyers.length === 0 ? (
          <div className="empty-state">No flyers uploaded yet.</div>
        ) : (
          flyers.map((flyer) => (
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
                <button 
                  onClick={() => handleDelete(flyer.id)} 
                  className="btn btn-small btn-danger"
                >
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

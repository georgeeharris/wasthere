import { useState, useEffect } from 'react';
import type { Flyer } from '../types';
import { flyersApi, type AutoPopulateResult } from '../services/api';

export function FlyerList() {
  const [flyers, setFlyers] = useState<Flyer[]>([]);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [autoPopulating, setAutoPopulating] = useState<number | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    loadFlyers();
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

    setUploading(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const result = await flyersApi.upload(selectedFile);
      
      if (result.success) {
        // Show success message with details
        const autoPopResult = result.autoPopulateResult;
        if (autoPopResult) {
          setSuccessMessage(
            `Flyer uploaded and analyzed! ${autoPopResult.message}`
          );
        } else {
          setSuccessMessage('Flyer uploaded successfully!');
        }
        
        // Reset form
        setSelectedFile(null);
        
        // Reset file input
        const fileInput = document.getElementById('flyer-file') as HTMLInputElement;
        if (fileInput) fileInput.value = '';
        
        // Reload flyers
        await loadFlyers();
        
        // Clear success message after 10 seconds
        setTimeout(() => setSuccessMessage(null), 10000);
      } else {
        setError(result.message || 'Failed to upload flyer');
      }
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

  const handleAutoPopulate = async (id: number) => {
    setAutoPopulating(id);
    setError(null);
    setSuccessMessage(null);

    try {
      const result: AutoPopulateResult = await flyersApi.autoPopulate(id);
      
      if (result.success) {
        setSuccessMessage(result.message);
        // Reload flyers to show updated club nights
        await loadFlyers();
        
        // Clear success message after 5 seconds
        setTimeout(() => setSuccessMessage(null), 5000);
      } else {
        setError(result.message || 'Failed to auto-populate from flyer');
      }
    } catch (error) {
      console.error('Failed to auto-populate:', error);
      setError(error instanceof Error ? error.message : 'Failed to auto-populate from flyer');
    } finally {
      setAutoPopulating(null);
    }
  };

  return (
    <div className="card">
      <h2>Flyers</h2>
      
      <div className="club-night-form">
        <h3>Upload New Flyer</h3>
        {error && <div className="error-message" style={{ color: 'red', marginBottom: '1rem' }}>{error}</div>}
        {successMessage && <div className="success-message" style={{ color: 'green', marginBottom: '1rem' }}>{successMessage}</div>}
        
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

          <div className="form-actions">
            <button 
              type="submit" 
              className="btn btn-primary"
              disabled={uploading || !selectedFile}
            >
              {uploading ? 'Uploading and Analyzing...' : 'Upload and Analyze Flyer'}
            </button>
          </div>
          
          <div style={{ marginTop: '1rem', fontSize: '0.9rem', color: '#666' }}>
            <p>The flyer will be automatically analyzed to extract:</p>
            <ul style={{ marginLeft: '1.5rem', marginTop: '0.5rem' }}>
              <li>Event and venue information</li>
              <li>Event dates (with automatic year inference)</li>
              <li>Performing acts/DJs</li>
            </ul>
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
    </div>
  );
}

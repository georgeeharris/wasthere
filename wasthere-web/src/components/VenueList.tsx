import { useState, useEffect } from 'react';
import type { Venue } from '../types';
import { venuesApi } from '../services/api';
import { usePagination } from '../hooks/usePagination';
import { Pagination } from './Pagination';
import { useNavigate } from 'react-router-dom';

export function VenueList() {
  const navigate = useNavigate();
  const [venues, setVenues] = useState<Venue[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [newVenueName, setNewVenueName] = useState('');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editingName, setEditingName] = useState('');

  // Filter venues based on search query
  const filteredVenues = venues.filter(venue =>
    venue.name.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const {
    currentPage,
    pageSize,
    paginatedItems,
    totalPages,
    totalItems,
    setPage,
    setPageSize,
  } = usePagination({ items: filteredVenues, initialPageSize: 10 });

  useEffect(() => {
    loadVenues();
  }, []);

  const loadVenues = async () => {
    try {
      const data = await venuesApi.getAll();
      setVenues(data);
    } catch (error) {
      console.error('Failed to load venues:', error);
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newVenueName.trim()) return;

    try {
      await venuesApi.create(newVenueName);
      setNewVenueName('');
      await loadVenues();
    } catch (error) {
      console.error('Failed to create venue:', error);
    }
  };

  const handleUpdate = async (id: number) => {
    if (!editingName.trim()) return;

    try {
      await venuesApi.update(id, editingName);
      setEditingId(null);
      setEditingName('');
      await loadVenues();
    } catch (error) {
      console.error('Failed to update venue:', error);
    }
  };

  const handleDelete = async (id: number) => {
    try {
      // Get delete impact first
      const impact = await venuesApi.getDeleteImpact(id);
      
      // Build warning message
      let message = 'Are you sure you want to delete this venue?';
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

      await venuesApi.delete(id);
      await loadVenues();
    } catch (error) {
      console.error('Failed to delete venue:', error);
      alert('Failed to delete venue. It may be referenced by other records.');
    }
  };

  const startEdit = (venue: Venue) => {
    setEditingId(venue.id);
    setEditingName(venue.name);
  };

  return (
    <div className="card">
      <div className="card-header">
        <h2>Venues</h2>
        <div className="master-list-nav">
          <button onClick={() => navigate('/master/events')} className="btn btn-small">
            Events
          </button>
          <button onClick={() => navigate('/master/venues')} className="btn btn-small active">
            Venues
          </button>
          <button onClick={() => navigate('/master/acts')} className="btn btn-small">
            Acts
          </button>
        </div>
      </div>

      <div className="search-box">
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="Search venues..."
          className="input"
        />
      </div>
      
      <form onSubmit={handleCreate} className="form">
        <input
          type="text"
          value={newVenueName}
          onChange={(e) => setNewVenueName(e.target.value)}
          placeholder="New venue name (e.g., Sankey's Soap)"
          className="input"
        />
        <button type="submit" className="btn btn-primary">Add Venue</button>
      </form>

      <ul className="list">
        {paginatedItems.map((venue) => (
          <li key={venue.id} className="list-item">
            {editingId === venue.id ? (
              <div className="edit-form">
                <input
                  type="text"
                  value={editingName}
                  onChange={(e) => setEditingName(e.target.value)}
                  className="input"
                />
                <button onClick={() => handleUpdate(venue.id)} className="btn btn-small">
                  Save
                </button>
                <button onClick={() => setEditingId(null)} className="btn btn-small">
                  Cancel
                </button>
              </div>
            ) : (
              <div className="list-item-content">
                <span className="list-item-text">{venue.name}</span>
                <div className="list-item-actions">
                  <button onClick={() => startEdit(venue)} className="btn btn-small">
                    Edit
                  </button>
                  <button onClick={() => handleDelete(venue.id)} className="btn btn-small btn-danger">
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

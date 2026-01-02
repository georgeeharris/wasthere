import { useState, useEffect } from 'react';
import type { Venue } from '../types';
import { venuesApi } from '../services/api';
import { usePagination } from '../hooks/usePagination';
import { Pagination } from './Pagination';

export function VenueList() {
  const [venues, setVenues] = useState<Venue[]>([]);
  const [newVenueName, setNewVenueName] = useState('');
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
  } = usePagination({ items: venues, initialPageSize: 10 });

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
    if (!confirm('Are you sure you want to delete this venue?')) return;

    try {
      await venuesApi.delete(id);
      await loadVenues();
    } catch (error) {
      console.error('Failed to delete venue:', error);
    }
  };

  const startEdit = (venue: Venue) => {
    setEditingId(venue.id);
    setEditingName(venue.name);
  };

  return (
    <div className="card">
      <h2>Venues</h2>
      
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

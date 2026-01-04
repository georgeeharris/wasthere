import { useState, useEffect } from 'react';
import type { Act } from '../types';
import { actsApi } from '../services/api';
import { usePagination } from '../hooks/usePagination';
import { Pagination } from './Pagination';

export function ActList() {
  const [acts, setActs] = useState<Act[]>([]);
  const [newActName, setNewActName] = useState('');
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
  } = usePagination({ items: acts, initialPageSize: 10 });

  useEffect(() => {
    loadActs();
  }, []);

  const loadActs = async () => {
    try {
      const data = await actsApi.getAll();
      setActs(data);
    } catch (error) {
      console.error('Failed to load acts:', error);
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newActName.trim()) return;

    try {
      await actsApi.create(newActName);
      setNewActName('');
      await loadActs();
    } catch (error) {
      console.error('Failed to create act:', error);
    }
  };

  const handleUpdate = async (id: number) => {
    if (!editingName.trim()) return;

    try {
      await actsApi.update(id, editingName);
      setEditingId(null);
      setEditingName('');
      await loadActs();
    } catch (error) {
      console.error('Failed to update act:', error);
    }
  };

  const handleDelete = async (id: number) => {
    try {
      // Get delete impact first
      const impact = await actsApi.getDeleteImpact(id);
      
      // Build warning message
      let message = 'Are you sure you want to delete this act?';
      if (impact.clubNightActsCount > 0) {
        message += '\n\nThis will also delete:';
        message += `\n- ${impact.clubNightActsCount} club night performance${impact.clubNightActsCount !== 1 ? 's' : ''}`;
      }
      
      if (!confirm(message)) return;

      await actsApi.delete(id);
      await loadActs();
    } catch (error) {
      console.error('Failed to delete act:', error);
      alert('Failed to delete act. It may be referenced by other records.');
    }
  };

  const startEdit = (act: Act) => {
    setEditingId(act.id);
    setEditingName(act.name);
  };

  return (
    <div className="card">
      <h2>Acts</h2>
      
      <form onSubmit={handleCreate} className="form">
        <input
          type="text"
          value={newActName}
          onChange={(e) => setNewActName(e.target.value)}
          placeholder="New act name"
          className="input"
        />
        <button type="submit" className="btn btn-primary">Add Act</button>
      </form>

      <ul className="list">
        {paginatedItems.map((act) => (
          <li key={act.id} className="list-item">
            {editingId === act.id ? (
              <div className="edit-form">
                <input
                  type="text"
                  value={editingName}
                  onChange={(e) => setEditingName(e.target.value)}
                  className="input"
                />
                <button onClick={() => handleUpdate(act.id)} className="btn btn-small">
                  Save
                </button>
                <button onClick={() => setEditingId(null)} className="btn btn-small">
                  Cancel
                </button>
              </div>
            ) : (
              <div className="list-item-content">
                <span className="list-item-text">{act.name}</span>
                <div className="list-item-actions">
                  <button onClick={() => startEdit(act)} className="btn btn-small">
                    Edit
                  </button>
                  <button onClick={() => handleDelete(act.id)} className="btn btn-small btn-danger">
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

import { useState, useRef, useEffect } from 'react';
import '../styles/SearchableMultiSelect.css';

interface Option {
  id: number;
  name: string;
}

interface SearchableMultiSelectProps {
  options: Option[];
  selectedIds: number[];
  onChange: (selectedIds: number[]) => void;
  placeholder?: string;
}

export function SearchableMultiSelect({
  options,
  selectedIds,
  onChange,
  placeholder = 'Select...',
}: SearchableMultiSelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setSearchTerm('');
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  const filteredOptions = options.filter((option) =>
    option.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const selectedOptions = options.filter((option) => selectedIds.includes(option.id));

  const toggleOption = (optionId: number) => {
    const newSelectedIds = selectedIds.includes(optionId)
      ? selectedIds.filter((id) => id !== optionId)
      : [...selectedIds, optionId];
    onChange(newSelectedIds);
  };

  const removeOption = (optionId: number) => {
    onChange(selectedIds.filter((id) => id !== optionId));
  };

  return (
    <div className="searchable-multiselect" ref={containerRef}>
      <div className="multiselect-control" onClick={() => setIsOpen(!isOpen)}>
        {selectedOptions.length === 0 ? (
          <span className="multiselect-placeholder">{placeholder}</span>
        ) : (
          <div className="selected-items">
            {selectedOptions.map((option) => (
              <span key={option.id} className="selected-item">
                {option.name}
                <button
                  type="button"
                  className="remove-item"
                  onClick={(e) => {
                    e.stopPropagation();
                    removeOption(option.id);
                  }}
                >
                  ×
                </button>
              </span>
            ))}
          </div>
        )}
        <span className="dropdown-arrow">{isOpen ? '▲' : '▼'}</span>
      </div>

      {isOpen && (
        <div className="multiselect-dropdown">
          <div className="search-box">
            <input
              type="text"
              className="search-input"
              placeholder="Search acts..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onClick={(e) => e.stopPropagation()}
              autoFocus
            />
          </div>
          <div className="options-list">
            {filteredOptions.length === 0 ? (
              <div className="no-options">No acts found</div>
            ) : (
              filteredOptions.map((option) => (
                <label key={option.id} className="option-item">
                  <input
                    type="checkbox"
                    checked={selectedIds.includes(option.id)}
                    onChange={() => toggleOption(option.id)}
                  />
                  <span>{option.name}</span>
                </label>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}

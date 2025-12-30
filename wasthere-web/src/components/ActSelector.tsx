import { useState, useRef, useEffect } from 'react';
import type { Act, ClubNightActDto } from '../types';
import '../styles/ActSelector.css';

interface ActSelectorProps {
  availableActs: Act[];
  selectedActs: ClubNightActDto[];
  onChange: (acts: ClubNightActDto[]) => void;
  placeholder?: string;
}

export function ActSelector({
  availableActs,
  selectedActs,
  onChange,
  placeholder = 'Select acts...',
}: ActSelectorProps) {
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

  const filteredActs = availableActs.filter((act) =>
    act.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const selectedActIds = selectedActs.map(a => a.actId);

  const toggleAct = (actId: number) => {
    if (selectedActIds.includes(actId)) {
      // Remove act
      onChange(selectedActs.filter((a) => a.actId !== actId));
    } else {
      // Add act (default to not live set)
      onChange([...selectedActs, { actId, isLiveSet: false }]);
    }
  };

  const removeAct = (actId: number) => {
    onChange(selectedActs.filter((a) => a.actId !== actId));
  };

  const toggleLiveSet = (actId: number) => {
    onChange(
      selectedActs.map((a) =>
        a.actId === actId ? { ...a, isLiveSet: !a.isLiveSet } : a
      )
    );
  };

  const getActName = (actId: number) => {
    return availableActs.find((a) => a.id === actId)?.name || '';
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      setIsOpen(!isOpen);
    } else if (event.key === 'Escape' && isOpen) {
      event.preventDefault();
      setIsOpen(false);
    }
  };

  return (
    <div className="act-selector" ref={containerRef}>
      <div 
        className="act-selector-control" 
        onClick={() => setIsOpen(!isOpen)} 
        onKeyDown={handleKeyDown} 
        role="button" 
        tabIndex={0} 
        aria-expanded={isOpen} 
        aria-label="Select acts"
      >
        {selectedActs.length === 0 ? (
          <span className="act-selector-placeholder">{placeholder}</span>
        ) : (
          <div className="selected-acts">
            {selectedActs.map((act) => (
              <span key={act.actId} className="selected-act-item">
                {getActName(act.actId)}
                {act.isLiveSet && <span className="live-badge">LIVE</span>}
                <button
                  type="button"
                  className="remove-act"
                  onClick={(e) => {
                    e.stopPropagation();
                    removeAct(act.actId);
                  }}
                  aria-label={`Remove ${getActName(act.actId)}`}
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
        <div className="act-selector-dropdown" role="listbox">
          <div className="search-box">
            <input
              type="text"
              className="search-input"
              placeholder="Search acts..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onClick={(e) => e.stopPropagation()}
              aria-label="Search acts"
            />
          </div>
          <div className="acts-list">
            {filteredActs.length === 0 ? (
              <div className="no-acts">No acts found</div>
            ) : (
              filteredActs.map((act) => {
                const isSelected = selectedActIds.includes(act.id);
                const selectedAct = selectedActs.find(a => a.actId === act.id);
                
                return (
                  <div key={act.id} className="act-list-item">
                    <label className="act-checkbox-label" htmlFor={`act-${act.id}`}>
                      <input
                        id={`act-${act.id}`}
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => toggleAct(act.id)}
                      />
                      <span>{act.name}</span>
                    </label>
                    <label className="live-set-label" htmlFor={`live-${act.id}`}>
                      <input
                        id={`live-${act.id}`}
                        type="checkbox"
                        checked={selectedAct?.isLiveSet || false}
                        onChange={(e) => {
                          e.stopPropagation();
                          toggleLiveSet(act.id);
                        }}
                        disabled={!isSelected}
                      />
                      <span>Live</span>
                    </label>
                  </div>
                );
              })
            )}
          </div>
        </div>
      )}
    </div>
  );
}

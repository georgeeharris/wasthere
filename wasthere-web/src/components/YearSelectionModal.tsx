import { useState } from 'react';
import type { ClubNightData } from '../types';

interface YearSelectionModalProps {
  clubNights: ClubNightData[];
  onConfirm: (selectedYears: Map<string, number>) => void;
  onCancel: () => void;
}

export function YearSelectionModal({ clubNights, onConfirm, onCancel }: YearSelectionModalProps) {
  // Initialize with first candidate year for each date
  const initialSelections = new Map<string, number>();
  clubNights.forEach((cn) => {
    if (cn.month && cn.day && cn.candidateYears.length > 0) {
      const key = `${cn.month}-${cn.day}`;
      initialSelections.set(key, cn.candidateYears[0]);
    }
  });

  const [selectedYears, setSelectedYears] = useState<Map<string, number>>(initialSelections);

  const handleYearChange = (month: number, day: number, year: number) => {
    const key = `${month}-${day}`;
    setSelectedYears(new Map(selectedYears.set(key, year)));
  };

  const handleConfirm = () => {
    onConfirm(selectedYears);
  };

  // Filter club nights that need year selection
  const clubNightsNeedingYearSelection = clubNights.filter(
    (cn) => !cn.date && cn.month && cn.day && cn.candidateYears.length > 0
  );

  if (clubNightsNeedingYearSelection.length === 0) {
    // No year selection needed, auto-confirm
    onConfirm(new Map());
    return null;
  }

  // Format month name
  const getMonthName = (month: number) => {
    const monthNames = [
      'January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December'
    ];
    return monthNames[month - 1] || month;
  };

  return (
    <div className="modal-overlay" onClick={onCancel}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <h3>Select Years for Dates</h3>
        <p style={{ marginBottom: '1rem', color: '#666' }}>
          Multiple possible years were found for the dates on this flyer. 
          Please select the correct year for each date.
        </p>
        
        <div className="year-selection-list">
          {clubNightsNeedingYearSelection.map((clubNight, index) => {
            const key = `${clubNight.month}-${clubNight.day}`;
            const selectedYear = selectedYears.get(key);
            
            return (
              <div key={index} className="year-selection-item">
                <div className="date-info">
                  <strong>
                    {clubNight.dayOfWeek || ''} {clubNight.day} {getMonthName(clubNight.month!)}
                  </strong>
                  {clubNight.eventName && (
                    <div style={{ fontSize: '0.9rem', color: '#666', marginTop: '0.25rem' }}>
                      {clubNight.eventName}
                      {clubNight.venueName && ` at ${clubNight.venueName}`}
                    </div>
                  )}
                </div>
                <div className="year-selector">
                  <label htmlFor={`year-${key}`}>Year:</label>
                  <select
                    id={`year-${key}`}
                    value={selectedYear || ''}
                    onChange={(e) => handleYearChange(clubNight.month!, clubNight.day!, parseInt(e.target.value))}
                    className="input"
                  >
                    {clubNight.candidateYears.map((year) => (
                      <option key={year} value={year}>
                        {year}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            );
          })}
        </div>
        
        <div className="modal-actions">
          <button onClick={handleConfirm} className="btn btn-primary">
            Confirm and Create Club Nights
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
          max-width: 600px;
          width: 90%;
          max-height: 80vh;
          overflow-y: auto;
        }
        
        .year-selection-list {
          display: flex;
          flex-direction: column;
          gap: 1rem;
          margin-bottom: 1.5rem;
        }
        
        .year-selection-item {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          padding: 1rem;
          border: 1px solid #ddd;
          border-radius: 4px;
          gap: 1rem;
        }
        
        .date-info {
          flex: 1;
        }
        
        .year-selector {
          display: flex;
          align-items: center;
          gap: 0.5rem;
        }
        
        .year-selector label {
          margin: 0;
          white-space: nowrap;
        }
        
        .year-selector select {
          min-width: 100px;
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

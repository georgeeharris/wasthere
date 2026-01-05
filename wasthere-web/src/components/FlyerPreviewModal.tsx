import { useState } from 'react';
import type { ClubNightData } from '../types';
import { flyersApi, type FlyerUploadResult } from '../services/api';

interface FlyerPreviewModalProps {
  flyerResults: FlyerUploadResult[];
  onConfirm: () => void;
  onCancel: () => void;
}

const MONTH_NAMES = ['January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'];

// Helper function to get club nights from a flyer result
const getClubNightsFromFlyer = (flyerResult: FlyerUploadResult): ClubNightData[] => {
  return flyerResult.analysisResult?.flyers?.[0]?.clubNights || flyerResult.analysisResult?.clubNights || [];
};

export function FlyerPreviewModal({ flyerResults, onConfirm, onCancel }: FlyerPreviewModalProps) {
  const [currentClubNightIndex, setCurrentClubNightIndex] = useState(0);

  // Get all club nights from all flyers
  const getAllClubNights = (): Array<{ flyer: FlyerUploadResult; clubNight: ClubNightData; clubNightIndex: number }> => {
    const allClubNights: Array<{ flyer: FlyerUploadResult; clubNight: ClubNightData; clubNightIndex: number }> = [];
    
    flyerResults.forEach((flyerResult) => {
      if (flyerResult.success) {
        const clubNights = getClubNightsFromFlyer(flyerResult);
        clubNights.forEach((clubNight: ClubNightData, idx: number) => {
          allClubNights.push({ flyer: flyerResult, clubNight, clubNightIndex: idx });
        });
      }
    });
    
    return allClubNights;
  };

  const allClubNights = getAllClubNights();
  const totalClubNights = allClubNights.length;
  
  if (totalClubNights === 0) {
    return (
      <div className="modal-overlay" onClick={onCancel}>
        <div className="modal-content" onClick={(e) => e.stopPropagation()}>
          <div className="modal-header">
            <h3>Preview Club Nights</h3>
          </div>
          <div className="modal-body">
            <p>No club nights were detected in the uploaded flyer(s).</p>
          </div>
          <div className="modal-actions">
            <button className="btn btn-secondary" onClick={onCancel}>
              Cancel
            </button>
          </div>
        </div>
      </div>
    );
  }

  const currentItem = allClubNights[currentClubNightIndex];
  const currentClubNight = currentItem.clubNight;
  const currentFlyer = currentItem.flyer;

  const formatDate = (clubNight: ClubNightData) => {
    if (clubNight.date) {
      const date = new Date(clubNight.date);
      return date.toLocaleDateString('en-GB', {
        weekday: 'long',
        day: '2-digit',
        month: 'long',
        year: 'numeric',
      });
    }
    
    if (clubNight.month && clubNight.day) {
      const monthName = MONTH_NAMES[clubNight.month - 1];
      let dateStr = `${clubNight.dayOfWeek || ''} ${clubNight.day} ${monthName}`.trim();
      
      if (clubNight.candidateYears.length > 0) {
        dateStr += ` (Year: ${clubNight.candidateYears.join(' or ')})`;
      }
      
      return dateStr;
    }
    
    return 'Date unknown';
  };

  const handlePrevious = () => {
    if (currentClubNightIndex > 0) {
      setCurrentClubNightIndex(currentClubNightIndex - 1);
    }
  };

  const handleNext = () => {
    if (currentClubNightIndex < totalClubNights - 1) {
      setCurrentClubNightIndex(currentClubNightIndex + 1);
    }
  };

  return (
    <div className="modal-overlay" onClick={onCancel}>
      <div className="modal-content preview-modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3>Preview Club Nights</h3>
            <p className="modal-subtitle">
              Review the club night{totalClubNights > 1 ? 's' : ''} that will be created from {flyerResults.length > 1 ? 'these flyers' : 'this flyer'}
            </p>
          </div>
        </div>
        
        <div className="modal-body">
          <div className="preview-navigation">
            <button 
              className="btn btn-small"
              onClick={handlePrevious}
              disabled={currentClubNightIndex === 0}
            >
              ← Previous
            </button>
            <span className="preview-counter">
              Club Night {currentClubNightIndex + 1} of {totalClubNights}
            </span>
            <button 
              className="btn btn-small"
              onClick={handleNext}
              disabled={currentClubNightIndex === totalClubNights - 1}
            >
              Next →
            </button>
          </div>

          <div className="club-night-preview">
            <div className="club-night-preview-layout">
              {currentFlyer.flyer?.filePath && (
                <div className="club-night-preview-flyer">
                  <img
                    src={flyersApi.getImageUrl(currentFlyer.flyer.filePath)}
                    alt={`Flyer preview`}
                    className="club-night-flyer-image"
                  />
                </div>
              )}

              <div className="club-night-preview-info">
                <div className="preview-info-card">
                  <div className="preview-info-header">
                    <h2>{currentClubNight.eventName || 'Unknown Event'}</h2>
                    <p className="preview-date">{formatDate(currentClubNight)}</p>
                    <p className="preview-venue">{currentClubNight.venueName || 'Unknown Venue'}</p>
                  </div>

                  {currentClubNight.acts && currentClubNight.acts.length > 0 && (
                    <div className="club-night-lineup">
                      <h3>Line-up</h3>
                      <ul className="lineup-list">
                        {currentClubNight.acts.map((act, idx) => (
                          <li key={idx} className="lineup-item">
                            <span className="act-name">{act.name}</span>
                            {act.isLiveSet && <span className="live-badge">LIVE</span>}
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                  
                  {currentClubNight.candidateYears && currentClubNight.candidateYears.length > 1 && (
                    <div className="preview-notice">
                      <p><strong>Note:</strong> You will be asked to select the correct year for this date.</p>
                    </div>
                  )}
                  
                  {!currentClubNight.eventName && (
                    <div className="preview-notice">
                      <p><strong>Note:</strong> You will be asked to select an event for this club night.</p>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={onCancel}>
            Cancel Upload
          </button>
          <button className="btn btn-primary" onClick={onConfirm}>
            Confirm and Continue
          </button>
        </div>
      </div>
    </div>
  );
}

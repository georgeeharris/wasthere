import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import type { ClubNight } from '../types';
import { clubNightsApi, flyersApi } from '../services/api';
import { ForumSection } from './ForumSection';

export function ClubNightDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [clubNight, setClubNight] = useState<ClubNight | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadData = async () => {
      if (!id) {
        setError('Invalid club night ID');
        setLoading(false);
        return;
      }

      setLoading(true);
      setError(null);
      try {
        const data = await clubNightsApi.getById(parseInt(id, 10));
        setClubNight(data);
      } catch (err) {
        console.error('Failed to load club night:', err);
        setError('Failed to load club night details');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [id]);

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB', {
      weekday: 'long',
      day: '2-digit',
      month: 'long',
      year: 'numeric',
    });
  };

  if (loading) {
    return (
      <div className="card">
        <p className="loading-state">Loading club night details...</p>
      </div>
    );
  }

  if (error || !clubNight) {
    return (
      <div className="card">
        <div className="card-header">
          <h2>Error</h2>
        </div>
        <p className="error-message">{error || 'Club night not found'}</p>
        <button onClick={() => navigate(-1)} className="btn">
          Go Back
        </button>
      </div>
    );
  }

  return (
    <div className="club-night-detail">
      <div className="club-night-detail-header">
        <button onClick={() => navigate(-1)} className="btn btn-back">
          ← Back
        </button>
        <h1>{clubNight.eventName}</h1>
      </div>

      <div className="club-night-detail-content">
        {clubNight.flyerFilePath && (
          <div className="club-night-detail-flyer">
            <img
              src={flyersApi.getImageUrl(clubNight.flyerFilePath)}
              alt={`Flyer for ${clubNight.eventName}`}
              className="club-night-flyer-image"
            />
          </div>
        )}

        <div className="club-night-detail-info">
          <div className="club-night-info-card">
            <div className="club-night-info-header">
              <h2>{formatDate(clubNight.date)}</h2>
              <p className="venue-name">{clubNight.venueName}</p>
            </div>

            {clubNight.acts.length > 0 && (
              <div className="club-night-lineup">
                <h3>Line-up</h3>
                <ul className="lineup-list">
                  {clubNight.acts.map((act) => (
                    <li key={act.actId} className="lineup-item">
                      <span className="act-name">{act.actName}</span>
                      {act.isLiveSet && <span className="live-badge">LIVE</span>}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </div>
      </div>

      <ForumSection clubNightId={clubNight.id} />
    </div>
  );
}

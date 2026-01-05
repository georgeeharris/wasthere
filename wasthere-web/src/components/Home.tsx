import { useNavigate } from 'react-router-dom';

export function Home() {
  const navigate = useNavigate();

  return (
    <div className="card home-page">
      <h2>Welcome to WasThere</h2>
      
      <div className="home-mission">
        <p>
          WasThere is a crowdsourced archive dedicated to preserving the history of club events 
          from the late 1990s through the early 2000s. Between 1995 and 2005, before platforms 
          like Resident Advisor began maintaining historic listings, countless incredible events 
          took place that deserve to be remembered.
        </p>
        <p>
          Our mission is to build a comprehensive timeline of these events by collecting flyers, 
          event details, venue information, and lineups from this golden era of club culture. 
          Every contribution helps preserve this important cultural history for future generations.
        </p>
      </div>

      <div className="home-actions">
        <h3>Get Started</h3>
        <div className="home-action-cards">
          <div className="home-action-card">
            <h4>Browse the Timeline</h4>
            <p>
              Explore club nights chronologically by event series. See which DJs and live acts 
              performed, complete with flyer images from the era.
            </p>
            <button 
              className="btn btn-primary"
              onClick={() => navigate('/timeline')}
            >
              View Timeline
            </button>
          </div>

          <div className="home-action-card">
            <h4>Upload a Flyer</h4>
            <p>
              Contribute to the archive by uploading flyer images. Our AI will automatically 
              extract event details, dates, venues, and lineup information.
            </p>
            <button 
              className="btn btn-primary"
              onClick={() => navigate('/contribute')}
            >
              Upload Flyer
            </button>
          </div>
        </div>
      </div>

      <div className="home-about">
        <h3>How It Works</h3>
        <ol className="home-steps">
          <li>
            <strong>Upload:</strong> Share flyer images from club events between 1995-2005
          </li>
          <li>
            <strong>Extract:</strong> Our AI automatically identifies events, venues, dates, and performing acts
          </li>
          <li>
            <strong>Archive:</strong> Verified information is added to our chronological timeline
          </li>
          <li>
            <strong>Explore:</strong> Browse the complete history of club events from this era
          </li>
        </ol>
      </div>
    </div>
  );
}

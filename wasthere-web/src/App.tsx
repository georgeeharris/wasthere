import { Routes, Route, Navigate, useLocation, useNavigate } from 'react-router-dom';
import './App.css';
import { EventList } from './components/EventList';
import { VenueList } from './components/VenueList';
import { ActList } from './components/ActList';
import { ClubNightList } from './components/ClubNightList';
import { FlyerList } from './components/FlyerList';
import { Timeline } from './components/Timeline';

function App() {
  const location = useLocation();
  const navigate = useNavigate();

  // Determine active tab from current path
  const getActiveTab = () => {
    if (location.pathname === '/timeline') return 'timeline';
    if (location.pathname === '/nights') return 'nights';
    if (location.pathname === '/flyers') return 'flyers';
    if (location.pathname === '/master') return 'master';
    return 'timeline'; // default
  };

  const activeTab = getActiveTab();

  return (
    <div className="app">
      <header className="app-header">
        <h1>WasThere - Club Events Archive</h1>
        <nav className="tabs">
          <button
            className={`tab ${activeTab === 'timeline' ? 'active' : ''}`}
            onClick={() => navigate('/timeline')}
          >
            Timeline
          </button>
          <button
            className={`tab ${activeTab === 'nights' ? 'active' : ''}`}
            onClick={() => navigate('/nights')}
          >
            Club Nights
          </button>
          <button
            className={`tab ${activeTab === 'flyers' ? 'active' : ''}`}
            onClick={() => navigate('/flyers')}
          >
            Flyers
          </button>
          <button
            className={`tab ${activeTab === 'master' ? 'active' : ''}`}
            onClick={() => navigate('/master')}
          >
            Master Lists
          </button>
        </nav>
      </header>

      <main className="app-main">
        <Routes>
          <Route path="/timeline" element={<Timeline />} />
          <Route path="/nights" element={<ClubNightList />} />
          <Route path="/flyers" element={<FlyerList />} />
          <Route path="/master" element={
            <div className="master-lists">
              <EventList />
              <VenueList />
              <ActList />
            </div>
          } />
          <Route path="/" element={<Navigate to="/timeline" replace />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;


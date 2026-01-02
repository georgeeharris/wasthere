import { Routes, Route, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth0 } from '@auth0/auth0-react';
import { useEffect } from 'react';
import './App.css';
import { EventList } from './components/EventList';
import { VenueList } from './components/VenueList';
import { ActList } from './components/ActList';
import { ClubNightList } from './components/ClubNightList';
import { FlyerList } from './components/FlyerList';
import { Timeline } from './components/Timeline';
import ProtectedRoute from './auth/ProtectedRoute';
import { setAccessTokenProvider } from './services/api';

function App() {
  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated, loginWithRedirect, logout, isLoading, getAccessTokenSilently } = useAuth0();

  // Setup the access token provider for API calls
  useEffect(() => {
    setAccessTokenProvider(async () => {
      if (!isAuthenticated) return null;
      try {
        return await getAccessTokenSilently();
      } catch (error) {
        console.error('Error getting access token:', error);
        return null;
      }
    });
  }, [isAuthenticated, getAccessTokenSilently]);

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
          <div style={{ marginLeft: 'auto' }}>
            {!isLoading && (
              isAuthenticated ? (
                <button 
                  className="tab" 
                  onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
                >
                  Log Out
                </button>
              ) : (
                <button 
                  className="tab" 
                  onClick={() => loginWithRedirect()}
                >
                  Log In
                </button>
              )
            )}
          </div>
        </nav>
      </header>

      <main className="app-main">
        <Routes>
          <Route path="/timeline" element={<Timeline />} />
          <Route path="/nights" element={
            <ProtectedRoute>
              <ClubNightList />
            </ProtectedRoute>
          } />
          <Route path="/flyers" element={
            <ProtectedRoute>
              <FlyerList />
            </ProtectedRoute>
          } />
          <Route path="/master" element={
            <ProtectedRoute>
              <div className="master-lists">
                <EventList />
                <VenueList />
                <ActList />
              </div>
            </ProtectedRoute>
          } />
          <Route path="/" element={<Navigate to="/timeline" replace />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;


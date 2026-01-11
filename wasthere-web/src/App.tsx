import { Routes, Route, useLocation, useNavigate } from 'react-router-dom';
import { useAuth0 } from '@auth0/auth0-react';
import { useEffect, useState } from 'react';
import './App.css';
import { EventList } from './components/EventList';
import { VenueList } from './components/VenueList';
import { ActList } from './components/ActList';
import { ClubNightList } from './components/ClubNightList';
import { ClubNightDetail } from './components/ClubNightDetail';
import { FlyerList } from './components/FlyerList';
import { Contribute } from './components/Contribute';
import { Timeline } from './components/Timeline';
import { Home } from './components/Home';
import { Profile } from './components/Profile';
import { ProfileCheckWrapper } from './components/ProfileCheckWrapper';
import { setAccessTokenProvider } from './services/api';

function App() {
  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated, isLoading, getAccessTokenSilently, user, logout, loginWithRedirect } = useAuth0();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  // Debug logging
  useEffect(() => {
    console.log('Auth State:', { isAuthenticated, isLoading, user: user?.email });
  }, [isAuthenticated, isLoading, user]);

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
    if (location.pathname === '/') return 'home';
    if (location.pathname === '/timeline') return 'timeline';
    if (location.pathname === '/nights') return 'nights';
    if (location.pathname === '/flyers') return 'flyers';
    if (location.pathname === '/contribute') return 'contribute';
    if (location.pathname === '/events') return 'events';
    if (location.pathname === '/venues') return 'venues';
    if (location.pathname === '/acts') return 'acts';
    if (location.pathname === '/profile') return 'profile';
    return 'home'; // default
  };

  const activeTab = getActiveTab();

  const handleNavigation = (path: string) => {
    navigate(path);
    setMobileMenuOpen(false);
  };

  return (
    <ProfileCheckWrapper>
      <div className="app">
        <header className="app-header">
          <div className="header-content">
            <h1>WasThere - Club Events Archive</h1>
            <button 
              className="burger-menu"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              aria-label="Toggle menu"
            >
              <span></span>
              <span></span>
              <span></span>
          </button>
        </div>
        <nav className={`tabs ${mobileMenuOpen ? 'mobile-open' : ''}`}>
          <button
            className={`tab ${activeTab === 'home' ? 'active' : ''}`}
            onClick={() => handleNavigation('/')}
          >
            Home
          </button>
          <button
            className={`tab ${activeTab === 'timeline' ? 'active' : ''}`}
            onClick={() => handleNavigation('/timeline')}
          >
            Timeline
          </button>
          <button
            className={`tab ${activeTab === 'flyers' ? 'active' : ''}`}
            onClick={() => handleNavigation('/flyers')}
          >
            Flyers
          </button>
          <button
            className={`tab ${activeTab === 'nights' ? 'active' : ''}`}
            onClick={() => handleNavigation('/nights')}
          >
            Club Nights
          </button>
          <button
            className={`tab ${activeTab === 'contribute' ? 'active' : ''}`}
            onClick={() => handleNavigation('/contribute')}
          >
            Contribute
          </button>
          <button
            className={`tab ${activeTab === 'events' ? 'active' : ''}`}
            onClick={() => handleNavigation('/events')}
          >
            Events
          </button>
          <button
            className={`tab ${activeTab === 'venues' ? 'active' : ''}`}
            onClick={() => handleNavigation('/venues')}
          >
            Venues
          </button>
          <button
            className={`tab ${activeTab === 'acts' ? 'active' : ''}`}
            onClick={() => handleNavigation('/acts')}
          >
            Acts
          </button>
          {!isLoading && (
            isAuthenticated ? (
              <>
                <button 
                  className={`tab ${activeTab === 'profile' ? 'active' : ''}`}
                  onClick={() => handleNavigation('/profile')}
                >
                  Profile
                </button>
                <button 
                  className="tab" 
                  onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
                >
                  Log Out
                </button>
              </>
            ) : (
              <button 
                className="tab" 
                onClick={() => loginWithRedirect()}
              >
                Log In
              </button>
            )
          )}
        </nav>
      </header>

      <main className="app-main">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/timeline" element={<Timeline />} />
          <Route path="/nights" element={<ClubNightList />} />
          <Route path="/nights/:id" element={<ClubNightDetail />} />
          <Route path="/flyers" element={<FlyerList />} />
          <Route path="/contribute" element={<Contribute />} />
          <Route path="/events" element={<EventList />} />
          <Route path="/venues" element={<VenueList />} />
          <Route path="/acts" element={<ActList />} />
          <Route path="/profile" element={<Profile />} />
          {/* Legacy routes for backwards compatibility */}
          <Route path="/master/events" element={<EventList />} />
          <Route path="/master/venues" element={<VenueList />} />
          <Route path="/master/acts" element={<ActList />} />
        </Routes>
      </main>
    </div>
    </ProfileCheckWrapper>
  );
}

export default App;


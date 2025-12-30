import { useState } from 'react';
import './App.css';
import { EventList } from './components/EventList';
import { VenueList } from './components/VenueList';
import { ActList } from './components/ActList';
import { ClubNightList } from './components/ClubNightList';
import { FlyerList } from './components/FlyerList';

function App() {
  const [activeTab, setActiveTab] = useState<'nights' | 'master' | 'flyers'>('nights');

  return (
    <div className="app">
      <header className="app-header">
        <h1>WasThere - Club Events Archive</h1>
        <nav className="tabs">
          <button
            className={`tab ${activeTab === 'nights' ? 'active' : ''}`}
            onClick={() => setActiveTab('nights')}
          >
            Club Nights
          </button>
          <button
            className={`tab ${activeTab === 'flyers' ? 'active' : ''}`}
            onClick={() => setActiveTab('flyers')}
          >
            Flyers
          </button>
          <button
            className={`tab ${activeTab === 'master' ? 'active' : ''}`}
            onClick={() => setActiveTab('master')}
          >
            Master Lists
          </button>
        </nav>
      </header>

      <main className="app-main">
        {activeTab === 'nights' ? (
          <ClubNightList />
        ) : activeTab === 'flyers' ? (
          <FlyerList />
        ) : (
          <div className="master-lists">
            <EventList />
            <VenueList />
            <ActList />
          </div>
        )}
      </main>
    </div>
  );
}

export default App;


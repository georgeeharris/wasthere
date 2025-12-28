import { useState } from 'react';
import './App.css';
import { EventList } from './components/EventList';
import { VenueList } from './components/VenueList';
import { ActList } from './components/ActList';
import { ClubNightList } from './components/ClubNightList';

function App() {
  const [activeTab, setActiveTab] = useState<'nights' | 'master'>('nights');

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


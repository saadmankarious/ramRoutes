import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useLocation } from 'react-router-dom';
import UserForm from './components/UserForm';
import BuildingEventForm from './components/BuildingEventForm';
import './App.css';

function Navigation() {
  const location = useLocation();

  return (
    <nav className="nav">
      <div className="nav-content">
        <h1>RamRoutes Admin Panel</h1>
        <div className="nav-links">
          <Link 
            to="/" 
            className={`nav-link ${location.pathname === '/' ? 'active' : ''}`}
          >
            Create User
          </Link>
          <Link 
            to="/building-event" 
            className={`nav-link ${location.pathname === '/building-event' ? 'active' : ''}`}
          >
            Create Building Event
          </Link>
        </div>
      </div>
    </nav>
  );
}

function App() {
  return (
    <Router>
      <div className="App">
        <Navigation />
        <Routes>
          <Route path="/" element={<UserForm />} />
          <Route path="/building-event" element={<BuildingEventForm />} />
        </Routes>
      </div>
    </Router>
  );
}

export default App;

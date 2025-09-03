import React from 'react';
import { signOut } from 'firebase/auth';
import { auth } from '../firebase';

function Header({ user, currentView, setCurrentView, onLogout }) {
  const handleLogout = async () => {
    try {
      await signOut(auth);
      onLogout();
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  return (
    <header className="app-header">
      <div className="header-container">
        <div className="header-left">
          <h1 className="app-title">Ram Routes Admin</h1>
          <span className="user-badge">
            {user.name} 
            <span className={`role-badge role-${user.role}`}>{user.role}</span>
          </span>
        </div>
        
        <nav className="header-nav">
          {user.role === 'superadmin' && (
            <>
              <button
                className={`nav-button ${currentView === 'admins' ? 'active' : ''}`}
                onClick={() => setCurrentView('admins')}
              >
                Create Admin
              </button>
              <button
                className={`nav-button ${currentView === 'view-admins' ? 'active' : ''}`}
                onClick={() => setCurrentView('view-admins')}
              >
                View Admins
              </button>
              <button
                className={`nav-button ${currentView === 'events' ? 'active' : ''}`}
                onClick={() => setCurrentView('events')}
              >
                Create Event
              </button>
              <button
                className={`nav-button ${currentView === 'view-events' ? 'active' : ''}`}
                onClick={() => setCurrentView('view-events')}
              >
                View Events
              </button>
            </>
          )}
          
          {user.role === 'admin' && (
            <>
              <button
                className={`nav-button ${currentView === 'events' ? 'active' : ''}`}
                onClick={() => setCurrentView('events')}
              >
                Create Event
              </button>
              <button
                className={`nav-button ${currentView === 'view-events' ? 'active' : ''}`}
                onClick={() => setCurrentView('view-events')}
              >
                My Events
              </button>
            </>
          )}
        </nav>

        <div className="header-right">
          <button className="logout-button" onClick={handleLogout}>
            <span className="logout-icon">🚪</span>
            Logout
          </button>
        </div>
      </div>
    </header>
  );
}

export default Header;

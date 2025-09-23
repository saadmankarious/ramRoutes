import React, { useState, useEffect } from 'react';
import { collection, query, where, getDocs, doc, updateDoc, deleteDoc, orderBy, getDoc } from 'firebase/firestore';
import { db } from '../firebase';
import '../styles/Attendees.css';

function EventList({ user, onEditEvent }) {
  const [events, setEvents] = useState([]);
  const [filteredEvents, setFilteredEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [expandedEvents, setExpandedEvents] = useState({});
  const [attendeeData, setAttendeeData] = useState({});
  const [selectedBuilding, setSelectedBuilding] = useState('all');

  // Building options - same as in BuildingEventForm
  const buildingOptions = [
    'McWethy', 'TC', 'Ebersole', 'SAW', 'Library', 'PR', 'Stoner'
  ];

  useEffect(() => {
    loadEvents();
  }, [user]);

  useEffect(() => {
    filterEvents();
  }, [events, selectedBuilding]);

  const loadEvents = async () => {
    try {
      setLoading(true);
      setError(null);
      
      console.log('Loading events for user:', user.uid);
      
      let eventsQuery;
      if (user.role === 'superadmin') {
        // Super admin can see all events - just order by createdAt
        eventsQuery = query(
          collection(db, 'building-events'),
          orderBy('createdAt', 'desc')
        );
      } else {
        // Regular admins - filter by createdBy without ordering to avoid index requirement
        eventsQuery = query(
          collection(db, 'building-events'),
          where('createdBy', '==', user.uid)
        );
      }
      
      const querySnapshot = await getDocs(eventsQuery);
      const eventsList = [];
      
      querySnapshot.forEach((doc) => {
        const data = doc.data();
        eventsList.push({
          id: doc.id,
          ...data,
          // Convert Firestore timestamp to Date for display
          createdAt: data.createdAt?.toDate(),
          date: data.date?.toDate ? data.date.toDate() : data.date
        });
      });
      
      // Sort client-side for regular admins to avoid needing composite index
      if (user.role !== 'superadmin') {
        eventsList.sort((a, b) => {
          if (!a.createdAt) return 1;
          if (!b.createdAt) return -1;
          return b.createdAt.getTime() - a.createdAt.getTime();
        });
      }
      
      console.log('Loaded events:', eventsList);
      setEvents(eventsList);
      
    } catch (error) {
      console.error('Error loading events:', error);
      setError('Failed to load events: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  const filterEvents = () => {
    if (selectedBuilding === 'all') {
      setFilteredEvents(events);
    } else {
      const filtered = events.filter(event => 
        event.buildingName === selectedBuilding
      );
      setFilteredEvents(filtered);
    }
  };

  const handleBuildingFilterChange = (e) => {
    setSelectedBuilding(e.target.value);
  };

  const handleDelete = async (eventId) => {
    try {
      await deleteDoc(doc(db, 'building-events', eventId));
      console.log('Event deleted:', eventId);
      
      // Remove from local state
      setEvents(events.filter(event => event.id !== eventId));
      setDeleteConfirm(null);
      
    } catch (error) {
      console.error('Error deleting event:', error);
      setError('Failed to delete event: ' + error.message);
    }
  };

  const formatDate = (date) => {
    if (!date) return 'N/A';
    if (date instanceof Date) {
      return date.toLocaleString();
    }
    return new Date(date).toLocaleString();
  };

  const toggleEventExpanded = async (eventId) => {
    const isExpanded = expandedEvents[eventId];
    const newExpandedState = {...expandedEvents, [eventId]: !isExpanded};
    setExpandedEvents(newExpandedState);
    
    // If we're expanding and don't have attendee data yet, fetch it
    if (!isExpanded && events.find(e => e.id === eventId)?.attendees?.length > 0) {
      await loadAttendeeData(eventId);
    }
  };
  
  const loadAttendeeData = async (eventId) => {
    const event = events.find(e => e.id === eventId);
    if (!event || !event.attendees || event.attendees.length === 0) return;
    
    try {
      const attendees = {};
      
      for (const userId of event.attendees) {
        // Check if we already have this user's data
        if (!attendeeData[userId]) {
          const userDoc = await getDoc(doc(db, 'users', userId));
          if (userDoc.exists()) {
            const userData = userDoc.data();
            attendees[userId] = {
              id: userId,
              name: userData.name || 'No Name',
              email: userData.email || 'No Email',
              major: userData.major || 'Not specified',
              graduationYear: userData.graduationYear || 'Not specified',
              // knowledgePoints: userData.knowledgePoints || 0,
              // coins: userData.coins || 0
            };
          } else {
            attendees[userId] = { id: userId, name: 'Unknown User', email: 'User not found' };
          }
        }
      }
      
      setAttendeeData(prevData => ({...prevData, ...attendees}));
    } catch (error) {
      console.error('Error loading attendee data:', error);
    }
  };

  const formatEventType = (event) => {
    // Use explicit eventType if available, otherwise infer from date
    const type = event.eventType || (event.date ? 'scheduled' : 'always');
    
    switch (type) {
      case 'always':
        return <span className="event-type always">Always Happening</span>;
      case 'weekly':
        return <span className="event-type weekly">Weekly</span>;
      case 'daily':
        return <span className="event-type daily">Daily</span>;
      case 'monthly':
        return <span className="event-type monthly">Monthly</span>;
      case 'scheduled':
      default:
        return <span className="event-type scheduled">Scheduled</span>;
    }
  };

  if (loading) {
    return (
      <div className="form-container">
        <div className="form-card">
          <div className="loading-spinner">
            <span className="spinner"></span>
            <p>Loading events...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="form-container">
      <div className="form-card events-list-card">
        <div className="events-header">
          <h2 className="form-title">
            {user.role === 'superadmin' ? 'All Building Events' : 'My Building Events'}
          </h2>
          
          <div className="events-controls">
            <div className="filter-section">
              <label htmlFor="building-filter" className="filter-label">
                Filter by Building:
              </label>
              <select
                id="building-filter"
                value={selectedBuilding}
                onChange={handleBuildingFilterChange}
                className="filter-select"
              >
                <option value="all">All Buildings</option>
                {buildingOptions.map(building => (
                  <option key={building} value={building}>
                    {building}
                  </option>
                ))}
              </select>
            </div>
            
            <button 
              className="refresh-button"
              onClick={loadEvents}
              disabled={loading}
            >
              🔄 Refresh
            </button>
          </div>
        </div>

        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
          </div>
        )}

        {filteredEvents.length === 0 ? (
          <div className="empty-state">
            <p>
              {selectedBuilding === 'all' 
                ? 'No building events found.' 
                : `No events found for ${selectedBuilding}.`
              }
            </p>
            {user.role !== 'superadmin' && selectedBuilding === 'all' && (
              <p>Create your first building event to see it here.</p>
            )}
            {selectedBuilding !== 'all' && (
              <button 
                className="form-button-secondary"
                onClick={() => setSelectedBuilding('all')}
              >
                Show All Events
              </button>
            )}
          </div>
        ) : (
          <div>
            <div className="results-summary">
              <p className="results-text">
                Showing {filteredEvents.length} of {events.length} event{events.length !== 1 ? 's' : ''}
                {selectedBuilding !== 'all' && ` for ${selectedBuilding}`}
              </p>
            </div>
            
            <div className="events-grid">
              {filteredEvents.map((event) => (
              <div key={event.id} className="event-card">
                <div className="event-header">
                  <h3 className="event-name">{event.eventName}</h3>
                  {formatEventType(event)}
                </div>
                
                <div className="event-details">
                  <div className="event-detail">
                    <span className="detail-label">Building:</span>
                    <span className="detail-value">{event.buildingName}</span>
                  </div>
                  
                  {event.description && (
                    <div className="event-detail">
                      <span className="detail-label">Description:</span>
                      <span className="detail-value description-text">{event.description}</span>
                    </div>
                  )}
                  
                  {(event.gainedCoins !== undefined && event.gainedCoins !== null) && (
                    <div className="event-detail">
                      <span className="detail-label">Gained Coins:</span>
                      <span className="detail-value">{event.gainedCoins}</span>
                    </div>
                  )}
                  
                  {(event.gainedKb !== undefined && event.gainedKb !== null) && (
                    <div className="event-detail">
                      <span className="detail-label">Gained Knowledge Points:</span>
                      <span className="detail-value">{event.gainedKb}</span>
                    </div>
                  )}
                  
                  {event.buildingId && (
                    <div className="event-detail">
                      <span className="detail-label">Building ID:</span>
                      <span className="detail-value event-id">{event.buildingId}</span>
                    </div>
                  )}
                  
                  {event.date && (
                    <div className="event-detail">
                      <span className="detail-label">Date:</span>
                      <span className="detail-value">{formatDate(event.date)}</span>
                    </div>
                  )}
                  
                  <div className="event-detail">
                    <span className="detail-label">Created:</span>
                    <span className="detail-value">{formatDate(event.createdAt)}</span>
                  </div>
                  
                  {event.createdBy && (
                    <div className="event-detail">
                      <span className="detail-label">Created By:</span>
                      <span className="detail-value">{event.createdBy}</span>
                    </div>
                  )}
                  
                  <div className="event-detail">
                    <span className="detail-label">Document ID:</span>
                    <span className="detail-value event-id">{event.id}</span>
                  </div>
                  
                  <div className="event-detail">
                    <span className="detail-label">Attendees:</span>
                    <span className="detail-value">
                      {event.attendees?.length || 0} student(s)
                      {event.attendees?.length > 0 && (
                        <button 
                          onClick={(e) => {
                            e.stopPropagation();
                            toggleEventExpanded(event.id);
                          }}
                          className="toggle-button"
                        >
                          {expandedEvents[event.id] ? 'Hide details' : 'Show details'}
                        </button>
                      )}
                    </span>
                  </div>
                </div>

                {expandedEvents[event.id] && event.attendees?.length > 0 && (
                  <div className="attendees-section">
                    <h4>Attendee Information</h4>
                    {event.attendees.map(attendeeId => {
                      const attendee = attendeeData[attendeeId] || { name: 'Loading...', email: 'Loading...' };
                      return (
                        <div key={attendeeId} className="attendee-card">
                          <div className="attendee-detail">
                            <span className="attendee-label">Name:</span>
                            <span className="attendee-value">{attendee.name}</span>
                          </div>
                          <div className="attendee-detail">
                            <span className="attendee-label">Email:</span>
                            <span className="attendee-value">{attendee.email}</span>
                          </div>
                          {attendee.major && (
                            <div className="attendee-detail">
                              <span className="attendee-label">Major:</span>
                              <span className="attendee-value">{attendee.major}</span>
                            </div>
                          )}
                          {attendee.graduationYear && (
                            <div className="attendee-detail">
                              <span className="attendee-label">Graduation Year:</span>
                              <span className="attendee-value">{attendee.graduationYear}</span>
                            </div>
                          )}
                          {/* <div className="attendee-detail">
                            <span className="attendee-label">Knowledge Points:</span>
                            <span className="attendee-value">{attendee.knowledgePoints}</span>
                          </div>
                          <div className="attendee-detail">
                            <span className="attendee-label">Coins:</span>
                            <span className="attendee-value">{attendee.coins}</span>
                          </div> */}
                        </div>
                      );
                    })}
                  </div>
                )}

                <div className="event-actions">
                  <button
                    className="action-button edit-button"
                    onClick={() => onEditEvent(event)}
                  >
                    ✏️ Edit
                  </button>
                  
                  <button
                    className="action-button delete-button"
                    onClick={() => setDeleteConfirm(event.id)}
                  >
                    🗑️ Delete
                  </button>
                </div>
              </div>
            ))}
            </div>
          </div>
        )}

        {/* Delete Confirmation Modal */}
        {deleteConfirm && (
          <div className="modal-overlay">
            <div className="modal-content">
              <h3>Confirm Delete</h3>
              <p>Are you sure you want to delete this building event? This action cannot be undone.</p>
              <div className="modal-actions">
                <button 
                  className="form-button cancel-button"
                  onClick={() => setDeleteConfirm(null)}
                >
                  Cancel
                </button>
                <button 
                  className="form-button delete-button"
                  onClick={() => handleDelete(deleteConfirm)}
                >
                  Delete Event
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default EventList;

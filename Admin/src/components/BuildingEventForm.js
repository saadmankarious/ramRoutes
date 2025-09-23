import React, { useState } from 'react';
import { collection, addDoc, serverTimestamp } from 'firebase/firestore';
import { db } from '../firebase';

// Generate a buildingId in the format "BEeee65f46"
const generateBuildingId = () => {
  const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
  let id = 'BE'; // Start with "BE"
  for (let i = 0; i < 8; i++) {
    id += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return id;
};

function BuildingEventForm({ user, onEventCreated }) {
  const [formData, setFormData] = useState({
    buildingName: '',
    eventName: '',
    eventType: 'scheduled', // 'scheduled', 'always', 'weekly', 'daily', 'monthly'
    date: '',
    description: '',
    gainedCoins: 0,
    gainedKb: 0
  });
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState(null);

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(false);

    try {
      console.log('Starting building event creation...');
      const { buildingName, eventName, eventType, date, description, gainedCoins, gainedKb } = formData;

      // Generate a unique buildingId
      const buildingId = generateBuildingId();

      // Create building event document in Firestore - Firebase will auto-generate the document ID
      const eventData = {
        buildingId,
        buildingName,
        eventName,
        eventType, // Store the event type explicitly
        description: description || '', // Add description field
        gainedCoins: parseInt(gainedCoins) || 0, // Add gainedCoins field
        gainedKb: parseInt(gainedKb) || 0, // Add gainedKb field
        createdBy: user?.uid || 'unknown', // Track who created the event
        createdAt: serverTimestamp()
      };

      // Add date field based on event type
      if (eventType === 'scheduled' && date) {
        eventData.date = new Date(date);
        console.log('Scheduled event with date:', eventData.date);
      } else if (eventType === 'always') {
        // For always happening events, set date to null
        eventData.date = null;
        console.log('Always happening event - no specific date');
      } else if ((eventType === 'weekly' || eventType === 'daily' || eventType === 'monthly') && date) {
        // For recurring events, store the pattern date/time
        eventData.date = new Date(date);
        console.log(`${eventType} recurring event with pattern date:`, eventData.date);
      }
      
      console.log('Event data to be added:', eventData);
      console.log('Adding to Firestore collection: building-events');
      
      const docRef = await addDoc(collection(db, 'building-events'), eventData);
      console.log('Building event created successfully with Firebase ID:', docRef.id);
      console.log('Building event buildingId:', buildingId);

      setSuccess(true);

      // Reset form
      setFormData({
        buildingName: '',
        eventName: '',
        eventType: 'scheduled',
        date: '',
        description: '',
        gainedCoins: 0,
        gainedKb: 0
      });

      // Callback for parent component
      if (onEventCreated) {
        onEventCreated(docRef.id);
      }

    } catch (error) {
      console.error('Error creating building event:', error);
      console.error('Error code:', error.code);
      console.error('Error message:', error.message);
      setError(error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-container">
      <div className="form-card">
        <h2 className="form-title">Create Building Event</h2>
        
        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
          </div>
        )}

        {success && (
          <div className="alert alert-success">
            <strong>Success!</strong> Building event created successfully!
            {onEventCreated && (
              <div style={{ marginTop: '1rem' }}>
                <button 
                  className="form-button" 
                  style={{ fontSize: '0.9rem', padding: '0.5rem 1rem' }}
                  onClick={() => onEventCreated('view')}
                >
                  View My Events
                </button>
              </div>
            )}
          </div>
        )}

        <form onSubmit={handleSubmit} className="event-form">
          <div className="form-group">
            <label htmlFor="buildingName" className="form-label">Building Name</label>
            <select
              id="buildingName"
              name="buildingName"
              value={formData.buildingName}
              onChange={handleChange}
              required
              disabled={loading}
              className="form-select"
            >
              <option value="">Select Building</option>
              <option value="McWethy">McWethy</option>
              <option value="TC">TC</option>
              <option value="Ebersole">Ebersole</option>
              <option value="SAW">SAW</option>
              <option value="Library">Library</option>
              <option value="PR">PR</option>
              <option value="Stoner">Stoner</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="eventName" className="form-label">Event Name</label>
            <input
              type="text"
              id="eventName"
              name="eventName"
              value={formData.eventName}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter event name"
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label htmlFor="description" className="form-label">Description (Optional)</label>
            <textarea
              id="description"
              name="description"
              value={formData.description}
              onChange={handleChange}
              disabled={loading}
              placeholder="Enter event description"
              className="form-input"
              rows="3"
              style={{ resize: 'vertical', minHeight: '80px' }}
            />
          </div>

          <div className="form-group">
            <label htmlFor="gainedCoins" className="form-label">Gained Coins</label>
            <input
              type="number"
              id="gainedCoins"
              name="gainedCoins"
              value={formData.gainedCoins}
              onChange={handleChange}
              disabled={loading}
              placeholder="Enter coins gained from this event"
              className="form-input"
              min="0"
            />
          </div>

          <div className="form-group">
            <label htmlFor="gainedKb" className="form-label">Gained Knowledge Points</label>
            <input
              type="number"
              id="gainedKb"
              name="gainedKb"
              value={formData.gainedKb}
              onChange={handleChange}
              disabled={loading}
              placeholder="Enter knowledge points gained from this event"
              className="form-input"
              min="0"
            />
          </div>

          <div className="form-group">
            <label htmlFor="eventType" className="form-label">Event Type</label>
            <select
              id="eventType"
              name="eventType"
              value={formData.eventType}
              onChange={handleChange}
              required
              disabled={loading}
              className="form-select"
            >
              <option value="scheduled">Scheduled Event</option>
              <option value="always">Always Happening</option>
              <option value="weekly">Weekly Recurring</option>
              <option value="daily">Daily Recurring</option>
              <option value="monthly">Monthly Recurring</option>
            </select>
          </div>

          {formData.eventType === 'scheduled' && (
            <div className="form-group">
              <label htmlFor="date" className="form-label">Event Date & Time</label>
              <input
                type="datetime-local"
                id="date"
                name="date"
                value={formData.date}
                onChange={handleChange}
                required={formData.eventType === 'scheduled'}
                disabled={loading}
                className="form-input"
              />
            </div>
          )}

          {(formData.eventType === 'weekly' || formData.eventType === 'daily' || formData.eventType === 'monthly') && (
            <div className="form-group">
              <label htmlFor="date" className="form-label">
                {formData.eventType === 'weekly' && 'Weekly Pattern - Day & Time'}
                {formData.eventType === 'daily' && 'Daily Pattern - Time'}
                {formData.eventType === 'monthly' && 'Monthly Pattern - Day & Time'}
              </label>
              <input
                type="datetime-local"
                id="date"
                name="date"
                value={formData.date}
                onChange={handleChange}
                required={true}
                disabled={loading}
                className="form-input"
              />
              <small className="form-help">
                {formData.eventType === 'weekly' && 'Select the day of the week and time when this event happens every week'}
                {formData.eventType === 'daily' && 'Select the time when this event happens every day'}
                {formData.eventType === 'monthly' && 'Select the day of the month and time when this event happens every month'}
              </small>
            </div>
          )}

          {formData.eventType === 'always' && (
            <div className="form-group">
              <div className="info-box">
                <span className="info-icon">ℹ️</span>
                <p>This event will be marked as "Always Happening" and won't have a specific date/time.</p>
              </div>
            </div>
          )}

          <button 
            type="submit" 
            className={`form-button ${loading ? 'loading' : ''}`}
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner"></span>
                Creating Event...
              </>
            ) : (
              'Create Building Event'
            )}
          </button>
        </form>
      </div>
    </div>
  );
}

export default BuildingEventForm;

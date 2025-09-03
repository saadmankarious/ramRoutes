import React, { useState } from 'react';
import { doc, updateDoc, serverTimestamp } from 'firebase/firestore';
import { db } from '../firebase';

function EventEdit({ event, onCancel, onSave }) {
  const [formData, setFormData] = useState({
    buildingName: event.buildingName || '',
    eventName: event.eventName || '',
    eventType: event.eventType || (event.date ? 'scheduled' : 'always'), // Use explicit eventType or infer from date
    date: event.date ? formatDateForInput(event.date) : ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  function formatDateForInput(date) {
    if (!date) return '';
    
    let dateObj;
    if (date instanceof Date) {
      dateObj = date;
    } else if (date.toDate) {
      dateObj = date.toDate();
    } else {
      dateObj = new Date(date);
    }
    
    // Format as YYYY-MM-DDTHH:MM for datetime-local input
    const year = dateObj.getFullYear();
    const month = String(dateObj.getMonth() + 1).padStart(2, '0');
    const day = String(dateObj.getDate()).padStart(2, '0');
    const hours = String(dateObj.getHours()).padStart(2, '0');
    const minutes = String(dateObj.getMinutes()).padStart(2, '0');
    
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

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

    try {
      console.log('Updating event:', event.id);
      console.log('Form data:', formData);

      const { buildingName, eventName, eventType, date } = formData;

      // Create update data - preserve buildingId from original event
      const updateData = {
        buildingName,
        eventName,
        eventType, // Include the event type in updates
        updatedAt: serverTimestamp()
      };

      // Preserve the original buildingId and createdBy if they exist
      if (event.buildingId) {
        updateData.buildingId = event.buildingId;
      }
      if (event.createdBy) {
        updateData.createdBy = event.createdBy;
      }

      // Handle date based on event type
      if (eventType === 'scheduled' && date) {
        updateData.date = new Date(date);
        console.log('Scheduled event with date:', updateData.date);
      } else if (eventType === 'always') {
        // For always happening events, set date to null
        updateData.date = null;
        console.log('Always happening event - no specific date');
      } else if ((eventType === 'weekly' || eventType === 'daily' || eventType === 'monthly') && date) {
        // For recurring events, store the pattern date/time
        updateData.date = new Date(date);
        console.log(`${eventType} recurring event with pattern date:`, updateData.date);
      }

      console.log('Update data:', updateData);

      // Update the document in Firestore
      await updateDoc(doc(db, 'building-events', event.id), updateData);
      
      console.log('Event updated successfully');
      onSave({
        ...event,
        ...updateData,
        updatedAt: new Date()
      });

    } catch (error) {
      console.error('Error updating event:', error);
      console.error('Error code:', error.code);
      console.error('Error message:', error.message);
      
      let userFriendlyError = 'Failed to update building event. ';
      if (error.code === 'permission-denied') {
        userFriendlyError += 'Permission denied. You may not have permission to edit this event.';
      } else if (error.code === 'not-found') {
        userFriendlyError += 'Event not found.';
      } else {
        userFriendlyError += error.message;
      }
      
      setError(userFriendlyError);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-container">
      <div className="form-card">
        <div className="form-header">
          <h2 className="form-title">Edit Building Event</h2>
          <div className="event-id-display">
            <span className="detail-label">Event ID:</span>
            <span className="detail-value event-id">{event.id}</span>
          </div>
        </div>
        
        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
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

          <div className="form-actions">
            <button 
              type="button"
              className="form-button cancel-button"
              onClick={onCancel}
              disabled={loading}
            >
              Cancel
            </button>
            
            <button 
              type="submit" 
              className={`form-button save-button ${loading ? 'loading' : ''}`}
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="spinner"></span>
                  Saving Changes...
                </>
              ) : (
                'Save Changes'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default EventEdit;

import React, { useState } from 'react';
import { collection, addDoc, serverTimestamp } from 'firebase/firestore';
import { db } from '../firebase';

// Utility function to generate building event ID
const generateBuildingEventId = (length = 8) => {
  const characters = '0123456789ABCDEF';
  let result = 'BE';
  for (let i = 0; i < length; i++) {
    result += characters.charAt(Math.floor(Math.random() * characters.length));
  }
  return result;
};

function BuildingEventForm() {
  const [formData, setFormData] = useState({
    buildingId: generateBuildingEventId(),
    buildingName: '',
    eventName: '',
    date: ''
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
      const { buildingId, buildingName, eventName, date } = formData;

      // Create building event document in Firestore
      await addDoc(collection(db, 'building-events'), {
        buildingId,
        buildingName,
        eventName,
        date: new Date(date), // Convert string to Date object
        createdAt: serverTimestamp()
      });

      setSuccess(true);

      // Reset form with new building ID
      setFormData({
        buildingId: generateBuildingEventId(),
        buildingName: '',
        eventName: '',
        date: ''
      });

    } catch (error) {
      console.error('Error creating building event:', error);
      setError(error.message);
    } finally {
      setLoading(false);
    }
  };

  const regenerateBuildingId = () => {
    setFormData({
      ...formData,
      buildingId: generateBuildingEventId()
    });
  };

  return (
    <div className="container">
      <h2>Create Building Event</h2>
      
      {error && (
        <div className="alert alert-error">
          Error: {error}
        </div>
      )}

      {success && (
        <div className="alert alert-success">
          Building event created successfully!
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="buildingId">Building ID:</label>
          <div style={{ display: 'flex', gap: '10px' }}>
            <input
              type="text"
              id="buildingId"
              name="buildingId"
              value={formData.buildingId}
              onChange={handleChange}
              required
              disabled={loading}
              style={{ flex: 1 }}
            />
            <button
              type="button"
              onClick={regenerateBuildingId}
              className="btn"
              disabled={loading}
              style={{ padding: '10px 16px', fontSize: '14px' }}
            >
              Regenerate
            </button>
          </div>
        </div>

        <div className="form-group">
          <label htmlFor="buildingName">Building Name:</label>
          <select
            id="buildingName"
            name="buildingName"
            value={formData.buildingName}
            onChange={handleChange}
            required
            disabled={loading}
          >
            <option value="">Select Building</option>
            <option value="Adler Journalism Building">Adler Journalism Building</option>
            <option value="Art Building">Art Building</option>
            <option value="Biology Building">Biology Building</option>
            <option value="Chemistry Building">Chemistry Building</option>
            <option value="Dey House">Dey House</option>
            <option value="English-Philosophy Building">English-Philosophy Building</option>
            <option value="Gilmore Hall">Gilmore Hall</option>
            <option value="Halsey Hall">Halsey Hall</option>
            <option value="Iowa Memorial Union">Iowa Memorial Union</option>
            <option value="Jessup Hall">Jessup Hall</option>
            <option value="Lindquist Center">Lindquist Center</option>
            <option value="MacLean Hall">MacLean Hall</option>
            <option value="Main Library">Main Library</option>
            <option value="Mayflower">Mayflower</option>
            <option value="Old Capitol">Old Capitol</option>
            <option value="Pentacrest Museums">Pentacrest Museums</option>
            <option value="Phillips Hall">Phillips Hall</option>
            <option value="Schaeffer Hall">Schaeffer Hall</option>
            <option value="Seashore Hall">Seashore Hall</option>
            <option value="Tippie College of Business">Tippie College of Business</option>
            <option value="University Capitol Centre">University Capitol Centre</option>
            <option value="Van Allen Hall">Van Allen Hall</option>
            <option value="Voxman Music Building">Voxman Music Building</option>
            <option value="Westlawn">Westlawn</option>
          </select>
        </div>

        <div className="form-group">
          <label htmlFor="eventName">Event Name:</label>
          <input
            type="text"
            id="eventName"
            name="eventName"
            value={formData.eventName}
            onChange={handleChange}
            required
            disabled={loading}
            placeholder="Enter event name"
          />
        </div>

        <div className="form-group">
          <label htmlFor="date">Event Date:</label>
          <input
            type="datetime-local"
            id="date"
            name="date"
            value={formData.date}
            onChange={handleChange}
            required
            disabled={loading}
          />
        </div>

        <button 
          type="submit" 
          className="btn" 
          disabled={loading}
        >
          {loading ? 'Creating Event...' : 'Create Building Event'}
        </button>
      </form>
    </div>
  );
}

export default BuildingEventForm;

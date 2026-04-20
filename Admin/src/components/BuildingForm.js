import React, { useState, useEffect } from 'react';
import { collection, addDoc, getDocs, query, orderBy, serverTimestamp } from 'firebase/firestore';
import { db } from '../firebase';

function BuildingForm({ onBuildingCreated }) {
  const [formData, setFormData] = useState({
    buildingName: '',
    schoolId: ''
  });
  const [schools, setSchools] = useState([]);
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchSchools = async () => {
      try {
        const snapshot = await getDocs(query(collection(db, 'schools'), orderBy('createdAt', 'desc')));
        const list = [];
        snapshot.forEach((doc) => list.push({ id: doc.id, ...doc.data() }));
        setSchools(list);
      } catch (err) {
        console.error('Error fetching schools:', err);
      }
    };
    fetchSchools();
  }, []);

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const { buildingName, schoolId } = formData;
      const selectedSchool = schools.find(s => s.schoolId === schoolId);
      const schoolName = selectedSchool ? selectedSchool.schoolName : '';

      const docRef = await addDoc(collection(db, 'buildings'), {
        buildingName,
        schoolId,
        schoolName,
        createdAt: serverTimestamp()
      });

      setSuccess({ buildingName, schoolName });
      setFormData({ buildingName: '', schoolId: '' });

      if (onBuildingCreated) onBuildingCreated(docRef.id);
    } catch (error) {
      console.error('Error creating building:', error);
      setError('Failed to create building: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-container">
      <div className="form-card">
        <h2 className="form-title">Create Building</h2>
        <p className="form-description">Add a new building to a school</p>

        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
          </div>
        )}

        {success && (
          <div className="alert alert-success">
            <h3 style={{ margin: '0 0 10px 0' }}>Building Created!</h3>
            <div className="success-details">
              <p><strong>Building Name:</strong> {success.buildingName}</p>
              <p><strong>School:</strong> {success.schoolName}</p>
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit} className="admin-form">
          <div className="form-group">
            <label htmlFor="buildingName" className="form-label">Building Name</label>
            <input
              type="text"
              id="buildingName"
              name="buildingName"
              value={formData.buildingName}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter building name"
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label htmlFor="schoolId" className="form-label">School</label>
            <select
              id="schoolId"
              name="schoolId"
              value={formData.schoolId}
              onChange={handleChange}
              required
              disabled={loading}
              className="form-select"
            >
              <option value="">Select a school</option>
              {schools.map((school) => (
                <option key={school.id} value={school.schoolId}>
                  {school.schoolName}
                </option>
              ))}
            </select>
          </div>

          <button
            type="submit"
            className={`form-button ${loading ? 'loading' : ''}`}
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner"></span>
                Creating...
              </>
            ) : (
              'Create Building'
            )}
          </button>
        </form>
      </div>
    </div>
  );
}

export default BuildingForm;

import React, { useState, useEffect } from 'react';
import { doc, updateDoc, serverTimestamp, collection, getDocs, query, orderBy } from 'firebase/firestore';
import { db } from '../firebase';

function BuildingEdit({ building, onCancel, onSave }) {
  const [formData, setFormData] = useState({
    buildingName: '',
    schoolId: ''
  });
  const [schools, setSchools] = useState([]);
  const [loading, setLoading] = useState(false);
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

  useEffect(() => {
    if (building) {
      setFormData({
        buildingName: building.buildingName || '',
        schoolId: building.schoolId || ''
      });
    }
  }, [building]);

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const selectedSchool = schools.find(s => s.schoolId === formData.schoolId);
      const schoolName = selectedSchool ? selectedSchool.schoolName : '';

      await updateDoc(doc(db, 'buildings', building.id), {
        buildingName: formData.buildingName,
        schoolId: formData.schoolId,
        schoolName,
        updatedAt: serverTimestamp()
      });

      if (onSave) onSave();
    } catch (error) {
      console.error('Error updating building:', error);
      setError('Failed to update building: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  if (!building) return null;

  return (
    <div className="edit-overlay">
      <div className="edit-modal">
        <div className="edit-header">
          <h2>Edit Building</h2>
          <button className="close-button" onClick={onCancel}>×</button>
        </div>

        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="edit-form">
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

          <div className="form-actions">
            <button type="button" className="cancel-button" onClick={onCancel} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className={`form-button ${loading ? 'loading' : ''}`} disabled={loading}>
              {loading ? (
                <>
                  <span className="spinner"></span>
                  Saving...
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

export default BuildingEdit;

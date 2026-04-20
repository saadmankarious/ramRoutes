import React, { useState, useEffect } from 'react';
import { doc, updateDoc, serverTimestamp } from 'firebase/firestore';
import { db } from '../firebase';

function SchoolEdit({ school, onCancel, onSave }) {
  const [formData, setFormData] = useState({
    schoolName: '',
    schoolId: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (school) {
      setFormData({
        schoolName: school.schoolName || '',
        schoolId: school.schoolId || ''
      });
    }
  }, [school]);

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
      const schoolRef = doc(db, 'schools', school.id);
      await updateDoc(schoolRef, {
        schoolName: formData.schoolName,
        schoolId: formData.schoolId,
        updatedAt: serverTimestamp()
      });

      if (onSave) onSave();
    } catch (error) {
      console.error('Error updating school:', error);
      setError('Failed to update school: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  if (!school) return null;

  return (
    <div className="edit-overlay">
      <div className="edit-modal">
        <div className="edit-header">
          <h2>Edit School</h2>
          <button className="close-button" onClick={onCancel}>×</button>
        </div>

        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="edit-form">
          <div className="form-group">
            <label htmlFor="schoolName" className="form-label">School Name</label>
            <input
              type="text"
              id="schoolName"
              name="schoolName"
              value={formData.schoolName}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter school name"
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label htmlFor="schoolId" className="form-label">School ID</label>
            <input
              type="text"
              id="schoolId"
              name="schoolId"
              value={formData.schoolId}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter school ID"
              className="form-input"
              pattern="[a-zA-Z0-9_-]+"
              title="School ID can only contain letters, numbers, hyphens, and underscores"
            />
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

export default SchoolEdit;

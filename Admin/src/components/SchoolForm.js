import React, { useState } from 'react';
import { doc, setDoc, serverTimestamp } from 'firebase/firestore';
import { db } from '../firebase';

function SchoolForm({ onSchoolCreated }) {
  const [formData, setFormData] = useState({
    schoolName: '',
    schoolId: ''
  });
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(null);
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
    setSuccess(null);

    try {
      const { schoolName, schoolId } = formData;

      const docRef = doc(db, 'schools', schoolId);
      await setDoc(docRef, {
        schoolName,
        schoolId,
        createdAt: serverTimestamp()
      });

      setSuccess({ schoolName, schoolId });
      setFormData({ schoolName: '', schoolId: '' });

      if (onSchoolCreated) {
        onSchoolCreated(schoolId);
      }
    } catch (error) {
      console.error('Error creating school:', error);
      setError('Failed to create school: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-container">
      <div className="form-card">
        <h2 className="form-title">Create School</h2>
        <p className="form-description">Add a new school to the system</p>

        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
          </div>
        )}

        {success && (
          <div className="alert alert-success">
            <h3 style={{ margin: '0 0 10px 0' }}>School Created!</h3>
            <div className="success-details">
              <p><strong>School Name:</strong> {success.schoolName}</p>
              <p><strong>School ID:</strong> {success.schoolId}</p>
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit} className="admin-form">
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
              'Create School'
            )}
          </button>
        </form>
      </div>
    </div>
  );
}

export default SchoolForm;

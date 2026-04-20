import React, { useState, useEffect } from 'react';
import { doc, updateDoc, serverTimestamp, collection, getDocs, query, orderBy } from 'firebase/firestore';
import { db } from '../firebase';

function AdminEdit({ adminToEdit, onCancel, onSave }) {
  const [formData, setFormData] = useState({
    username: '',
    name: '',
    role: 'admin',
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
    if (adminToEdit) {
      setFormData({
        username: adminToEdit.username || '',
        name: adminToEdit.name || '',
        role: adminToEdit.role || 'admin',
        schoolId: adminToEdit.schoolId || ''
      });
    }
  }, [adminToEdit]);

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
      console.log('Updating admin:', adminToEdit.id, formData);
      
      const adminRef = doc(db, 'admins', adminToEdit.id);
      
      const selectedSchool = schools.find(s => s.schoolId === formData.schoolId);
      const schoolName = selectedSchool ? selectedSchool.schoolName : '';

      await updateDoc(adminRef, {
        username: formData.username,
        name: formData.name,
        role: formData.role,
        schoolId: formData.schoolId,
        schoolName,
        updatedAt: serverTimestamp(),
        updatedBy: 'superadmin'
      });

      console.log('Admin updated successfully');
      
      // Call the onSave callback to refresh the list and close the edit form
      if (onSave) {
        onSave();
      }
      
    } catch (error) {
      console.error('Error updating admin:', error);
      setError('Failed to update admin user: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  if (!adminToEdit) {
    return null;
  }

  return (
    <div className="edit-overlay">
      <div className="edit-modal">
        <div className="edit-header">
          <h2>Edit Admin User</h2>
          <button className="close-button" onClick={onCancel}>×</button>
        </div>

        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="edit-form">
          <div className="form-group">
            <label htmlFor="username" className="form-label">Username</label>
            <input
              type="text"
              id="username"
              name="username"
              value={formData.username}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter username (no spaces)"
              className="form-input"
              pattern="[a-zA-Z0-9_-]+"
              title="Username can only contain letters, numbers, hyphens, and underscores"
            />
            <small className="form-help">Email will be {formData.username}@admin.local</small>
          </div>

          <div className="form-group">
            <label htmlFor="name" className="form-label">Full Name</label>
            <input
              type="text"
              id="name"
              name="name"
              value={formData.name}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter full name"
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label htmlFor="role" className="form-label">Role</label>
            <select
              id="role"
              name="role"
              value={formData.role}
              onChange={handleChange}
              required
              disabled={loading}
              className="form-select"
            >
              <option value="admin">Admin</option>
              <option value="superadmin">Super Admin</option>
            </select>
            <small className="form-help">Super admin can manage other admins</small>
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
            <button
              type="button"
              className="cancel-button"
              onClick={onCancel}
              disabled={loading}
            >
              Cancel
            </button>
            
            <button
              type="submit"
              className={`form-button ${loading ? 'loading' : ''}`}
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="spinner"></span>
                  Updating...
                </>
              ) : (
                'Update Admin'
              )}
            </button>
          </div>
        </form>

        <div className="edit-footer">
          <p className="help-text">
            📝 <strong>Note:</strong> Changing username will not update the Firebase Auth email. 
            Password changes must be done through Firebase Console.
          </p>
        </div>
      </div>
    </div>
  );
}

export default AdminEdit;

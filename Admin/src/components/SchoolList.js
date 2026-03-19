import React, { useState, useEffect } from 'react';
import { collection, query, getDocs, deleteDoc, doc, orderBy } from 'firebase/firestore';
import { db } from '../firebase';

function SchoolList({ onEditSchool }) {
  const [schools, setSchools] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(null);

  useEffect(() => {
    fetchSchools();
  }, []);

  const fetchSchools = async () => {
    try {
      setLoading(true);
      const schoolsQuery = query(collection(db, 'schools'), orderBy('createdAt', 'desc'));
      const snapshot = await getDocs(schoolsQuery);
      const list = [];
      snapshot.forEach((doc) => {
        list.push({ id: doc.id, ...doc.data() });
      });
      setSchools(list);
    } catch (error) {
      console.error('Error fetching schools:', error);
      setError('Failed to load schools');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (schoolDocId, schoolName) => {
    if (!window.confirm(`Delete school "${schoolName}"? This cannot be undone.`)) return;
    setDeleteLoading(schoolDocId);
    try {
      await deleteDoc(doc(db, 'schools', schoolDocId));
      await fetchSchools();
    } catch (error) {
      console.error('Error deleting school:', error);
      setError('Failed to delete school');
    } finally {
      setDeleteLoading(null);
    }
  };

  const formatDate = (timestamp) => {
    if (!timestamp) return 'Unknown';
    const date = timestamp.toDate ? timestamp.toDate() : new Date(timestamp);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  };

  if (loading) {
    return (
      <div className="loading-container">
        <div className="spinner"></div>
        <p>Loading schools...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="error-container">
        <div className="alert alert-error">
          <strong>Error:</strong> {error}
        </div>
        <button className="form-button" onClick={fetchSchools}>Try Again</button>
      </div>
    );
  }

  return (
    <div className="admin-list-container">
      <div className="admin-list-header">
        <h2>Schools</h2>
        <p className="admin-list-description">Manage schools in the system.</p>
        <button className="form-button refresh-button" onClick={fetchSchools}>🔄 Refresh List</button>
      </div>

      {schools.length === 0 ? (
        <div className="no-admins">
          <div className="empty-state">
            <div className="empty-icon">🏫</div>
            <h3>No Schools Found</h3>
            <p>Create a school to get started.</p>
          </div>
        </div>
      ) : (
        <div className="admin-list">
          {schools.map((school) => (
            <div key={school.id} className="admin-card">
              <div className="admin-info">
                <div className="admin-header">
                  <h3 className="admin-name">{school.schoolName}</h3>
                </div>
                <div className="admin-details">
                  <p><strong>School ID:</strong> {school.schoolId}</p>
                  <p><strong>Created:</strong> {formatDate(school.createdAt)}</p>
                </div>
              </div>
              <div className="admin-actions">
                <button className="edit-button" onClick={() => onEditSchool(school)}>✏️ Edit</button>
                <button
                  className="delete-button"
                  onClick={() => handleDelete(school.id, school.schoolName)}
                  disabled={deleteLoading === school.id}
                >
                  {deleteLoading === school.id ? 'Deleting...' : '🗑️ Delete'}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default SchoolList;

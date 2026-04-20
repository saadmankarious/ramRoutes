import React, { useState, useEffect } from 'react';
import { collection, query, getDocs, deleteDoc, doc, orderBy } from 'firebase/firestore';
import { db } from '../firebase';

function BuildingList({ onEditBuilding }) {
  const [buildings, setBuildings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(null);

  useEffect(() => {
    fetchBuildings();
  }, []);

  const fetchBuildings = async () => {
    try {
      setLoading(true);
      const snapshot = await getDocs(query(collection(db, 'buildings'), orderBy('createdAt', 'desc')));
      const list = [];
      snapshot.forEach((doc) => list.push({ id: doc.id, ...doc.data() }));
      setBuildings(list);
    } catch (error) {
      console.error('Error fetching buildings:', error);
      setError('Failed to load buildings');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (buildingId, buildingName) => {
    if (!window.confirm(`Delete building "${buildingName}"? This cannot be undone.`)) return;
    setDeleteLoading(buildingId);
    try {
      await deleteDoc(doc(db, 'buildings', buildingId));
      await fetchBuildings();
    } catch (error) {
      console.error('Error deleting building:', error);
      setError('Failed to delete building');
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
        <p>Loading buildings...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="error-container">
        <div className="alert alert-error">
          <strong>Error:</strong> {error}
        </div>
        <button className="form-button" onClick={fetchBuildings}>Try Again</button>
      </div>
    );
  }

  return (
    <div className="admin-list-container">
      <div className="admin-list-header">
        <h2>Buildings</h2>
        <p className="admin-list-description">Manage buildings across schools.</p>
        <button className="form-button refresh-button" onClick={fetchBuildings}>🔄 Refresh List</button>
      </div>

      {buildings.length === 0 ? (
        <div className="no-admins">
          <div className="empty-state">
            <div className="empty-icon">🏢</div>
            <h3>No Buildings Found</h3>
            <p>Create a building to get started.</p>
          </div>
        </div>
      ) : (
        <div className="admin-list">
          {buildings.map((building) => (
            <div key={building.id} className="admin-card">
              <div className="admin-info">
                <div className="admin-header">
                  <h3 className="admin-name">{building.buildingName}</h3>
                </div>
                <div className="admin-details">
                  <p><strong>School:</strong> {building.schoolName || 'Not assigned'}</p>
                  <p><strong>Created:</strong> {formatDate(building.createdAt)}</p>
                </div>
              </div>
              <div className="admin-actions">
                <button className="edit-button" onClick={() => onEditBuilding(building)}>✏️ Edit</button>
                <button
                  className="delete-button"
                  onClick={() => handleDelete(building.id, building.buildingName)}
                  disabled={deleteLoading === building.id}
                >
                  {deleteLoading === building.id ? 'Deleting...' : '🗑️ Delete'}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default BuildingList;

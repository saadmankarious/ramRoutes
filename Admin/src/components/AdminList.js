import React, { useState, useEffect } from 'react';
import { collection, query, getDocs, deleteDoc, doc } from 'firebase/firestore';
import { deleteUser } from 'firebase/auth';
import { db } from '../firebase';

function AdminList({ user, onEditAdmin }) {
  const [admins, setAdmins] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(null);

  useEffect(() => {
    fetchAdmins();
  }, []);

  const fetchAdmins = async () => {
    try {
      console.log('Fetching admin users...');
      const adminsRef = collection(db, 'admins');
      const q = query(adminsRef);
      
      const querySnapshot = await getDocs(q);
      const adminsList = [];
      
      querySnapshot.forEach((doc) => {
        adminsList.push({
          id: doc.id,
          ...doc.data()
        });
      });

      // Sort by creation date (newest first)
      adminsList.sort((a, b) => {
        if (a.createdAt && b.createdAt) {
          return b.createdAt.seconds - a.createdAt.seconds;
        }
        return 0;
      });

      console.log(`Found ${adminsList.length} admin users`);
      setAdmins(adminsList);
    } catch (error) {
      console.error('Error fetching admins:', error);
      setError('Failed to load admin users');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (adminId, adminUsername) => {
    const confirmed = window.confirm(
      `Are you sure you want to delete admin "${adminUsername}"? This action cannot be undone.`
    );
    
    if (!confirmed) return;

    setDeleteLoading(adminId);
    
    try {
      console.log(`Deleting admin: ${adminId}`);
      
      // Delete from Firestore
      await deleteDoc(doc(db, 'admins', adminId));
      
      // Note: We cannot delete Firebase Auth users from the client side
      // The auth user will remain but won't have access to the admin panel
      
      console.log('Admin deleted successfully');
      
      // Refresh the list
      await fetchAdmins();
      
    } catch (error) {
      console.error('Error deleting admin:', error);
      setError('Failed to delete admin user');
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
        <p>Loading admin users...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="error-container">
        <div className="alert alert-error">
          <strong>Error:</strong> {error}
        </div>
        <button className="form-button" onClick={fetchAdmins}>
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div className="admin-list-container">
        <div className="admin-list-header">
          <h2>Admin Users</h2>
          <p className="admin-list-description">
            Manage administrator accounts that can create and manage building events.
          </p>
          
          <button className="form-button refresh-button" onClick={fetchAdmins}>
            🔄 Refresh List
          </button>
        </div>      {admins.length === 0 ? (
        <div className="no-admins">
          <div className="empty-state">
            <div className="empty-icon">👥</div>
            <h3>No Admin Users Found</h3>
            <p>Create admin users to manage building events.</p>
          </div>
        </div>
      ) : (
        <div className="admin-list">
          {admins.map((admin) => (
            <div key={admin.id} className="admin-card">
              <div className="admin-info">
                <div className="admin-header">
                  <h3 className="admin-name">{admin.name}</h3>
                  <span className={`role-badge role-${admin.role}`}>
                    {admin.role}
                  </span>
                </div>
                
                <div className="admin-details">
                  <p><strong>Username:</strong> {admin.username}</p>
                  <p><strong>Email:</strong> {admin.email}</p>
                  <p><strong>School:</strong> {admin.schoolName || 'Not assigned'}</p>
                  <p><strong>Created:</strong> {formatDate(admin.createdAt)}</p>
                  {admin.createdBy && <p><strong>Created By:</strong> {admin.createdBy}</p>}
                </div>
              </div>

              <div className="admin-actions">
                <button
                  className="edit-button"
                  onClick={() => onEditAdmin(admin)}
                  title="Edit Admin"
                >
                  ✏️ Edit
                </button>
                
                <button
                  className="delete-button"
                  onClick={() => handleDelete(admin.id, admin.username)}
                  disabled={deleteLoading === admin.id}
                  title="Delete Admin"
                >
                  {deleteLoading === admin.id ? (
                    <span className="spinner-small"></span>
                  ) : (
                    '🗑️ Delete'
                  )}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
      
      <div className="admin-list-footer">
        <p className="help-text">
          📝 <strong>Note:</strong> Deleting an admin removes their access to the admin panel, 
          but their Firebase Auth account will remain. They won't be able to login to this system.
        </p>
      </div>
    </div>
  );
}

export default AdminList;

import React, { useState, useEffect } from 'react';
import { collection, query, getDocs, doc, deleteDoc, orderBy } from 'firebase/firestore';
import { db } from '../firebase';
import '../styles/Attendees.css';

function StoreItemList({ user, onEditItem }) {
  const [items, setItems] = useState([]);
  const [filteredItems, setFilteredItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [selectedStatus, setSelectedStatus] = useState('all');

  const categoryOptions = [
    'all', 'general', 'clothing', 'accessories', 'consumables', 'upgrades', 'special', 'whisper'
  ];

  useEffect(() => {
    loadItems();
  }, [user]);

  useEffect(() => {
    filterItems();
  }, [items, selectedCategory, selectedStatus]);

  const loadItems = async () => {
    try {
      setLoading(true);
      setError(null);
      
      console.log('Loading store items for user:', user.uid);
      
      const itemsQuery = query(
        collection(db, 'store-items'),
        orderBy('createdAt', 'desc')
      );
      
      const snapshot = await getDocs(itemsQuery);
      console.log('Raw snapshot size:', snapshot.size);
      
      const itemsData = [];
      snapshot.forEach((doc) => {
        const data = doc.data();
        itemsData.push({
          id: doc.id,
          ...data
        });
      });

      console.log('Loaded items:', itemsData.length);
      setItems(itemsData);
      
    } catch (err) {
      console.error('Error loading store items:', err);
      setError('Failed to load store items: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const filterItems = () => {
    let filtered = [...items];

    if (selectedCategory !== 'all') {
      filtered = filtered.filter(item => item.category === selectedCategory);
    }

    if (selectedStatus !== 'all') {
      const isAvailable = selectedStatus === 'available';
      filtered = filtered.filter(item => item.available === isAvailable);
    }

    setFilteredItems(filtered);
  };

  const handleDelete = async (itemId) => {
    try {
      await deleteDoc(doc(db, 'store-items', itemId));
      console.log('Store item deleted:', itemId);
      
      setItems(prevItems => prevItems.filter(item => item.id !== itemId));
      setDeleteConfirm(null);
      
    } catch (err) {
      console.error('Error deleting store item:', err);
      setError('Failed to delete store item: ' + err.message);
    }
  };

  const formatPrice = (coins, kb) => {
    const parts = [];
    if (coins > 0) parts.push(`${coins} coins`);
    if (kb > 0) parts.push(`${kb} KB`);
    return parts.length > 0 ? parts.join(' + ') : 'Free';
  };

  const formatDate = (timestamp) => {
    if (!timestamp) return 'N/A';
    
    try {
      let date;
      if (timestamp.toDate) {
        date = timestamp.toDate();
      } else {
        date = new Date(timestamp);
      }
      
      return date.toLocaleString();
    } catch (error) {
      console.error('Error formatting date:', error);
      return 'Invalid Date';
    }
  };

  const getWhisperTypeName = (whisperType) => {
    const whisperNames = {
      0: 'Greeting',
      1: 'Have A Nice Lift',
      2: 'Heart',
      3: 'Aros Hi',
      4: 'Aros Nice Day',
      5: 'Beat Coe'
    };
    return whisperNames[whisperType] || `Unknown (${whisperType})`;
  };

  if (user.role !== 'superadmin') {
    return (
      <div className="access-denied">
        <h2>Access Denied</h2>
        <p>Super admin role required to manage store items.</p>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="loading-container">
        <div className="loading-spinner">
          <span className="spinner"></span>
          <p>Loading store items...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-list-container">
      <div className="admin-list-header">
        <div>
          <h2>Store Items Management</h2>
          <p className="admin-list-description">Manage items available in the game store</p>
        </div>
      </div>

      {error && (
        <div className="alert alert-error">
          ❌ {error}
        </div>
      )}

      <div className="admin-list-controls">
        <div className="filter-controls">
          <div className="filter-group">
            <label className="form-label">Category:</label>
            <select 
              value={selectedCategory} 
              onChange={(e) => setSelectedCategory(e.target.value)}
              className="form-select"
            >
              {categoryOptions.map(category => (
                <option key={category} value={category}>
                  {category === 'all' ? 'All Categories' : category.charAt(0).toUpperCase() + category.slice(1)}
                </option>
              ))}
            </select>
          </div>

          <div className="filter-group">
            <label className="form-label">Status:</label>
            <select 
              value={selectedStatus} 
              onChange={(e) => setSelectedStatus(e.target.value)}
              className="form-select"
            >
              <option value="all">All Items</option>
              <option value="available">Available Only</option>
              <option value="unavailable">Unavailable Only</option>
            </select>
          </div>

          <div className="stats">
            <span>Total: {items.length}</span>
            <span>Filtered: {filteredItems.length}</span>
          </div>
        </div>
      </div>

      {filteredItems.length === 0 ? (
        <div className="no-data">
          <p>No store items found matching the current filters.</p>
        </div>
      ) : (
        <div className="admin-list">
          {filteredItems.map((item) => (
            <div key={item.id} className="admin-card">
              <div className="admin-info">
                <div className="admin-header">
                  <h3 className="admin-name">{item.name}</h3>
                  <div className="item-badges">
                    <span className={`status-badge ${item.available ? 'available' : 'unavailable'}`}>
                      {item.available ? 'Available' : 'Unavailable'}
                    </span>
                    <span className="category-badge">
                      {item.category}
                    </span>
                  </div>
                </div>

                <div className="admin-details">
                  {item.imageUrl && (
                    <div className="item-image" style={{ marginBottom: '10px' }}>
                      <img 
                        src={item.imageUrl} 
                        alt={item.name}
                        style={{ 
                          maxWidth: '80px', 
                          maxHeight: '80px', 
                          objectFit: 'cover',
                          borderRadius: '4px'
                        }}
                        onError={(e) => { e.target.style.display = 'none'; }}
                      />
                    </div>
                  )}
                  
                  {item.description && (
                    <p>{item.description}</p>
                  )}
                  
                  <p><strong>Price: {formatPrice(item.priceCoins, item.priceKb)}</strong></p>

                  {item.category === 'whisper' && item.whisperType !== undefined && (
                    <p><strong>Whisper Type: {getWhisperTypeName(item.whisperType)}</strong></p>
                  )}

                  <p><small>Created: {formatDate(item.createdAt)}</small></p>
                  {item.updatedAt && item.updatedAt !== item.createdAt && (
                    <p><small>Updated: {formatDate(item.updatedAt)}</small></p>
                  )}
                </div>
              </div>

              <div className="admin-actions">
                <button 
                  className="form-button"
                  onClick={() => onEditItem(item)}
                >
                  Edit
                </button>
                <button 
                  className="form-button delete-button"
                  onClick={() => setDeleteConfirm(item.id)}
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {deleteConfirm && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h3>Confirm Delete</h3>
            <p>Are you sure you want to delete this store item?</p>
            <p><strong>{items.find(item => item.id === deleteConfirm)?.name}</strong></p>
            <div className="form-actions">
              <button 
                className="form-button delete-button"
                onClick={() => handleDelete(deleteConfirm)}
              >
                Yes, Delete
              </button>
              <button 
                className="form-button cancel-button"
                onClick={() => setDeleteConfirm(null)}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default StoreItemList;

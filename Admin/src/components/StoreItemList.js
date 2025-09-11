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
    'all', 'general', 'clothing', 'accessories', 'consumables', 'upgrades', 'special'
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
    <div className="list-container">
      <div className="list-header">
        <h2>Store Items Management</h2>
        <p>Manage items available in the game store</p>
      </div>

      {error && (
        <div className="error-message">
          ❌ {error}
        </div>
      )}

      <div className="filter-controls">
        <div className="filter-group">
          <label>Category:</label>
          <select 
            value={selectedCategory} 
            onChange={(e) => setSelectedCategory(e.target.value)}
          >
            {categoryOptions.map(category => (
              <option key={category} value={category}>
                {category === 'all' ? 'All Categories' : category.charAt(0).toUpperCase() + category.slice(1)}
              </option>
            ))}
          </select>
        </div>

        <div className="filter-group">
          <label>Status:</label>
          <select 
            value={selectedStatus} 
            onChange={(e) => setSelectedStatus(e.target.value)}
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

      {filteredItems.length === 0 ? (
        <div className="no-data">
          <p>No store items found matching the current filters.</p>
        </div>
      ) : (
        <div className="items-grid">
          {filteredItems.map((item) => (
            <div key={item.id} className="item-card">
              <div className="item-header">
                <h3>{item.name}</h3>
                <div className="item-badges">
                  <span className={`status-badge ${item.available ? 'available' : 'unavailable'}`}>
                    {item.available ? 'Available' : 'Unavailable'}
                  </span>
                  <span className="category-badge">
                    {item.category}
                  </span>
                </div>
              </div>

              <div className="item-content">
                {item.imageUrl && (
                  <div className="item-image">
                    <img 
                      src={item.imageUrl} 
                      alt={item.name}
                      style={{ 
                        maxWidth: '80px', 
                        maxHeight: '80px', 
                        objectFit: 'cover',
                        borderRadius: '4px',
                        marginBottom: '10px'
                      }}
                      onError={(e) => { e.target.style.display = 'none'; }}
                    />
                  </div>
                )}
                
                {item.description && (
                  <p className="item-description">{item.description}</p>
                )}
                
                <div className="item-price">
                  <strong>Price: {formatPrice(item.priceCoins, item.priceKb)}</strong>
                </div>

                <div className="item-meta">
                  <small>Created: {formatDate(item.createdAt)}</small>
                  {item.updatedAt && item.updatedAt !== item.createdAt && (
                    <small>Updated: {formatDate(item.updatedAt)}</small>
                  )}
                </div>
              </div>

              <div className="item-actions">
                <button 
                  className="edit-button"
                  onClick={() => onEditItem(item)}
                >
                  Edit
                </button>
                <button 
                  className="delete-button"
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
        <div className="delete-modal">
          <div className="delete-modal-content">
            <h3>Confirm Delete</h3>
            <p>Are you sure you want to delete this store item?</p>
            <p><strong>{items.find(item => item.id === deleteConfirm)?.name}</strong></p>
            <div className="delete-modal-actions">
              <button 
                className="confirm-delete"
                onClick={() => handleDelete(deleteConfirm)}
              >
                Yes, Delete
              </button>
              <button 
                className="cancel-delete"
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

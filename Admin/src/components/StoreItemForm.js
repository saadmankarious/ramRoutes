import React, { useState } from 'react';
import { collection, addDoc, serverTimestamp } from 'firebase/firestore';
import { db } from '../firebase';

function StoreItemForm({ user, onItemCreated }) {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    priceCoins: 0,
    priceKb: 0,
    category: 'general',
    available: true
  });
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState(null);

  const categoryOptions = [
    'general',
    'clothing',
    'accessories',
    'consumables',
    'upgrades',
    'special'
  ];

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData({
      ...formData,
      [name]: type === 'checkbox' ? checked : (type === 'number' ? parseInt(value) || 0 : value)
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(false);

    try {
      console.log('Starting store item creation...');
      
      const itemData = {
        ...formData,
        createdBy: user.uid,
        createdAt: serverTimestamp(),
        updatedAt: serverTimestamp()
      };

      const docRef = await addDoc(collection(db, 'store-items'), itemData);
      console.log('Store item created with ID:', docRef.id);

      setSuccess(true);
      setFormData({
        name: '',
        description: '',
        priceCoins: 0,
        priceKb: 0,
        category: 'general',
        available: true
      });

      setTimeout(() => {
        setSuccess(false);
        if (onItemCreated) {
          onItemCreated(docRef.id);
        }
      }, 2000);

    } catch (error) {
      console.error('Error creating store item:', error);
      setError('Failed to create store item: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  if (user.role !== 'superadmin') {
    return (
      <div className="access-denied">
        <h2>Access Denied</h2>
        <p>Super admin role required to create store items.</p>
      </div>
    );
  }

  return (
    <div className="form-container">
      <div className="form-header">
        <h2>Create Store Item</h2>
        <p>Add a new item to the game store</p>
      </div>

      {success && (
        <div className="success-message">
          ✅ Store item created successfully!
        </div>
      )}

      {error && (
        <div className="error-message">
          ❌ {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="admin-form">
        <div className="form-group">
          <label htmlFor="name">Item Name *</label>
          <input
            type="text"
            id="name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            required
            placeholder="Enter item name"
          />
        </div>

        <div className="form-group">
          <label htmlFor="description">Description</label>
          <textarea
            id="description"
            name="description"
            value={formData.description}
            onChange={handleChange}
            rows="3"
            placeholder="Enter item description"
          />
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="priceCoins">Price (Coins)</label>
            <input
              type="number"
              id="priceCoins"
              name="priceCoins"
              value={formData.priceCoins}
              onChange={handleChange}
              min="0"
              placeholder="0"
            />
          </div>

          <div className="form-group">
            <label htmlFor="priceKb">Price (Knowledge Points)</label>
            <input
              type="number"
              id="priceKb"
              name="priceKb"
              value={formData.priceKb}
              onChange={handleChange}
              min="0"
              placeholder="0"
            />
          </div>
        </div>

        <div className="form-group">
          <label htmlFor="category">Category</label>
          <select
            id="category"
            name="category"
            value={formData.category}
            onChange={handleChange}
          >
            {categoryOptions.map(category => (
              <option key={category} value={category}>
                {category.charAt(0).toUpperCase() + category.slice(1)}
              </option>
            ))}
          </select>
        </div>

        <div className="form-group checkbox-group">
          <label>
            <input
              type="checkbox"
              name="available"
              checked={formData.available}
              onChange={handleChange}
            />
            Available for purchase
          </label>
        </div>

        <div className="form-actions">
          <button 
            type="submit" 
            className="submit-button"
            disabled={loading}
          >
            {loading ? 'Creating...' : 'Create Store Item'}
          </button>
          
          <button 
            type="button" 
            className="view-button"
            onClick={() => onItemCreated && onItemCreated('view')}
          >
            View Store Items
          </button>
        </div>
      </form>
    </div>
  );
}

export default StoreItemForm;

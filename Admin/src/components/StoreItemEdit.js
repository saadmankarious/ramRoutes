import React, { useState } from 'react';
import { doc, updateDoc, serverTimestamp } from 'firebase/firestore';
import { db } from '../firebase';

function StoreItemEdit({ item, onCancel, onSave }) {
  const [formData, setFormData] = useState({
    name: item.name || '',
    description: item.description || '',
    priceCoins: item.priceCoins || 0,
    priceKb: item.priceKb || 0,
    category: item.category || 'general',
    imageUrl: item.imageUrl || '',
    available: item.available !== undefined ? item.available : true,
    whisperType: item.whisperType || 0
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const categoryOptions = [
    'general',
    'clothing',
    'accessories',
    'consumables',
    'upgrades',
    'special',
    'whisper'
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

    try {
      console.log('Updating store item:', item.id);
      
      const updateData = {
        ...formData,
        updatedAt: serverTimestamp()
      };

      const itemRef = doc(db, 'store-items', item.id);
      await updateDoc(itemRef, updateData);

      console.log('Store item updated successfully');
      onSave({ ...item, ...updateData });

    } catch (err) {
      console.error('Error updating store item:', err);
      setError('Failed to update store item: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="edit-container">
      <div className="edit-header">
        <h2>Edit Store Item</h2>
        <p>Update item details</p>
      </div>

      {error && (
        <div className="error-message">
          ❌ {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="edit-form">
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

        <div className="form-group">
          <label htmlFor="imageUrl">Image URL</label>
          <input
            type="url"
            id="imageUrl"
            name="imageUrl"
            value={formData.imageUrl}
            onChange={handleChange}
            placeholder="https://example.com/image.jpg"
          />
          {formData.imageUrl && (
            <div className="image-preview">
              <img 
                src={formData.imageUrl} 
                alt="Preview" 
                style={{ maxWidth: '100px', maxHeight: '100px', marginTop: '10px' }}
                onError={(e) => { e.target.style.display = 'none'; }}
              />
            </div>
          )}
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

        {formData.category === 'whisper' && (
          <div className="form-group">
            <label htmlFor="whisperType">Whisper Type</label>
            <select
              id="whisperType"
              name="whisperType"
              value={formData.whisperType}
              onChange={handleChange}
            >
              <option value={0}>Greeting</option>
              <option value={1}>Have A Nice Lift</option>
              <option value={2}>Heart</option>
              <option value={3}>Aros Hi</option>
              <option value={4}>Aros Nice Day</option>
              <option value={5}>Beat Coe</option>
            </select>
          </div>
        )}

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
            className="save-button"
            disabled={loading}
          >
            {loading ? 'Saving...' : 'Save Changes'}
          </button>
          
          <button 
            type="button" 
            className="cancel-button"
            onClick={onCancel}
            disabled={loading}
          >
            Cancel
          </button>
        </div>
      </form>

      <div className="item-info">
        <h3>Item Information</h3>
        <div className="info-grid">
          <div className="info-item">
            <strong>Item ID:</strong> {item.id}
          </div>
          <div className="info-item">
            <strong>Created:</strong> {item.createdAt ? new Date(item.createdAt.toDate()).toLocaleString() : 'N/A'}
          </div>
          <div className="info-item">
            <strong>Last Updated:</strong> {item.updatedAt ? new Date(item.updatedAt.toDate()).toLocaleString() : 'N/A'}
          </div>
        </div>
      </div>
    </div>
  );
}

export default StoreItemEdit;

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
    imageUrl: '',
    available: true,
    whisperType: 0 // Default to Greeting (0)
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
        available: true,
        whisperType: 0
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
      <div className="form-card">
        <div className="form-header">
          <h2 className="form-title">Create Store Item</h2>
          <p className="form-description">Add a new item to the game store</p>
        </div>

        {success && (
          <div className="alert alert-success">
            ✅ Store item created successfully!
          </div>
        )}

        {error && (
          <div className="alert alert-error">
            ❌ {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="name" className="form-label">Item Name *</label>
            <input
              type="text"
              id="name"
              name="name"
              value={formData.name}
              onChange={handleChange}
              required
              placeholder="Enter item name"
              className="form-input"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="description" className="form-label">Description</label>
            <textarea
              id="description"
              name="description"
              value={formData.description}
              onChange={handleChange}
              rows="3"
              placeholder="Enter item description"
              className="form-input"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="imageUrl" className="form-label">Image URL</label>
            <input
              type="url"
              id="imageUrl"
              name="imageUrl"
              value={formData.imageUrl}
              onChange={handleChange}
              placeholder="https://example.com/image.jpg"
              className="form-input"
              disabled={loading}
            />
            {formData.imageUrl && (
              <div className="image-preview">
                <img 
                  src={formData.imageUrl} 
                  alt="Preview" 
                  style={{ maxWidth: '100px', maxHeight: '100px', marginTop: '10px', borderRadius: '4px' }}
                  onError={(e) => { e.target.style.display = 'none'; }}
                />
              </div>
            )}
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div className="form-group">
              <label htmlFor="priceCoins" className="form-label">Price (Coins)</label>
              <input
                type="number"
                id="priceCoins"
                name="priceCoins"
                value={formData.priceCoins}
                onChange={handleChange}
                min="0"
                placeholder="0"
                className="form-input"
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label htmlFor="priceKb" className="form-label">Price (Knowledge Points)</label>
              <input
                type="number"
                id="priceKb"
                name="priceKb"
                value={formData.priceKb}
                onChange={handleChange}
                min="0"
                placeholder="0"
                className="form-input"
                disabled={loading}
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="category" className="form-label">Category</label>
            <select
              id="category"
              name="category"
              value={formData.category}
              onChange={handleChange}
              className="form-select"
              disabled={loading}
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
              <label htmlFor="whisperType" className="form-label">Whisper Type</label>
              <select
                id="whisperType"
                name="whisperType"
                value={formData.whisperType}
                onChange={handleChange}
                className="form-select"
                disabled={loading}
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

          <div className="form-group">
            <label className="form-label">
              <input
                type="checkbox"
                name="available"
                checked={formData.available}
                onChange={handleChange}
                disabled={loading}
                style={{ marginRight: '0.5rem' }}
              />
              Available for purchase
            </label>
          </div>

          <div className="form-actions">
            <button 
              type="submit" 
              className="form-button"
              disabled={loading}
            >
              {loading ? 'Creating...' : 'Create Store Item'}
            </button>
            
            <button 
              type="button" 
              className="form-button cancel-button"
              onClick={() => onItemCreated && onItemCreated('view')}
              disabled={loading}
            >
              View Store Items
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default StoreItemForm;

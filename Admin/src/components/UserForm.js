import React, { useState } from 'react';
import { createUserWithEmailAndPassword } from 'firebase/auth';
import { doc, setDoc, serverTimestamp } from 'firebase/firestore';
import { auth, db } from '../firebase';

// Utility function to generate random password
const generateRandomPassword = (length = 12) => {
  const charset = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*';
  let password = '';
  for (let i = 0; i < length; i++) {
    password += charset.charAt(Math.floor(Math.random() * charset.length));
  }
  return password;
};

function UserForm() {
  const [formData, setFormData] = useState({
    email: '',
    name: '',
    residenceHall: '',
    password: ''
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
      const { email, name, residenceHall, password: providedPassword } = formData;

      // Validate and use provided password or generate random one
      let password;
      if (providedPassword && providedPassword.trim().length >= 6) {
        password = providedPassword.trim();
      } else {
        password = generateRandomPassword();
      }

      // Create Firebase Auth user
      const userCredential = await createUserWithEmailAndPassword(auth, email, password);
      const user = userCredential.user;

      // Save additional data to Firestore
      await setDoc(doc(db, 'users', user.uid), {
        id: user.uid,
        name,
        email,
        residenceHall,
        createdAt: serverTimestamp()
      });

      setSuccess({
        email,
        password,
        name,
        residenceHall
      });

      // Reset form
      setFormData({
        email: '',
        name: '',
        residenceHall: '',
        password: ''
      });

    } catch (error) {
      console.error('Error creating user:', error);
      setError(error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container">
      <h2>Create New User</h2>
      
      {error && (
        <div className="alert alert-error">
          Error: {error}
        </div>
      )}

      {success && (
        <div className="alert alert-success">
          <h3>User Created Successfully!</h3>
          <p><strong>Name:</strong> {success.name}</p>
          <p><strong>Email:</strong> {success.email}</p>
          <p><strong>Residence Hall:</strong> {success.residenceHall}</p>
          <p><strong>Generated Password:</strong> {success.password}</p>
          <p><em>Please save this password as it cannot be retrieved later.</em></p>
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="email">Email Address:</label>
          <input
            type="email"
            id="email"
            name="email"
            value={formData.email}
            onChange={handleChange}
            required
            disabled={loading}
          />
        </div>

        <div className="form-group">
          <label htmlFor="name">Full Name:</label>
          <input
            type="text"
            id="name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            required
            disabled={loading}
          />
        </div>

        <div className="form-group">
          <label htmlFor="residenceHall">Residence Hall:</label>
          <select
            id="residenceHall"
            name="residenceHall"
            value={formData.residenceHall}
            onChange={handleChange}
            required
            disabled={loading}
          >
            <option value="">Select Residence Hall</option>
            <option value="Rienow">Rienow</option>
            <option value="Slater">Slater</option>
            <option value="Stanley">Stanley</option>
            <option value="Burge">Burge</option>
            <option value="Currier">Currier</option>
            <option value="Daum">Daum</option>
            <option value="Catlett">Catlett</option>
            <option value="Hillcrest">Hillcrest</option>
            <option value="Mayflower">Mayflower</option>
            <option value="Parklawn">Parklawn</option>
          </select>
        </div>

        <div className="form-group">
          <label htmlFor="password">
            Password (optional - leave blank for auto-generated):
          </label>
          <input
            type="password"
            id="password"
            name="password"
            value={formData.password}
            onChange={handleChange}
            placeholder="Min 6 characters, or leave blank for auto-generated"
            disabled={loading}
          />
        </div>

        <button 
          type="submit" 
          className="btn" 
          disabled={loading}
        >
          {loading ? 'Creating User...' : 'Create User'}
        </button>
      </form>
    </div>
  );
}

export default UserForm;

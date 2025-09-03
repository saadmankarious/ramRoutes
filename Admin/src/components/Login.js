import React, { useState } from 'react';
import { signInWithEmailAndPassword } from 'firebase/auth';
import { doc, getDoc } from 'firebase/firestore';
import { auth, db } from '../firebase';

function Login({ onLogin }) {
  const [credentials, setCredentials] = useState({
    username: '',
    password: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handleChange = (e) => {
    setCredentials({
      ...credentials,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const { username, password } = credentials;

      // Convert username to email format if it doesn't contain @
      const email = username.includes('@') ? username : `${username}@admin.local`;
      
      console.log('Attempting Firebase login for:', email);
      const userCredential = await signInWithEmailAndPassword(auth, email, password);
      const user = userCredential.user;

      // Check if this is the super admin account
      if (user.email === 'root@ramroutes.com') {
        console.log('Super admin login successful');
        onLogin({
          uid: user.uid,
          email: user.email,
          role: 'superadmin',
          name: 'Super Administrator'
        });
        return;
      }

      // For regular admin users, get role from Firestore
      console.log('Getting admin user data from Firestore...');
      const userDoc = await getDoc(doc(db, 'admins', user.uid));
      
      if (!userDoc.exists()) {
        throw new Error('User not found in admin database. Only authorized administrators can access this panel.');
      }

      const userData = userDoc.data();
      console.log('Admin user data:', userData);

      onLogin({
        uid: user.uid,
        email: user.email,
        role: userData.role || 'admin',
        name: userData.name
      });

    } catch (error) {
      console.error('Login error:', error);
      
      let errorMessage = 'Login failed. ';
      if (error.code === 'auth/user-not-found') {
        errorMessage += 'User not found.';
      } else if (error.code === 'auth/wrong-password') {
        errorMessage += 'Invalid password.';
      } else if (error.code === 'auth/invalid-email') {
        errorMessage += 'Invalid email format.';
      } else if (error.code === 'auth/too-many-requests') {
        errorMessage += 'Too many failed attempts. Please try again later.';
      } else {
        errorMessage += error.message;
      }
      
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <div className="login-card">
        <div className="login-header">
          <h1 className="login-title">Ram Routes Admin</h1>
          <p className="login-subtitle">Sign in to manage building events</p>
        </div>

        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label htmlFor="username" className="form-label">Username / Email</label>
            <input
              type="text"
              id="username"
              name="username"
              value={credentials.username}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter username or email"
              className="form-input"
              autoComplete="username"
            />
          </div>

          <div className="form-group">
            <label htmlFor="password" className="form-label">Password</label>
            <input
              type="password"
              id="password"
              name="password"
              value={credentials.password}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter password"
              className="form-input"
              autoComplete="current-password"
            />
          </div>

          <button 
            type="submit" 
            className={`form-button login-button ${loading ? 'loading' : ''}`}
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner"></span>
                Signing In...
              </>
            ) : (
              'Sign In'
            )}
          </button>
        </form>

        <div className="login-footer">
          <p className="help-text">
            Super Admin: Use root@ramroutes.com<br />
            Admins: Use your assigned credentials
          </p>
        </div>
      </div>
    </div>
  );
}

export default Login;

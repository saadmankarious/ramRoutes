import React, { useState, useEffect } from 'react';
import { createUserWithEmailAndPassword } from 'firebase/auth';
import { doc, setDoc, serverTimestamp, collection, getDocs, query, orderBy } from 'firebase/firestore';
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

function AdminForm() {
  const [formData, setFormData] = useState({
    username: '',
    name: '',
    password: '',
    schoolId: ''
  });
  const [schools, setSchools] = useState([]);
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchSchools = async () => {
      try {
        const snapshot = await getDocs(query(collection(db, 'schools'), orderBy('createdAt', 'desc')));
        const list = [];
        snapshot.forEach((doc) => list.push({ id: doc.id, ...doc.data() }));
        setSchools(list);
      } catch (err) {
        console.error('Error fetching schools:', err);
      }
    };
    fetchSchools();
  }, []);

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
      const { username, name, password: providedPassword, schoolId } = formData;

      // Validate and use provided password or generate random one
      let password;
      if (providedPassword && providedPassword.trim().length >= 6) {
        password = providedPassword.trim();
      } else {
        password = generateRandomPassword();
      }

      // Find selected school name
      const selectedSchool = schools.find(s => s.schoolId === schoolId);
      const schoolName = selectedSchool ? selectedSchool.schoolName : '';

      // Create email format from username
      const email = `${username}@admin.local`;

      console.log('Creating admin user:', { email, name, username });

      // Create Firebase Auth user
      const userCredential = await createUserWithEmailAndPassword(auth, email, password);
      const user = userCredential.user;

      // Save admin data to Firestore in 'admins' collection
      await setDoc(doc(db, 'admins', user.uid), {
        id: user.uid,
        username,
        name,
        email,
        role: 'admin',
        schoolId,
        schoolName,
        createdAt: serverTimestamp(),
        createdBy: 'superadmin'
      });

      console.log('Admin user created successfully');

      setSuccess({
        username,
        email,
        password,
        name,
        schoolName
      });

      // Reset form
      setFormData({
        username: '',
        name: '',
        password: '',
        schoolId: ''
      });

    } catch (error) {
      console.error('Error creating admin:', error);
      
      let errorMessage = 'Failed to create admin. ';
      if (error.code === 'auth/email-already-in-use') {
        errorMessage += 'Username already exists.';
      } else if (error.code === 'auth/weak-password') {
        errorMessage += 'Password is too weak.';
      } else {
        errorMessage += error.message;
      }
      
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-container">
      <div className="form-card">
        <h2 className="form-title">Create Admin User</h2>
        <p className="form-description">Create a new administrator who can manage building events</p>
        
        {error && (
          <div className="alert alert-error">
            <strong>Error:</strong> {error}
          </div>
        )}

        {success && (
          <div className="alert alert-success">
            <h3 style={{ margin: '0 0 10px 0' }}>Admin Created Successfully!</h3>
            <div className="success-details">
              <p><strong>Name:</strong> {success.name}</p>
              <p><strong>Username:</strong> {success.username}</p>
              <p><strong>Email:</strong> {success.email}</p>
              <p><strong>School:</strong> {success.schoolName}</p>
              <p><strong>Generated Password:</strong> <span className="password-text">{success.password}</span></p>
              <p className="warning-text">⚠️ Please save these credentials as the password cannot be retrieved later.</p>
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit} className="admin-form">
          <div className="form-group">
            <label htmlFor="username" className="form-label">Username</label>
            <input
              type="text"
              id="username"
              name="username"
              value={formData.username}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter username (no spaces)"
              className="form-input"
              pattern="[a-zA-Z0-9_-]+"
              title="Username can only contain letters, numbers, hyphens, and underscores"
            />
            <small className="form-help">This will be used as {formData.username}@admin.local</small>
          </div>

          <div className="form-group">
            <label htmlFor="name" className="form-label">Full Name</label>
            <input
              type="text"
              id="name"
              name="name"
              value={formData.name}
              onChange={handleChange}
              required
              disabled={loading}
              placeholder="Enter full name"
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label htmlFor="password" className="form-label">
              Password <span className="optional">(optional)</span>
            </label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              placeholder="Leave blank for auto-generated password"
              disabled={loading}
              className="form-input"
              minLength="6"
            />
            <small className="form-help">Minimum 6 characters, or leave blank for auto-generated secure password</small>
          </div>

          <div className="form-group">
            <label htmlFor="schoolId" className="form-label">School</label>
            <select
              id="schoolId"
              name="schoolId"
              value={formData.schoolId}
              onChange={handleChange}
              required
              disabled={loading}
              className="form-select"
            >
              <option value="">Select a school</option>
              {schools.map((school) => (
                <option key={school.id} value={school.schoolId}>
                  {school.schoolName}
                </option>
              ))}
            </select>
          </div>

          <button 
            type="submit" 
            className={`form-button ${loading ? 'loading' : ''}`}
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner"></span>
                Creating Admin...
              </>
            ) : (
              'Create Admin User'
            )}
          </button>
        </form>
      </div>
    </div>
  );
}

export default AdminForm;

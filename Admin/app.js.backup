const express = require('express');
const bodyParser = require('body-parser');
const admin = require('firebase-admin');
const crypto = require('crypto');

// Initialize Express app
const app = express();
app.use(bodyParser.urlencoded({ extended: true }));
app.set('view engine', 'ejs');

// Initialize Firebase Admin SDK
const serviceAccount = require('./serviceAccountKey.json'); // Download from Firebase Console

admin.initializeApp({
  credential: admin.credential.cert(serviceAccount),
  databaseURL: "https://YOUR_PROJECT_ID.firebaseio.com"
});

const db = admin.firestore();
const auth = admin.auth();

// Generate random password
function generateRandomPassword(length = 12) {
  return crypto.randomBytes(length).toString('hex').slice(0, length);
}

// Generate random ID for building events
function generateBuildingEventId(length = 8) {
    return 'BE' + crypto.randomBytes(length).toString('hex').slice(0, length);
}

// Routes
app.get('/', (req, res) => {
  res.render('form');
});

app.post('/create-user', async (req, res) => {
  try {    const { email, name, residenceHall, password: providedPassword } = req.body;
    
    // Validate and use provided password or generate random one
    let password;
    if (providedPassword && providedPassword.trim().length >= 6) {
      password = providedPassword.trim();
    } else {
      password = generateRandomPassword();
    }
    
    // Create Firebase Auth user
    const userRecord = await auth.createUser({
      email,
      password,
      emailVerified: false,
      disabled: false
    });
      // Save additional data to Firestore
    await db.collection('users').doc(userRecord.uid).set({
      id: userRecord.uid,
      name,
      email,
      residenceHall,
      createdAt: admin.firestore.FieldValue.serverTimestamp()
    });
    
    // Show success page with generated password
    res.render('success', { 
      email, 
      password,
      name,
      residenceHall
    });
    
  } catch (error) {
    console.error('Error creating user:', error);
    res.render('error', { error: error.message });
  }
});

// Building Event Routes
app.get('/building-event', (req, res) => {
    const buildingId = generateBuildingEventId();
    res.render('building-event-form', { buildingId });
});

app.post('/create-building-event', async (req, res) => {
    try {
        const { buildingId, buildingName, eventName, date } = req.body;
        
        // Create building event document in Firestore
        await db.collection('building-events').add({
            buildingId,
            buildingName,
            eventName,
            date: new Date(date), // Convert string to Date object
            createdAt: admin.firestore.FieldValue.serverTimestamp()
        });
        
        res.send('Building event created successfully!');
    } catch (error) {
        console.error('Error creating building event:', error);
        res.status(500).send('Error creating building event: ' + error.message);
    }
});

// Start server
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Server running on http://localhost:${PORT}`);
});
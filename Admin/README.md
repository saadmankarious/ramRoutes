# RamRoutes Admin Panel - React App

This is a React-based admin panel for managing users and building events in the RamRoutes application.

## Features

- Create new users with Firebase Authentication
- Store user data in Firestore
- Create building events with auto-generated IDs
- Modern React UI with routing
- Form validation and error handling

## Setup Instructions

### 1. Install Dependencies

```bash
npm install
```

### 2. Configure Firebase

1. Go to your Firebase Console (https://console.firebase.google.com/)
2. Select your RamRoutes project
3. Go to Project Settings > General
4. Scroll down to "Your apps" section
5. If you don't have a web app, click "Add app" and select Web
6. Copy your Firebase configuration object

### 3. Update Firebase Configuration

Edit `src/firebase.js` and replace the placeholder values with your actual Firebase configuration:

```javascript
const firebaseConfig = {
  apiKey: "your-actual-api-key",
  authDomain: "your-project-id.firebaseapp.com",
  projectId: "your-actual-project-id",
  storageBucket: "your-project-id.appspot.com",
  messagingSenderId: "your-actual-sender-id",
  appId: "your-actual-app-id"
};
```

### 4. Firebase Security Rules

Make sure your Firestore has appropriate security rules. For development, you can use:

```javascript
// Firestore Security Rules
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Allow read/write access on all documents to any user signed in to the application
    match /{document=**} {
      allow read, write: if request.auth != null;
    }
  }
}
```

For production, implement more restrictive rules based on your needs.

### 5. Firebase Authentication Setup

1. In Firebase Console, go to Authentication > Sign-in method
2. Enable "Email/Password" provider
3. Optionally enable "Email link (passwordless sign-in)" if needed

## Running the Application

### Development Server

```bash
npm start
```

This runs the app in development mode. Open [http://localhost:3000](http://localhost:3000) to view it in your browser.

The page will reload when you make changes. You may also see any lint errors in the console.

### Build for Production

```bash
npm run build
```

Builds the app for production to the `build` folder. It correctly bundles React in production mode and optimizes the build for the best performance.

## Usage

### Creating Users

1. Navigate to the "Create User" tab
2. Fill in the required fields:
   - Email Address
   - Full Name
   - Residence Hall
   - Password (optional - auto-generated if left blank)
3. Click "Create User"
4. The generated password will be displayed - make sure to save it!

### Creating Building Events

1. Navigate to the "Create Building Event" tab
2. Fill in the required fields:
   - Building ID (auto-generated, can be regenerated)
   - Building Name (select from dropdown)
   - Event Name
   - Event Date
3. Click "Create Building Event"

## File Structure

```
src/
  components/
    UserForm.js          # User creation form
    BuildingEventForm.js # Building event creation form
  App.js                 # Main app component with routing
  firebase.js            # Firebase configuration
  index.js              # App entry point
  index.css             # Global styles
  App.css               # App-specific styles

public/
  index.html            # HTML template

package.json            # Dependencies and scripts
```

## Migration Notes

This React app replaces the previous Express.js server-based admin panel. Key changes:

- **Client-side**: Now runs entirely in the browser
- **Firebase SDK**: Uses the web SDK instead of the admin SDK
- **Authentication**: Uses Firebase Auth for user creation
- **UI**: Modern React-based interface with routing
- **Real-time**: Can be extended with real-time Firestore listeners

## Troubleshooting

### Common Issues

1. **Firebase configuration errors**: Make sure all config values are correct
2. **Authentication errors**: Ensure Email/Password is enabled in Firebase Console
3. **Permission errors**: Check Firestore security rules
4. **Build errors**: Make sure all dependencies are installed with `npm install`

### Development Tips

- Use browser developer tools to debug Firebase operations
- Check the Firebase Console for created users and documents
- Monitor the browser console for any JavaScript errors

## Next Steps

Potential enhancements:
- Add user listing and management
- Add building event listing and editing
- Implement user authentication for admin panel access
- Add data validation and better error handling
- Add export functionality for user lists

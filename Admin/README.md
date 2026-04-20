# RamRoutes Admin Panel - React App

This is a React-based admin panel for managing users and building events in the RamRoutes application with role-based authentication.

## Features

- **Role-based Authentication**: Super Admin and Admin roles with different permissions
- **Super Admin Features**: Can create admin users
- **Admin Features**: Can create building events  
- **Firebase Integration**: Uses Firestore for data storage and Firebase Auth for authentication
- **Modern UI**: Clean, responsive design with form validation
- **Auto-generated IDs**: Building events use Firebase auto-generated document IDs

## Authentication System

### User Roles

1. **Super Admin**
   - Fixed credentials: `root` / `ramroutes2025ll`
   - Can create admin users
   - Can create building events
   - Full system access

2. **Admin**
   - Created by Super Admin
   - Can create building events
   - Limited to event management

### Login Process

- Landing page requires authentication
- Super Admin uses fixed credentials
- Admin users use credentials provided by Super Admin

## Setup Instructions

### 1. Install Dependencies

```bash
npm install
```

### 2. Firebase Configuration

The app is already configured with your Firebase project:
- **Project ID**: `trials-of-venus`
- **API Key**: Already configured
- **Authentication**: Email/Password enabled

### 3. Firebase Security Rules

Ensure your Firestore has appropriate security rules:

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Allow authenticated users to read/write
    match /{document=**} {
      allow read, write: if request.auth != null;
    }
    
    // Admin-specific collection
    match /admins/{adminId} {
      allow read, write: if request.auth != null;
    }
    
    // Building events collection
    match /building-events/{eventId} {
      allow read, write: if request.auth != null;
    }
  }
}
```

## Running the Application

### Development Server

```bash
npm start
```

Open [http://localhost:3000](http://localhost:3000) to view the application.

### Build for Production

```bash
npm run build
```

## Usage Guide

### First-Time Setup

1. **Super Admin Login**: Use `root` / `ramroutes2025ll`
2. **Create Admin Users**: Navigate to "Manage Admins" 
3. **Provide Credentials**: Share generated credentials with admin users

### Creating Admin Users (Super Admin Only)

1. Click "Manage Admins" in the navigation
2. Fill in the admin details:
   - **Username**: Will become `username@admin.local`
   - **Full Name**: Display name for the admin
   - **Password**: Optional (auto-generated if blank)
3. Save the generated credentials securely
4. Share credentials with the new admin

### Creating Building Events

1. Click "Building Events" in the navigation
2. Fill in the event details:
   - **Building Name**: Select from available options (McWethy, TC, Ebersole, SAW, Library, PR, Stoner)
   - **Event Name**: Descriptive name for the event
   - **Event Type**: 
     - **Scheduled Event**: Requires specific date/time
     - **Always Happening**: No date required, perpetual event
3. Click "Create Building Event"
4. Firebase will auto-generate a unique document ID

## File Structure

```
src/
  components/
    Login.js              # Authentication component
    Header.js             # Navigation and user info
    AdminForm.js          # Create admin users (Super Admin only)
    BuildingEventForm.js  # Create building events
  App.js                  # Main app with auth state management
  firebase.js             # Firebase configuration
  App.css                 # Comprehensive styling
  index.js               # App entry point

Firebase Collections:
  admins/                 # Admin user data
  building-events/        # Event data with auto-generated IDs
```

## Security Features

- **Authentication Required**: All functionality requires login
- **Role-based Access**: Different permissions for Super Admin vs Admin
- **Session Management**: Automatic logout handling
- **Firebase Auth**: Secure user authentication
- **Input Validation**: Form validation and error handling

## UI/UX Improvements

- **Modern Design**: Clean, professional interface
- **Responsive Layout**: Works on desktop and mobile
- **Loading States**: Visual feedback during operations
- **Error Handling**: Clear error messages
- **Success Feedback**: Confirmation of successful operations
- **Intuitive Navigation**: Role-appropriate menu options

## Migration from Express.js

This replaces the previous server-based admin panel with:
- **Client-side Architecture**: Runs entirely in browser
- **Firebase Web SDK**: Instead of admin SDK
- **Role-based Security**: Multi-tiered access control
- **Modern React UI**: Better user experience
- **Auto-generated IDs**: Simplified ID management

## Troubleshooting

### Login Issues
- Super Admin: Ensure exact credentials `root` / `ramroutes2025ll`
- Admin: Verify credentials were created correctly
- Check Firebase Console for authentication logs

### Permission Errors
- Verify Firestore security rules allow authenticated access
- Check user exists in `admins` collection
- Ensure proper role assignment

### Firebase Connection
- Check browser console for Firebase errors
- Verify internet connection
- Check Firebase project status

## Future Enhancements

- **User Management**: List and edit existing admin users
- **Event Management**: View and edit existing building events
- **Audit Logging**: Track admin actions
- **Bulk Operations**: Import/export functionality
- **Real-time Updates**: Live data synchronization

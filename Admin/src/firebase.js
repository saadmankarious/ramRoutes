// Import the functions you need from the SDKs you need
import { initializeApp } from 'firebase/app';
import { getAuth } from 'firebase/auth';
import { getFirestore } from 'firebase/firestore';

// Firebase configuration extracted from google-services.json
const firebaseConfig = {
  apiKey: "AIzaSyBoyfyjD7mnV5Ne27N4_T0LSUFC13muuWI",
  authDomain: "trials-of-venus.firebaseapp.com",
  projectId: "trials-of-venus",
  storageBucket: "trials-of-venus.firebasestorage.app",
  messagingSenderId: "460631177881",
  appId: "1:460631177881:web:app-id" // Web app ID would be different but this structure works
};

console.log('Initializing Firebase with project:', firebaseConfig.projectId);

// Initialize Firebase
const app = initializeApp(firebaseConfig);

// Initialize Firebase Authentication and get a reference to the service
export const auth = getAuth(app);

// Initialize Cloud Firestore and get a reference to the service  
export const db = getFirestore(app);

console.log('Firebase initialized successfully');

export default app;

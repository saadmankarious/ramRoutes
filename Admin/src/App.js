import React, { useState, useEffect } from 'react';
import { onAuthStateChanged } from 'firebase/auth';
import { doc, getDoc } from 'firebase/firestore';
import { auth, db } from './firebase';
import Login from './components/Login';
import Header from './components/Header';
import AdminForm from './components/AdminForm';
import AdminList from './components/AdminList';
import AdminEdit from './components/AdminEdit';
import BuildingEventForm from './components/BuildingEventForm';
import EventList from './components/EventList';
import EventEdit from './components/EventEdit';
import StoreItemForm from './components/StoreItemForm';
import StoreItemList from './components/StoreItemList';
import StoreItemEdit from './components/StoreItemEdit';
import SchoolForm from './components/SchoolForm';
import SchoolList from './components/SchoolList';
import SchoolEdit from './components/SchoolEdit';
import BuildingForm from './components/BuildingForm';
import BuildingList from './components/BuildingList';
import BuildingEdit from './components/BuildingEdit';
import './App.css';

function App() {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [currentView, setCurrentView] = useState('events');
  const [editingEvent, setEditingEvent] = useState(null);
  const [editingAdmin, setEditingAdmin] = useState(null);
  const [editingStoreItem, setEditingStoreItem] = useState(null);
  const [editingSchool, setEditingSchool] = useState(null);
  const [editingBuilding, setEditingBuilding] = useState(null);

  useEffect(() => {
    console.log('Setting up auth state listener...');
    
    const unsubscribe = onAuthStateChanged(auth, async (firebaseUser) => {
      try {
        if (firebaseUser) {
          console.log('Firebase user found:', firebaseUser.email);
          
          // Check if this is the super admin account
          if (firebaseUser.email === 'root@ramroutes.com') {
            console.log('Super admin authenticated');
            setUser({
              uid: firebaseUser.uid,
              email: firebaseUser.email,
              role: 'superadmin',
              name: 'Super Administrator'
            });
          } else {
            // For regular admin users, get role from Firestore
            const userDoc = await getDoc(doc(db, 'admins', firebaseUser.uid));
            
            if (userDoc.exists()) {
              const userData = userDoc.data();
              setUser({
                uid: firebaseUser.uid,
                email: firebaseUser.email,
                role: userData.role || 'admin',
                name: userData.name,
                schoolId: userData.schoolId || '',
                schoolName: userData.schoolName || ''
              });
              console.log('Admin user authenticated with role:', userData.role);
            } else {
              console.log('User not found in admin database, logging out...');
              setUser(null);
            }
          }
        } else {
          console.log('No Firebase user found');
          setUser(null);
        }
      } catch (error) {
        console.error('Error checking user auth:', error);
        setUser(null);
      } finally {
        setLoading(false);
      }
    });

    return () => unsubscribe();
  }, []);

  const handleLogin = (userData) => {
    console.log('User logged in:', userData);
    setUser(userData);
    
    // Set default view based on role
    if (userData.role === 'superadmin') {
      setCurrentView('view-admins');
    } else {
      setCurrentView('events');
    }
  };

  const handleLogout = () => {
    console.log('User logged out');
    setUser(null);
    setCurrentView('events');
    setEditingEvent(null);
    setEditingAdmin(null);
    setEditingStoreItem(null);
    setEditingSchool(null);
    setEditingBuilding(null);
  };

  const handleEditAdmin = (admin) => {
    console.log('Editing admin:', admin);
    setEditingAdmin(admin);
    setCurrentView('edit-admin');
  };

  const handleSaveAdmin = () => {
    console.log('Admin saved');
    setEditingAdmin(null);
    setCurrentView('view-admins');
  };

  const handleCancelAdminEdit = () => {
    console.log('Admin edit cancelled');
    setEditingAdmin(null);
    setCurrentView('view-admins');
  };

  const handleEditEvent = (event) => {
    console.log('Editing event:', event);
    setEditingEvent(event);
    setCurrentView('edit-event');
  };

  const handleSaveEvent = (updatedEvent) => {
    console.log('Event saved:', updatedEvent);
    setEditingEvent(null);
    setCurrentView('view-events');
  };

  const handleCancelEdit = () => {
    console.log('Edit cancelled');
    setEditingEvent(null);
    setCurrentView('view-events');
  };

  const handleEventCreated = (eventId) => {
    console.log('Event created, navigating to events list:', eventId);
    if (eventId === 'view') {
      setCurrentView('view-events');
    }
  };

  const handleEditStoreItem = (item) => {
    console.log('Editing store item:', item);
    setEditingStoreItem(item);
    setCurrentView('edit-store-item');
  };

  const handleSaveStoreItem = (updatedItem) => {
    console.log('Store item saved:', updatedItem);
    setEditingStoreItem(null);
    setCurrentView('view-store-items');
  };

  const handleCancelStoreItemEdit = () => {
    console.log('Store item edit cancelled');
    setEditingStoreItem(null);
    setCurrentView('view-store-items');
  };

  const handleStoreItemCreated = (itemId) => {
    console.log('Store item created, navigating to items list:', itemId);
    if (itemId === 'view') {
      setCurrentView('view-store-items');
    }
  };

  const handleEditSchool = (school) => {
    setEditingSchool(school);
    setCurrentView('edit-school');
  };

  const handleSaveSchool = () => {
    setEditingSchool(null);
    setCurrentView('view-schools');
  };

  const handleCancelSchoolEdit = () => {
    setEditingSchool(null);
    setCurrentView('view-schools');
  };

  const handleSchoolCreated = (schoolId) => {
    if (schoolId === 'view') {
      setCurrentView('view-schools');
    }
  };

  const handleEditBuilding = (building) => {
    setEditingBuilding(building);
    setCurrentView('edit-building');
  };

  const handleSaveBuilding = () => {
    setEditingBuilding(null);
    setCurrentView('view-buildings');
  };

  const handleCancelBuildingEdit = () => {
    setEditingBuilding(null);
    setCurrentView('view-buildings');
  };

  const handleBuildingCreated = (buildingId) => {
    if (buildingId === 'view') {
      setCurrentView('view-buildings');
    }
  };

  if (loading) {
    return (
      <div className="loading-container">
        <div className="loading-spinner">
          <span className="spinner"></span>
          <p>Loading...</p>
        </div>
      </div>
    );
  }

  if (!user) {
    return <Login onLogin={handleLogin} />;
  }

  const renderCurrentView = () => {
    switch (currentView) {
      case 'admins':
        if (user.role === 'superadmin') {
          return <AdminForm />;
        }
        return <div className="access-denied">Access denied. Super admin role required.</div>;
      
      case 'view-admins':
        if (user.role === 'superadmin') {
          return <AdminList user={user} onEditAdmin={handleEditAdmin} />;
        }
        return <div className="access-denied">Access denied. Super admin role required.</div>;
      
      case 'edit-admin':
        if (user.role === 'superadmin' && editingAdmin) {
          return (
            <AdminEdit 
              adminToEdit={editingAdmin}
              onCancel={handleCancelAdminEdit}
              onSave={handleSaveAdmin}
            />
          );
        }
        return <div className="access-denied">Access denied or no admin selected for editing.</div>;
      
      case 'events':
        return <BuildingEventForm user={user} onEventCreated={handleEventCreated} />;
      
      case 'view-events':
        return <EventList user={user} onEditEvent={handleEditEvent} />;
      
      case 'edit-event':
        if (editingEvent) {
          return (
            <EventEdit 
              event={editingEvent}
              onCancel={handleCancelEdit}
              onSave={handleSaveEvent}
            />
          );
        }
        return <div className="access-denied">No event selected for editing.</div>;
      
      case 'store-items':
        if (user.role === 'superadmin') {
          return <StoreItemForm user={user} onItemCreated={handleStoreItemCreated} />;
        }
        return <div className="access-denied">Access denied. Super admin role required.</div>;
      
      case 'view-store-items':
        if (user.role === 'superadmin') {
          return <StoreItemList user={user} onEditItem={handleEditStoreItem} />;
        }
        return <div className="access-denied">Access denied. Super admin role required.</div>;
      
      case 'edit-store-item':
        if (user.role === 'superadmin' && editingStoreItem) {
          return (
            <StoreItemEdit 
              item={editingStoreItem}
              onCancel={handleCancelStoreItemEdit}
              onSave={handleSaveStoreItem}
            />
          );
        }
        return <div className="access-denied">Access denied or no store item selected for editing.</div>;
      
      case 'schools':
        if (user.role === 'superadmin') {
          return <SchoolForm onSchoolCreated={handleSchoolCreated} />;
        }
        return <div className="access-denied">Access denied. Super admin role required.</div>;
      
      case 'view-schools':
        if (user.role === 'superadmin') {
          return <SchoolList onEditSchool={handleEditSchool} />;
        }
        return <div className="access-denied">Access denied. Super admin role required.</div>;
      
      case 'edit-school':
        if (user.role === 'superadmin' && editingSchool) {
          return (
            <SchoolEdit
              school={editingSchool}
              onCancel={handleCancelSchoolEdit}
              onSave={handleSaveSchool}
            />
          );
        }
        return <div className="access-denied">Access denied or no school selected for editing.</div>;
      
      case 'buildings':
        if (user.role === 'superadmin') {
          return <BuildingForm onBuildingCreated={handleBuildingCreated} />;
        }
        return <div className="access-denied">Access denied. Super admin role required.</div>;
      
      case 'view-buildings':
        if (user.role === 'superadmin') {
          return <BuildingList onEditBuilding={handleEditBuilding} />;
        }
        return <div className="access-denied">Access denied. Super admin role required.</div>;
      
      case 'edit-building':
        if (user.role === 'superadmin' && editingBuilding) {
          return (
            <BuildingEdit
              building={editingBuilding}
              onCancel={handleCancelBuildingEdit}
              onSave={handleSaveBuilding}
            />
          );
        }
        return <div className="access-denied">Access denied or no building selected for editing.</div>;
      
      default:
        return <BuildingEventForm user={user} onEventCreated={handleEventCreated} />;
    }
  };

  return (
    <div className="app">
      <Header 
        user={user} 
        currentView={currentView} 
        setCurrentView={setCurrentView}
        onLogout={handleLogout}
      />
      <main className="app-main">
        {renderCurrentView()}
      </main>
    </div>
  );
}

export default App;

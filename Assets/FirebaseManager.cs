using UnityEngine;
using Firebase;
using Firebase.Messaging;
using Firebase.Firestore;
using System.Threading.Tasks;
#if UNITY_ANDROID && UNITY_NOTIFICATIONS_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS && UNITY_NOTIFICATIONS_IOS
using Unity.Notifications.iOS;
#endif
public class FirebaseMessagingManager : MonoBehaviour
{
    private string _deviceToken;
    private bool _firebaseInitialized = false;
    private string _cachedDeviceId;
    private bool _topicsSubscribed = false;

    async void Start()
    {
        _cachedDeviceId = SystemInfo.deviceUniqueIdentifier;

        await InitializeFirebase();
        if (_firebaseInitialized)
        {
            SetupMessaging();
            // ...existing code...
        }
    }

    private async Task InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            _firebaseInitialized = true;
            Debug.Log("Firebase initialized successfully");
            
            // Initialize Firestore if needed
            await FirestoreUtility.Initialize();
            bool connectionSuccess = await FirestoreUtility.TestConnection();
            Debug.Log(connectionSuccess ? "Firestore connected" : "Firestore connection failed");
        }
        else
        {
            Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
        }
    }
    
    

    private void SetupMessaging()
    {
        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;

#if UNITY_IOS
        // iOS: request permission first to ensure APNS token, then request FCM token
        FirebaseMessaging.RequestPermissionAsync().ContinueWith(task =>
        {
            Debug.Log("Notification permission requested (iOS)");
            RequestToken();
        });
#else
        // Non-iOS: it's safe to request token immediately
        RequestToken();
        FirebaseMessaging.RequestPermissionAsync().ContinueWith(task =>
        {
            Debug.Log("Notification permission requested (non-iOS)");
        });
#endif
    }

    private async Task SubscribeToTopics()
    {
        if (_topicsSubscribed) return;
        try
        {
            await FirebaseMessaging.SubscribeAsync("/topics/general");  // Changed from "global" to "general"
            await FirebaseMessaging.SubscribeAsync("/topics/updates");
            _topicsSubscribed = true;
            Debug.Log("Subscribed to notification topics: general, updates");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Topic subscription failed: {e.Message}");
        }
    }

    private void RequestToken()
    {
        FirebaseMessaging.GetTokenAsync().ContinueWith(task => {
            if (task.IsCompletedSuccessfully && !string.IsNullOrEmpty(task.Result))
            {
                _deviceToken = task.Result;
                Debug.Log($"FCM Token (manual): {_deviceToken}");
                SendTokenToServer(_deviceToken);
                // Subscribe once we have a token
                _ = SubscribeToTopics();
            }
            else if (task.IsFaulted)
            {
                Debug.LogError($"Failed to get FCM token: {task.Exception}");
            }
            else
            {
                Debug.LogWarning("FCM token request completed but no token received");
            }
        });
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        _deviceToken = token.Token;
        Debug.Log($"FCM Token (auto): {_deviceToken}");
        SendTokenToServer(_deviceToken);
        // Subscribe once we have a token (idempotent via _topicsSubscribed)
        if (!_topicsSubscribed)
        {
            _ = SubscribeToTopics();
        }
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log($"Received message from: {e.Message.From}");
        
        // Handle notification data
        if (e.Message.Notification != null)
        {
            Debug.Log($"Title: {e.Message.Notification.Title}");
            Debug.Log($"Body: {e.Message.Notification.Body}");

            // Show notification in system tray (works in background/foreground)
            ShowSystemNotification(
                e.Message.Notification.Title, 
                e.Message.Notification.Body
            );
        }
        
        // Handle custom data payload
        foreach (var pair in e.Message.Data)
        {
            Debug.Log($"{pair.Key}: {pair.Value}");
        }
    }

    // Helper method to display notifications
    private void ShowSystemNotification(string title, string message)
    {
#if UNITY_ANDROID && UNITY_NOTIFICATIONS_ANDROID
        // Android Notification (New API)
        var androidNotification = new AndroidNotification
        {
            Title = title,
            Text = message,
            FireTime = System.DateTime.Now.AddSeconds(1),
            SmallIcon = "icon_small",  // Must match your .png name in Assets/Plugins/Android/res/drawable
            LargeIcon = "icon_large"    // Optional
        };

        AndroidNotificationCenter.SendNotification(androidNotification, "default_channel");

#elif UNITY_IOS && UNITY_NOTIFICATIONS_IOS
        // iOS Notification (New API)
        var iosNotification = new iOSNotification
        {
            Title = title,
            Body = message,
            ShowInForeground = true,    // Display even if app is open
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = System.TimeSpan.FromSeconds(1),
                Repeats = false
            }
        };
        
        iOSNotificationCenter.ScheduleNotification(iosNotification);
#else
        // Fallback for editor or unsupported platforms
        Debug.Log($"Local Notification: {title} - {message}");
#endif
    }

    private async void SendTokenToServer(string token)
    {
        if (string.IsNullOrEmpty(_cachedDeviceId)) 
        {
            Debug.LogError("Device ID not cached");
            return;
        }

        if (FirebaseFirestore.DefaultInstance != null)
        {
            try
            {
                DocumentReference docRef = FirebaseFirestore.DefaultInstance
                    .Collection("devices")
                    .Document(_cachedDeviceId);  // Use the cached version here

                await docRef.SetAsync(new
                {
                    token = token,
                    lastUpdated = FieldValue.ServerTimestamp,
                    platform = Application.platform.ToString()
                });

                Debug.Log("Device token saved to Firestore");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save token: {e.Message}");
            }
        }
    }

    void OnEnable()
    {
        if (_firebaseInitialized)
        {
            FirebaseMessaging.TokenReceived += OnTokenReceived;
            FirebaseMessaging.MessageReceived += OnMessageReceived;
        }
    }

    void OnDisable()
    {
        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
    }
}
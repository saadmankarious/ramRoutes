using UnityEngine;
using Firebase;
using Firebase.Messaging;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections.Generic;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

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
#elif UNITY_ANDROID && !UNITY_EDITOR
        // Android: Check and request notification permissions first
        RequestAndroidNotificationPermissions().ContinueWith(task =>
        {
            RequestToken();
            FirebaseMessaging.RequestPermissionAsync().ContinueWith(permissionTask =>
            {
                Debug.Log("Firebase notification permission requested (Android)");
            });
        });
#else
        // Other platforms or Editor: request token immediately
        RequestToken();
        FirebaseMessaging.RequestPermissionAsync().ContinueWith(task =>
        {
            Debug.Log("Notification permission requested");
        });
#endif
    }

    private async Task RequestAndroidNotificationPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // For Android 13 (API level 33) and above, we need explicit permission
            string notificationPermission = "android.permission.POST_NOTIFICATIONS";
            
            if (!Permission.HasUserAuthorizedPermission(notificationPermission))
            {
                Debug.Log("Requesting notification permission for Android 13+");
                
                Permission.RequestUserPermission(notificationPermission);
                
                // Wait for user response (up to 10 seconds)
                int attempts = 0;
                while (!Permission.HasUserAuthorizedPermission(notificationPermission) && attempts < 100)
                {
                    await Task.Delay(100);
                    attempts++;
                }
                
                if (Permission.HasUserAuthorizedPermission(notificationPermission))
                {
                    Debug.Log("Android notification permission granted");
                }
                else
                {
                    Debug.LogWarning("Android notification permission denied. Notifications may not work properly.");
                }
            }
            else
            {
                Debug.Log("Android notification permission already granted");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error requesting Android notification permissions: {e.Message}");
        }
#else
        await Task.CompletedTask;
        Debug.Log("Not on Android platform, notification permissions handled automatically");
#endif
    }

    public bool AreNotificationsEnabled()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string notificationPermission = "android.permission.POST_NOTIFICATIONS";
        return Permission.HasUserAuthorizedPermission(notificationPermission);
#else
        return true; // On other platforms, assume notifications work
#endif
    }

    public void OpenNotificationSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Open the app's notification settings
            using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", "android.settings.APP_NOTIFICATION_SETTINGS");
                intent.Call<AndroidJavaObject>("putExtra", "android.provider.extra.APP_PACKAGE", 
                    currentActivity.Call<string>("getPackageName"));
                
                currentActivity.Call("startActivity", intent);
            }
            Debug.Log("Opened notification settings");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open notification settings: {e.Message}");
            
            // Fallback: Open general app settings
            try
            {
                using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                {
                    var intent = new AndroidJavaObject("android.content.Intent");
                    intent.Call<AndroidJavaObject>("setAction", "android.settings.APPLICATION_DETAILS_SETTINGS");
                    
                    string packageName = currentActivity.Call<string>("getPackageName");
                    var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + packageName);
                    intent.Call<AndroidJavaObject>("setData", uri);
                    
                    currentActivity.Call("startActivity", intent);
                }
                Debug.Log("Opened app settings as fallback");
            }
            catch (System.Exception fallbackError)
            {
                Debug.LogError($"Failed to open app settings: {fallbackError.Message}");
            }
        }
#else
        Debug.Log("Notification settings only available on Android");
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
        
        // Check if this is a whisper notification
        bool isWhisperNotification = e.Message.Data.ContainsKey("type") && 
                                   e.Message.Data["type"] == "whisper_received";
        
        // Handle notification data
        if (e.Message.Notification != null)
        {
            Debug.Log($"Title: {e.Message.Notification.Title}");
            Debug.Log($"Body: {e.Message.Notification.Body}");

            // If it's a whisper, show in-game notification using NotificationManager
            if (isWhisperNotification)
            {
                ShowWhisperNotification(e.Message.Notification.Title, e.Message.Notification.Body);
            }
            
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

    // Show whisper notification using NotificationManager
    private void ShowWhisperNotification(string title, string body)
    {
        NotificationManager notificationManager = FindObjectOfType<NotificationManager>();
        if (notificationManager != null)
        {
            notificationManager.ShowNotification(title, body);
            Debug.Log($"Displayed whisper notification: {title}");
        }
        else
        {
            Debug.LogWarning("NotificationManager not found - cannot display whisper notification");
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
                // Save to devices collection (keep existing functionality)
                DocumentReference deviceDocRef = FirebaseFirestore.DefaultInstance
                    .Collection("devices")
                    .Document(_cachedDeviceId);

                await deviceDocRef.SetAsync(new
                {
                    token = token,
                    lastUpdated = FieldValue.ServerTimestamp,
                    platform = Application.platform.ToString()
                });

                Debug.Log("Device token saved to Firestore devices collection");

                // Also update user profile if user is logged in
                await UpdateUserProfileToken(token);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save token: {e.Message}");
            }
        }
    }

    private async Task UpdateUserProfileToken(string token)
    {
        try
        {
            // Check if user is logged in via Firebase Auth
            if (Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser != null)
            {
                string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
                
                DocumentReference userDocRef = FirebaseFirestore.DefaultInstance
                    .Collection("users")
                    .Document(userId);

                // Update the user's FCM token
                await userDocRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "notificationToken", token },
                    { "tokenLastUpdated", FieldValue.ServerTimestamp },
                    { "platform", Application.platform.ToString() }
                });

                Debug.Log($"FCM token updated in user profile for userId: {userId}");
            }
            else
            {
                Debug.Log("User not logged in, skipping user profile token update");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update user profile token: {e.Message}");
        }
    }

    // Public method to be called from LoginManager when user logs in
    public async void UpdateCurrentUserToken()
    {
        if (!string.IsNullOrEmpty(_deviceToken))
        {
            await UpdateUserProfileToken(_deviceToken);
        }
        else
        {
            Debug.Log("No FCM token available yet, will update when token is received");
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
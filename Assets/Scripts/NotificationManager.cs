using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Firebase.Messaging;

public class NotificationManager : MonoBehaviour
{
    [Header("Notification Settings")]
    [SerializeField] private GameObject notificationPrefab;
    [SerializeField] private Transform notificationParent;
    [SerializeField] private float displayDuration = 3f;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio")]
    [SerializeField] private AudioClip notificationSound;
    
    [Header("Positioning")]
    [SerializeField] private Vector2 startOffset = new Vector2(-400f, 0f);
    [SerializeField] private Vector2 endOffset = new Vector2(0f, 0f);
    
    // Private variables
    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private bool isDisplayingNotification = false;
    private AudioSource audioSource;
    
    [System.Serializable]
    public class NotificationData
    {
        public string title;
        public string message;
        public Sprite icon;
        
        public NotificationData(string title, string message, Sprite icon = null)
        {
            this.title = title;
            this.message = message;
            this.icon = icon;
        }
    }
    
    void Start()
    {
        InitializeAudioSource();
        
        // Set up Firebase message listening for in-game notifications
        SetupFirebaseMessageListening();
        
        // Validate required components
        if (notificationPrefab == null)
        {
            Debug.LogError("NotificationManager: Notification prefab is not assigned!");
        }
        
        if (notificationParent == null)
        {
            Debug.LogWarning("NotificationManager: Notification parent not assigned, using this transform");
            notificationParent = transform;
        }
    }
    
    /// <summary>
    /// Initialize audio source for notification sounds
    /// </summary>
    private void InitializeAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure audio source for notification sounds
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 0.7f;
    }
    
    /// <summary>
    /// Show a notification with title and message
    /// </summary>
    public void ShowNotification(string title, string message, Sprite icon = null)
    {
        var notificationData = new NotificationData(title, message, icon);
        notificationQueue.Enqueue(notificationData);
        
        // Start processing queue if not already displaying
        if (!isDisplayingNotification)
        {
            StartCoroutine(ProcessNotificationQueue());
        }
    }
    
    /// <summary>
    /// Show a simple notification with just a message
    /// </summary>
    public void ShowNotification(string message)
    {
        ShowNotification("", message);
    }
    
    /// <summary>
    /// Process the notification queue one by one
    /// </summary>
    private IEnumerator ProcessNotificationQueue()
    {
        isDisplayingNotification = true;
        
        while (notificationQueue.Count > 0)
        {
            var notificationData = notificationQueue.Dequeue();
            yield return StartCoroutine(DisplayNotification(notificationData));
            
            // Small delay between notifications
            yield return new WaitForSeconds(0.2f);
        }
        
        isDisplayingNotification = false;
    }
    
    /// <summary>
    /// Display a single notification with animation
    /// </summary>
    private IEnumerator DisplayNotification(NotificationData data)
    {
        if (notificationPrefab == null)
        {
            Debug.LogError("NotificationManager: Cannot display notification - prefab is null");
            yield break;
        }
        
        // Instantiate notification
        GameObject notificationInstance = Instantiate(notificationPrefab, notificationParent);
        RectTransform rectTransform = notificationInstance.GetComponent<RectTransform>();
        
        if (rectTransform == null)
        {
            Debug.LogError("NotificationManager: Notification prefab must have a RectTransform component");
            Destroy(notificationInstance);
            yield break;
        }
        
        // Setup notification content
        SetupNotificationContent(notificationInstance, data);
        
        // Play notification sound
        PlayNotificationSound();
        
        // Set initial position (off-screen left)
        Vector2 targetPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = targetPosition + startOffset;
        
        // Animate slide in from left
        yield return StartCoroutine(AnimateSlideIn(rectTransform, targetPosition + startOffset, targetPosition + endOffset));
        
        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);
        
        // Animate slide out to right
        Vector2 exitPosition = targetPosition + new Vector2(-startOffset.x, endOffset.y); // Slide out to right
        yield return StartCoroutine(AnimateSlideOut(rectTransform, targetPosition + endOffset, exitPosition));
        
        // Destroy notification
        Destroy(notificationInstance);
    }
    
    /// <summary>
    /// Setup the content of the notification (text, icons, etc.)
    /// </summary>
    private void SetupNotificationContent(GameObject notificationInstance, NotificationData data)
    {
        // Try to find and set title text
        Text titleText = notificationInstance.transform.Find("Title")?.GetComponent<Text>();
        if (titleText != null && !string.IsNullOrEmpty(data.title))
        {
            titleText.text = data.title;
            titleText.gameObject.SetActive(true);
        }
        else if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
        }
        
        // Try to find and set message text
        Text messageText = notificationInstance.transform.Find("Message")?.GetComponent<Text>();
        if (messageText == null)
        {
            // Fallback: try to find any Text component
            messageText = notificationInstance.GetComponentInChildren<Text>();
        }
        
        if (messageText != null)
        {
            messageText.text = data.message;
        }
        
        // Try to find and set icon
        Image iconImage = notificationInstance.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Animate notification sliding in from left
    /// </summary>
    private IEnumerator AnimateSlideIn(RectTransform rectTransform, Vector2 startPos, Vector2 endPos)
    {
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            float curveValue = slideCurve.Evaluate(progress);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);
            yield return null;
        }
        
        rectTransform.anchoredPosition = endPos;
    }
    
    /// <summary>
    /// Animate notification sliding out to right
    /// </summary>
    private IEnumerator AnimateSlideOut(RectTransform rectTransform, Vector2 startPos, Vector2 endPos)
    {
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            float curveValue = slideCurve.Evaluate(progress);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);
            yield return null;
        }
        
        rectTransform.anchoredPosition = endPos;
    }
    
    /// <summary>
    /// Play notification sound if available
    /// </summary>
    private void PlayNotificationSound()
    {
        if (notificationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(notificationSound);
        }
    }
    
    /// <summary>
    /// Clear all pending notifications
    /// </summary>
    public void ClearNotifications()
    {
        notificationQueue.Clear();
    }
    
    /// <summary>
    /// Get the number of pending notifications
    /// </summary>
    public int GetPendingNotificationCount()
    {
        return notificationQueue.Count;
    }
    
    /// <summary>
    /// Check if a notification is currently being displayed
    /// </summary>
    public bool IsDisplayingNotification()
    {
        return isDisplayingNotification;
    }
    
    void OnDestroy()
    {
        // Clean up any remaining coroutines
        StopAllCoroutines();
        ClearNotifications();
        
        // Clean up Firebase listeners
        CleanupFirebaseListeners();
    }

    #region Firebase Message Handling
    
    /// <summary>
    /// Set up Firebase message listening for in-game notifications
    /// </summary>
    private void SetupFirebaseMessageListening()
    {
        try
        {
            FirebaseMessaging.MessageReceived += OnFirebaseMessageReceived;
            Debug.Log("NotificationManager: Firebase message listening enabled");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"NotificationManager: Could not set up Firebase listening: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Clean up Firebase message listeners
    /// </summary>
    private void CleanupFirebaseListeners()
    {
        try
        {
            FirebaseMessaging.MessageReceived -= OnFirebaseMessageReceived;
            Debug.Log("NotificationManager: Firebase listeners cleaned up");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"NotificationManager: Error cleaning up Firebase listeners: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handle incoming Firebase messages and display them as in-game notifications
    /// </summary>
    private void OnFirebaseMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log($"NotificationManager: Received Firebase message from: {e.Message.From}");
        
        // Handle notification data
        if (e.Message.Notification != null)
        {
            Debug.Log($"NotificationManager: Title: {e.Message.Notification.Title}");
            Debug.Log($"NotificationManager: Body: {e.Message.Notification.Body}");

            // Display all Firebase notifications as in-game notifications
            ShowNotification(e.Message.Notification.Title, e.Message.Notification.Title + "\n" + e.Message.Notification.Body);
        }
        
        // Log custom data payload for debugging
        foreach (var pair in e.Message.Data)
        {
            Debug.Log($"NotificationManager: Data - {pair.Key}: {pair.Value}");
        }
    }
    
    #endregion
    
    #if UNITY_EDITOR
    /// <summary>
    /// Test method for Unity Editor - shows a sample notification
    /// </summary>
    [ContextMenu("Test Notification")]
    private void TestNotification()
    {
        ShowNotification("Test Title", "This is a test notification message!");
    }
    #endif
}

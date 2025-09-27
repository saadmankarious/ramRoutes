using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RamRoutes.Services;
using RamRoutes.Model;

/// <summary>
/// Dedicated script to notify players about locked buildings in their current stage.
/// Works independently from UIManager with hardcoded stage-building mappings.
/// </summary>
public class GameProgressNotifier : MonoBehaviour
{
    [Header("Notification Settings")]
    [SerializeField] private float notificationInterval = 60f; // Notify every 60 seconds
    [SerializeField] private bool enableProgressNotifications = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // References
    private NotificationManager notificationManager;
    private Coroutine progressNotificationCoroutine;
    
    // Current state tracking
    private GameStage currentStage;
    private HashSet<string> unlockedBuildings = new HashSet<string>();
    
    // Hardcoded stage-building mappings
    private readonly Dictionary<Stage, List<string>> stageBuildingMap = new Dictionary<Stage, List<string>>
    {
        { Stage.TC, new List<string> { "TC" } },
        { Stage.EasternCampus, new List<string> { "PR" } },
        { Stage.FirstStreet, new List<string> { "McWethy", "SAW", "Stoner" } },
        { Stage.Pedmall, new List<string> { "Ebersole", "Library" } }
    };
    
    void Start()
    {
        // Find required components
        notificationManager = FindObjectOfType<NotificationManager>();
        
        if (notificationManager == null)
        {
            Debug.LogWarning("GameProgressNotifier: No NotificationManager found - notifications will be logged to console");
        }
        
        // Initialize current stage
        currentStage = GameStageService.LoadStageFromPrefs();
        
        // Start monitoring if enabled
        if (enableProgressNotifications)
        {
            StartProgressNotifications();
        }
        
        // Initialize unlocked buildings
        _ = RefreshUnlockedBuildings();
    }
    
    /// <summary>
    /// Starts the progress notification system
    /// </summary>
    public void StartProgressNotifications()
    {
        if (progressNotificationCoroutine != null)
        {
            StopCoroutine(progressNotificationCoroutine);
        }
        
        progressNotificationCoroutine = StartCoroutine(ProgressNotificationCoroutine());
        
        if (debugMode)
        {
            Debug.Log("GameProgressNotifier: Started progress notifications");
        }
    }
    
    /// <summary>
    /// Stops the progress notification system
    /// </summary>
    public void StopProgressNotifications()
    {
        if (progressNotificationCoroutine != null)
        {
            StopCoroutine(progressNotificationCoroutine);
            progressNotificationCoroutine = null;
            
            if (debugMode)
            {
                Debug.Log("GameProgressNotifier: Stopped progress notifications");
            }
        }
    }
    
    /// <summary>
    /// Main coroutine that handles progress notifications
    /// </summary>
    private IEnumerator ProgressNotificationCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(notificationInterval);
            
            // Refresh current state
            var newStage = GameStageService.LoadStageFromPrefs();
            bool stageChanged = (currentStage?.area != newStage?.area);
            
            if (stageChanged)
            {
                currentStage = newStage;
                if (debugMode)
                {
                    Debug.Log($"GameProgressNotifier: Stage changed to {currentStage?.area}");
                }
            }
            
            // Refresh unlocked buildings
            yield return StartCoroutine(RefreshUnlockedBuildingsCoroutine());
            
            // Check for progress notifications
            CheckAndNotifyProgress();
        }
    }
    
    /// <summary>
    /// Refreshes the list of unlocked buildings from Firebase
    /// </summary>
    private async System.Threading.Tasks.Task RefreshUnlockedBuildings()
    {
        try
        {
            var buildingService = new UnlockedBuildingService();
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            
            if (string.IsNullOrEmpty(userId))
            {
                if (debugMode)
                {
                    Debug.LogWarning("GameProgressNotifier: No user ID available");
                }
                return;
            }
            
            var userUnlockedBuildings = await buildingService.RetrieveUnlockedBuildings(userId);
            unlockedBuildings.Clear();
            
            foreach (var building in userUnlockedBuildings)
            {
                unlockedBuildings.Add(building.buildingName);
            }
            
            if (debugMode)
            {
                Debug.Log($"GameProgressNotifier: Refreshed unlocked buildings - {unlockedBuildings.Count} buildings unlocked");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GameProgressNotifier: Failed to refresh unlocked buildings - {ex.Message}");
        }
    }
    
    /// <summary>
    /// Coroutine wrapper for RefreshUnlockedBuildings to use in main coroutine
    /// </summary>
    private IEnumerator RefreshUnlockedBuildingsCoroutine()
    {
        var task = RefreshUnlockedBuildings();
        yield return new WaitUntil(() => task.IsCompleted);
    }
    
    /// <summary>
    /// Checks current progress and sends appropriate notifications
    /// </summary>
    private void CheckAndNotifyProgress()
    {
        if (currentStage == null)
        {
            return;
        }
        
        // Only notify about locked buildings in the current stage
        NotifyLockedBuildingsInCurrentStage();
    }
    
    /// <summary>
    /// Notifies about locked buildings in the current stage only
    /// </summary>
    private void NotifyLockedBuildingsInCurrentStage()
    {
        if (currentStage == null) return;
        
        // Get buildings for current stage
        if (!stageBuildingMap.TryGetValue(currentStage.area, out List<string> stageBuildings))
        {
            if (debugMode)
            {
                Debug.Log($"GameProgressNotifier: No buildings defined for stage {currentStage.area}");
            }
            return;
        }
        
        // Find locked buildings in current stage
        List<string> lockedBuildings = new List<string>();
        
        foreach (string buildingName in stageBuildings)
        {
            if (!unlockedBuildings.Contains(buildingName))
            {
                // Get display name from BuildingDataManager
                var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
                string displayName = buildingInfo?.displayName ?? buildingName;
                lockedBuildings.Add(displayName);
            }
        }
        
        // Send notification if there are locked buildings
        if (lockedBuildings.Count > 0)
        {
            string title = "Buildings to Unlock";
            string message = CreateLockedBuildingsMessage(lockedBuildings);
            SendNotification(title, message);
        }
        else if (debugMode)
        {
            Debug.Log($"GameProgressNotifier: All buildings in stage {currentStage.area} are unlocked");
        }
    }
    
    /// <summary>
    /// Creates a message about locked buildings
    /// </summary>
    private string CreateLockedBuildingsMessage(List<string> lockedBuildings)
    {
        if (lockedBuildings.Count == 1)
        {
            return $"Unlock {lockedBuildings[0]} to advance.";
        }
        else if (lockedBuildings.Count == 2)
        {
            return $"Unlock {lockedBuildings[0]} and {lockedBuildings[1]} to advance!";
        }
        else
        {
            string buildingsList = FormatBuildingsList(lockedBuildings);
            return $"Unlock {buildingsList} to advance!";
        }
    }
    
    /// <summary>
    /// Formats a list of building names for display
    /// </summary>
    private string FormatBuildingsList(List<string> buildings)
    {
        if (buildings.Count == 1)
        {
            return buildings[0];
        }
        else if (buildings.Count == 2)
        {
            return $"{buildings[0]} and {buildings[1]}";
        }
        else
        {
            string list = string.Join(", ", buildings.GetRange(0, 1));
            return $"{list}, and {buildings[buildings.Count - 1]}";
        }
    }
    

    
    /// <summary>
    /// Sends a notification through the NotificationManager or logs to console
    /// </summary>
    private void SendNotification(string title, string message)
    {
        if (notificationManager != null)
        {
            notificationManager.ShowNotification(title, message);
            
            if (debugMode)
            {
                Debug.Log($"GameProgressNotifier: Sent notification - {title}: {message}");
            }
        }
        else
        {
            Debug.Log($"GameProgressNotifier: {title}: {message}");
        }
    }
    
    /// <summary>
    /// Manually triggers a progress check (useful for external calls)
    /// </summary>
    public void TriggerProgressCheck()
    {
        if (enableProgressNotifications)
        {
            StartCoroutine(ManualProgressCheck());
        }
    }
    
    /// <summary>
    /// Coroutine for manual progress check
    /// </summary>
    private IEnumerator ManualProgressCheck()
    {
        yield return StartCoroutine(RefreshUnlockedBuildingsCoroutine());
        CheckAndNotifyProgress();
    }
    
    /// <summary>
    /// Enables or disables progress notifications
    /// </summary>
    public void SetNotificationsEnabled(bool enabled)
    {
        enableProgressNotifications = enabled;
        
        if (enabled)
        {
            StartProgressNotifications();
        }
        else
        {
            StopProgressNotifications();
        }
    }
    
    void OnDestroy()
    {
        StopProgressNotifications();
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Trigger Manual Progress Check")]
    private void DebugTriggerProgressCheck()
    {
        TriggerProgressCheck();
    }
    
    [ContextMenu("Toggle Debug Mode")]
    private void ToggleDebugMode()
    {
        debugMode = !debugMode;
        Debug.Log($"GameProgressNotifier: Debug mode {(debugMode ? "enabled" : "disabled")}");
    }
    #endif
}

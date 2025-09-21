using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using RamRoutes.Services;
using RamRoutes.Model;

public class RamsManager : MonoBehaviour
{
     private BuildingInteraction building;
    
    [Header("Stage Control")]
    [SerializeField] private bool requireTerminalStage = false; // Flag to control terminal stage restrictions
    
    [Header("Ram Prefab")]
    [SerializeField] private GameObject ramPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints; // Array of spawn points for random spawning
    [SerializeField] private float spawnRadius = 5f; // Radius around spawn point to spread rams in circle
    [SerializeField] private int maxRams = 10;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip despawnSound;
    [SerializeField] private AudioClip backflipClip;
    
    [Header("User Info Panel")]
    [SerializeField] private UserInfoPanel userInfoPanel;
    
    [Header("Chat System")]
    [SerializeField] private ChatManager chatManager;
    
    [Header("Player Count Display")]
    [SerializeField] private GameObject playerCountCanvasPrefab;
    [SerializeField] private Transform playerCountSpawnPoint;
    
    private UserService userService;
    private NotificationManager notificationManager; // Will be found automatically
    private List<GameObject> spawnedRams = new List<GameObject>();
    private HashSet<string> spawnedUserIds = new HashSet<string>(); // Track spawned user IDs
    private GameObject playerCountCanvasInstance; // Instance of the player count canvas
    private bool hasBeenActivated = false; // Prevent double activation
    private Coroutine refreshCoroutine; // Reference to the refresh coroutine
    
    // Static flag to prevent duplicate building activity notifications on startup
    private static bool hasNotifiedBuildingActivity = false;
    
    // Instance-specific tracking for previously seen players in THIS building
    private HashSet<string> previousPlayersInThisBuilding = new HashSet<string>();
    
    // Coroutine reference for live building monitoring
    private Coroutine buildingMonitorCoroutine;
    
    // Color assignment tracking for unique colors
    private Dictionary<string, Color> userColorAssignments = new Dictionary<string, Color>();
    private List<Color> availableColors = new List<Color>();
    private int colorIndex = 0;
    
    void Start()
    {
        building = GetComponent<BuildingInteraction>();
        userService = new UserService();
        
        // Find NotificationManager in the scene
        notificationManager = FindObjectOfType<NotificationManager>();
        if (notificationManager != null)
        {
            Debug.Log("RamsManager: Found NotificationManager in scene");
        }
        else
        {
            Debug.LogWarning("RamsManager: No NotificationManager found in scene - notifications will be logged to console");
        }
        
        // Initialize colors that are visible on green background
        InitializeVisibleColors();
        
        // Initialize player count display immediately
        InitializePlayerCountDisplay();
        
        // Get players in all buildings and notify current player (initial notification)
        // GetPlayersInAllBuildingsAndNotify();
        
        // Start live monitoring for new players entering buildings
        StartLiveBuildingMonitoring();
        
        // Validate spawn points configuration
        ValidateSpawnPoints();
        
        // Rams will only spawn when OnBuildingActivated() is called from BuildingInteraction
    }
    
    /// <summary>
    /// Validates spawn points configuration and provides helpful warnings
    /// </summary>
    private void ValidateSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"RamsManager on {gameObject.name}: No spawn points assigned! Rams will spawn at RamsManager position. Please assign spawn points in the inspector.");
            return;
        }
        
        int nullCount = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
                nullCount++;
        }
        
        if (nullCount > 0)
        {
            Debug.LogWarning($"RamsManager on {gameObject.name}: {nullCount} out of {spawnPoints.Length} spawn points are null. Please assign all spawn points.");
        }
        else
        {
            Debug.Log($"RamsManager on {gameObject.name}: {spawnPoints.Length} spawn points configured successfully.");
        }
    }
    
    /// <summary>
    /// Debug method to visualize spawn points in Scene view
    /// </summary>
    [ContextMenu("Debug Spawn Points")]
    private void DebugSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points to debug!");
            return;
        }
        
        Debug.Log($"=== Spawn Points Debug ({spawnPoints.Length} points) ===");
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Debug.Log($"Spawn Point {i}: {spawnPoints[i].name} at position {spawnPoints[i].position}");
            }
            else
            {
                Debug.LogWarning($"Spawn Point {i}: NULL");
            }
        }
    }
    
    /// <summary>
    /// Initialize colors that are visible on green background
    /// </summary>
    private void InitializeVisibleColors()
    {
        availableColors.Clear();
        
        // Colors that contrast well with green background
        availableColors.Add(new Color(1f, 0.2f, 0.2f));        // Red
        availableColors.Add(new Color(0.2f, 0.2f, 1f));        // Blue  
        availableColors.Add(new Color(1f, 0.6f, 0f));          // Orange
        availableColors.Add(new Color(0.8f, 0f, 0.8f));        // Magenta
        availableColors.Add(new Color(0.4f, 0.2f, 0.6f));      // Purple
        availableColors.Add(new Color(1f, 1f, 0.2f));          // Yellow
        availableColors.Add(new Color(0f, 0.8f, 0.8f));        // Cyan
        availableColors.Add(new Color(0.8f, 0.4f, 0.2f));      // Brown
        availableColors.Add(new Color(1f, 0.4f, 0.8f));        // Pink
        availableColors.Add(new Color(0.2f, 0.2f, 0.2f));      // Dark Gray
        availableColors.Add(new Color(0.6f, 0.3f, 0f));        // Dark Orange
        availableColors.Add(new Color(0.2f, 0.6f, 0.8f));      // Light Blue
        
        colorIndex = 0;
        Debug.Log($"Initialized {availableColors.Count} colors visible on green background");
    }
    
    /// <summary>
    /// Initialize the player count display on start
    /// </summary>
    private async void InitializePlayerCountDisplay()
    {
        try
        {
            // Small delay to ensure building is properly initialized
            await Task.Delay(500);
            await DisplayPlayerCount();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error initializing player count display: {ex.Message}");
        }
    }

    /// <summary>
    /// Get players in all buildings upon start and notify current player of who is where
    /// Only runs once per session to avoid duplicate notifications
    /// </summary>
    private async void GetPlayersInAllBuildingsAndNotify()
    {
        try
        {
            // Check if we've already notified about building activity this session
            if (hasNotifiedBuildingActivity)
            {
                Debug.Log("RamsManager: Building activity already notified this session, skipping");
                return;
            }
            
            // Mark as notified to prevent other RamsManagers from running this
            hasNotifiedBuildingActivity = true;
            
            // Small delay to ensure services are properly initialized
            await Task.Delay(1000);
            
            // Get current player info
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogWarning("RamsManager: No authenticated user, cannot notify about building occupancy");
                return;
            }
            
            // Get all users in all buildings
            var buildingUsers = await userService.GetUsersInAllBuildings();
            
            if (buildingUsers.Count == 0)
            {
                Debug.Log("RamsManager: No players found in any buildings");
                if (notificationManager != null)
                {
                    notificationManager.ShowNotification("Campus Status", "No players currently in any buildings");
                }
                return;
            }
            
            // Notify current player about each building's occupancy in a queue
            foreach (var kvp in buildingUsers)
            {
                string buildingName = kvp.Key;
                var users = kvp.Value;
                
                if (users.Count > 0)
                {
                    // Create notification message
                    string message;
                    if (users.Count == 1)
                    {
                        message = $"{users[0].name} is in {buildingName}. Go say hi!";
                    }
                    else if (users.Count <= 3)
                    {
                        var names = users.Take(3).Select(u => u.name).ToArray();
                        message = $"{string.Join(", ", names)} are in {buildingName}. Go say hi!";
                    }
                    else
                    {
                        var firstThree = users.Take(3).Select(u => u.name).ToArray();
                        message = $"{string.Join(", ", firstThree)} and {users.Count - 3} others are in {buildingName}. Go say hi!";
                    }
                    
                    // Show notification if NotificationManager is available
                    if (notificationManager != null)
                    {
                        notificationManager.ShowNotification("Building Activity", message);
                        
                        // Small delay between notifications to create a proper queue
                        await Task.Delay(500);
                    }
                    else
                    {
                        Debug.Log($"RamsManager: {message}");
                    }
                }
            }
            
            Debug.Log($"RamsManager: Notified current player about {buildingUsers.Count} buildings with active players");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RamsManager: Error getting players in all buildings: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts live monitoring of building occupancy to detect new players entering buildings
    /// </summary>
    private void StartLiveBuildingMonitoring()
    {
        if (buildingMonitorCoroutine == null)
        {
            buildingMonitorCoroutine = StartCoroutine(MonitorBuildingOccupancyCoroutine());
            Debug.Log("RamsManager: Started live building occupancy monitoring (10 second intervals)");
        }
    }
    
    /// <summary>
    /// Coroutine that monitors building occupancy every 10 seconds to detect new players
    /// </summary>
    private IEnumerator MonitorBuildingOccupancyCoroutine()
    {
        // Wait a bit before starting monitoring to let initial notification complete
        yield return new WaitForSeconds(5f);
        
        while (true)
        {
            yield return new WaitForSeconds(3f); // Check every 10 seconds

            // Start the async task and wait for it to complete
            if (requireTerminalStage)
            {
                var currentStage = GameStageService.LoadStageFromPrefs();
                if (currentStage != null && currentStage.area == Stage.Terminal)
                {
                    var task = CheckForNewPlayersInBuildings();
                    yield return new WaitUntil(() => task.IsCompleted);
                }
            }
            else
            {
                // If terminal stage not required, always check for new players
                var task = CheckForNewPlayersInBuildings();
                yield return new WaitUntil(() => task.IsCompleted);
            }
        }
    }
    
    /// <summary>
    /// Checks for new players that have joined THIS building since the last check
    /// Only notifies about NEW players, not existing ones
    /// </summary>
    private async Task CheckForNewPlayersInBuildings()
    {
        try
        {
            // Get current player info
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return; // No authenticated user
            }
            
            // Get building name for this specific manager
            string buildingName = building.buildingName;
            if (string.IsNullOrEmpty(buildingName))
            {
                return; // No building name set
            }
            
            // Get users currently in THIS building only
            var currentUsersInBuilding = await userService.GetUsersInBuildingWithPoints(buildingName);
            
            if (currentUsersInBuilding.Count > 0)
            {
                // Get current user IDs in this building
                var currentUserIds = currentUsersInBuilding.Select(u => u.userId).ToHashSet();
                
                // Find NEW users (present now but not in previous check)
                var newUserIds = currentUserIds.Except(previousPlayersInThisBuilding).ToList();
                
                if (newUserIds.Count > 0)
                {
                    // Get the new user objects
                    var newUsers = currentUsersInBuilding.Where(u => newUserIds.Contains(u.userId)).ToList();
                    
                    // Create notification for new players (excluding current player)
                    foreach (var newUser in newUsers)
                    {
                        // Skip notification if this is the current player
                        if (newUser.userId == currentUserId)
                        {
                            Debug.Log($"RamsManager ({buildingName}): Skipping notification for current player: {newUser.name}");
                            continue;
                        }
                        
                        string message = $"{newUser.name} just entered {buildingName}. Go say hi!";
                        
                        // Show notification if NotificationManager is available
                        if (notificationManager != null)
                        {
                            notificationManager.ShowNotification("New Player Activity", message);
                            
                            // Small delay between notifications
                            await Task.Delay(300);
                        }
                        else
                        {
                            Debug.Log($"RamsManager: {message}");
                        }
                        
                        Debug.Log($"RamsManager ({buildingName}): New player detected - {newUser.name} entered building");
                    }
                }
                
                // Update the tracking with current occupancy for this building
                previousPlayersInThisBuilding = currentUserIds;
            }
            else
            {
                // Building is empty, clear its tracking
                previousPlayersInThisBuilding.Clear();
            }
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RamsManager ({building.buildingName}): Error checking for new players in building: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when building gets activated - spawns rams if not already spawned
    /// </summary>
    public async void OnBuildingActivated()
    {
        if (building.activated && !hasBeenActivated)
        {
            // Check if we're in the Terminal game stage - only activate ram system during Terminal stage
            if (requireTerminalStage)
            {
                var currentStage = GameStageService.LoadStageFromPrefs();
                if (currentStage == null || currentStage.area != Stage.Terminal)
                {
                    Debug.Log($"RamsManager: Not in Terminal stage (current: {currentStage?.area}), skipping ram system activation");
                    return;
                }
            }
            
            hasBeenActivated = true;
            Debug.Log($"Building {building.buildingName} activated in Terminal stage, spawning rams");
            await SpawnRams();
            
            // Display player count if there are players in the building
            await DisplayPlayerCount();
            
            // Start the refresh coroutine to check for new players every 5 seconds
            if (refreshCoroutine == null)
            {
                refreshCoroutine = StartCoroutine(RefreshPlayersCoroutine());
                Debug.Log("Started refresh coroutine to check for new players every 5 seconds");
            }
        }
        else if (hasBeenActivated)
        {
            Debug.Log($"Building {building.buildingName} activation called again, ignoring duplicate call");
        }
    }
    
    public void OnPlayerLeavesBuilding()
    {
        if (hasBeenActivated)
        {
            hasBeenActivated = false;
            Debug.Log($"Building {building.buildingName} deactivated, despawning rams in 10 seconds");
            
            // Update player count display instead of hiding it
            // This will show the remaining players in the building
            Task.Run(async () => {
                try
                {
                    await DisplayPlayerCount();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error updating player count when player leaves: {ex.Message}");
                }
            });
            
            // Stop the refresh coroutine if running
            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
                refreshCoroutine = null;
                Debug.Log("Stopped refresh coroutine as building is no longer activated");
            }
            
            // Check if GameObject is active before starting coroutine
            if (gameObject.activeInHierarchy)
            {
                // Start coroutine to handle delayed despawn and user cleanup
                StartCoroutine(HandlePlayerLeavingWithDelay());
            }
            else
            {
                // GameObject is inactive, handle cleanup immediately using a static method
                Debug.Log("GameObject is inactive, handling cleanup immediately");
                HandleImmediateCleanup();
            }
        }
    }
    
    /// <summary>
    /// Handles immediate cleanup when GameObject is inactive and coroutines can't be started
    /// </summary>
    private async void HandleImmediateCleanup()
    {
        // Clear rams immediately
        ClearSpawnedRams();
        
        // Clear current building for this user
        string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            try
            {
                //await userService.ClearCurrentUserBuilding();
                Debug.Log($"Successfully cleared current building for user {userId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to clear current building: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Handles the delayed despawn process and user cleanup when player leaves
    /// </summary>
    private IEnumerator HandlePlayerLeavingWithDelay()
    {
        // Wait 10 seconds before starting despawn process
        Debug.Log("Waiting 10 seconds before despawning rams...");
        yield return new WaitForSeconds(5f);
        
        // Start despawn coroutine after the delay
        Debug.Log("Starting ram despawn process");
        yield return StartCoroutine(DespawnRamsWithDelay());
        
        // Clear current building for this user after despawning
        string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("No authenticated user found, cannot clear current building");
            yield break;
        }

        // Clear current building for this user (convert to coroutine-friendly approach)
        // var clearBuildingTask = userService.ClearCurrentUserBuilding();
        // yield return new WaitUntil(() => clearBuildingTask.IsCompleted);
        
        // if (clearBuildingTask.Exception != null)
        // {
        //     Debug.LogError($"Failed to clear current building: {clearBuildingTask.Exception.Message}");
        // }
        // else
        // {
        //     Debug.Log("Successfully cleared current building for user");
        // }
    }

    /// <summary>
    /// Spawns rams for users currently in this building
    /// </summary>
    private async Task SpawnRams()
    {
        // Check if we're in the Terminal game stage - only spawn rams during Terminal stage
        if (requireTerminalStage)
        {
            var currentStage = GameStageService.LoadStageFromPrefs();
            if (currentStage == null || currentStage.area != Stage.Terminal)
            {
                Debug.Log($"RamsManager: Not in Terminal stage (current: {currentStage?.area}), skipping ram spawning");
                return;
            }
        }
        
        string buildingName = building.buildingName;
        if (string.IsNullOrEmpty(buildingName))
        {
            Debug.LogError("Building name not set in RamsManager!");
            return;
        }

        if (ramPrefab == null)
        {
            Debug.LogError("Ram prefab not assigned in RamsManager!");
            return;
        }

        try
        {
            // Get users currently in this building
            var usersInBuilding = await userService.GetUsersInBuildingWithPoints(buildingName);

            // Include current player in ram spawning along with other players
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            
            Debug.Log($"Found {usersInBuilding.Count} users in {buildingName} (including current player)");

            // Clear existing rams and reset tracking
            ClearSpawnedRams();
            spawnedUserIds.Clear();

            // Start coroutine to spawn rams with delay
            int spawnCount = Mathf.Min(usersInBuilding.Count, maxRams);
            if (spawnCount > 0)
            {
                StartCoroutine(SpawnRamsWithDelay(usersInBuilding, spawnCount));

                // Track the user IDs that we're spawning
                foreach (var user in usersInBuilding.Take(spawnCount))
                {
                    spawnedUserIds.Add(user.userId);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to spawn rams: {e.Message}");
        }
    }
    
    /// <summary>
    /// Coroutine that refreshes every 5 seconds to check for new players
    /// </summary>
    private IEnumerator RefreshPlayersCoroutine()
    {
        while (building.activated && hasBeenActivated)
        {
            yield return new WaitForSeconds(1f);
            
            if (building.activated)
            {
                // Start the async task and wait for it to complete
                var task = CheckForNewPlayers();
                yield return new WaitUntil(() => task.IsCompleted);
            }
        }
    }
    
    /// <summary>
    /// Checks for new players and spawns only the new ones
    /// </summary>
    private async Task CheckForNewPlayers()
    {
        // Check if we're in the Terminal game stage - only spawn rams during Terminal stage
        if (requireTerminalStage)
        {
            var currentStage = GameStageService.LoadStageFromPrefs();
            if (currentStage == null || currentStage.area != Stage.Terminal)
            {
                Debug.Log($"RamsManager: Not in Terminal stage (current: {currentStage?.area}), skipping new player check");
                return;
            }
        }
        
        string buildingName = building.buildingName;
        if (string.IsNullOrEmpty(buildingName))
        {
            return;
        }
        
        try
        {
            // Get current users in building
            var usersInBuilding = await userService.GetUsersInBuildingWithPoints(buildingName);

            // Include current player in new player checking
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

            // Find new users that haven't been spawned yet
            var newUsers = usersInBuilding.Where(user => !spawnedUserIds.Contains(user.userId)).ToList();
            
            if (newUsers.Count > 0)
            {
                Debug.Log($"Found {newUsers.Count} new users to spawn in {buildingName}");
                
                // Check if we can spawn more rams (within maxRams limit)
                int availableSlots = maxRams - spawnedRams.Count;
                int usersToSpawn = Mathf.Min(newUsers.Count, availableSlots);
                
                if (usersToSpawn > 0)
                {
                    // Spawn new users with delay
                    StartCoroutine(SpawnNewUsersWithDelay(newUsers.Take(usersToSpawn).ToList()));
                }
                else
                {
                    Debug.Log($"Max rams ({maxRams}) reached, cannot spawn more users");
                }
            }
            
            // Remove users who are no longer in the building
            var currentUserIds = usersInBuilding.Select(u => u.userId).ToHashSet();
            var usersToRemove = spawnedUserIds.Where(id => !currentUserIds.Contains(id)).ToList();
            
            if (usersToRemove.Count > 0)
            {
                Debug.Log($"Removing {usersToRemove.Count} users who left the building");

                // Play despawn sound for users leaving
                PlayDespawnSound();
                
                RemoveRamsForUsers(usersToRemove);
            }
            
            // Update player count display after changes
            await DisplayPlayerCount();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to check for new players: {e.Message}");
        }
    }
    
    /// <summary>
    /// Spawns new users with delay
    /// </summary>
    private IEnumerator SpawnNewUsersWithDelay(List<User> newUsers)
    {
        for (int i = 0; i < newUsers.Count; i++)
        {
            var user = newUsers[i];
            int nextIndex = spawnedRams.Count; // Use current count as index for new spawns
            SpawnRamForUser(user, nextIndex);
            spawnedUserIds.Add(user.userId);
            
            // Wait a bit before spawning the next new user
            if (i < newUsers.Count - 1)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
    
    /// <summary>
    /// Removes rams for users who left the building
    /// </summary>
    private void RemoveRamsForUsers(List<string> userIdsToRemove)
    {
        for (int i = spawnedRams.Count - 1; i >= 0; i--)
        {
            var ram = spawnedRams[i];
            if (ram != null)
            {
                // Try to find the user ID from the ram's name text component
                var allTexts = ram.GetComponentsInChildren<UnityEngine.UI.Text>();
                var nameText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
                string ramUserName = "";
                if (nameText != null)
                {
                    ramUserName = nameText.text.Split('(')[0].Trim(); // Extract name before building info
                }
                
                // Check if this ram belongs to a user who left
                // Note: This is a simple implementation. For better tracking, you might want to store user ID in the ram GameObject
                bool shouldRemove = false;
                foreach (string userIdToRemove in userIdsToRemove)
                {
                    // This is a simplified check - in a real implementation you'd want better user tracking
                    if (spawnedUserIds.Contains(userIdToRemove))
                    {
                        shouldRemove = true;
                        spawnedUserIds.Remove(userIdToRemove);
                        break;
                    }
                }
                
                if (shouldRemove)
                {
                    Destroy(ram);
                    spawnedRams.RemoveAt(i);
                    break; // Remove one ram per user who left
                }
            }
        }
    }
    
    /// <summary>
    /// Coroutine to spawn rams with a 2-second delay between each spawn
    /// </summary>
    private IEnumerator SpawnRamsWithDelay(List<User> users, int spawnCount)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            var user = users[i];
            SpawnRamForUser(user, i);
            
            // Wait 2 seconds before spawning the next ram (except for the last one)
            if (i < spawnCount - 1)
            {
                yield return new WaitForSeconds(.1f);
            }
        }
    }

    /// <summary>
    /// Coroutine to despawn rams one by one with delay and sound
    /// </summary>
    private IEnumerator DespawnRamsWithDelay()
    {
        // Create a copy of the list to avoid modification during iteration
        var ramsToDestroy = new List<GameObject>(spawnedRams);
        
        for (int i = 0; i < ramsToDestroy.Count; i++)
        {
            var ram = ramsToDestroy[i];
            if (ram != null)
            {
                // Play despawn sound
                PlayDespawnSound();
                
                // Destroy the ram
                Destroy(ram);
                Debug.Log($"Despawned ram {i + 1}/{ramsToDestroy.Count}");
                
                // Wait a bit before despawning the next ram (except for the last one)
                if (i < ramsToDestroy.Count - 1)
                {
                    yield return new WaitForSeconds(0.3f); // 300ms delay between despawns
                }
            }
        }
        
        // Clear the lists after all rams are despawned
        spawnedRams.Clear();
        spawnedUserIds.Clear();
        Debug.Log("All rams despawned successfully");
    }

    /// <summary>
    /// Coroutine that triggers random backflip animations for a specific ram
    /// </summary>
    private IEnumerator RandomBackflipAnimation(GameObject ramInstance)
    {
        if (ramInstance == null) yield break;
        
        // Get the Animator component from the ram
        Animator animator = ramInstance.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("No Animator found on ram instance - cannot trigger backflip animation");
            yield break;
        }
        
        // Wait for initial spawn animation to complete
        yield return new WaitForSeconds(2f);
        
        // Continue until the ram is destroyed
        while (ramInstance != null)
        {
            // Wait for a random interval between backflips (10-30 seconds)
            float waitTime = Random.Range(10f, 30f);
            yield return new WaitForSeconds(waitTime);
            
            // Check if ram still exists before triggering animation
            if (ramInstance != null && animator != null)
            {
                // Trigger the backflip animation
                animator.SetBool("backflip", true);
                Debug.Log($"Triggered backflip animation for ram: {ramInstance.name}");
                
                // Wait for backflip animation to complete, then set bool to false
                StartCoroutine(ResumeMovementAfterBackflip(ramInstance, animator));
            }
        }
    }

    /// <summary>
    /// Resumes movement animation after backflip completes
    /// </summary>
    private IEnumerator ResumeMovementAfterBackflip(GameObject ramInstance, Animator animator)
    {
        // Wait for the backflip animation duration (adjust this based on your animation length)
        yield return new WaitForSeconds(.5f); // Assuming backflip takes ~2 seconds
        
        // Check if ram and animator still exist
        if (ramInstance != null && animator != null)
        {
            // Set backflip bool to false to return to normal state
            animator.SetBool("backflip", false);
            
            Debug.Log($"Finished backflip for ram: {ramInstance.name}");
        }
    }

    /// <summary>
    /// Spawns a single ram for a specific user
    /// </summary>
    private void SpawnRamForUser(User user, int index)
    {
        // Calculate spawn position distributed across all spawn points
        Vector3 spawnPosition = CalculateSpawnPosition(index);

        // Instantiate ram prefab
        GameObject ramInstance = Instantiate(ramPrefab, spawnPosition, Quaternion.identity);

        // Set parent to the spawn point based on the index distribution
        Transform parentTransform = transform; // Default fallback
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Determine which spawn point this ram should belong to
            int spawnPointIndex = index % spawnPoints.Length;
            Transform selectedSpawnPoint = spawnPoints[spawnPointIndex];
            
            if (selectedSpawnPoint != null)
            {
                parentTransform = selectedSpawnPoint;
            }
            else
            {
                // Find first valid spawn point as fallback
                parentTransform = spawnPoints.FirstOrDefault(sp => sp != null) ?? transform;
            }
        }
        ramInstance.transform.SetParent(parentTransform);

        // Note: Scaling will be applied AFTER pop animation to prevent animation from resetting it

        // Find and set username and coins text using specific component names
        var allTexts = ramInstance.GetComponentsInChildren<UnityEngine.UI.Text>();
        
        // Get current user ID to check if this ram is for the current player
        string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        bool isCurrentPlayer = !string.IsNullOrEmpty(currentUserId) && user.userId == currentUserId;
        
        // Find the "name" text component
        var nameText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
        if (nameText != null)
        {
            // Display "Me" if this is the current player, otherwise show username
            string displayName = isCurrentPlayer ? "just me" : user.name;
            var building = " (" + user.currentBuilding + ")";
            nameText.text = displayName + (isCurrentPlayer ? "" : building);
        }
        else
        {
            Debug.LogWarning($"No 'name' Text component found in ram prefab for user {user.name}");
        }
        
        // Find the "coins" text component and display knowledge points
        var coinsText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("coins"));
        if (coinsText != null)
        {
            coinsText.text = $"{user.knowledgePoints}";
        }
        else
        {
            Debug.LogWarning($"No 'coins' Text component found in ram prefab for user {user.name}");
        }

        // Fallback: Try TMPro text components if UI Text not found
        if (nameText == null)
        {
            var tmpTexts = ramInstance.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            var tmpNameText = tmpTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
            if (tmpNameText != null)
            {
                // Display "Me" if this is the current player, otherwise show username
                string displayName = isCurrentPlayer ? "Me" : (user.name ?? "Unknown");
                tmpNameText.text = displayName;
            }
        }

        // Add to spawned rams list
        spawnedRams.Add(ramInstance);

        // Setup click handling using ButtonHandler
        SetupRamClickHandler(ramInstance, user);

        // Apply user's equipped skin to the RAM
        ApplyUserSkinToRam(ramInstance, user);

        // Play spawn sound
        PlaySpawnSound();

        // Add pop animation using UIManager, then apply scaling after animation
        if (UIManager.Instance != null)
        {
            StartCoroutine(ApplyScaleAfterPopAnimation(ramInstance, user));
        }
        else
        {
            // No UIManager, apply scale and unique color directly
            float scale = CalculateRamScaleByRank(user);
            ramInstance.transform.localScale = Vector3.one * scale;
            ApplyUniqueColorToRamText(ramInstance, user);
            Debug.Log($"Applied scale {scale:F2}x and unique color directly (no UIManager)");
        }
        
        // Start random backflip animations for this ram
        StartCoroutine(RandomBackflipAnimation(ramInstance));
        
        Debug.Log($"Spawned ram for user: {user.name} at position {spawnPosition}");
    }

    private void OnRamClicked(User user)
    {
        Debug.Log($"Ram clicked for user: {user.name}");
        
        // Try assigned reference first, then singleton, then find in scene for UserInfoPanel
        // UserInfoPanel panel = userInfoPanel;
        // if (panel == null)
        // {
        //     panel = UserInfoPanel.Instance;
        // }
        // if (panel == null)
        // {
        //     panel = FindObjectOfType<UserInfoPanel>();
        // }
        
        // // Show user info panel
        // if (panel != null)
        // {
        //     panel.ShowUserInfo(user);
        // }
        // else
        // {
        //     Debug.LogWarning("RamsManager: No UserInfoPanel found in scene!");
        // }
        
        // Start chat with the clicked user
        ChatManager chat = chatManager;
        if (chat == null)
        {
            chat = FindObjectOfType<ChatManager>();
        }
        
        if (chat != null)
        {
            chat.StartChatWithUser(user);
            Debug.Log($"Started chat with user: {user.name}");
        }
        else
        {
            Debug.LogWarning("RamsManager: No ChatManager found in scene!");
        }
    }
    
    /// <summary>
    /// Sets up click handling using RamClickHandler component
    /// </summary>
    private void SetupRamClickHandler(GameObject ramInstance, User user)
    {
        // Don't add click handler for current player's ram
        string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(currentUserId) && user.userId == currentUserId)
        {
            Debug.Log($"Skipping click handler setup for current player's ram: {user.name}");
            return;
        }
        
        // Add RamClickHandler component
        RamClickHandler ramClickHandler = ramInstance.AddComponent<RamClickHandler>();
        
        // Initialize RamClickHandler with user and manager reference
        ramClickHandler.Initialize(user, this);
        
        Debug.Log($"RamClickHandler setup for ram: {user.name}");
    }
    
    /// <summary>
    /// Applies the user's equipped skin to the spawned RAM using SkinManager
    /// </summary>
    /// <param name="ramInstance">The spawned RAM GameObject</param>
    /// <param name="user">The user data containing equipped skin information</param>
    private void ApplyUserSkinToRam(GameObject ramInstance, User user)
    {
        try
        {
            // Find the Animator component on the RAM
            var ramAnimator = ramInstance.GetComponent<Animator>();
            if (ramAnimator == null)
            {
                Debug.LogWarning($"No Animator component found on RAM for user {user.name} - cannot apply skin");
                return;
            }
            
            // Find SkinManager in the scene
            var skinManager = FindObjectOfType<SkinManager>();
            if (skinManager == null)
            {
                Debug.LogWarning("SkinManager not found in scene - cannot apply user skin to RAM");
                return;
            }
            
            // Get the appropriate animator controller for the user's equipped skin
            RuntimeAnimatorController skinAnimator = skinManager.GetAnimatorForSkin(user.equippedSkin);
            if (skinAnimator != null)
            {
                // Apply the skin's animator controller to the RAM
                ramAnimator.runtimeAnimatorController = skinAnimator;
                Debug.Log($"Applied {user.equippedSkin} skin to RAM for user {user.name}");
            }
            else
            {
                Debug.LogWarning($"No animator controller found for skin {user.equippedSkin} - RAM will use default appearance");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to apply skin to RAM for user {user.name}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Called by RamClickHandler when a ram is clicked
    /// </summary>
    public void HandleRamClick(User user, GameObject ramInstance)
    {
        // Trigger backflip animation
        TriggerRamBackflip(ramInstance);
        
        // Play backflip sound
        PlayBackflipSound();
        
        // Continue with existing click behavior
        OnRamClicked(user);
    }

    /// <summary>
    /// Triggers backflip animation on a specific ram
    /// </summary>
    private void TriggerRamBackflip(GameObject ramInstance)
    {
        if (ramInstance == null) return;
        
        Animator animator = ramInstance.GetComponent<Animator>();
        if (animator != null)
        {
            // Set backflip bool to true
            animator.SetBool("backflip", true);
            Debug.Log($"Triggered click backflip for ram: {ramInstance.name}");
            
            // Reset backflip bool after animation completes
            StartCoroutine(ResumeMovementAfterBackflip(ramInstance, animator));
        }
        else
        {
            Debug.LogWarning("No Animator found on clicked ram - cannot trigger backflip");
        }
    }

    /// <summary>
    /// Plays the backflip sound effect
    /// </summary>
    private void PlayBackflipSound()
    {
        if (backflipClip != null && building != null)
        {
            var buildingAudioSource = building.GetComponent<AudioSource>();
            if (buildingAudioSource != null)
            {
                buildingAudioSource.PlayOneShot(backflipClip);
                Debug.Log("Played backflip sound");
            }
            else
            {
                Debug.LogWarning("No AudioSource found on building - cannot play backflip sound");
            }
        }
        else
        {
            Debug.LogWarning("Backflip clip not assigned or building reference missing");
        }
    }
    
    /// <summary>
    /// Plays the spawn sound effect using the building's audio source
    /// </summary>
    private void PlaySpawnSound()
    {
        if (spawnSound != null && building != null)
        {
            // Use the building's audio source to play the spawn sound
            var buildingAudioSource = building.GetComponent<AudioSource>();
            if (buildingAudioSource != null)
            {
                buildingAudioSource.PlayOneShot(spawnSound);
            }
            else
            {
                Debug.LogWarning("No AudioSource found on building for spawn sound!");
            }
        }
        else if (spawnSound == null)
        {
            Debug.LogWarning("Spawn sound not assigned in RamsManager!");
        }
    }
    
    /// <summary>
    /// Plays the despawn sound effect using the building's audio source
    /// </summary>
    private void PlayDespawnSound()
    {
        if (despawnSound != null && building != null)
        {
            // Use the building's audio source to play the despawn sound
            var buildingAudioSource = building.GetComponent<AudioSource>();
            if (buildingAudioSource != null)
            {
                buildingAudioSource.PlayOneShot(despawnSound);
            }
            else
            {
                Debug.LogWarning("No AudioSource found on building for despawn sound!");
            }
        }
        else if (despawnSound == null)
        {
            Debug.LogWarning("Despawn sound not assigned in RamsManager!");
        }
    }
    
    /// <summary>
    /// Gets a unique color for a specific user
    /// </summary>
    private Color GetUniqueColorForUser(string userId)
    {
        // Check if user already has an assigned color
        if (userColorAssignments.ContainsKey(userId))
        {
            return userColorAssignments[userId];
        }
        
        // Assign next available color
        if (availableColors.Count == 0)
        {
            InitializeVisibleColors(); // Re-initialize if we run out
        }
        
        Color assignedColor = availableColors[colorIndex % availableColors.Count];
        userColorAssignments[userId] = assignedColor;
        
        colorIndex++;
        Debug.Log($"Assigned unique color {assignedColor} to user {userId}");
        
        return assignedColor;
    }
    
    /// <summary>
    /// Applies unique color to the RAM's name and knowledge points text components based on user
    /// </summary>
    private void ApplyUniqueColorToRamText(GameObject ramInstance, User user)
    {
        Color userColor = GetUniqueColorForUser(user.userId);
        
        // Find and color both name and coins text components
        var allTexts = ramInstance.GetComponentsInChildren<UnityEngine.UI.Text>();
        var nameText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
        var coinsText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("coins"));
        
        // Apply color to name text
        if (nameText != null)
        {
            nameText.color = userColor;
            Debug.Log($"Applied unique color {userColor} to RAM name text for user {user.name}");
        }
        
        // Apply same color to knowledge points text
        if (coinsText != null)
        {
            coinsText.color = userColor;
            Debug.Log($"Applied unique color {userColor} to RAM knowledge points text for user {user.name}");
        }
        
        // Fallback: Try TextMeshPro if regular Text components not found
        if (nameText == null || coinsText == null)
        {
            var tmpTexts = ramInstance.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            
            if (nameText == null)
            {
                var tmpNameText = tmpTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
                if (tmpNameText != null)
                {
                    tmpNameText.color = userColor;
                    Debug.Log($"Applied unique color {userColor} to RAM TMPro name text for user {user.name}");
                }
            }
            
            if (coinsText == null)
            {
                var tmpCoinsText = tmpTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("coins"));
                if (tmpCoinsText != null)
                {
                    tmpCoinsText.color = userColor;
                    Debug.Log($"Applied unique color {userColor} to RAM TMPro knowledge points text for user {user.name}");
                }
            }
            
            if (nameText == null && coinsText == null)
            {
                Debug.LogWarning($"No name or knowledge points text components found to apply color to RAM for user {user.name}");
            }
        }
    }
    
    /// <summary>
    /// Calculates ram scale based on user rank from UserService (Small/Medium/Large categories)
    /// </summary>
    private float CalculateRamScaleByRank(User user)
    {
        // Get user rank directly from UserService
        int userRank = userService.CalculateUserRank(user.coins, user.knowledgePoints);
        
        // Define scales for each rank
        const float smallScale = 1f;     // Small size for rank 1
        const float mediumScale = 1.5f;    // Medium size for rank 2
        const float largeScale = 2f;     // Large size for rank 3
        
        switch (userRank)
        {
            case 1:
                return smallScale;
            case 2:
                return mediumScale;
            case 3:
                return largeScale;
            default:
                return smallScale;
        }
    }
    
    /// <summary>
    /// Calculates ram scale based on knowledge points (1x to 2x) - Legacy method for compatibility
    /// </summary>
    private float CalculateRamScale(int knowledgePoints)
    {
        const int minKnowledgePoints = 0;
        const int maxKnowledgePoints = 1500;
        const float minScale = 0.5f;
        const float maxScale = 2.0f;
        
        int clampedKP = Mathf.Clamp(knowledgePoints, minKnowledgePoints, maxKnowledgePoints);
        float normalizedKP = (float)(clampedKP - minKnowledgePoints) / (maxKnowledgePoints - minKnowledgePoints);
        return Mathf.Lerp(minScale, maxScale, normalizedKP);
    }
    
    /// <summary>
    /// Calculates spawn position distributed across all spawn points in circles
    /// </summary>
    private Vector3 CalculateSpawnPosition(int index)
    {
        // If no spawn points, use this transform
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Vector3 fallbackPosition = transform.position;
            float fallbackAngle = (360f / maxRams) * index * Mathf.Deg2Rad;
            float fallbackX = fallbackPosition.x + Mathf.Cos(fallbackAngle) * spawnRadius;
            float fallbackZ = fallbackPosition.z + Mathf.Sin(fallbackAngle) * spawnRadius;
            return new Vector3(fallbackX, fallbackPosition.y, fallbackZ);
        }
        
        // Distribute rams across all spawn points
        int spawnPointIndex = index % spawnPoints.Length;
        Transform selectedSpawnPoint = spawnPoints[spawnPointIndex];
        
        // If selected spawn point is null, use first valid one or fallback
        if (selectedSpawnPoint == null)
        {
            selectedSpawnPoint = spawnPoints.FirstOrDefault(sp => sp != null) ?? transform;
        }
        
        Vector3 basePosition = selectedSpawnPoint.position;
        
        // Calculate which "ring" this ram is in around this spawn point
        int ramsPerSpawnPoint = Mathf.CeilToInt((float)maxRams / spawnPoints.Length);
        int localIndex = index / spawnPoints.Length; // Which position around this specific spawn point
        
        // Spread rams in a circle around the selected spawn point
        float angle = (360f / ramsPerSpawnPoint) * localIndex * Mathf.Deg2Rad;
        float x = basePosition.x + Mathf.Cos(angle) * spawnRadius;
        float z = basePosition.z + Mathf.Sin(angle) * spawnRadius;
        
        return new Vector3(x, basePosition.y, z);
    }
    
    /// <summary>
    /// Clears all currently spawned rams
    /// </summary>
    private void ClearSpawnedRams()
    {
        foreach (var ram in spawnedRams)
        {
            if (ram != null)
            {
                Destroy(ram);
            }
        }
        spawnedRams.Clear();
        spawnedUserIds.Clear();
        
        // Clear color assignments to allow fresh unique colors
        userColorAssignments.Clear();
        colorIndex = 0;
        Debug.Log("Cleared all spawned rams and reset color assignments");
    }
    
    /// <summary>
    /// Scales a ram based on knowledge points (1x to 2x scale)
    /// </summary>
    /// <param name="ramInstance">The ram GameObject to scale</param>
    /// <param name="knowledgePoints">The user's knowledge points</param>
    private void ScaleRamByKnowledgePoints(GameObject ramInstance, int knowledgePoints)
    {
        // Define scaling parameters
        const int minKnowledgePoints = 0;    // Minimum knowledge points (1x scale)
        const int maxKnowledgePoints = 1000; // Knowledge points for 2x scale (adjust as needed)
        const float minScale = 1.0f;         // Minimum scale (1x)
        const float maxScale = 2.0f;         // Maximum scale (2x)
        
        // Calculate scale based on knowledge points
        // Clamp knowledge points to our range
        int clampedKP = Mathf.Clamp(knowledgePoints, minKnowledgePoints, maxKnowledgePoints);
        
        // Calculate scale factor using linear interpolation
        float normalizedKP = (float)(clampedKP - minKnowledgePoints) / (maxKnowledgePoints - minKnowledgePoints);
        float scaleMultiplier = Mathf.Lerp(minScale, maxScale, normalizedKP);
        
        // Apply scale to the ram
        ramInstance.transform.localScale = Vector3.one * scaleMultiplier;
        
        Debug.Log($"Scaled ram for user with {knowledgePoints} KB to {scaleMultiplier:F2}x scale");
    }

    
    /// <summary>
    /// Coroutine that runs pop animation first, then applies knowledge-based scaling and random coloring
    /// </summary>
    private IEnumerator ApplyScaleAfterPopAnimation(GameObject ramInstance, User user)
    {
        // Start the pop animation
        yield return StartCoroutine(UIManager.Instance.AnimatePanelPopup(ramInstance));
        
        // Animation is complete, now apply our rank-based scaling and unique coloring
        float scale = CalculateRamScaleByRank(user);
        ramInstance.transform.localScale = Vector3.one * scale;
        
        // Apply unique color to the ram's name text based on user
        ApplyUniqueColorToRamText(ramInstance, user);
        
        int userRank = userService.CalculateUserRank(user.coins, user.knowledgePoints);
        Debug.Log($"Applied rank-based scale {scale:F2}x and unique color AFTER pop animation for user {user.name} (Rank {userRank})");
    }
    
    /// <summary>
    /// Display player count in the building if non-zero and building is active
    /// </summary>
    private async Task DisplayPlayerCount()
    {
        try
        {
            // Check if we're in the Terminal game stage - only display player count during Terminal stage
            if (requireTerminalStage)
            {
                var currentStage = GameStageService.LoadStageFromPrefs();
                if (currentStage == null || currentStage.area != Stage.Terminal)
                {
                    Debug.Log($"RamsManager: Not in Terminal stage (current: {currentStage?.area}), hiding player count");
                    // Hide the canvas when not in Terminal stage - use coroutine to ensure main thread
                    StartCoroutine(HidePlayerCountCanvas());
                    return;
                }
            }
            
            // Get the current player count in this building (this runs on background thread)
            var playersInBuilding = await userService.GetUsersInBuilding(building.buildingName);
            int playerCount = playersInBuilding?.Count ?? 0;
            
            // Use coroutine to ensure UI updates happen on main thread
            StartCoroutine(UpdatePlayerCountCoroutine(playerCount));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error displaying player count: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Coroutine to hide player count canvas on main thread
    /// </summary>
    private IEnumerator HidePlayerCountCanvas()
    {
        if (playerCountCanvasInstance != null)
        {
            playerCountCanvasInstance.SetActive(false);
        }
        yield return null;
    }
    
    /// <summary>
    /// Coroutine to update player count display on main thread
    /// </summary>
    private IEnumerator UpdatePlayerCountCoroutine(int count)
    {
        try
        {
            if (count > 0)
            {
                // Create the canvas if it doesn't exist
                if (playerCountCanvasInstance == null && playerCountCanvasPrefab != null)
                {
                    // Use spawn point if assigned, otherwise use this transform
                    Transform spawnParentTransform = playerCountSpawnPoint != null ? playerCountSpawnPoint : transform;
                    playerCountCanvasInstance = Instantiate(playerCountCanvasPrefab, spawnParentTransform);
                    Debug.Log($"Created player count canvas for building {building.buildingName} at spawn point");
                }
                
                // Update the count display and show it
                UpdatePlayerCountDisplay(count);
            }
            else
            {
                // Hide the canvas when count is zero
                if (playerCountCanvasInstance != null)
                {
                    playerCountCanvasInstance.SetActive(false);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error updating player count display in coroutine: {ex.Message}");
        }
        
        yield return null;
    }
    
    /// <summary>
    /// Update the player count display text
    /// </summary>
    private void UpdatePlayerCountDisplay(int count)
    {
        if (playerCountCanvasInstance == null) return;
        
        // Find the Text component in the canvas (nested)
        var countText = playerCountCanvasInstance.GetComponentInChildren<UnityEngine.UI.Text>();
        
        if (countText == null)
        {
            // Try TextMeshPro if regular Text component not found
            var tmpText = playerCountCanvasInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = count.ToString();
                playerCountCanvasInstance.SetActive(true);
                Debug.Log($"Updated TMPro player count display to: {count}");
                return;
            }
            
            Debug.LogWarning("No Text or TextMeshPro component found in player count canvas");
            return;
        }
        
        // Update the text and make sure canvas is active
        countText.text = count.ToString();
        playerCountCanvasInstance.SetActive(true);
        
        // Start beating animation to attract attention
        StartCoroutine(BeatingAnimation());
        
        Debug.Log($"Updated player count display to: {count}");
    }
    
    /// <summary>
    /// Simple beating animation for player count canvas
    /// </summary>
    private IEnumerator BeatingAnimation()
    {
        if (playerCountCanvasInstance == null) yield break;
        while (playerCountCanvasInstance.activeInHierarchy)
        {
            playerCountCanvasInstance.transform.localScale = Vector3.one * 1.2f;
            yield return new WaitForSeconds(1.5f);
            playerCountCanvasInstance.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(1.5f);
        }
    }
    
    void Update()
    {
        
    }
    
    /// <summary>
    /// Public method to reset building activity notification flag - useful for testing
    /// </summary>
    public static void ResetBuildingActivityNotification()
    {
        hasNotifiedBuildingActivity = false;
        Debug.Log("RamsManager: Building activity notification flag reset - will allow showing notifications again");
    }
    
    private void OnDestroy()
    {
        // Clean up the refresh coroutine when the object is destroyed
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
        
        // Clean up the building monitor coroutine
        if (buildingMonitorCoroutine != null)
        {
            StopCoroutine(buildingMonitorCoroutine);
            buildingMonitorCoroutine = null;
        }
        
        // Clean up player count canvas
        if (playerCountCanvasInstance != null)
        {
            Destroy(playerCountCanvasInstance);
            playerCountCanvasInstance = null;
        }
        
        ClearSpawnedRams();
    }
    
    /// <summary>
    /// Setup physics collision for a spawned ram
    /// </summary>
    private void SetupRamPhysics(GameObject ramInstance)
    {
        // Add collider for collision detection
        SphereCollider ramCollider = ramInstance.GetComponent<SphereCollider>();
        if (ramCollider == null)
        {
            ramCollider = ramInstance.AddComponent<SphereCollider>();
        }
        
        // Configure collider
        ramCollider.radius = 1f;
        ramCollider.isTrigger = false; // Physical collision
        
        // Add Rigidbody for physics
        Rigidbody ramRigidbody = ramInstance.GetComponent<Rigidbody>();
        if (ramRigidbody == null)
        {
            ramRigidbody = ramInstance.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody to prevent rams from affecting each other too much
        ramRigidbody.mass = 1f;
        ramRigidbody.linearDamping = 5f; // High drag for smooth stopping
        ramRigidbody.freezeRotation = true; // Keep rams upright
        
        // Set physics layer for ram-to-ram interaction
        ramInstance.layer = LayerMask.NameToLayer("Rams"); // Create "Rams" layer
    }
}

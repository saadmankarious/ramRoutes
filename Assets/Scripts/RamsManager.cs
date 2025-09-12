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
    
    [Header("Ram Prefab")]
    [SerializeField] private GameObject ramPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private int maxRams = 10;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip despawnSound;
    
    [Header("User Info Panel")]
    [SerializeField] private UserInfoPanel userInfoPanel;
    
    [Header("Player Count Display")]
    [SerializeField] private GameObject playerCountCanvasPrefab;
    
    private UserService userService;
    private List<GameObject> spawnedRams = new List<GameObject>();
    private HashSet<string> spawnedUserIds = new HashSet<string>(); // Track spawned user IDs
    private GameObject playerCountCanvasInstance; // Instance of the player count canvas
    private bool hasBeenActivated = false; // Prevent double activation
    private Coroutine refreshCoroutine; // Reference to the refresh coroutine
    
    void Start()
    {
        building = GetComponent<BuildingInteraction>();
        userService = new UserService();
        
        // Initialize player count display immediately
        InitializePlayerCountDisplay();
        
        // Rams will only spawn when OnBuildingActivated() is called from BuildingInteraction
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
    /// Called when building gets activated - spawns rams if not already spawned
    /// </summary>
    public async void OnBuildingActivated()
    {
        if (building.activated && !hasBeenActivated)
        {
            // // Check if we're in the Terminal game stage - only activate ram system during Terminal stage
            // var currentStage = GameStageService.LoadStageFromPrefs();
            // if (currentStage == null || currentStage.area != Stage.Terminal)
            // {
            //     Debug.Log($"RamsManager: Not in Terminal stage (current: {currentStage?.area}), skipping ram system activation");
            //     return;
            // }
            
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
                await userService.ClearCurrentUserBuilding();
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
        var clearBuildingTask = userService.ClearCurrentUserBuilding();
        yield return new WaitUntil(() => clearBuildingTask.IsCompleted);
        
        if (clearBuildingTask.Exception != null)
        {
            Debug.LogError($"Failed to clear current building: {clearBuildingTask.Exception.Message}");
        }
        else
        {
            Debug.Log("Successfully cleared current building for user");
        }
    }

    /// <summary>
    /// Spawns rams for users currently in this building
    /// </summary>
    private async Task SpawnRams()
    {
        // Check if we're in the Terminal game stage - only spawn rams during Terminal stage
        // var currentStage = GameStageService.LoadStageFromPrefs();
        // if (currentStage == null || currentStage.area != Stage.Terminal)
        // {
        //     Debug.Log($"RamsManager: Not in Terminal stage (current: {currentStage?.area}), skipping ram spawning");
        //     return;
        // }
        
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

            // Get current player's user ID to exclude them from ram spawning
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

            // Filter out the current player from the users list
            if (!string.IsNullOrEmpty(currentUserId))
            {
                usersInBuilding.RemoveAll(user => user.userId == currentUserId);
                Debug.Log($"Excluded current player ({currentUserId}) from ram spawning");
            }

            Debug.Log($"Found {usersInBuilding.Count} other users in {buildingName} (excluding current player)");

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
            yield return new WaitForSeconds(3f);
            
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
        // var currentStage = GameStageService.LoadStageFromPrefs();
        // if (currentStage == null || currentStage.area != Stage.Terminal)
        // {
        //     Debug.Log($"RamsManager: Not in Terminal stage (current: {currentStage?.area}), skipping new player check");
        //     return;
        // }
        
        string buildingName = building.buildingName;
        if (string.IsNullOrEmpty(buildingName))
        {
            return;
        }
        
        try
        {
            // Get current users in building
            var usersInBuilding = await userService.GetUsersInBuildingWithPoints(buildingName);

            // Get current player's user ID to exclude them
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                usersInBuilding.RemoveAll(user => user.userId == currentUserId);
            }

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
    /// Spawns a single ram for a specific user
    /// </summary>
    private void SpawnRamForUser(User user, int index)
    {
        // Calculate spawn position in a circle around the spawn parent
        Vector3 spawnPosition = CalculateSpawnPosition(index);

        // Instantiate ram prefab
        GameObject ramInstance = Instantiate(ramPrefab, spawnPosition, Quaternion.identity);

        // Set parent if specified
        if (spawnParent != null)
        {
            ramInstance.transform.SetParent(spawnParent);
        }

        // Note: Scaling will be applied AFTER pop animation to prevent animation from resetting it

        // Find and set username and coins text using specific component names
        var allTexts = ramInstance.GetComponentsInChildren<UnityEngine.UI.Text>();
        
        // Find the "name" text component
        var nameText = allTexts.FirstOrDefault(t => t.gameObject.name.ToLower().Contains("name"));
        if (nameText != null)
        {
            nameText.text = user.name + " (" + user.currentBuilding + ")";
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
                tmpNameText.text = user.name ?? "Unknown";
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
            StartCoroutine(ApplyScaleAfterPopAnimation(ramInstance, user.knowledgePoints));
        }
        else
        {
            // No UIManager, apply scale directly
            float scaleMultiplier = CalculateRamScale(user.knowledgePoints);
            ramInstance.transform.localScale = Vector3.one * scaleMultiplier;
            Debug.Log($"Applied scale {scaleMultiplier:F2}x directly (no UIManager)");
        }

        Debug.Log($"Spawned ram for user: {user.name} at position {spawnPosition}");
    }

    private void OnRamClicked(User user)
    {
        Debug.Log($"Ram clicked for user: {user.name}");
        
        // Try assigned reference first, then singleton, then find in scene
        UserInfoPanel panel = userInfoPanel;
        if (panel == null)
        {
            panel = UserInfoPanel.Instance;
        }
        if (panel == null)
        {
            panel = FindObjectOfType<UserInfoPanel>();
        }
        
        // Show user info panel
        if (panel != null)
        {
            panel.ShowUserInfo(user);
        }
        else
        {
            Debug.LogWarning("RamsManager: No UserInfoPanel found in scene!");
        }
    }
    
    /// <summary>
    /// Sets up click handling using RamClickHandler component
    /// </summary>
    private void SetupRamClickHandler(GameObject ramInstance, User user)
    {
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
    public void HandleRamClick(User user)
    {
        OnRamClicked(user);
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
    /// Calculates ram scale based on knowledge points (1x to 2x)
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
    /// Calculates spawn position in a circle pattern
    /// </summary>
    private Vector3 CalculateSpawnPosition(int index)
    {
        Vector3 basePosition = spawnParent != null ? spawnParent.position : transform.position;
        
        // Arrange in circle pattern
        float angle = (360f / maxRams) * index * Mathf.Deg2Rad;
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
    /// Coroutine that runs pop animation first, then applies knowledge-based scaling
    /// </summary>
    private IEnumerator ApplyScaleAfterPopAnimation(GameObject ramInstance, int knowledgePoints)
    {
        // Start the pop animation
        yield return StartCoroutine(UIManager.Instance.AnimatePanelPopup(ramInstance));
        
        // Animation is complete, now apply our knowledge-based scaling
        float scaleMultiplier = CalculateRamScale(knowledgePoints);
        ramInstance.transform.localScale = Vector3.one * scaleMultiplier;
        
        Debug.Log($"Applied scale {scaleMultiplier:F2}x AFTER pop animation for {knowledgePoints} KB");
    }
    
    /// <summary>
    /// Display player count in the building if non-zero and building is active
    /// </summary>
    private async Task DisplayPlayerCount()
    {
        try
        {
            // Only display if building is activated
            // if (!building.activated)
            // {
            //     // Hide canvas if building is not active
            //     if (playerCountCanvasInstance != null)
            //     {
            //         playerCountCanvasInstance.SetActive(false);
            //         Debug.Log($"Hiding player count canvas - building {building.buildingName} is not active");
            //     }
            //     return;
            // }
            
            // Get the current player count in this building
            var playersInBuilding = await userService.GetUsersInBuilding(building.buildingName);
            int playerCount = playersInBuilding?.Count ?? 0;
            
            if (playerCount > 0)
            {
                // Create the canvas if it doesn't exist
                if (playerCountCanvasInstance == null && playerCountCanvasPrefab != null)
                {
                    playerCountCanvasInstance = Instantiate(playerCountCanvasPrefab, transform);
                    Debug.Log($"Created player count canvas for building {building.buildingName}");
                }
                
                // Update the count display and show it
                UpdatePlayerCountDisplay(playerCount);
                Debug.Log($"Displaying player count: {playerCount} in building {building.buildingName}");
            }
            else
            {
                // Hide the canvas when count is zero
                if (playerCountCanvasInstance != null)
                {
                    playerCountCanvasInstance.SetActive(false);
                    Debug.Log($"Hiding player count canvas - zero players in building {building.buildingName}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error displaying player count: {ex.Message}");
        }
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
        
        Debug.Log($"Updated player count display to: {count}");
    }
    
    void Update()
    {
        
    }
    
    private void OnDestroy()
    {
        // Clean up the refresh coroutine when the object is destroyed
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
        
        // Clean up player count canvas
        if (playerCountCanvasInstance != null)
        {
            Destroy(playerCountCanvasInstance);
            playerCountCanvasInstance = null;
        }
        
        ClearSpawnedRams();
    }
}

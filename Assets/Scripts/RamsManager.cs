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
    
    [Header("User Info Panel")]
    [SerializeField] private UserInfoPanel userInfoPanel;
    
    private UserService userService;
    private List<GameObject> spawnedRams = new List<GameObject>();
    private HashSet<string> spawnedUserIds = new HashSet<string>(); // Track spawned user IDs
    private bool hasBeenActivated = false; // Prevent double activation
    private Coroutine refreshCoroutine; // Reference to the refresh coroutine
    
    void Start()
    {
        building = GetComponent<BuildingInteraction>();
        userService = new UserService();
        
        // Rams will only spawn when OnBuildingActivated() is called from BuildingInteraction
    }

    /// <summary>
    /// Called when building gets activated - spawns rams if not already spawned
    /// </summary>
    public async void OnBuildingActivated()
    {
        if (building.activated && !hasBeenActivated)
        {
            hasBeenActivated = true;
            Debug.Log($"Building {building.buildingName} activated, spawning rams");
            await SpawnRams();
            
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

    /// <summary>
    /// Spawns rams for users currently in this building
    /// </summary>
    private async Task SpawnRams()
    {
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
            yield return new WaitForSeconds(5f);
            
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
                RemoveRamsForUsers(usersToRemove);
            }
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
                // Try to find the user ID from the ram's text component
                var usernameText = ram.GetComponentInChildren<UnityEngine.UI.Text>();
                string ramUserName = "";
                if (usernameText != null)
                {
                    ramUserName = usernameText.text.Split('(')[0].Trim(); // Extract name before building info
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

        // Find and set username text
        var usernameText = ramInstance.GetComponentInChildren<UnityEngine.UI.Text>();
        if (usernameText != null)
        {
            usernameText.text = user.name + " (" + user.currentBuilding + ")";
        }
        else
        {
            // Try TMPro text as fallback
            var tmpText = ramInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = user.name ?? "Unknown";
            }
            else
            {
                Debug.LogWarning($"No Text component found in ram prefab for user {user.name}");
            }
        }

        // Try to set user stats if there are additional text components
        var allTexts = ramInstance.GetComponentsInChildren<UnityEngine.UI.Text>();
        if (allTexts.Length > 1)
        {
            // Second text could be for stats
            allTexts[1].text = $"Coins: {user.coins} | KB: {user.knowledgePoints}";
        }

        // Add to spawned rams list
        spawnedRams.Add(ramInstance);

        // Setup click handling using ButtonHandler
        SetupRamClickHandler(ramInstance, user);

        // Play spawn sound
        PlaySpawnSound();

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
        
        ClearSpawnedRams();
    }
}

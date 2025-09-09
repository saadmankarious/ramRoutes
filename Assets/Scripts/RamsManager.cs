using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using RamRoutes.Services;
using RamRoutes.Model;

public class RamsManager : MonoBehaviour
{
 private string buildingName;
    
    [Header("Ram Prefab")]
    [SerializeField] private GameObject ramPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private int maxRams = 10;
    
    private UserService userService;
    private List<GameObject> spawnedRams = new List<GameObject>();
    
    void Start()
    {
        buildingName = GetComponent<BuildingInteraction>()?.buildingName;
        userService = new UserService();
        _ = SpawnRams();
    }

    /// <summary>
    /// Spawns rams for users currently in this building
    /// </summary>
    private async Task SpawnRams()
    {
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
            
            Debug.Log($"Found {usersInBuilding.Count} users in {buildingName}");
            
            // Clear existing rams
            ClearSpawnedRams();
            
            // Spawn rams for each user (up to max limit)
            int spawnCount = Mathf.Min(usersInBuilding.Count, maxRams);
            for (int i = 0; i < spawnCount; i++)
            {
                var user = usersInBuilding[i];
                SpawnRamForUser(user, i);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to spawn rams: {e.Message}");
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
            usernameText.text = user.name ?? "Unknown";
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
        
        Debug.Log($"Spawned ram for user: {user.name} at position {spawnPosition}");
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
    }
    
    /// <summary>
    /// Refreshes the spawned rams (useful for updates)
    /// </summary>
    public async void RefreshRams()
    {
        await SpawnRams();
    }
    
    /// <summary>
    /// Sets the building name for this manager
    /// </summary>
    public void SetBuildingName(string building)
    {
        buildingName = building;
    }

    void Update()
    {
        
    }
    
    void OnDestroy()
    {
        ClearSpawnedRams();
    }
}

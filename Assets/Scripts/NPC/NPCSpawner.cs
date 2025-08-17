using UnityEngine;
using System.Collections.Generic;
using RamRoutes.Services;
using Firebase.Auth;

[System.Serializable]
public class BuildingNPC
{
    [Header("NPC Info")]
    public string npcName;
    public GameObject npcPrefab;
    public string associatedBuilding; // Building name this NPC belongs to
    
    [Header("Spawn Settings")]
    public Transform spawnPoint; // Editor-assigned spawn point instead of offset
    public Vector3 spawnOffset = Vector3.zero; // Fallback offset if no spawn point assigned
    public float spawnDelay = 1f; // Delay before spawning after building unlock
    public float despawnDelay = 2f; // Delay before despawning after player walks away
    
    [Header("Conversation")]
    [TextArea(3, 10)]
    public string[] conversationLines = {
        "Hello! Welcome to this building!",
        "I'm so glad you unlocked this place!",
        "Feel free to explore around."
    };
}

public class NPCSpawner : MonoBehaviour
{
    [Header("NPC Configuration")]
    public BuildingNPC[] buildingNPCs;
    
    [Header("Spawn Settings")]
    public bool spawnOnStart = false; // For testing - spawn all NPCs immediately
    
    private Dictionary<string, GameObject> spawnedNPCs = new Dictionary<string, GameObject>();
    private UIManager uiManager;
    
    public static NPCSpawner Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        uiManager = UIManager.Instance;
        
        // Subscribe to building unlock events
        if (uiManager != null)
        {
            uiManager.OnBuildingUnlocked.AddListener(HandleBuildingUnlocked);
        }
        
        // For testing - spawn all NPCs if enabled
        if (spawnOnStart)
        {
            SpawnAllNPCs();
        }
        else
        {
            // Check for already unlocked buildings and spawn their NPCs
            StartCoroutine(SpawnNPCsForUnlockedBuildings());
        }
    }
    
    private System.Collections.IEnumerator SpawnNPCsForUnlockedBuildings()
    {
        // Wait a frame to ensure all systems are initialized
        yield return new WaitForEndOfFrame();
        
        var service = new RamRoutes.Services.UnlockedBuildingService();
        var task = service.RetrieveUnlockedBuildings();
        
        // Wait for the task to complete
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        if (task.Result != null)
        {
            var userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "unknown";
            
            foreach (var unlockedBuilding in task.Result)
            {
                if (unlockedBuilding.userId == userId)
                {
                    yield return StartCoroutine(SpawnNPCForBuilding(unlockedBuilding.buildingName));
                }
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (uiManager != null)
        {
            uiManager.OnBuildingUnlocked.RemoveListener(HandleBuildingUnlocked);
        }
    }
    
    public void HandleBuildingUnlocked(BuildingInteraction building)
    {
        StartCoroutine(SpawnNPCForBuilding(building.buildingName));
    }
    
    public System.Collections.IEnumerator SpawnNPCForBuilding(string buildingName)
    {
        // Find NPCs associated with this building
        foreach (var buildingNPC in buildingNPCs)
        {
            if (buildingNPC.associatedBuilding == buildingName && !spawnedNPCs.ContainsKey(buildingNPC.npcName))
            {
                // Wait for spawn delay
                yield return new WaitForSeconds(buildingNPC.spawnDelay);
                
                // Find the building GameObject
                BuildingInteraction[] buildings = FindObjectsOfType<BuildingInteraction>();
                BuildingInteraction targetBuilding = null;
                
                foreach (var b in buildings)
                {
                    if (b.buildingName == buildingName)
                    {
                        targetBuilding = b;
                        break;
                    }
                }
                
                if (targetBuilding != null && buildingNPC.npcPrefab != null)
                {
                    // Determine spawn position - prefer spawn point, fallback to building + offset
                    Vector3 spawnPosition;
                    if (buildingNPC.spawnPoint != null)
                    {
                        spawnPosition = buildingNPC.spawnPoint.position;
                    }
                    else
                    {
                        spawnPosition = targetBuilding.transform.position + buildingNPC.spawnOffset;
                    }
                    
                    GameObject npcInstance = Instantiate(buildingNPC.npcPrefab, spawnPosition, Quaternion.identity);
                    
                    // Configure NPC with conversation data and building association
                    NpcAutoMovement npcMovement = npcInstance.GetComponent<NpcAutoMovement>();
                    if (npcMovement != null)
                    {
                        npcMovement.conversationLines = buildingNPC.conversationLines;
                        npcMovement.associatedBuilding = buildingName;
                        npcMovement.stayNearBuilding = true;
                        npcMovement.spawnPoint = spawnPosition; // Store spawn position for returning
                        npcMovement.despawnDelay = buildingNPC.despawnDelay; // Configure despawn delay
                        npcMovement.npcSpawner = this; // Reference to spawner for despawn callback
                        
                        // Set the NPC's name for identification
                        npcInstance.name = $"{buildingNPC.npcName} (Building: {buildingName})";
                    }
                    
                    // Store spawned NPC
                    spawnedNPCs[buildingNPC.npcName] = npcInstance;
                    
                    // Add some spawn effect if UIManager has teleport effects
                    if (uiManager != null && uiManager.teleportEffect != null)
                    {
                        StartCoroutine(uiManager.PlayTeleportEffect(spawnPosition));
                    }
                    
                    Debug.Log($"Spawned NPC '{buildingNPC.npcName}' for building '{buildingName}' at position {spawnPosition}");
                }
                else
                {
                    Debug.LogWarning($"Could not spawn NPC '{buildingNPC.npcName}' - building '{buildingName}' not found or NPC prefab is null");
                }
            }
        }
    }
    
    // Method to spawn all NPCs (for testing or loading saved game state)
    public void SpawnAllNPCs()
    {
        foreach (var buildingNPC in buildingNPCs)
        {
            if (!spawnedNPCs.ContainsKey(buildingNPC.npcName))
            {
                StartCoroutine(SpawnNPCForBuilding(buildingNPC.associatedBuilding));
            }
        }
    }
    
    // Method to check if NPC has been spawned
    public bool IsNPCSpawned(string npcName)
    {
        return spawnedNPCs.ContainsKey(npcName);
    }
    
    // Method to get spawned NPC GameObject
    public GameObject GetSpawnedNPC(string npcName)
    {
        return spawnedNPCs.ContainsKey(npcName) ? spawnedNPCs[npcName] : null;
    }
    
    // Method to manually spawn NPC (for specific use cases)
    public void ManuallySpawnNPC(string buildingName)
    {
        StartCoroutine(SpawnNPCForBuilding(buildingName));
    }
    
    // Method to despawn NPC
    public void DespawnNPC(string npcName)
    {
        if (spawnedNPCs.ContainsKey(npcName))
        {
            GameObject npcToDestroy = spawnedNPCs[npcName];
            if (npcToDestroy != null)
            {
                Destroy(npcToDestroy);
            }
            spawnedNPCs.Remove(npcName);
            Debug.Log($"Despawned NPC '{npcName}'");
        }
    }
    
    // Method to despawn all NPCs for a building
    public void DespawnNPCsForBuilding(string buildingName)
    {
        List<string> npcsToRemove = new List<string>();
        
        foreach (var buildingNPC in buildingNPCs)
        {
            if (buildingNPC.associatedBuilding == buildingName && spawnedNPCs.ContainsKey(buildingNPC.npcName))
            {
                npcsToRemove.Add(buildingNPC.npcName);
            }
        }
        
        foreach (string npcName in npcsToRemove)
        {
            DespawnNPC(npcName);
        }
    }
    
    // Method to handle NPC despawn (called by NPC itself)
    public void OnNPCDespawned(string npcName)
    {
        if (spawnedNPCs.ContainsKey(npcName))
        {
            spawnedNPCs.Remove(npcName);
            Debug.Log($"NPC '{npcName}' removed from spawner tracking");
        }
    }

    // Method to get all NPCs for a specific building
    public BuildingNPC[] GetNPCsForBuilding(string buildingName)
    {
        List<BuildingNPC> npcsForBuilding = new List<BuildingNPC>();
        
        foreach (var buildingNPC in buildingNPCs)
        {
            if (buildingNPC.associatedBuilding == buildingName)
            {
                npcsForBuilding.Add(buildingNPC);
            }
        }
        
        return npcsForBuilding.ToArray();
    }
}

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
    public Transform spawnPoint; // Editor-assigned spawn point
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
        Debug.Log($"NPCSpawner: Starting up with {buildingNPCs?.Length ?? 0} NPCs configured");
    }
    
    public void SpawnNPCForBuildingOnEnter(string buildingName)
    {
        Debug.Log($"NPCSpawner: Player entered building '{buildingName}', spawning NPCs");
        
        // Find NPCs associated with this building
        foreach (var buildingNPC in buildingNPCs)
        {
            if (buildingNPC.associatedBuilding == buildingName && !spawnedNPCs.ContainsKey(buildingNPC.npcName))
            {
                Debug.Log($"NPCSpawner: Found matching NPC '{buildingNPC.npcName}', spawning now");
                
                if (buildingNPC.npcPrefab != null && buildingNPC.spawnPoint != null)
                {
                    Vector3 spawnPosition = buildingNPC.spawnPoint.position;
                    Debug.Log($"NPCSpawner: Spawning at position {spawnPosition}");
                    
                    GameObject npcInstance = Instantiate(buildingNPC.npcPrefab, spawnPosition, Quaternion.identity);
                    
                    // Configure NPC with building association
                    NpcAutoMovement npcMovement = npcInstance.GetComponent<NpcAutoMovement>();
                    if (npcMovement != null)
                    {
                        npcMovement.associatedBuilding = buildingName;
                        npcMovement.stayNearBuilding = true;
                        npcMovement.spawnPoint = buildingNPC.spawnPoint;
                        npcMovement.npcSpawner = this;
                        
                        // Set the NPC's name for identification
                        npcInstance.name = $"{buildingNPC.npcName} (Building: {buildingName})";
                    }
                    
                    // Store spawned NPC
                    spawnedNPCs[buildingNPC.npcName] = npcInstance;
                    
                    Debug.Log($"NPCSpawner: Spawned NPC '{buildingNPC.npcName}' for building '{buildingName}' at position {spawnPosition}");
                    Debug.Log($"NPCSpawner: Current spawned NPCs count: {spawnedNPCs.Count}");
                }
                else
                {
                    if (buildingNPC.npcPrefab == null)
                        Debug.LogWarning($"Could not spawn NPC '{buildingNPC.npcName}' - NPC prefab is null");
                    if (buildingNPC.spawnPoint == null)
                        Debug.LogWarning($"Could not spawn NPC '{buildingNPC.npcName}' - spawn point is null");
                }
            }
            else if (spawnedNPCs.ContainsKey(buildingNPC.npcName))
            {
                Debug.Log($"NPCSpawner: NPC '{buildingNPC.npcName}' already spawned, skipping");
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
                SpawnNPCForBuildingOnEnter(buildingNPC.associatedBuilding);
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
        SpawnNPCForBuildingOnEnter(buildingName);
    }
    
    // Method to despawn NPC
    public void DespawnNPC(string npcName)
    {
        Debug.Log($"NPCSpawner: DespawnNPC called for '{npcName}'");
        
        if (spawnedNPCs.ContainsKey(npcName))
        {
            GameObject npcToDestroy = spawnedNPCs[npcName];
            if (npcToDestroy != null)
            {
                Debug.Log($"NPCSpawner: Destroying NPC GameObject '{npcName}'");
                Destroy(npcToDestroy);
            }
            else
            {
                Debug.LogWarning($"NPCSpawner: NPC GameObject '{npcName}' was null when trying to destroy");
            }
            spawnedNPCs.Remove(npcName);
            Debug.Log($"NPCSpawner: Successfully despawned NPC '{npcName}'");
        }
        else
        {
            Debug.LogWarning($"NPCSpawner: Cannot find NPC '{npcName}' in spawned NPCs dictionary");
        }
    }
    
    // Method to despawn all NPCs for a building
    public void DespawnNPCsForBuilding(string buildingName)
    {
        Debug.Log($"NPCSpawner: DespawnNPCsForBuilding called for building '{buildingName}'");
        
        List<string> npcsToRemove = new List<string>();
        
        foreach (var buildingNPC in buildingNPCs)
        {
            if (buildingNPC.associatedBuilding == buildingName && spawnedNPCs.ContainsKey(buildingNPC.npcName))
            {
                Debug.Log($"NPCSpawner: Found NPC '{buildingNPC.npcName}' to remove for building '{buildingName}'");
                npcsToRemove.Add(buildingNPC.npcName);
            }
        }
        
        Debug.Log($"NPCSpawner: Found {npcsToRemove.Count} NPCs to despawn for building '{buildingName}'");
        
        foreach (string npcName in npcsToRemove)
        {
            Debug.Log($"NPCSpawner: Despawning NPC '{npcName}'");
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

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using RamRoutes.Services;
using RamRoutes.Model;
using Firebase.Auth;

[System.Serializable]
public class BuildingNPC
{
    [Header("NPC Info")]
    public string npcName;
    public GameObject npcPrefab;
    public string associatedBuilding; // Building name this NPC belongs to
    
    [Header("Building Unlock Dialog (Auto-populated from prefab if empty)")]
    public Sprite npcImage;           // NPC image - auto-read from prefab's SpriteRenderer if null
    public string npcTitle;           // NPC title - auto-read from prefab's NpcAutoMovement.npcName if empty
    
    [Header("Spawn Settings")]
    public Transform spawnPoint; // Editor-assigned spawn point
}

public class NPCSpawner : MonoBehaviour
{
    [Header("NPC Configuration")]
    public BuildingNPC[] buildingNPCs;
    
    [Header("Spawn Settings")]
    public bool spawnOnStart = false; // For testing - spawn all NPCs immediately
    
    [Header("UI References")]
    public UnityEngine.UI.Image npcPanel; // Main NPC panel in the scene
    public UnityEngine.UI.Text npcNameText; // Text component for NPC name
    public UnityEngine.UI.Text conversationText; // Text component for conversation
    public UnityEngine.UI.Image npcSpriteImage; // Image component for NPC sprite
    
    private Dictionary<string, GameObject> spawnedNPCs = new Dictionary<string, GameObject>();
    private UIManager uiManager;
    
    public static NPCSpawner Instance { get; private set; }
    
    void Awake()
    {
        // if (Instance == null)
        // {
        //     Instance = this;
        //     DontDestroyOnLoad(gameObject);
        // }
        // else
        // {
        //     Destroy(gameObject);
        // }
    }
    
    void Start()
    {
        Debug.Log($"NPCSpawner: Starting up with {buildingNPCs?.Length ?? 0} NPCs configured");
        
        // Check if we're in Terminal stage and despawn all NPCs if so
        CheckAndHideNPCsInTerminalStage();
    }
    
    public void SpawnNPCForBuildingOnEnter(string buildingName)
    {
        // Check if we're in the Terminal game stage - hide NPCs during Terminal stage
        var currentStage = GameStageService.LoadStageFromPrefs();
        if (currentStage != null && currentStage.area == Stage.Terminal)
        {
            Debug.Log($"NPCSpawner: In Terminal stage, hiding NPCs for building '{buildingName}'");
            return;
        }
        
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
                        npcMovement.npcName = buildingNPC.npcName; // Set NPC name for UI
                        
                        // Assign UI references from spawner
                        npcMovement.npcPanel = this.npcPanel;
                        npcMovement.npcNameText = this.npcNameText;
                        npcMovement.conversationText = this.conversationText;
                        npcMovement.npcSpriteImage = this.npcSpriteImage;
                        
                        // Set the NPC sprite in the UI
                        if (this.npcSpriteImage != null)
                        {
                            SpriteRenderer npcSpriteRenderer = npcInstance.GetComponent<SpriteRenderer>();
                            if (npcSpriteRenderer != null && npcSpriteRenderer.sprite != null)
                            {
                                this.npcSpriteImage.sprite = npcSpriteRenderer.sprite;
                                Debug.Log($"NPCSpawner: Assigned sprite '{npcSpriteRenderer.sprite.name}' to UI for '{buildingNPC.npcName}'");
                            }
                            else
                            {
                                Debug.LogWarning($"NPCSpawner: No SpriteRenderer or sprite found on '{buildingNPC.npcName}'");
                            }
                        }
                        
                        // Set the NPC's name for identification
                        npcInstance.name = $"{buildingNPC.npcName} (Building: {buildingName})";
                        
                        Debug.Log($"NPCSpawner: Assigned UI references to '{buildingNPC.npcName}' - using conversation lines from prefab");
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
    
    // Method to despawn all NPCs for a building with delay
    public void DespawnNPCsForBuilding(string buildingName)
    {
        Debug.Log($"NPCSpawner: DespawnNPCsForBuilding called for building '{buildingName}' - starting 5 second delay");
        StartCoroutine(DespawnNPCsAfterDelay(buildingName, 5f));
    }
    
    private IEnumerator DespawnNPCsAfterDelay(string buildingName, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"NPCSpawner: 5 second delay complete, checking NPCs for building '{buildingName}'");
        
        List<string> npcsToReturn = new List<string>();
        
        foreach (var buildingNPC in buildingNPCs)
        {
            if (buildingNPC.associatedBuilding == buildingName && spawnedNPCs.ContainsKey(buildingNPC.npcName))
            {
                GameObject npcObject = spawnedNPCs[buildingNPC.npcName];
                if (npcObject != null)
                {
                    NpcAutoMovement npcMovement = npcObject.GetComponent<NpcAutoMovement>();
                    if (npcMovement != null)
                    {
                        // Check if NPC is currently in conversation with player
                        if (npcMovement.IsInConversation())
                        {
                            Debug.Log($"NPCSpawner: NPC '{buildingNPC.npcName}' is in conversation, will wait for conversation to end");
                            // Start a coroutine to wait for this specific NPC's conversation to end
                            StartCoroutine(WaitForConversationEndThenDespawn(buildingNPC.npcName, npcObject));
                        }
                        else
                        {
                            Debug.Log($"NPCSpawner: Found NPC '{buildingNPC.npcName}' to return to spawn for building '{buildingName}'");
                            npcsToReturn.Add(buildingNPC.npcName);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"NPCSpawner: NPC '{buildingNPC.npcName}' has no NpcAutoMovement component");
                        npcsToReturn.Add(buildingNPC.npcName);
                    }
                }
                else
                {
                    Debug.LogWarning($"NPCSpawner: NPC GameObject '{buildingNPC.npcName}' was null, removing from tracking");
                    spawnedNPCs.Remove(buildingNPC.npcName);
                }
            }
        }
        
        Debug.Log($"NPCSpawner: Found {npcsToReturn.Count} NPCs ready to return to spawn for building '{buildingName}'");
        
        foreach (string npcName in npcsToReturn)
        {
            if (spawnedNPCs.ContainsKey(npcName))
            {
                GameObject npcObject = spawnedNPCs[npcName];
                if (npcObject != null)
                {
                    NpcAutoMovement npcMovement = npcObject.GetComponent<NpcAutoMovement>();
                    if (npcMovement != null)
                    {
                        Debug.Log($"NPCSpawner: Triggering return-to-spawn for NPC '{npcName}'");
                        npcMovement.StartReturnToSpawnForDespawn();
                    }
                    else
                    {
                        Debug.LogWarning($"NPCSpawner: NPC '{npcName}' has no NpcAutoMovement component, destroying immediately");
                        DespawnNPC(npcName);
                    }
                }
            }
        }
    }
    
    private IEnumerator WaitForConversationEndThenDespawn(string npcName, GameObject npcObject)
    {
        NpcAutoMovement npcMovement = npcObject.GetComponent<NpcAutoMovement>();
        
        // Wait until conversation ends
        while (npcMovement != null && npcMovement.IsInConversation())
        {
            yield return new WaitForSeconds(0.5f); // Check every half second
        }
        
        Debug.Log($"NPCSpawner: Conversation ended for NPC '{npcName}', starting return to spawn");
        
        if (npcMovement != null)
        {
            npcMovement.StartReturnToSpawnForDespawn();
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
    
    // Method to get first NPC info for a building (for unlocked dialog)
    public BuildingNPC GetFirstNPCForBuilding(string buildingName)
    {
        foreach (var buildingNPC in buildingNPCs)
        {
            if (buildingNPC.associatedBuilding == buildingName)
            {
                // Automatically populate NPC image and title from prefab if not already set
                if (buildingNPC.npcPrefab != null)
                {
                    // Get sprite from prefab's SpriteRenderer
                    if (buildingNPC.npcImage == null)
                    {
                        SpriteRenderer spriteRenderer = buildingNPC.npcPrefab.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null && spriteRenderer.sprite != null)
                        {
                            buildingNPC.npcImage = spriteRenderer.sprite;
                        }
                    }
                    
                    // Always read NPC name from prefab's NpcAutoMovement component
                    NpcAutoMovement npcMovement = buildingNPC.npcPrefab.GetComponent<NpcAutoMovement>();
                    if (npcMovement != null && !string.IsNullOrEmpty(npcMovement.npcName))
                    {
                        buildingNPC.npcTitle = npcMovement.npcName;
                        Debug.Log($"NPCSpawner: Read NPC title '{npcMovement.npcName}' from prefab for building '{buildingName}'");
                    }
                    else
                    {
                        buildingNPC.npcTitle = buildingNPC.npcName;
                        Debug.Log($"NPCSpawner: Using fallback NPC title '{buildingNPC.npcName}' for building '{buildingName}'");
                    }
                }
                
                return buildingNPC;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Check if we're in Terminal stage and despawn all NPCs if so
    /// </summary>
    private void CheckAndHideNPCsInTerminalStage()
    {
        var currentStage = GameStageService.LoadStageFromPrefs();
        if (currentStage != null && currentStage.area == Stage.Terminal)
        {
            Debug.Log("NPCSpawner: In Terminal stage, despawning all existing NPCs");
            DespawnAllNPCs();
        }
    }
    
    /// <summary>
    /// Despawn all currently spawned NPCs immediately
    /// </summary>
    private void DespawnAllNPCs()
    {
        var npcsToRemove = new List<string>(spawnedNPCs.Keys);
        
        foreach (string npcName in npcsToRemove)
        {
            if (spawnedNPCs.ContainsKey(npcName))
            {
                GameObject npcToDestroy = spawnedNPCs[npcName];
                if (npcToDestroy != null)
                {
                    Debug.Log($"NPCSpawner: Despawning NPC '{npcName}' due to Terminal stage");
                    Destroy(npcToDestroy);
                }
                spawnedNPCs.Remove(npcName);
            }
        }
        
        Debug.Log($"NPCSpawner: Despawned all NPCs for Terminal stage. Remaining count: {spawnedNPCs.Count}");
    }
}

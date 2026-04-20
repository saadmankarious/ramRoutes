using UnityEngine;

/// <summary>
/// Quick integration example for the NPC-Building system.
/// This shows how the system works together.
/// </summary>
public class NPCBuildingSystemExample : MonoBehaviour
{
    [Header("Example Setup")]
    public NPCSpawner npcSpawner;
    
    void Start()
    {
        // The system works automatically:
        // 1. When a building is unlocked via BuildingInteraction
        // 2. UIManager fires OnBuildingUnlocked event  
        // 3. NPCSpawner receives the event and spawns associated NPCs
        // 4. NPCs appear near the building with custom conversations
        
        // You can also manually trigger spawning:
        // npcSpawner.ManuallySpawnNPC("Library");
        
        // Or check if an NPC is already spawned:
        // bool isLibrarianSpawned = npcSpawner.IsNPCSpawned("Librarian");
    }
    
    // Example method to spawn NPCs for testing
    [ContextMenu("Test Spawn All NPCs")]
    void TestSpawnAllNPCs()
    {
        if (npcSpawner != null)
        {
            npcSpawner.SpawnAllNPCs();
        }
    }
}

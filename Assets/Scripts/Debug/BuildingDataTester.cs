using UnityEngine;

public class BuildingDataTester : MonoBehaviour
{
    [Header("Test Settings")]
    public bool testOnStart = true;
    public string[] testBuildingNames = {"Library", "McWeathy", "PR", "Ebersole", "Stoner"};

    void Start()
    {
        if (testOnStart)
        {
            TestBuildingData();
        }
    }

    [ContextMenu("Test Building Data")]
    public void TestBuildingData()
    {
        Debug.Log("=== Testing Building Data ===");
        
        // Load building data
        BuildingDataManager.LoadBuildingData();
        
        // Test each building
        foreach (string buildingName in testBuildingNames)
        {
            var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
            Debug.Log($"Building: {buildingName}");
            Debug.Log($"  Display Name: {buildingInfo.displayName}");
            Debug.Log($"  Unlocked Message: {buildingInfo.unlockedMessage}");
            Debug.Log($"  Description: {buildingInfo.description}");
            Debug.Log("---");
        }
        
        // List all available buildings
        var allBuildings = BuildingDataManager.GetAllBuildingNames();
        Debug.Log($"All available buildings: {string.Join(", ", allBuildings)}");
        
        Debug.Log("=== Building Data Test Complete ===");
    }
}

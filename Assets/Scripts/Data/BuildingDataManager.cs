using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class BuildingInfo
{
    public string name;
    public string displayName;
    public string unlockedMessage;
    public string description;
}

[System.Serializable]
public class BuildingDataContainer
{
    public BuildingInfo[] buildings;
}

public static class BuildingDataManager
{
    private static BuildingDataContainer _buildingData;
    private static bool _isLoaded = false;

    public static void LoadBuildingData()
    {
        if (_isLoaded) return;

        try
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("BuildingData");
            if (jsonFile != null)
            {
                _buildingData = JsonUtility.FromJson<BuildingDataContainer>(jsonFile.text);
                _isLoaded = true;
                Debug.Log($"Loaded building data for {_buildingData.buildings.Length} buildings");
            }
            else
            {
                Debug.LogError("BuildingData.json not found in Resources folder!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load building data: {ex.Message}");
        }
    }

    public static BuildingInfo GetBuildingInfo(string buildingName)
    {
        if (!_isLoaded)
        {
            LoadBuildingData();
        }

        if (_buildingData?.buildings != null)
        {
            var buildingInfo = _buildingData.buildings.FirstOrDefault(b => 
                string.Equals(b.name, buildingName, StringComparison.OrdinalIgnoreCase));
            
            if (buildingInfo != null)
            {
                return buildingInfo;
            }
        }

        // Fallback if building not found
        Debug.LogWarning($"Building info not found for: {buildingName}. Using fallback.");
        return new BuildingInfo
        {
            name = buildingName,
            displayName = buildingName,
            unlockedMessage = $"You've unlocked {buildingName}!",
            description = $"Welcome to {buildingName}"
        };
    }

    public static string GetDisplayName(string buildingName)
    {
        return GetBuildingInfo(buildingName).displayName;
    }

    public static string GetUnlockedMessage(string buildingName)
    {
        return GetBuildingInfo(buildingName).unlockedMessage;
    }

    public static string GetDescription(string buildingName)
    {
        return GetBuildingInfo(buildingName).description;
    }

    // Method to get all building names for validation/debugging
    public static List<string> GetAllBuildingNames()
    {
        if (!_isLoaded)
        {
            LoadBuildingData();
        }

        return _buildingData?.buildings?.Select(b => b.name).ToList() ?? new List<string>();
    }
}

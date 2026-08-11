using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;

/// <summary>
/// Looks up building identity directly from the live "buildings" Firestore collection.
/// There is no local file or cache backing this - every call queries Firestore, and a
/// building that isn't found there simply isn't found (no synthesized fallback data).
/// </summary>
public static class BuildingDataManager
{
    /// <summary>
    /// Returns the building's name as registered in Firestore, or null if no matching
    /// building document exists.
    /// </summary>
    public static async Task<string> GetBuildingDisplayNameAsync(string buildingName)
    {
        var db = FirebaseFirestore.DefaultInstance;
        var query = db.Collection("buildings").WhereEqualTo("buildingName", buildingName).Limit(1);
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        if (snapshot.Count == 0)
        {
            Debug.LogWarning($"BuildingDataManager: No building found in Firestore for '{buildingName}'.");
            return null;
        }

        var data = snapshot.Documents.First().ToDictionary();
        return data.ContainsKey("buildingName") ? data["buildingName"].ToString() : null;
    }
}

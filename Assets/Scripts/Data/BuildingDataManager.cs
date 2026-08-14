using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;

/// <summary>
/// Looks up building identity from the "buildings" Firestore collection. Display names
/// are cached in memory per buildingName after the first successful lookup (they don't
/// change mid-session), so repeatedly selecting/switching buildings doesn't re-pay the
/// same Firestore round trip every time.
/// </summary>
public static class BuildingDataManager
{
    private static readonly Dictionary<string, Task<string>> displayNameCache = new Dictionary<string, Task<string>>();

    /// <summary>
    /// Returns the building's name as registered in Firestore, or null if no matching
    /// building document exists.
    /// </summary>
    public static Task<string> GetBuildingDisplayNameAsync(string buildingName)
    {
        if (displayNameCache.TryGetValue(buildingName, out Task<string> cached))
        {
            return cached;
        }

        Task<string> fetchTask = FetchAndCacheAsync(buildingName);
        displayNameCache[buildingName] = fetchTask;
        return fetchTask;
    }

    // Only a successful, non-null lookup is worth keeping cached forever - a miss or
    // transient failure shouldn't poison the cache and block every future retry.
    private static async Task<string> FetchAndCacheAsync(string buildingName)
    {
        try
        {
            string result = await FetchBuildingDisplayNameAsync(buildingName);
            if (result == null)
            {
                displayNameCache.Remove(buildingName);
            }
            return result;
        }
        catch
        {
            displayNameCache.Remove(buildingName);
            throw;
        }
    }

    private static async Task<string> FetchBuildingDisplayNameAsync(string buildingName)
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

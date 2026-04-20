namespace RamRoutes.Services
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;
    using Firebase.Firestore;
    using RamRoutes.Model;
    using System.Collections.Generic;
    using UnityEngine.SocialPlatforms;
    using System.Linq;

    public class UnlockedBuildingService
    {
        private FirebaseFirestore db;

        public UnlockedBuildingService()
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        public async Task SaveUnlockedBuildingAsync(UnlockedBuildingRecord record)
        {
            var docData = new Dictionary<string, object>
            {
                { "userId", record.userId },
                { "unlockTime", record.unlockTime.ToString("o") },
                { "buildingId", record.buildingId },
                { "userName", record.userName },
                { "buildingName", record.buildingName },
                { "coinPoints", record.coinPoints },
                { "knowledgePoints", record.knowledgePoints }
            };
            try
            {
                await db.Collection("unlocked-trials").AddAsync(docData);
                Debug.Log($"Unlocked building saved for user {record.userId} at {record.unlockTime}");
                // Save locally
                // string json = PlayerPrefs.GetString("unlocked_buildings_cache", "");
                // List<UnlockedBuildingRecord> buildings = new List<UnlockedBuildingRecord>();
                // if (!string.IsNullOrEmpty(json))
                // {
                //     try
                //     {
                //         var wrapper = JsonUtility.FromJson<UnlockedBuildingListWrapper>(json);
                //         if (wrapper != null && wrapper.buildings != null)
                //         {
                //             buildings = wrapper.buildings;
                //         }
                //     }
                //     catch (Exception ex)
                //     {
                //         Debug.LogError($"Failed to parse local unlocked buildings cache: {ex.Message}");
                //     }
                // }
                // buildings.Add(record);
                // string newJson = JsonUtility.ToJson(new UnlockedBuildingListWrapper { buildings = buildings });
                // PlayerPrefs.SetString("unlocked_buildings_cache", newJson);
                // PlayerPrefs.Save();
                // Debug.Log($"Unlocked building also saved locally");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save unlocked building for user {record.userId}: {ex.Message}");
            }
        }

        public async Task<List<UnlockedBuildingRecord>> RetrieveUnlockedBuildings(string userId)
        {
            var buildings = new List<UnlockedBuildingRecord>();
            try
            {
                // Query only for the specific user's unlocked buildings
                Query query = db.Collection("unlocked-trials").WhereEqualTo("userId", userId);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    string docUserId = data.ContainsKey("userId") ? data["userId"].ToString() : "";
                    DateTime unlockTime = data.ContainsKey("unlockTime") ? DateTime.Parse(data["unlockTime"].ToString()) : DateTime.MinValue;
                    string buildingId = data.ContainsKey("buildingId") ? data["buildingId"].ToString() : "";
                    string buildingName = data.ContainsKey("buildingName") ? data["buildingName"].ToString() : "";
                    int coinPoints = data.ContainsKey("coinPoints") ? Convert.ToInt32(data["coinPoints"]) : 0;
                    int knowledgePoints = data.ContainsKey("knowledgePoints") ? Convert.ToInt32(data["knowledgePoints"]) : 0;
                    string userName = data.ContainsKey("userName") ? data["userName"].ToString() : "";
                    
                    // Create record without building position (obsolete)
                    buildings.Add(new UnlockedBuildingRecord(docUserId, userName, unlockTime, buildingId, buildingName, Vector3.zero, coinPoints, knowledgePoints));
                }
                
                Debug.Log($"Retrieved {buildings.Count} unlocked buildings for user {userId} from Firebase");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load unlocked buildings from Firestore for user {userId}: {ex.Message}");
            }
            
            return buildings;
        }

        public async Task<List<UnlockedBuildingRecord>> RetrieveUnlockedBuildingsForBuilding(string buildingName)
        {
            var buildings = new List<UnlockedBuildingRecord>();
            try
            {
                // Query for users who unlocked a specific building (without ordering to avoid index requirement)
                Query query = db.Collection("unlocked-trials")
                    .WhereEqualTo("buildingName", buildingName);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    string userId = data.ContainsKey("userId") ? data["userId"].ToString() : "";
                    DateTime unlockTime = data.ContainsKey("unlockTime") ? DateTime.Parse(data["unlockTime"].ToString()) : DateTime.MinValue;
                    string buildingId = data.ContainsKey("buildingId") ? data["buildingId"].ToString() : "";
                    string docBuildingName = data.ContainsKey("buildingName") ? data["buildingName"].ToString() : "";
                    int coinPoints = data.ContainsKey("coinPoints") ? Convert.ToInt32(data["coinPoints"]) : 0;
                    int knowledgePoints = data.ContainsKey("knowledgePoints") ? Convert.ToInt32(data["knowledgePoints"]) : 0;
                    string userName = data.ContainsKey("userName") ? data["userName"].ToString() : "";
                    
                    // Create record without building position (obsolete)
                    buildings.Add(new UnlockedBuildingRecord(userId, userName, unlockTime, buildingId, docBuildingName, Vector3.zero, coinPoints, knowledgePoints));
                }
                
                // Sort by unlockTime descending and take only the most recent 10 (client-side)
                buildings = buildings.OrderByDescending(b => b.unlockTime).Take(10).ToList();
                
                Debug.Log($"Retrieved {buildings.Count} most recent users who unlocked building {buildingName} from Firebase");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load users who unlocked building {buildingName} from Firestore: {ex.Message}");
            }
            
            return buildings;
        }

        [Serializable]
        private class UnlockedBuildingListWrapper
        {
            public List<UnlockedBuildingRecord> buildings;
        }
            public static void ClearUnlockedBuildingsCache()
        {
            PlayerPrefs.DeleteKey("unlocked_buildings_cache");
            PlayerPrefs.Save();
            Debug.Log("Cleared local unlocked buildings cache");
        }
    }


}

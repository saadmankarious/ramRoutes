namespace RamRoutes.Services
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;
    using Firebase.Firestore;
    using RamRoutes.Model;
    using System.Collections.Generic;
    using UnityEngine.SocialPlatforms;

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
                { "buildingName", record.buildingName },
                { "buildingPosition", new Dictionary<string, object>
                    {
                        { "x", record.buildingPosition.x },
                        { "y", record.buildingPosition.y },
                        { "z", record.buildingPosition.z }
                    }
                }
            };
            try
            {
                await db.Collection("unlocked-trials").AddAsync(docData);
                Debug.Log($"Unlocked building saved for user {record.userId} at {record.unlockTime}");
                // Save locally
                string json = PlayerPrefs.GetString("unlocked_buildings_cache", "");
                List<UnlockedBuildingRecord> buildings = new List<UnlockedBuildingRecord>();
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var wrapper = JsonUtility.FromJson<UnlockedBuildingListWrapper>(json);
                        if (wrapper != null && wrapper.buildings != null)
                        {
                            buildings = wrapper.buildings;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to parse local unlocked buildings cache: {ex.Message}");
                    }
                }
                buildings.Add(record);
                string newJson = JsonUtility.ToJson(new UnlockedBuildingListWrapper { buildings = buildings });
                PlayerPrefs.SetString("unlocked_buildings_cache", newJson);
                PlayerPrefs.Save();
                Debug.Log($"Unlocked building also saved locally");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save unlocked building for user {record.userId}: {ex.Message}");
            }
        }

        public async Task<List<UnlockedBuildingRecord>> RetrieveUnlockedBuildings()
        {
            var buildings = new List<UnlockedBuildingRecord>();
            bool loadedFromFirestore = false;
            try
            {
                QuerySnapshot snapshot = await db.Collection("unlocked-trials").GetSnapshotAsync();
                foreach (var doc in snapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    string userId = data.ContainsKey("userId") ? data["userId"].ToString() : "";
                    DateTime unlockTime = data.ContainsKey("unlockTime") ? DateTime.Parse(data["unlockTime"].ToString()) : DateTime.MinValue;
                    string buildingId = data.ContainsKey("buildingId") ? data["buildingId"].ToString() : "";
                    string buildingName = data.ContainsKey("buildingName") ? data["buildingName"].ToString() : "";
                    Vector3 buildingPosition = Vector3.zero;
                    if (data.ContainsKey("buildingPosition"))
                    {
                        var posDict = data["buildingPosition"] as Dictionary<string, object>;
                        if (posDict != null)
                        {
                            float x = posDict.ContainsKey("x") ? Convert.ToSingle(posDict["x"]) : 0f;
                            float y = posDict.ContainsKey("y") ? Convert.ToSingle(posDict["y"]) : 0f;
                            float z = posDict.ContainsKey("z") ? Convert.ToSingle(posDict["z"]) : 0f;
                            buildingPosition = new Vector3(x, y, z);
                        }
                    }
                    string userName = data.ContainsKey("userName") ? data["userName"].ToString() : "";
                    buildings.Add(new UnlockedBuildingRecord(userId, userName, unlockTime, buildingId, buildingName, buildingPosition));
                }
                // Save to local storage
                string json = JsonUtility.ToJson(new UnlockedBuildingListWrapper { buildings = buildings });
                PlayerPrefs.SetString("unlocked_buildings_cache", json);
                PlayerPrefs.Save();
                loadedFromFirestore = true;
                Debug.Log($"Got unlocked buildings from firestore");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load unlocked buildings from Firestore: {ex.Message}");
            }
            if (!loadedFromFirestore)
            {
                // Try to load from local storage
                string json = PlayerPrefs.GetString("unlocked_buildings_cache", "");
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var wrapper = JsonUtility.FromJson<UnlockedBuildingListWrapper>(json);
                        if (wrapper != null && wrapper.buildings != null)
                        {
                            buildings = wrapper.buildings;
                            Debug.Log($"Loaded unlocked buildings from local storage");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to parse unlocked buildings from local storage: {ex.Message}");
                    }
                }
            }
            return buildings;
        }

        [Serializable]
        private class UnlockedBuildingListWrapper
        {
            public List<UnlockedBuildingRecord> buildings;
        }
    }
}

namespace RamRoutes.Services
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;
    using Firebase.Firestore;
    using RamRoutes.Model;
    using System.Collections.Generic;

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
                { "unlockTime", record.unlockTime.ToString("o") }, // ISO 8601
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
                await db.Collection("unlocked_trials").AddAsync(docData);
                Debug.Log($"Unlocked building saved for user {record.userId} at {record.unlockTime}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save unlocked building for user {record.userId}: {ex.Message}");
            }
        }
    }
}

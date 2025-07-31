using System;
using UnityEngine;

namespace RamRoutes.Model
{
    [Serializable]
    public class UnlockedBuildingRecord
    {
        public string userId;
        public DateTime unlockTime;
        public string buildingId;
        public string buildingName;
        public Vector3 buildingPosition;
        // Add other building info fields as needed

        public UnlockedBuildingRecord(string userId, DateTime unlockTime, string buildingId, string buildingName, Vector3 buildingPosition)
        {
            this.userId = userId;
            this.unlockTime = unlockTime;
            this.buildingId = buildingId;
            this.buildingName = buildingName;
            this.buildingPosition = buildingPosition;
        }
    }
}

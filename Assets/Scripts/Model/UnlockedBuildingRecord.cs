using System;
using UnityEngine;

namespace RamRoutes.Model
{
    [Serializable]
    public class UnlockedBuildingRecord
    {
        public string userId;
        public string userName;
        public DateTime unlockTime;
        public string buildingId;
        public string buildingName;
        public Vector3 buildingPosition;
        // Add other building info fields as needed

        public UnlockedBuildingRecord(string userId, string userName, DateTime unlockTime, string buildingId, string buildingName, Vector3 buildingPosition)
        {
            this.userId = userId;
            this.userName = userName;
            this.unlockTime = unlockTime;
            this.buildingId = buildingId;
            this.buildingName = buildingName;
            this.buildingPosition = buildingPosition;
        }
    }
}

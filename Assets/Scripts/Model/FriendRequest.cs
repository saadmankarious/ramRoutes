using System;
using UnityEngine;

namespace RamRoutes.Model
{
    [Serializable]
    public class FriendRequest
    {
        public string fromId;
        public string toId;
        public string timestamp; // Store as string for Firebase compatibility
        
        // This field is not stored in Firestore - it's set after retrieval
        [System.NonSerialized]
        public string requestId;

        public FriendRequest()
        {
            // Default constructor for Firebase deserialization
        }

        public FriendRequest(string fromId, string toId)
        {
            this.fromId = fromId;
            this.toId = toId;
            this.timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
        }
        
        public DateTime GetTimestampAsDateTime()
        {
            if (DateTime.TryParse(timestamp, out DateTime result))
            {
                return result;
            }
            return DateTime.MinValue;
        }
    }
}

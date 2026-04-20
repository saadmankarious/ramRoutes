using System;
using UnityEngine;
using Firebase.Firestore;

namespace RamRoutes.Model
{
    [Serializable]
    [FirestoreData]
    public class FriendRequest
    {
        [FirestoreProperty]
        public string fromId { get; set; }
        
        [FirestoreProperty]
        public string toId { get; set; }
        
        [FirestoreProperty]
        public string timestamp { get; set; }
        
        [FirestoreProperty]
        public string fromName { get; set; }
        
        [FirestoreProperty]
        public string toName { get; set; }
        
        [FirestoreProperty]
        public bool accepted { get; set; }
        
        [FirestoreProperty]
        public string requestId { get; set; }

        public FriendRequest()
        {
            // Default constructor for Firebase deserialization
        }

        public FriendRequest(string fromId, string toId)
        {
            this.fromId = fromId;
            this.toId = toId;
            this.timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
            this.accepted = false; // Default to not accepted
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

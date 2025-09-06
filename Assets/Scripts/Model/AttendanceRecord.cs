using System;
using UnityEngine;

namespace RamRoutes.Model
{
    [Serializable]
    public class AttendanceRecord
    {
        public string recordId { set; get; }  // Firestore document ID
        public string eventId { set; get; }
        public string eventName { set; get; }
        public string buildingName { set; get; }
        public string playerName { set; get; }
        public DateTime checkInDate { set; get; }
        public string playerId { set; get; }  // For linking to player accounts
        public string buildingId { set; get; }  // For additional building reference

        public AttendanceRecord(string eventId, string eventName, string buildingName, 
                               string playerName, DateTime checkInDate, string playerId = null, 
                               string buildingId = null, string recordId = null)
        {
            this.recordId = recordId;
            this.eventId = eventId;
            this.eventName = eventName;
            this.buildingName = buildingName;
            this.playerName = playerName;
            this.checkInDate = checkInDate;
            this.playerId = playerId ?? playerName; // Use playerId if provided, otherwise use playerName
            this.buildingId = buildingId;
        }

        // Default constructor for Firebase deserialization
        public AttendanceRecord()
        {
        }

        // Helper property to get display-friendly check-in time
        public string GetDisplayCheckInTime()
        {
            return checkInDate.ToString("MMM dd, yyyy h:mm tt");
        }

        // Helper property to get check-in date only
        public string GetCheckInDateOnly()
        {
            return checkInDate.ToString("MMM dd, yyyy");
        }

        // Helper property to get check-in time only
        public string GetCheckInTimeOnly()
        {
            return checkInDate.ToString("h:mm tt");
        }

        // Helper method to check if check-in was today
        public bool IsToday()
        {
            return checkInDate.Date == DateTime.Today;
        }

        // Helper method to check if check-in was within last X days
        public bool IsWithinDays(int days)
        {
            return (DateTime.Now - checkInDate).TotalDays <= days;
        }

        // Convert to dictionary for Firestore
        public System.Collections.Generic.Dictionary<string, object> ToFirestoreData()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                ["eventId"] = eventId,
                ["eventName"] = eventName,
                ["buildingName"] = buildingName,
                ["playerName"] = playerName,
                ["checkInDate"] = checkInDate,
                ["playerId"] = playerId,
                ["buildingId"] = buildingId
            };
        }

        // Create from Firestore data
        public static AttendanceRecord FromFirestoreData(System.Collections.Generic.Dictionary<string, object> data, string documentId = null)
        {
            var record = new AttendanceRecord();
            record.recordId = documentId;

            if (data.ContainsKey("eventId"))
                record.eventId = data["eventId"]?.ToString();

            if (data.ContainsKey("eventName"))
                record.eventName = data["eventName"]?.ToString();

            if (data.ContainsKey("buildingName"))
                record.buildingName = data["buildingName"]?.ToString();

            if (data.ContainsKey("playerName"))
                record.playerName = data["playerName"]?.ToString();

            if (data.ContainsKey("playerId"))
                record.playerId = data["playerId"]?.ToString();

            if (data.ContainsKey("buildingId"))
                record.buildingId = data["buildingId"]?.ToString();

            if (data.ContainsKey("checkInDate"))
            {
                // Handle different possible date formats from Firestore
                var dateValue = data["checkInDate"];
                if (dateValue is DateTime dt)
                {
                    record.checkInDate = dt;
                }
                else if (dateValue is Firebase.Firestore.Timestamp timestamp)
                {
                    record.checkInDate = timestamp.ToDateTime();
                }
                else if (DateTime.TryParse(dateValue?.ToString(), out DateTime parsedDate))
                {
                    record.checkInDate = parsedDate;
                }
                else
                {
                    record.checkInDate = DateTime.Now; // Fallback to current time
                }
            }

            return record;
        }

        public override string ToString()
        {
            return $"AttendanceRecord: {playerName} checked into {eventName} at {buildingName} on {GetDisplayCheckInTime()}";
        }
    }
}

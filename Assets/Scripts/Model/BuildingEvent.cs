using System;
using UnityEngine;
using System.Collections.Generic;
namespace RamRoutes.Model
{
    public enum EventType
    {
        Scheduled,
        Always,
        Weekly,
        Monthly,
        Daily
    }

    [Serializable]
    public class BuildingEvent
    {
        public string buildingId;
        public string buildingName;
        public string eventName;
        public string description; // Added description property
        public string imageUrl; // URL of the event's image, if any
        public List<string> tags = new List<string>(); // Category tags computed by the scraper (e.g. "academic", "social")
        public int gainedCoins; // Added gainedCoins property
        public int gainedKb; // Added gainedKb property
        public string eventId;  // Added explicit eventId field
        public string date; // Raw display-ready date string, stored as-is - no parsing/conversion
        public EventType eventType;
        public string recurrenceData; // JSON string for complex recurrence patterns
        public List<string> attendees; // List of player IDs who have checked in
        public List<string> interestedUsers; // List of player IDs who have shown interest/RSVP'd

        public BuildingEvent(string buildingId, string buildingName, string eventName, string date,
                            EventType eventType = EventType.Scheduled, string recurrenceData = null,
                            List<string> attendees = null, string eventId = null, List<string> interestedUsers = null,
                            string description = null, int gainedCoins = 0, int gainedKb = 0)
        {
            this.buildingId = buildingId;
            this.buildingName = buildingName;
            this.eventName = eventName;
            this.description = description ?? ""; // Default to empty string if not provided
            this.gainedCoins = gainedCoins; // Default to 0 if not provided
            this.gainedKb = gainedKb; // Default to 0 if not provided
            this.eventId = eventId ?? buildingId; // Use eventId if provided, otherwise use buildingId
            this.date = date;
            this.eventType = eventType;
            this.recurrenceData = recurrenceData;
            this.attendees = attendees ?? new List<string>();
            this.interestedUsers = interestedUsers ?? new List<string>();
        }
        
        // Helper property to check if this is an always-happening event
        public bool IsAlwaysHappening => eventType == EventType.Always;
        
        // Helper property to check if this is a recurring event
        public bool IsRecurring => eventType == EventType.Weekly || eventType == EventType.Monthly || eventType == EventType.Daily;
        
        // Helper method to create an attendance record for this event
        public AttendanceRecord CreateAttendanceRecord(string playerName, string playerId = null)
        {
            return new AttendanceRecord(
                this.eventId,
                this.eventName,
                this.buildingName,
                playerName,
                DateTime.Now,
                playerId,
                this.buildingId
            );
        }
    }
}

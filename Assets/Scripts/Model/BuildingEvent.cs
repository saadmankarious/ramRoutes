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
        public string buildingId { set; get; }
        public string buildingName  { set; get; }
        public string eventName  { set; get; }
        public string description { set; get; } // Added description property
        public string eventId { set; get; }  // Added explicit eventId field
        public DateTime date  { set; get; }
        public EventType eventType { set; get; }
        public string recurrenceData { set; get; } // JSON string for complex recurrence patterns
        public List<string> attendees { set; get; } // List of player IDs who have checked in
        public List<string> interestedUsers { set; get; } // List of player IDs who have shown interest/RSVP'd

        public BuildingEvent(string buildingId, string buildingName, string eventName, DateTime date, 
                            EventType eventType = EventType.Scheduled, string recurrenceData = null, 
                            List<string> attendees = null, string eventId = null, List<string> interestedUsers = null,
                            string description = null)
        {
            this.buildingId = buildingId;
            this.buildingName = buildingName;
            this.eventName = eventName;
            this.description = description ?? ""; // Default to empty string if not provided
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
        
        // Helper property to get display-friendly date
        public string GetDisplayDate()
        {
            switch (eventType)
            {
                case EventType.Always:
                    return "Always Happening";
                case EventType.Weekly:
                    return $"Every {date.DayOfWeek} at {date.ToString("h:mm tt")}";
                case EventType.Daily:
                    return $"Daily at {date.ToString("h:mm tt")}";
                case EventType.Monthly:
                    return $"Monthly on {date.Day}{GetOrdinalSuffix(date.Day)} at {date.ToString("h:mm tt")}";
                case EventType.Scheduled:
                default:
                    return date.ToString("MMM dd, yyyy h:mm tt");
            }
        }
        
        private string GetOrdinalSuffix(int day)
        {
            if (day >= 11 && day <= 13)
                return "th";
            
            switch (day % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }
        
        // Helper method to check if event is active at a given time
        public bool IsActiveAt(DateTime checkTime)
        {
            switch (eventType)
            {
                case EventType.Always:
                    return true;
                    
                case EventType.Scheduled:
                    // For scheduled events, check if the time is within reasonable range (e.g., same day)
                    return checkTime.Date == date.Date;
                    
                case EventType.Weekly:
                    return checkTime.DayOfWeek == date.DayOfWeek;
                    
                case EventType.Daily:
                    return true; // Daily events are always active
                    
                case EventType.Monthly:
                    return checkTime.Day == date.Day;
                    
                default:
                    return false;
            }
        }
        
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

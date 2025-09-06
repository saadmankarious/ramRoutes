using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using Firebase.Firestore;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    [Serializable]
    public class BuildingEventList
    {
        public List<BuildingEvent> events;
        
        public BuildingEventList()
        {
            events = new List<BuildingEvent>();
        }
    }

    public class BuildingEventService
    {
        private FirebaseFirestore db;
        private const string CACHE_KEY = "building_events_cache";
        private List<BuildingEvent> cachedEvents;

        public BuildingEventService()
        {
            db = FirebaseFirestore.DefaultInstance;
            LoadFromCache();
        }

        private void LoadFromCache()
        {
            string json = PlayerPrefs.GetString(CACHE_KEY, "");
            try
            {
                if (!string.IsNullOrEmpty(json))
                {
                    var wrapper = JsonUtility.FromJson<BuildingEventList>(json);
                    cachedEvents = wrapper.events;
                    Debug.Log($"Loaded {cachedEvents.Count} building events from cache");
                }
                else
                {
                    cachedEvents = new List<BuildingEvent>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading building events from cache: {ex.Message}");
                cachedEvents = new List<BuildingEvent>();
            }
        }

        private void SaveToCache(List<BuildingEvent> events)
        {
            try
            {
                var wrapper = new BuildingEventList { events = events };
                string json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString(CACHE_KEY, json);
                PlayerPrefs.Save();
                cachedEvents = events;
                Debug.Log($"Saved {events.Count} building events to cache");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving building events to cache: {ex.Message}");
            }
        }

        public async Task<List<BuildingEvent>> GetBuildingEventsAsync(bool forceRefresh = false)
        {
            // Return cached data if available and not forcing refresh
            // if (!forceRefresh && cachedEvents != null && cachedEvents.Count > 0)
            // {
            //     Debug.Log("Returning building events from cache");
            //     return cachedEvents;
            // }

            try
            {
                QuerySnapshot querySnapshot = await db.Collection("building-events").GetSnapshotAsync();
                List<BuildingEvent> events = new List<BuildingEvent>();

                foreach (DocumentSnapshot doc in querySnapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    
                    // Get the document ID from Firestore (this is the actual event ID)
                    string documentId = doc.Id;
                    Debug.Log($"Processing event document with ID: {documentId}");
                    
                    // Parse event type and handle date accordingly
                    RamRoutes.Model.EventType eventType = ParseEventType(data);
                    DateTime eventDate = ParseEventDate(data, eventType);
                    string recurrenceData = data.ContainsKey("recurrenceData") ? data["recurrenceData"]?.ToString() : null;
                    
                    // Get attendees list if available
                    List<string> attendees = new List<string>();
                    if (data.ContainsKey("attendees") && data["attendees"] is IEnumerable<object> attendeesList)
                    {
                        foreach (var item in attendeesList)
                        {
                            if (item != null)
                            {
                                attendees.Add(item.ToString());
                            }
                        }
                    }
                    
                    var buildingEvent = new BuildingEvent(
                        data.ContainsKey("buildingId") ? data["buildingId"].ToString() : string.Empty,
                        data.ContainsKey("buildingName") ? data["buildingName"].ToString() : string.Empty,
                        data.ContainsKey("eventName") ? data["eventName"].ToString() : string.Empty,
                        eventDate,
                        eventType,
                        recurrenceData,
                        attendees,
                        documentId  // Pass the document ID as the event ID
                    );
                    events.Add(buildingEvent);
                }

                // Update cache with new data
                SaveToCache(events);
                return events;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching building events: {ex.Message}");
                // Return cached data as fallback if fetch fails
                return cachedEvents ?? new List<BuildingEvent>();
            }
        }

        public async Task<BuildingEvent> GetBuildingEventByIdAsync(string buildingId)
        {
            // First try to find in cache
            if (cachedEvents != null)
            {
                var cachedEvent = cachedEvents.Find(e => e.buildingId == buildingId);
                if (cachedEvent != null)
                {
                    Debug.Log($"Found building event {buildingId} in cache");
                    return cachedEvent;
                }
            }

            try
            {
                var query = await db.Collection("building-events")
                    .WhereEqualTo("buildingId", buildingId)
                    .GetSnapshotAsync();

                var doc = query.Documents.FirstOrDefault();
                if (doc != null)
                {
                    var data = doc.ToDictionary();
                    
                    // Parse event type and handle date accordingly
                    RamRoutes.Model.EventType eventType = ParseEventType(data);
                    DateTime eventDate = ParseEventDate(data, eventType);
                    string recurrenceData = data.ContainsKey("recurrenceData") ? data["recurrenceData"]?.ToString() : null;
                    
                    return new BuildingEvent(
                        data["buildingId"].ToString(),
                        data["buildingName"].ToString(),
                        data["eventName"].ToString(),
                        eventDate,
                        eventType,
                        recurrenceData
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching building event {buildingId}: {ex.Message}");
            }

            return null;
        }

        private RamRoutes.Model.EventType ParseEventType(System.Collections.Generic.Dictionary<string, object> data)
        {
            // Check if we have explicit event type data from newer admin panel
            if (data.ContainsKey("eventType"))
            {
                string typeString = data["eventType"]?.ToString();
                switch (typeString?.ToLower())
                {
                    case "always": return RamRoutes.Model.EventType.Always;
                    case "weekly": return RamRoutes.Model.EventType.Weekly;
                    case "daily": return RamRoutes.Model.EventType.Daily;
                    case "monthly": return RamRoutes.Model.EventType.Monthly;
                    case "scheduled":
                    default: return RamRoutes.Model.EventType.Scheduled;
                }
            }
            
            // Fallback: determine type based on date field (for backward compatibility)
            if (!data.ContainsKey("date") || data["date"] == null)
            {
                return RamRoutes.Model.EventType.Always;
            }
            
            return RamRoutes.Model.EventType.Scheduled;
        }

        private DateTime ParseEventDate(System.Collections.Generic.Dictionary<string, object> data, RamRoutes.Model.EventType eventType)
        {
            switch (eventType)
            {
                case RamRoutes.Model.EventType.Always:
                    return DateTime.MaxValue;
                    
                case RamRoutes.Model.EventType.Weekly:
                case RamRoutes.Model.EventType.Daily:
                case RamRoutes.Model.EventType.Monthly:
                    // For recurring events, use the base date/time pattern
                    if (data.ContainsKey("date") && data["date"] != null)
                    {
                        // Convert UTC to local time
                        DateTime utcDateTime = ((Timestamp)data["date"]).ToDateTime();
                        return utcDateTime.ToLocalTime();
                    }
                    // Fallback for recurring events without date
                    return DateTime.Today.AddHours(12); // Default to noon today
                    
                case RamRoutes.Model.EventType.Scheduled:
                default:
                    if (data.ContainsKey("date") && data["date"] != null)
                    {
                        // Convert UTC to local time
                        DateTime utcDateTime = ((Timestamp)data["date"]).ToDateTime();
                        return utcDateTime.ToLocalTime();
                    }
                    return DateTime.MaxValue; // Treat as always-happening if no date
            }
        }

        public void ClearCache()
        {
            PlayerPrefs.DeleteKey(CACHE_KEY);
            cachedEvents = new List<BuildingEvent>();
            Debug.Log("Building events cache cleared");
        }
        
        public async Task<bool> RecordAttendanceAsync(string eventId, string playerId)
        {
            try
            {
                DocumentReference eventRef = db.Collection("building-events").Document(eventId);
                DocumentSnapshot eventSnap = await eventRef.GetSnapshotAsync();
                
                if (!eventSnap.Exists)
                {
                    Debug.LogError($"Event {eventId} not found when recording attendance");
                    return false;
                }
                
                // Use FieldValue.ArrayUnion to add the player ID to the attendees array without affecting other fields
                await eventRef.UpdateAsync("attendees", FieldValue.ArrayUnion(playerId));
                
                // Update local cache
                var evt = cachedEvents?.FirstOrDefault(e => e.eventId == eventId) ?? 
                          cachedEvents?.FirstOrDefault(e => e.buildingId == eventId);
                if (evt != null && !evt.attendees.Contains(playerId))
                {
                    evt.attendees.Add(playerId);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error recording attendance: {ex.Message}");
                return false;
            }
        }
        
        public bool HasPlayerAttended(string eventId, string playerId)
        {
            // Try to find by eventId first, then by buildingId as fallback
            var evt = cachedEvents?.FirstOrDefault(e => e.eventId == eventId) ?? 
                      cachedEvents?.FirstOrDefault(e => e.buildingId == eventId);
                      
            return evt != null && evt.attendees != null && evt.attendees.Contains(playerId);
        }
    }
}

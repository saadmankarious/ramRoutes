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
                        documentId,  // Pass the document ID as the event ID
                        null,  // interested users will be populated separately if needed
                        data.ContainsKey("description") ? data["description"].ToString() : string.Empty
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

        public async Task<BuildingEvent> GetBuildingEventByIdAsync(string eventId)
        {
            // First try to find in cache by eventId
            if (cachedEvents != null)
            {
                var cachedEvent = cachedEvents.Find(e => e.eventId == eventId);
                if (cachedEvent != null)
                {
                    Debug.Log($"Found building event {eventId} in cache");
                    return cachedEvent;
                }
            }

            try
            {
                DocumentReference eventRef = db.Collection("building-events").Document(eventId);
                DocumentSnapshot doc = await eventRef.GetSnapshotAsync();

                if (doc.Exists)
                {
                    var data = doc.ToDictionary();
                    
                    // Parse event type and handle date accordingly
                    RamRoutes.Model.EventType eventType = ParseEventType(data);
                    DateTime eventDate = ParseEventDate(data, eventType);
                    string recurrenceData = data.ContainsKey("recurrenceData") ? data["recurrenceData"]?.ToString() : null;
                    
                    // Parse attendees and interested users lists
                    List<string> attendees = new List<string>();
                    if (data.ContainsKey("attendees") && data["attendees"] is IEnumerable<object> attendeesArray)
                    {
                        attendees = attendeesArray.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    }
                    
                    List<string> interestedUsers = new List<string>();
                    if (data.ContainsKey("interestedUsers") && data["interestedUsers"] is IEnumerable<object> interestedArray)
                    {
                        interestedUsers = interestedArray.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    }
                    
                    return new BuildingEvent(
                        data["buildingId"].ToString(),
                        data["buildingName"].ToString(),
                        data["eventName"].ToString(),
                        eventDate,
                        eventType,
                        recurrenceData,
                        attendees,
                        eventId,
                        interestedUsers,
                        data.ContainsKey("description") ? data["description"].ToString() : string.Empty
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching building event {eventId}: {ex.Message}");
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
        
        /// <summary>
        /// Static method to clear building events cache without needing an instance
        /// </summary>
        public static void ClearBuildingEventsCache()
        {
            PlayerPrefs.DeleteKey(CACHE_KEY);
            PlayerPrefs.Save();
            Debug.Log("BuildingEventService: Cleared building events cache");
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
            // Find by eventId only
            var evt = cachedEvents?.FirstOrDefault(e => e.eventId == eventId);
                      
            return evt != null && evt.attendees != null && evt.attendees.Contains(playerId);
        }

        /// <summary>
        /// Record interest/RSVP for an event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <param name="playerId">The player ID showing interest</param>
        /// <returns>True if successful</returns>
        public async Task<bool> RecordInterestAsync(string eventId, string playerId)
        {
            try
            {
                DocumentReference eventRef = db.Collection("building-events").Document(eventId);
                
                await eventRef.UpdateAsync("interestedUsers", FieldValue.ArrayUnion(playerId));
                
                // Update cached data if available
                var cachedEvent = cachedEvents?.FirstOrDefault(e => e.eventId == eventId);
                if (cachedEvent != null)
                {
                    if (cachedEvent.interestedUsers == null)
                        cachedEvent.interestedUsers = new List<string>();
                    
                    if (!cachedEvent.interestedUsers.Contains(playerId))
                        cachedEvent.interestedUsers.Add(playerId);
                }
                
                Debug.Log($"Successfully recorded interest for player {playerId} in event {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error recording interest for event {eventId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove interest/RSVP for an event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <param name="playerId">The player ID removing interest</param>
        /// <returns>True if successful</returns>
        public async Task<bool> RemoveInterestAsync(string eventId, string playerId)
        {
            try
            {
                DocumentReference eventRef = db.Collection("building-events").Document(eventId);

                await eventRef.UpdateAsync("interestedUsers", FieldValue.ArrayRemove(playerId));
                
                // Update cached data if available
                var cachedEvent = cachedEvents?.FirstOrDefault(e => e.eventId == eventId);
                if (cachedEvent != null && cachedEvent.interestedUsers != null)
                {
                    cachedEvent.interestedUsers.Remove(playerId);
                }
                
                Debug.Log($"Successfully removed interest for player {playerId} from event {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error removing interest for event {eventId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a player has shown interest in an event
        /// </summary>
        /// <param name="eventId">The event ID</param>
        /// <param name="playerId">The player ID</param>
        /// <returns>True if player has shown interest</returns>
        public async Task<bool> HasPlayerShownInterest(string eventId, string playerId)
        {
            try
            {
                DocumentReference eventRef = db.Collection("building-events").Document(eventId);
                DocumentSnapshot doc = await eventRef.GetSnapshotAsync();

                if (!doc.Exists)
                {
                    Debug.LogWarning($"Event {eventId} not found when checking player interest");
                    return false;
                }

                var data = doc.ToDictionary();
                if (data.ContainsKey("interestedUsers") && data["interestedUsers"] is IEnumerable<object> interestedArray)
                {
                    var interestedUsers = interestedArray.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    return interestedUsers.Contains(playerId);
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error checking player interest for event {eventId}: {ex.Message}");
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using Firebase.Firestore;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    public class BuildingEventService
    {
        private static BuildingEventService instance;

        /// <summary>
        /// Shared instance so callers don't each construct their own FirebaseFirestore
        /// reference. Holds no data of its own - every call hits Firestore directly.
        /// </summary>
        public static BuildingEventService Instance => instance ??= new BuildingEventService();

        private readonly FirebaseFirestore db;

        public BuildingEventService()
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        public async Task<List<BuildingEvent>> GetBuildingEventsAsync()
        {
            QuerySnapshot querySnapshot = await db.Collection("building-events").GetSnapshotAsync();
            return querySnapshot.Documents.Select(ParseBuildingEvent).ToList();
        }

        /// <summary>
        /// Queries Firestore directly for just one building's events, instead of fetching
        /// the whole collection and filtering client-side.
        /// </summary>
        public async Task<List<BuildingEvent>> GetBuildingEventsForBuildingAsync(string buildingName)
        {
            QuerySnapshot querySnapshot = await db.Collection("building-events")
                .WhereEqualTo("buildingName", buildingName)
                .GetSnapshotAsync();
            return querySnapshot.Documents.Select(ParseBuildingEvent).ToList();
        }

        public async Task<BuildingEvent> GetBuildingEventByIdAsync(string eventId)
        {
            DocumentReference eventRef = db.Collection("building-events").Document(eventId);
            DocumentSnapshot doc = await eventRef.GetSnapshotAsync();

            return doc.Exists ? ParseBuildingEvent(doc) : null;
        }

        private BuildingEvent ParseBuildingEvent(DocumentSnapshot doc)
        {
            var data = doc.ToDictionary();

            RamRoutes.Model.EventType eventType = ParseEventType(data);
            string eventDate = data.ContainsKey("date") ? data["date"]?.ToString() : string.Empty;
            string recurrenceData = data.ContainsKey("recurrenceData") ? data["recurrenceData"]?.ToString() : null;

            List<string> attendees = new List<string>();
            if (data.ContainsKey("attendees") && data["attendees"] is IEnumerable<object> attendeesList)
            {
                attendees = attendeesList.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList();
            }

            // Interested users are included here too, so callers displaying a list of
            // events don't need a second per-event Firestore round trip for RSVP counts.
            List<string> interestedUsers = new List<string>();
            if (data.ContainsKey("interestedUsers") && data["interestedUsers"] is IEnumerable<object> interestedList)
            {
                interestedUsers = interestedList.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList();
            }

            List<string> tags = new List<string>();
            if (data.ContainsKey("tags") && data["tags"] is IEnumerable<object> tagsList)
            {
                tags = tagsList.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList();
            }

            return new BuildingEvent(
                data.ContainsKey("buildingId") ? data["buildingId"].ToString() : string.Empty,
                data.ContainsKey("buildingName") ? data["buildingName"].ToString() : string.Empty,
                data.ContainsKey("eventName") ? data["eventName"].ToString() : string.Empty,
                eventDate,
                eventType,
                recurrenceData,
                attendees,
                doc.Id, // Firestore document ID is the event ID
                interestedUsers,
                data.ContainsKey("description") ? data["description"].ToString() : string.Empty,
                data.ContainsKey("gainedCoins") ? Convert.ToInt32(data["gainedCoins"]) : 0,
                data.ContainsKey("gainedKb") ? Convert.ToInt32(data["gainedKb"]) : 0
            )
            {
                imageUrl = data.ContainsKey("imageUrl") ? data["imageUrl"]?.ToString() : null,
                tags = tags
            };
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

            // Determine type based on date field (for events without an explicit eventType)
            if (!data.ContainsKey("date") || data["date"] == null)
            {
                return RamRoutes.Model.EventType.Always;
            }

            return RamRoutes.Model.EventType.Scheduled;
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
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error recording attendance: {ex.Message}");
                return false;
            }
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

        /// <summary>
        /// Get all building events. Date is a raw display string now, so there's no
        /// way to filter to "today" client-side - callers get everything.
        /// </summary>
        public async Task<List<BuildingEvent>> GetDailyEvents()
        {
            var allEvents = await GetBuildingEventsAsync();
            Debug.Log($"BuildingEventService: Found {allEvents.Count} events");
            return allEvents;
        }
    }
}

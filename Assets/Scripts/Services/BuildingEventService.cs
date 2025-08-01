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
            if (!forceRefresh && cachedEvents != null && cachedEvents.Count > 0)
            {
                Debug.Log("Returning building events from cache");
                return cachedEvents;
            }

            try
            {
                QuerySnapshot querySnapshot = await db.Collection("building-events").GetSnapshotAsync();
                List<BuildingEvent> events = new List<BuildingEvent>();

                foreach (DocumentSnapshot doc in querySnapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    var buildingEvent = new BuildingEvent(
                        data["buildingId"].ToString(),
                        data["buildingName"].ToString(),
                        data["eventName"].ToString(),
                        ((Timestamp)data["date"]).ToDateTime()
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
                    return new BuildingEvent(
                        data["buildingId"].ToString(),
                        data["buildingName"].ToString(),
                        data["eventName"].ToString(),
                        ((Timestamp)data["date"]).ToDateTime()
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching building event {buildingId}: {ex.Message}");
            }

            return null;
        }

        public void ClearCache()
        {
            PlayerPrefs.DeleteKey(CACHE_KEY);
            cachedEvents = new List<BuildingEvent>();
            Debug.Log("Building events cache cleared");
        }
    }
}

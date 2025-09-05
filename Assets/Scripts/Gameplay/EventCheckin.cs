using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using RamRoutes.Services;
using RamRoutes.Model;

public class EventCheckin : MonoBehaviour
{
    [SerializeField] private GameObject eventsPanel;
    [SerializeField] private Text buildingTitleText;
    [SerializeField] private ScrollRect eventsScrollView;
    [SerializeField] private Transform eventsContentParent;
    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private Button closeButton;
    
    private BuildingEventService eventService;
    private readonly TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private List<BuildingEvent> currentEvents; // Track currently displayed events

    private DateTime NormalizeDate(DateTime date)
    {
        DateTime utcDate = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        DateTime easternDate = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcDate, DateTimeKind.Utc), 
            easternZone);
        return easternDate;
    }
    
    private string FormatEventDate(DateTime date)
    {
        DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(date.ToUniversalTime(), DateTimeKind.Utc), 
            TimeZoneInfo.Local);
        return localDate.ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture);
    }

    void Start()
    {
        BuildingProximityDetector.OnEnterBuilding += OnBuildingEntered;
        BuildingInteraction.OnVirtualBuildingEntered += OnVirtualBuildingEntered;
        eventService = new BuildingEventService();
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideEventsPanel);
        }
    }

    async void OnBuildingEntered(BuildingProximityDetector.Building building)
    {
        var events = await eventService.GetBuildingEventsAsync(true);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
        var earliestTime = now.AddMinutes(-15);
        var latestTime = now.AddMinutes(15);

        List<BuildingEvent> relevantEvents = new List<BuildingEvent>();
        foreach (var evt in events)
        {
            if (evt.buildingName != building.name) continue;

            if (evt.eventType == RamRoutes.Model.EventType.Scheduled) 
            {
                DateTime normalizedEventDate = NormalizeDate(evt.date);
                if (normalizedEventDate >= earliestTime && normalizedEventDate <= latestTime)
                {
                    relevantEvents.Add(evt);
                }
            }
            else if (evt.IsAlwaysHappening)
            {
                relevantEvents.Add(evt);
            }
            else if (evt.IsRecurring && evt.IsActiveAt(now))
            {
                relevantEvents.Add(evt);
            }
        }
    
        DisplayEvents(relevantEvents, building.name);
    }
    
    private void DisplayEvents(List<BuildingEvent> events, string buildingName)
    {
        if (eventsContentParent == null || eventPrefab == null)
        {
            Debug.LogError("Events display components not set up properly");
            return;
        }
        
        // Store the current events list for index-based access
        currentEvents = new List<BuildingEvent>(events);
        
        if (buildingTitleText != null)
        {
            buildingTitleText.text = $"Now happening at {buildingName}";
        }
        
        foreach (Transform child in eventsContentParent)
        {
            Destroy(child.gameObject);
        }
        
        if (events.Any())
        {
            foreach (var evt in events)
            {
                GameObject eventItem = Instantiate(eventPrefab, eventsContentParent);
                
                Text[] allTexts = eventItem.GetComponentsInChildren<Text>();
                Text eventNameText = null;
                Text eventDateText = null;
                
                foreach (Text text in allTexts)
                {
                    if (text.CompareTag("MainText"))
                    {
                        eventNameText = text;
                    }
                    else if (text.CompareTag("SubText"))
                    {
                        eventDateText = text;
                    }
                }
                
                if (eventNameText != null)
                {
                    eventNameText.text = evt.eventName;
                }
                
                if (eventDateText != null)
                {
                    eventDateText.text = evt.GetDisplayDate();
                }
                Button checkInButton = eventItem.GetComponentInChildren<Button>();
                if (checkInButton != null)
                {
                    // Get or add EventItemButton component
                    EventItemButton itemButton = checkInButton.gameObject.GetComponent<EventItemButton>();
                    if (itemButton == null)
                    {
                        itemButton = checkInButton.gameObject.AddComponent<EventItemButton>();
                    }
                    
                    // Setup the button with the index in the current events list
                    int eventIndex = events.IndexOf(evt);
                    itemButton.Setup(this, eventIndex);
                }
            }
            
            if (eventsPanel != null)
            {
                eventsPanel.SetActive(true);
            }
            
            if (eventsScrollView != null)
            {
                eventsScrollView.normalizedPosition = new Vector2(0, 0);
            }
        }
        else
        {
            GameObject noEventsItem = Instantiate(eventPrefab, eventsContentParent);
            
            Text[] allTexts = noEventsItem.GetComponentsInChildren<Text>();
            Text noEventsMainText = null;
            Text noEventsSubText = null;
            
            foreach (Text text in allTexts)
            {
                if (text.CompareTag("MainText"))
                {
                    noEventsMainText = text;
                }
                else if (text.CompareTag("SubText"))
                {
                    noEventsSubText = text;
                }
            }
            
            if (noEventsMainText != null)
            {
                noEventsMainText.text = "No events happening soon";
            }
            
            if (noEventsSubText != null)
            {
                noEventsSubText.text = "";
            }
            
            Button checkInButton = noEventsItem.GetComponentInChildren<Button>();
            if (checkInButton != null)
            {
                checkInButton.gameObject.SetActive(false);
            }
            
            if (eventsPanel != null)
            {
                eventsPanel.SetActive(true);
            }
        }
    }
    
    private async void CheckInToEvent(BuildingEvent evt)
    {
        Debug.Log($"CheckInToEvent method called for event: {evt.eventName}");
        
        // Get the current player ID from PlayerPrefs or other source
        string playerId = PlayerPrefs.GetString("PlayerId", SystemInfo.deviceUniqueIdentifier);
        Debug.Log($"Player ID for check-in: {playerId}");
        
        // Check if player already checked in
        if (evt.attendees != null && evt.attendees.Contains(playerId))
        {
            Debug.Log($"Player {playerId} already checked in to event {evt.eventName}");
            return;
        }
        
        // Use eventId if available, otherwise fallback to buildingId
        string eventId = !string.IsNullOrEmpty(evt.eventId) ? evt.eventId : evt.buildingId;
        Debug.Log($"Attempting to record attendance for event: {evt.eventName} with ID: {eventId}");
        
        try
        {
            // Use the FirestoreUtility to record attendance
            bool success = await FirestoreUtility.RecordEventAttendance(
                eventId, 
                playerId, 
                evt.eventName, 
                evt.buildingName
            );
            
            if (success)
            {
                Debug.Log($"Successfully checked in to event: {evt.eventName}");
                
                // Add player to attendees list locally
                if (evt.attendees == null)
                {
                    evt.attendees = new List<string>();
                }
                evt.attendees.Add(playerId);
                
                // Update the UI
                RefreshAllEventItems();
            }
            else
            {
                Debug.LogError($"Failed to check in to event: {evt.eventName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception during check-in: {ex.Message}");
        }
    }
    
    // Refresh all event items in the UI - simpler approach
    private void RefreshAllEventItems()
    {
        Debug.Log("RefreshAllEventItems: Recreating all event UI items");
        
        // Store the current building name
        string currentBuildingName = buildingTitleText != null ? 
            buildingTitleText.text.Replace("Now happening at ", "") : "";
            
        // Just redisplay all events with updated attendance data
        DisplayEvents(currentEvents, currentBuildingName);
    }
    
    // Public method to check in to an event by index
    public void CheckInToEventByIndex(int index)
    {
        if (currentEvents != null && index >= 0 && index < currentEvents.Count)
        {
            CheckInToEvent(currentEvents[index]);
        }
        else
        {
            Debug.LogError($"Invalid event index: {index}. Current events count: {(currentEvents != null ? currentEvents.Count : 0)}");
        }
    }
    
    public void HideEventsPanel()
    {
        if (eventsPanel != null)
        {
            eventsPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        BuildingProximityDetector.OnEnterBuilding -= OnBuildingEntered;
        BuildingInteraction.OnVirtualBuildingEntered -= OnVirtualBuildingEntered;
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideEventsPanel);
        }
    }
    
    async void OnVirtualBuildingEntered(string buildingName)
    {
        var events = await eventService.GetBuildingEventsAsync(true);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
        var earliestTime = now.AddMinutes(-15);
        var latestTime = now.AddMinutes(15);

        List<BuildingEvent> relevantEvents = new List<BuildingEvent>();
        foreach (var evt in events)
        {
            if (evt.buildingName != buildingName) continue;
            
            if (evt.eventType == RamRoutes.Model.EventType.Scheduled)
            {
                DateTime normalizedEventDate = NormalizeDate(evt.date);
                if (normalizedEventDate >= earliestTime && normalizedEventDate <= latestTime)
                {
                    relevantEvents.Add(evt);
                }
            }
            else if (evt.IsAlwaysHappening)
            {
                relevantEvents.Add(evt);
            }
            else if (evt.IsRecurring && evt.IsActiveAt(now))
            {
                relevantEvents.Add(evt);
            }
        }
        
        DisplayEvents(relevantEvents, buildingName);
    }
}

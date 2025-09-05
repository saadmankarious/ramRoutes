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
                    BuildingEvent capturedEvent = evt;
                    checkInButton.onClick.AddListener(() => CheckInToEvent(capturedEvent));
                    
                    // Get player ID for checking attendance
                    string playerId = PlayerPrefs.GetString("PlayerId", SystemInfo.deviceUniqueIdentifier);
                    
                    // If player has already attended, disable the check-in button
                    if (evt.attendees != null && evt.attendees.Contains(playerId))
                    {
                        checkInButton.interactable = false;
                        checkInButton.GetComponentInChildren<Text>().text = "Checked In";
                    }
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
        // Get the current player ID from PlayerPrefs or other source
        string playerId = PlayerPrefs.GetString("PlayerId", SystemInfo.deviceUniqueIdentifier);
        
        // Check if player already checked in
        if (evt.attendees != null && evt.attendees.Contains(playerId))
        {
            Debug.Log($"Player {playerId} already checked in to event {evt.eventName}");
            return;
        }
        
        // Record the attendance
        bool success = await eventService.RecordAttendanceAsync(evt.buildingId, playerId);
        
        if (success)
        {
            Debug.Log($"Successfully checked in to event: {evt.eventName}");
            
            // Visual feedback for successful check-in
            if (eventsPanel != null)
            {
                // Update UI to reflect the check-in
                // For example, disable the check-in button or show a confirmation
                RefreshEventDisplay(evt);
            }
        }
        else
        {
            Debug.LogError($"Failed to check in to event: {evt.eventName}");
        }
    }
    
    // Helper method to refresh a specific event's display after check-in
    private void RefreshEventDisplay(BuildingEvent evt)
    {
        // Find the event display in the UI and update it
        foreach (Transform child in eventsContentParent)
        {
            // Find the event item corresponding to this event
            Button checkInButton = child.GetComponentInChildren<Button>();
            if (checkInButton != null)
            {
                // Get player ID for checking attendance
                string playerId = PlayerPrefs.GetString("PlayerId", SystemInfo.deviceUniqueIdentifier);
                
                // If player has attended, disable the check-in button
                if (eventService.HasPlayerAttended(evt.buildingId, playerId))
                {
                    checkInButton.interactable = false;
                    checkInButton.GetComponentInChildren<Text>().text = "Checked In";
                }
            }
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

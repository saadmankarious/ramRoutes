using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using RamRoutes.Services;
using RamRoutes.Model;
using Firebase.Auth;

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
        
        // Get the current authenticated user ID for filtering events
        string userId = "unknown";
        if (FirebaseAuth.DefaultInstance?.CurrentUser != null) 
        {
            userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        }
        
        // Filter out events the player has already checked into
        List<BuildingEvent> availableEvents = new List<BuildingEvent>();
        foreach (var evt in events)
        {
            if (evt.attendees == null || !evt.attendees.Contains(userId))
            {
                availableEvents.Add(evt);
            }
        }
        
        currentEvents = new List<BuildingEvent>(availableEvents);
        
        if (buildingTitleText != null)
        {
            buildingTitleText.text = $"Now happening at {buildingName}";
        }
        
        foreach (Transform child in eventsContentParent)
        {
            Destroy(child.gameObject);
        }
        
        if (currentEvents.Any())
        {
            foreach (var evt in currentEvents)
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
                ButtonHandler checkInButton = eventItem.GetComponentInChildren<ButtonHandler>();
                if (checkInButton != null)
                {
                    //Setup
        checkInButton.Initialize("data", () => CheckInToEvent(evt));
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
                // Check if we filtered out all events because user attended them all
                bool allEventsFiltered = events.Any() && !currentEvents.Any();
                noEventsMainText.text = allEventsFiltered ? 
                    "You've already checked in to all events here" : 
                    "No events happening soon";
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
    
    private UIManager uiManager;
    private RamRoutes.Services.UserService userService;
    
    void Awake()
    {
        // Find or get UIManager
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            uiManager = UIManager.Instance;
        }
        
        // Initialize UserService
        userService = new RamRoutes.Services.UserService();
    }
    
    private async void CheckInToEvent(BuildingEvent evt)
    {
        // Get the current authenticated user ID from Firebase Auth
        string playerId = "unknown";
        if (FirebaseAuth.DefaultInstance?.CurrentUser != null) 
        {
            playerId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        } 
        else 
        {
            if (uiManager != null)
            {
                HideEventsPanel();
                uiManager.ShowDialog("Please sign in to check in to events", 3f, false);
            }
            return;
        }
        
        // Check if player already checked in
        if (evt.attendees != null && evt.attendees.Contains(playerId))
        {
            if (uiManager != null)
            {
                HideEventsPanel();
                uiManager.ShowDialog($"You've already checked in to this event!", 3f, false);
            }
            return;
        }
        
        // Use eventId if available, otherwise fallback to buildingId
        string eventId = !string.IsNullOrEmpty(evt.eventId) ? evt.eventId : evt.buildingId;

        try
        {
            bool success = await eventService.RecordAttendanceAsync(eventId, playerId);
            
            if (success)
            {
                HideEventsPanel();

                // Award points to the user (50 coins and 50 knowledge points)
                if (userService != null)
                {
                    await userService.UpdateUserCoins(playerId, 50);
                    await userService.UpdateUserKnowledgePoints(playerId, 50);
                    var points = await userService.GetUserCoins(playerId);
                    var kb = await userService.GetUserKnowledgePoints(playerId);

                    uiManager.UpdateCoins(points);
                    uiManager.UpdateKnowledgePoints(kb);
                }
                
                if (uiManager != null)
                {
                    uiManager.ShowDialog($"You've checked in to {evt.eventName}! +50 coins, +50 knowledge points", 3f, false);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception during check-in: {ex.Message}");
        }
    }
    
    private void RefreshAllEventItems()
    {
        string currentBuildingName = buildingTitleText != null ? 
            buildingTitleText.text.Replace("Now happening at ", "") : "";
            
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

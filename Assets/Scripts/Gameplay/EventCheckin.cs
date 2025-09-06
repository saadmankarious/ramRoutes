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
    [SerializeField]
    private GameObject noEventsText;

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

    private BuildingProximityDetector proximityDetector;
    private BuildingInteraction currentBuildingInteraction;
    private UIManager uiManager;

    void Start()
    {
        // BuildingProximityDetector.OnEnterBuilding += OnBuildingEntered;
        BuildingInteraction.OnVirtualBuildingEntered += OnVirtualBuildingEntered;
        BuildingInteraction.OnVirtualBuildingExited += OnVirtualBuildingExited;
        eventService = new BuildingEventService();
        
        proximityDetector = FindObjectOfType<BuildingProximityDetector>();
        uiManager = FindObjectOfType<UIManager>();
        
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
    
    private async void DisplayEvents(List<BuildingEvent> events, string buildingName)
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
        
        // Filter out events the player has already checked into (check both systems)
        List<BuildingEvent> availableEvents = new List<BuildingEvent>();
        foreach (var evt in events)
        {
            bool checkedInOldSystem = evt.attendees != null && evt.attendees.Contains(userId);
            bool checkedInNewSystem = false;
            
            try
            {
                string checkEventId = !string.IsNullOrEmpty(evt.eventId) ? evt.eventId : evt.buildingId;
                checkedInNewSystem = await AttendanceService.HasPlayerCheckedInAsync(checkEventId, userId);
            }
            catch (Exception ex)
            {
            }
            
            // Only add if not checked in to either system
            if (!checkedInOldSystem && !checkedInNewSystem)
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
            
            // Show scrollview, hide no events text
            if (eventsScrollView != null)
            {
                eventsScrollView.gameObject.SetActive(true);
                eventsScrollView.normalizedPosition = new Vector2(0, 0);
            }
            
            // Make sure no events text is hidden when we have events
            if (noEventsText != null)
            {
                noEventsText.SetActive(false);
            }
        }
        else
        {
            // Show the events panel but hide the scroll view
            if (eventsPanel != null)
            {
                eventsPanel.SetActive(true);
            }
            
            if (eventsScrollView != null)
            {
                eventsScrollView.gameObject.SetActive(false);
            }
            
            // Check if we filtered out all events because user attended them all
            bool allEventsFiltered = events.Any() && !currentEvents.Any();
            
            // Show the "no events" text
            if (noEventsText != null)
            {
                noEventsText.SetActive(true);
                
                // Update the text component in noEventsText if available
                Text messageText = noEventsText.GetComponentInChildren<Text>();
                if (messageText != null)
                {
                    messageText.text = allEventsFiltered ? 
                        "You've already checked in to all events at this location" : 
                        "No events happening at this location right now";
                }
            }
        }
    }
    
    private RamRoutes.Services.UserService userService;
    
    void Awake()
    {
        // Find or get UIManager if not already set in Start
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                uiManager = UIManager.Instance;
            }
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
        
        // Check if player already checked in (check both systems)
        bool alreadyCheckedInOldSystem = evt.attendees != null && evt.attendees.Contains(playerId);
        bool alreadyCheckedInNewSystem = false;
        
        try
        {
            string checkEventId = !string.IsNullOrEmpty(evt.eventId) ? evt.eventId : evt.buildingId;
            alreadyCheckedInNewSystem = await AttendanceService.HasPlayerCheckedInAsync(checkEventId, playerId);
        }
        catch (Exception ex)
        {
        }
        
        if (alreadyCheckedInOldSystem || alreadyCheckedInNewSystem)
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
            bool eventServiceSuccess = await eventService.RecordAttendanceAsync(eventId, playerId);
            
            var user = await userService.RetrieveUserById(playerId);
            string username = user?.name ?? $"Player_{playerId.Substring(0, Math.Min(8, playerId.Length))}";

            var attendanceRecord = new AttendanceRecord(
                eventId,
                evt.eventName,
                evt.buildingName,
                username,
                DateTime.Now,
                playerId,
                evt.buildingId
            );

            await AttendanceService.RecordAttendanceAsync(attendanceRecord);
            
            HideEventsPanel();

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
        catch (Exception ex)
        {
        }
    }
    
    private void RefreshAllEventItems()
    {
        string currentBuildingName = buildingTitleText != null ? 
            buildingTitleText.text.Replace("NOW happening at ", "") : "";
            
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

    private bool IsPlayerCloseToBuilding(string buildingName, bool bypassGpsCheck = false)
    {
        if (bypassGpsCheck)
        {
            return true;
        }
        // Ensure we have a valid proximity detector
        if (proximityDetector == null)
        {
            proximityDetector = FindObjectOfType<BuildingProximityDetector>();
            if (proximityDetector == null)
            {
                Debug.LogWarning("BuildingProximityDetector not found in scene. Cannot check GPS proximity.");
                return false;
            }
        }
        
        // Check if location services are available and running
        if (!Input.location.isEnabledByUser || Input.location.status != LocationServiceStatus.Running)
        {
            return false;
        }
        
        // Find the target building in the proximity detector's building list
        BuildingProximityDetector.Building targetBuilding = null;
        foreach (var building in proximityDetector.buildings)
        {
            if (building.name == buildingName)
            {
                targetBuilding = building;
                break;
            }
        }
        
        if (targetBuilding == null)
        {
            Debug.LogWarning($"Building '{buildingName}' not found in BuildingProximityDetector's building list.");
            return false;
        }
        
        // Get current player location
        LocationInfo currentLocation = Input.location.lastData;
        
        // Calculate distance using the proximity detector's method
        float distance = proximityDetector.CalculatePreciseDistance(
            currentLocation.latitude,
            currentLocation.longitude,
            targetBuilding.entranceGPS.x,
            targetBuilding.entranceGPS.y
        );
        
        return distance <= targetBuilding.detectionRadius;
    }

    void OnDestroy()
    {
        // BuildingProximityDetector.OnEnterBuilding -= OnBuildingEntered;
        BuildingInteraction.OnVirtualBuildingEntered -= OnVirtualBuildingEntered;
        BuildingInteraction.OnVirtualBuildingExited -= OnVirtualBuildingExited;
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideEventsPanel);
        }
    }
    
    async void OnVirtualBuildingEntered(BuildingInteraction buildingData)
    {
        if (buildingData == null)
        {
            Debug.LogError("BuildingInteraction is null in OnVirtualBuildingEntered");
            return;
        }
        
        string buildingName = buildingData.buildingName;
        currentBuildingInteraction = buildingData;
        
        if (string.IsNullOrEmpty(buildingName))
        {
            Debug.LogError("Building name is null or empty in OnVirtualBuildingEntered");
            return;
        }
        
        // Fetch events for the building first to determine if there are any events
        if (eventService == null)
        {
            eventService = new BuildingEventService();
        }
        
        var events = await eventService.GetBuildingEventsAsync(true);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
        var earliestTime = now.AddMinutes(-15);
        var latestTime = now.AddMinutes(15);

        // Determine relevant events first
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
        
        // Only proceed if there are events
        if (relevantEvents.Count == 0)
        {
            // No events to show, just return
            return;
        }
        
        // Check if player is close to the building
        if (!IsPlayerCloseToBuilding(buildingName, buildingData.bypassGpsCheck))
        {
            // Get the current authenticated user ID for filtering events
            string userId = "unknown";
            if (FirebaseAuth.DefaultInstance?.CurrentUser != null) 
            {
                userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            }
            
            // Filter out events the player has already checked into (check both systems)
            List<BuildingEvent> availableEvents = new List<BuildingEvent>();
            foreach (var evt in relevantEvents)
            {
                bool checkedInOldSystem = evt.attendees != null && evt.attendees.Contains(userId);
                bool checkedInNewSystem = false;
                
                try
                {
                    string checkEventId = !string.IsNullOrEmpty(evt.eventId) ? evt.eventId : evt.buildingId;
                    checkedInNewSystem = await AttendanceService.HasPlayerCheckedInAsync(checkEventId, userId);
                }
                catch (Exception ex)
                {
                }
                
                // Only add if not checked in to either system
                if (!checkedInOldSystem && !checkedInNewSystem)
                {
                    availableEvents.Add(evt);
                }
            }
            
            // Only show proximity message if there are events the player hasn't checked into yet
            if (availableEvents.Count > 0)
            {
                string eventCountText = availableEvents.Count == 1 ? "1 event" : $"{availableEvents.Count} events";
                string messagePrefix = availableEvents.Count == 1 ? "There is" : "There are";
                string messageSuffix = availableEvents.Count == 1 ? "it" : "them";
                if (uiManager != null)
                {
                    uiManager.ShowDialog($"{messagePrefix} {eventCountText} happening NOW at {buildingName}, but you need to be closer in real life to see {messageSuffix}", 3f, false);
                }
            }
            return;
        }
        
        if (buildingTitleText != null)
        {
            buildingTitleText.text = $"Now happening at {buildingName}";
        }
        
        // Display the events since we're close enough
        DisplayEvents(relevantEvents, buildingName);
    }
    
    void OnVirtualBuildingExited(BuildingInteraction buildingData)
    {
        // Hide the events panel when player leaves the building
        HideEventsPanel();
    }
}

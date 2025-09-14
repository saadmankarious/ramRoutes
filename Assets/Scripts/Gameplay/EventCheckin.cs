using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
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
        
        // Filter out events the player has already checked into
        List<BuildingEvent> availableEvents = new List<BuildingEvent>();
        foreach (var evt in events)
        {
            bool canCheckIn = true;
            
            try
            {
                string checkEventId = !string.IsNullOrEmpty(evt.eventId) ? evt.eventId : evt.buildingId;
                
                if (evt.eventType == RamRoutes.Model.EventType.Daily)
                {
                    // For daily events, check if player already checked in today
                    canCheckIn = !await AttendanceService.HasPlayerCheckedInTodayAsync(checkEventId, userId);
                }
                else
                {
                    // For non-daily events, check if player has ever checked in
                    canCheckIn = !await AttendanceService.HasPlayerCheckedInAsync(checkEventId, userId);
                }
            }
            catch (Exception ex)
            {
            }
            
            if (canCheckIn)
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
        
        // Check if player can check in
        bool canCheckIn = true;
        
        try
        {
            string checkEventId = !string.IsNullOrEmpty(evt.eventId) ? evt.eventId : evt.buildingId;
            
            if (evt.eventType == RamRoutes.Model.EventType.Daily)
            {
                // For daily events, check if player already checked in today
                canCheckIn = !await AttendanceService.HasPlayerCheckedInTodayAsync(checkEventId, playerId);
            }
            else
            {
                // For non-daily events, check if player has ever checked in
                canCheckIn = !await AttendanceService.HasPlayerCheckedInAsync(checkEventId, playerId);
            }
        }
        catch (Exception ex)
        {
        }
        
        if (!canCheckIn)
        {
            if (uiManager != null)
            {
                HideEventsPanel();
                string message = evt.eventType == RamRoutes.Model.EventType.Daily ? 
                    "You've already checked in to this event today!" : 
                    "You've already checked in to this event!";
                uiManager.ShowQuickUpdate(message);
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
                uiManager.ShowQuickUpdate($"You've checked in to {evt.eventName}!");
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
        var now = DateTime.Now;
        var earliestTime = now.AddMinutes(-15);
        var latestTime = now.AddMinutes(15);

        // Get the current authenticated user ID for filtering events
        string userId = "unknown";
        if (FirebaseAuth.DefaultInstance?.CurrentUser != null) 
        {
            userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        }

        // Determine relevant events using the new eligibility function
        List<BuildingEvent> relevantEvents = new List<BuildingEvent>();
        foreach (var evt in events)
        {
            if (await IsEventEligibleForCheckInRightNow(evt, buildingName, userId, now, earliestTime, latestTime))
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
            // Only show proximity message if there are events available
            if (relevantEvents.Count > 0)
            {
                string eventCountText = relevantEvents.Count == 1 ? "1 event" : $"{relevantEvents.Count} events";
                string messagePrefix = relevantEvents.Count == 1 ? "There is" : "There are";
                string messageSuffix = relevantEvents.Count == 1 ? "it" : "them";
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

    private async Task<bool> IsEventEligibleForCheckInRightNow(BuildingEvent evt, string buildingName, string userId, DateTime now, DateTime earliestTime, DateTime latestTime)
    {
        // Event MUST be in the correct building
        if (evt.buildingName != buildingName) 
            return false;

        // Player cannot check in for same event twice a day regardless of type
        try
        {
            string checkEventId = !string.IsNullOrEmpty(evt.eventId) ? evt.eventId : evt.buildingId;
            bool hasCheckedInToday = await AttendanceService.HasPlayerCheckedInTodayAsync(checkEventId, userId);
            if (hasCheckedInToday)
                return false;
        }
        catch (Exception ex)
        {
            return false;
        }

        // Event MUST be happening within 15 minutes regardless of type (except always happening)
        if (evt.IsAlwaysHappening)
        {
            return true;
        }
        else if (evt.eventType == RamRoutes.Model.EventType.Scheduled)
        {
            DateTime normalizedEventDate = NormalizeDate(evt.date);
            return normalizedEventDate >= earliestTime && normalizedEventDate <= latestTime;
        }
        else if (evt.eventType == RamRoutes.Model.EventType.Daily)
        {
            // For daily events, check if the time of day matches (within 15 minutes)
            // DateTime normalizedEventDate = NormalizeDate(evt.date);
            TimeSpan eventTimeOfDay = evt.date.TimeOfDay;
            TimeSpan currentTimeOfDay = now.TimeOfDay;
            
            // Check if current time is within 15 minutes of the event time
            TimeSpan timeDifference = (currentTimeOfDay - eventTimeOfDay).Duration();
            return timeDifference <= TimeSpan.FromMinutes(15);
        }
        else if (evt.IsRecurring && evt.IsActiveAt(now))
        {
            // For recurring events (weekly, monthly), check if the time of day matches
            DateTime normalizedEventDate = NormalizeDate(evt.date);
            TimeSpan eventTimeOfDay = normalizedEventDate.TimeOfDay;
            TimeSpan currentTimeOfDay = now.TimeOfDay;
            
            // Check if current time is within 15 minutes of the event time
            TimeSpan timeDifference = (currentTimeOfDay - eventTimeOfDay).Duration();
            return timeDifference <= TimeSpan.FromMinutes(15);
        }

        return false;
    }
}

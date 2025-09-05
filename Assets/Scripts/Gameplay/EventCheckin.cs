using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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

    void Start()
    {
        BuildingProximityDetector.OnEnterBuilding += OnBuildingEntered;
        BuildingInteraction.OnVirtualBuildingEntered += OnVirtualBuildingEntered;
        eventService = new BuildingEventService();
        
        // Set up close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideEventsPanel);
        }
    }

    async void OnBuildingEntered(BuildingProximityDetector.Building building)
    {
        Debug.Log($"EventCheckin: Building entered - {building.name}");
        
        var events = await eventService.GetBuildingEventsAsync(true);
        var now = DateTime.Now;
        var soon = now.AddMinutes(15);
        
        var relevantEvents = events.Where(evt => 
            evt.buildingName == building.name && 
            (evt.IsAlwaysHappening || evt.IsActiveAt(now) || evt.IsRecurring || 
            (evt.date >= now && evt.date <= soon))).ToList();
        
        DisplayEvents(relevantEvents, building.name);
    }
    
    private void DisplayEvents(List<BuildingEvent> events, string buildingName)
    {
        if (eventsContentParent == null || eventPrefab == null)
        {
            Debug.LogError("Events display components not set up properly");
            return;
        }
        
        // Set building name in title text
        if (buildingTitleText != null)
        {
            buildingTitleText.text = $"Now happening at {buildingName}";
        }
        
        // Clear existing event items
        foreach (Transform child in eventsContentParent)
        {
            Destroy(child.gameObject);
        }
        
        if (events.Any())
        {
            Debug.Log($"Found {events.Count} events happening now or soon at {buildingName}");
            
            foreach (var evt in events)
            {
                GameObject eventItem = Instantiate(eventPrefab, eventsContentParent);
                
                // Find text components tagged as MainText and SubText
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
                
                // Set event name in MainText
                if (eventNameText != null)
                {
                    eventNameText.text = evt.eventName;
                }
                
                // Set event date in SubText
                if (eventDateText != null)
                {
                    eventDateText.text = evt.GetDisplayDate();
                }
                
                // Set up check-in button
                Button checkInButton = eventItem.GetComponentInChildren<Button>();
                if (checkInButton != null)
                {
                    BuildingEvent capturedEvent = evt; // Capture for lambda
                    checkInButton.onClick.AddListener(() => CheckInToEvent(capturedEvent));
                }
            }
            
            // Show the panel and reset horizontal scroll position
            if (eventsPanel != null)
            {
                eventsPanel.SetActive(true);
            }
            
            if (eventsScrollView != null)
            {
                // Reset scroll position to left for horizontal scroll view
                eventsScrollView.normalizedPosition = new Vector2(0, 0);
            }
        }
        else
        {
            Debug.Log($"No events happening soon at {buildingName}");
            // Optionally show "no events" message
            GameObject noEventsItem = Instantiate(eventPrefab, eventsContentParent);
            
            // Find text components tagged as MainText and SubText
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
            
            // Set "no events" message in MainText
            if (noEventsMainText != null)
            {
                noEventsMainText.text = "No events happening soon";
            }
            
            // Clear SubText
            if (noEventsSubText != null)
            {
                noEventsSubText.text = "";
            }
            
            Button checkInButton = noEventsItem.GetComponentInChildren<Button>();
            if (checkInButton != null)
            {
                checkInButton.gameObject.SetActive(false);
            }
            
            // Show the panel
            if (eventsPanel != null)
            {
                eventsPanel.SetActive(true);
            }
        }
    }
    
    private void CheckInToEvent(BuildingEvent evt)
    {
        Debug.Log($"Checking in to event: {evt.eventName} at {evt.buildingName}");
        // Here you would implement the check-in logic
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
        // Clean up event listeners
        BuildingProximityDetector.OnEnterBuilding -= OnBuildingEntered;
        BuildingInteraction.OnVirtualBuildingEntered -= OnVirtualBuildingEntered;
        
        // Clean up button listener
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideEventsPanel);
        }
    }
    
    async void OnVirtualBuildingEntered(string buildingName)
    {
        Debug.Log($"EventCheckin: Building entered virtually - {buildingName}");
        
        var events = await eventService.GetBuildingEventsAsync(true);
        var now = DateTime.Now;
        var soon = now.AddMinutes(15);
        
        var relevantEvents = events.Where(evt => 
            evt.buildingName == buildingName && 
            (evt.IsAlwaysHappening || evt.IsActiveAt(now) || evt.IsRecurring || 
            (evt.date >= now && evt.date <= soon))).ToList();
        
        DisplayEvents(relevantEvents, buildingName);
    }
}

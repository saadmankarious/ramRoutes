using UnityEngine;
using UnityEngine.UI;
using RamRoutes.Model;
using RamRoutes.Services;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class DailyEvents : MonoBehaviour
{
    public static DailyEvents Instance { get; private set; }
    
    [Header("Events ScrollView")]
    public ScrollRect eventsScrollView; // ScrollView component
    public Transform eventListContent; // Content area of the ScrollView
    public GameObject eventPrefab; // Prefab with "name", "coins", "kb" objects
    public Text totalEventsText;
    public Text loadingText;
    
    private BuildingEventService eventService;
    private List<BuildingEvent> allEvents;
    private bool isLoading = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        eventService = new BuildingEventService();
        allEvents = new List<BuildingEvent>();
    }
    
    void Start()
    {
        LoadAllEvents();
        StartCoroutine(AutoScroll());
    }
    
    private async Task LoadAllEvents()
    {
        if (isLoading) return;
        
        isLoading = true;
        SetLoadingState(true);
        
        try
        {
            Debug.Log("DailyEvents: Loading daily events from Firestore...");
            allEvents = await eventService.GetDailyEvents();
            DisplayEventsInScrollView();
            
            if (totalEventsText != null)
            {
                totalEventsText.text = $"Total Events: {allEvents.Count}";
            }
            
            Debug.Log($"DailyEvents: Successfully loaded {allEvents.Count} events");
        }
        catch (Exception ex)
        {
            Debug.LogError($"DailyEvents: Error loading events: {ex.Message}");
            
            if (totalEventsText != null)
            {
                totalEventsText.text = "Error loading events";
            }
        }
        finally
        {
            isLoading = false;
            SetLoadingState(false);
        }
    }
    
    private IEnumerator AutoScroll()
    {
        yield return new WaitForSeconds(2f); // Wait for events to load
        
        while (true)
        {
            if (eventsScrollView != null && allEvents.Count > 0)
            {
                float scrollSpeed = 0.1f; // Adjust scroll speed
                
                // Scroll down slowly
                while (eventsScrollView.verticalNormalizedPosition > 0f)
                {
                    eventsScrollView.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;
                    yield return null;
                }
                
                yield return new WaitForSeconds(1f); // Pause at bottom
                
                // Reset to top
                eventsScrollView.verticalNormalizedPosition = 1f;
                yield return new WaitForSeconds(1f); // Pause at top
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }
    
    private void DisplayEventsInScrollView()
    {
        // Clear existing items in ScrollView content
        if (eventListContent != null)
        {
            foreach (Transform child in eventListContent)
            {
                Destroy(child.gameObject);
            }
        }
        
        // Create event items in ScrollView
        foreach (var evt in allEvents)
        {
            CreateEventItem(evt);
        }
        
        // Reset scroll position to top
        if (eventsScrollView != null)
        {
            Canvas.ForceUpdateCanvases();
            eventsScrollView.verticalNormalizedPosition = 1f;
        }
    }
    
    private void CreateEventItem(BuildingEvent evt)
    {
        if (eventPrefab == null || eventListContent == null) return;

        // Only show events with status "Terminal"
        var gameStage = GameStageService.LoadStageFromPrefs();

        if (gameStage != null && gameStage.area != Stage.Terminal) return;

        GameObject eventItem = Instantiate(eventPrefab, eventListContent);
        
        // Find the required objects by name: "name", "coins", "kb"
        GameObject nameObject = FindChildByName(eventItem, "name");
        GameObject coinsObject = FindChildByName(eventItem, "coins");
        GameObject kbObject = FindChildByName(eventItem, "kb");
        
        // Set event name
        if (nameObject != null)
        {
            Text nameText = nameObject.GetComponent<Text>();
            if (nameText != null)
            {
                // Create comprehensive event name with status and location
        string status = evt.GetDisplayDate();
                string eventName = evt.eventName;
                
                if (!string.IsNullOrEmpty(evt.buildingName))
                {
                    eventName += $" @ {evt.buildingName}";
                }
                
                nameText.text = $"[ {eventName}";
                // nameText.color = GetStatusColor(status);
                
                Debug.Log($"Set event name: {nameText.text}");
            }
        }
        
        // Set coins information
        if (coinsObject != null)
        {
            Text coinsText = coinsObject.GetComponent<Text>();
            if (coinsText != null)
            {
                if (evt.gainedCoins > 0)
                {
                    coinsText.text = $"Coins: {evt.gainedCoins}";
                }
                else
                {
                    coinsText.text = evt.date.ToString("MMM dd");
                }
                
                Debug.Log($"Set coins: {coinsText.text}");
            }
        }
        
        // Set KB (Knowledge Points) information
        if (kbObject != null)
        {
            Text kbText = kbObject.GetComponent<Text>();
            if (kbText != null)
            {
                if (evt.gainedKb > 0)
                {
                    kbText.text = $"KB: {evt.gainedKb}";
                }
                else if (!string.IsNullOrEmpty(evt.description))
                {
                    string desc = evt.description.Length > 25 
                        ? evt.description.Substring(0, 25) + "..." 
                        : evt.description;
                    kbText.text = desc;
                }
                else
                {
                    kbText.text = evt.date.ToString("HH:mm");
                }
                
                Debug.Log($"Set KB: {kbText.text}");
            }
        }
    }
    
    private GameObject FindChildByName(GameObject parent, string childName)
    {
        Transform found = parent.transform.Find(childName);
        return found != null ? found.gameObject : null;
    }
    

    
    private Color GetStatusColor(string status)
    {
        return status switch
        {
            "ALWAYS" => new Color(1f, 0.8f, 0f), // Golden
            "TODAY" => new Color(0.2f, 0.8f, 0.2f), // Green
            "UPCOMING" => Color.white, // White
            "PAST" => Color.gray, // Gray
            _ => Color.white
        };
    }
    
    private void SetLoadingState(bool loading)
    {
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(loading);
            loadingText.text = loading ? "Loading events..." : "";
        }
        
    
    }
    

}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using RamRoutes.Model;
using RamRoutes.Services;
using Firebase.Auth;
using System;

public class BuildingInteraction : MonoBehaviour
{

    [Header("Inactive Display")]
            public Text buildingTitleUnlcoked;
    [SerializeField] private GameObject buildingEventsPanel;
    [SerializeField] private ScrollRect eventsScrollView;
    [SerializeField] private Transform eventsContentParent;
    [SerializeField] private GameObject eventPrefab;
        [SerializeField] private Button buildingEventsToggle;


    [Header("User Location Display")]
    private Dictionary<string, GameObject> activeUserLocations = new Dictionary<string, GameObject>();

     private float eventsScrollSpeed = 0.001f;
   private float eventsResetDelay = 2f;
    private bool isEventsScrollingPaused = false;
    private bool isEventsResetting = false;

    [Header("GPS Integration")]
    [SerializeField] public bool bypassGpsCheck = false;

    [Header("Player Movement")]
    [SerializeField] public GameObject playerTargetPoint;

    public bool isPlayerInRange = false;

    private AudioSource audioSource;
    public string buildingName;
    private GameObject inactiveInstance;
    private Material originalMaterial;
    private SpriteRenderer sr;
    private bool lastGpsProximityState = false;


    public GameObject rsvpUserPrefab;

    private List<BuildingEvent> cachedBuildingEvents;
    private bool eventsLoaded = false;
    private UIManager uiManager;
    private BuildingProximityDetector proximityDetector;
    private RamsManager ramsManager;
    
    private Dictionary<string, List<string>> eventRsvpLists = new Dictionary<string, List<string>>();
    
    public delegate void VirtualBuildingEntryEvent(BuildingInteraction buildingData);
    public static event VirtualBuildingEntryEvent OnVirtualBuildingEntered;
    public static event VirtualBuildingEntryEvent OnVirtualBuildingExited;

    private async void EnterBuildingViewingMode(bool showEventsHappening = true)
    {
        await Task.Delay(1000);
        if (buildingTitleUnlcoked != null)
        {
            var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
            buildingTitleUnlcoked.text = buildingInfo.displayName;
        }

        var buildingData = GetComponent<BuildingInteraction>();
        if (showEventsHappening) OnVirtualBuildingEntered?.Invoke(buildingData);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetBuildingViewingMode(true, buildingName);
        }

        var npcSpawner = FindObjectOfType<NPCSpawner>();
        if (npcSpawner != null)
        {
            npcSpawner.SpawnNPCForBuildingOnEnter(buildingName);
        }

        if (buildingEventsPanel != null)
        {
            if (eventsLoaded)
            {
                DisplayBuildingEvents();
            }
            else
            {
                StartCoroutine(ShowEventsWhenReady());
            }
        }

        if (uiManager != null)
        {
            uiManager.DisplayCurrentUsersForBuilding(buildingName);
        }
        if (ramsManager != null)
        {
            ramsManager.OnBuildingActivated();
        }
                string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";

                var userService = new UserService();

                    await userService.UpdateCurrentBuilding(userId, buildingName);

    }

    private IEnumerator ShowEventsWhenReady()
    {
        while (!eventsLoaded)
        {
            yield return null;
        }
        DisplayBuildingEvents();
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 0.5f;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalMaterial = sr.material;
        }
    }

    void Start()
    {
        uiManager = UIManager.Instance;
        proximityDetector = FindObjectOfType<BuildingProximityDetector>();
        ramsManager = GetComponent<RamsManager>();

        var service = new UnlockedBuildingService();

        if(buildingEventsToggle != null)
        {
            buildingEventsToggle.onClick.AddListener(ToggleEventsVisibility);
        }

        _ = FetchBuildingEvents();
    }


    void OnDestroy()
    {
        if (buildingEventsToggle != null)
        {
            buildingEventsToggle.onClick.RemoveListener(ToggleEventsVisibility);
        }
        
    }

    void Update()
    {
    
        if (eventsScrollView != null && !isEventsScrollingPaused && eventsContentParent.childCount > 0 && buildingEventsPanel != null && buildingEventsPanel.activeInHierarchy)
        {
            float contentHeight = 0;
            foreach (RectTransform child in eventsContentParent)
            {
                contentHeight += child.rect.height;
            }

            if (contentHeight > eventsScrollView.viewport.rect.height)
            {
                float newPosition = eventsScrollView.verticalNormalizedPosition - eventsScrollSpeed;

                if (newPosition <= 0)
                {
                    StartCoroutine(ResetEventsScrollPosition());
                }
                else
                {
                    eventsScrollView.verticalNormalizedPosition = newPosition;
                }
            }
        }
    }
    
    private float lastToggleTime = 0f;
    private const float TOGGLE_COOLDOWN = 0.2f;
    
    public void ToggleEventsVisibility()
    {
        if (Time.time - lastToggleTime < TOGGLE_COOLDOWN)
        {
            return;
        }
        lastToggleTime = Time.time;
        
        if (buildingEventsPanel != null)
        {
            bool isCurrentlyActive = buildingEventsPanel.activeSelf;
            buildingEventsPanel.SetActive(!isCurrentlyActive);
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Spaceship"))
        {
            isPlayerInRange = true;
            EnterBuildingViewingMode();                
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Spaceship"))
        {
            Debug.Log($"BuildingInteraction: Player exited building '{buildingName}' trigger area");

            isPlayerInRange = false;
            lastGpsProximityState = false;

            if (uiManager != null && uiManager.IsDialogActive())
            {
                uiManager.HideDialog();
            }

            if (buildingEventsPanel != null)
            {
                buildingEventsPanel.SetActive(false);
            }

            OnVirtualBuildingExited?.Invoke(this);


            foreach (var indicator in activeUserLocations.Values)
            {
                Destroy(indicator);
            }
            activeUserLocations.Clear();

            var npcSpawner = FindObjectOfType<NPCSpawner>();
            if ( npcSpawner != null)
            {
                Debug.Log($"BuildingInteraction: Player left activated building '{buildingName}', calling despawn NPCs");
                npcSpawner.DespawnNPCsForBuilding(buildingName);
            }
          

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetBuildingViewingMode(false);
            }
            
               if (ramsManager != null)
        {
            ramsManager.OnPlayerLeavesBuilding();
        }
        }
    }

    private void HandleApproachBuilding(BuildingProximityDetector.Building building)
    {
        Debug.Log("Building Interaction:: Approaching building " + building.name);
    }


    private bool IsPlayerCloseToBuilding()
    {
        if (bypassGpsCheck)
        {
            return true;
        }
        
        if (proximityDetector == null)
        {
            Debug.LogWarning("BuildingProximityDetector not found in scene. Cannot check GPS proximity.");
            return false;
        }
        
        if (!Input.location.isEnabledByUser || Input.location.status != LocationServiceStatus.Running)
        {
            return false;
        }
        
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
        
        LocationInfo currentLocation = Input.location.lastData;
        
        float distance = proximityDetector.CalculatePreciseDistance(
            currentLocation.latitude,
            currentLocation.longitude,
            targetBuilding.entranceGPS.x,
            targetBuilding.entranceGPS.y
        );
        
        Debug.Log($"GPS Distance to {buildingName}: {distance:F1}m (threshold: {targetBuilding.detectionRadius}m)");
        
        return distance <= targetBuilding.detectionRadius;
    }


    private async Task FetchBuildingEvents()
    {
        if (eventsLoaded) return;
        var eventService = new BuildingEventService();
        var allEvents = await eventService.GetBuildingEventsAsync(forceRefresh: true);
        cachedBuildingEvents = allEvents.FindAll(e => e.buildingName == buildingName);
        Debug.Log("fetching events for building " + buildingName + cachedBuildingEvents.Count);

        eventsLoaded = true;
    }

    private async void RsvpToEvent(string eventId)
    {
        string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
        
        var eventService = new BuildingEventService();
        
        bool isAlreadyInterested = await eventService.HasPlayerShownInterest(eventId, userId);
        
        if (isAlreadyInterested)
        {
            await eventService.RemoveInterestAsync(eventId, userId);
            Debug.Log($"Removed interest for user {userId} from event {eventId}");
            
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate("Removed from event interest");
            }
        }
        else
        {
            await eventService.RecordInterestAsync(eventId, userId);
            
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate("Added to Interest List!");
            }
        }
        
        await LoadEventRsvpList(eventId);
        
        UpdateEventRsvpDisplay(eventId);
    }
    
    private async Task LoadEventRsvpList(string eventId)
    {
        var eventService = new BuildingEventService();
        var eventData = await eventService.GetBuildingEventByIdAsync(eventId);
        if (eventData != null)
        {
            eventRsvpLists[eventId] = eventData.interestedUsers ?? new List<string>();
        }
        else
        {
            eventRsvpLists[eventId] = new List<string>();
        }
    }
    
    private System.Collections.IEnumerator LoadAndDisplayRsvpList(GameObject eventGO, string eventId)
    {
        var loadTask = LoadEventRsvpList(eventId);
        yield return new WaitUntil(() => loadTask.IsCompleted);
        
        PopulateEventRsvpList(eventGO, eventId);
    }
    
    private void UpdateEventRsvpDisplay(string eventId)
    {
        foreach (Transform child in eventsContentParent)
        {
            var eventData = child.GetComponent<EventDisplayData>();
            if (eventData != null && eventData.eventId == eventId)
            {
                PopulateEventRsvpList(child.gameObject, eventId);
                break;
            }
        }
    }
    
    private async void PopulateEventRsvpList(GameObject eventGO, string eventId)
    {
        ScrollRect studentsScrollView = null;
        Transform studentsContentParent = null;
        Text emptyStateText = null;
        
        ScrollRect[] scrollRects = eventGO.GetComponentsInChildren<ScrollRect>();
        foreach (var scroll in scrollRects)
        {
            if (scroll.gameObject.name == "students-list")
            {
                studentsScrollView = scroll;
                studentsContentParent = scroll.content;
                break;
            }
        }
        
        Text[] texts = eventGO.GetComponentsInChildren<Text>(true);
        foreach (var text in texts)
        {
            if (text.gameObject.name.ToLower().Contains("empty") || text.gameObject.name == "empty")
            {
                emptyStateText = text;
                break;
            }
        }
        
        if (studentsScrollView == null || studentsContentParent == null)
        {
            Debug.LogWarning($"Students list not found in event prefab for event {eventId}");
            return;
        }
        
        foreach (Transform child in studentsContentParent)
        {
            Destroy(child.gameObject);
        }
        
        if (!eventRsvpLists.ContainsKey(eventId))
        {
            await LoadEventRsvpList(eventId);
        }
        
        var rsvpList = eventRsvpLists.ContainsKey(eventId) ? eventRsvpLists[eventId] : new List<string>();
        
        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(rsvpList.Count == 0);
        }
        
        var userService = new UserService();
        foreach (var userId in rsvpList)
        {
            var user = await userService.RetrieveUserById(userId);
            if (user != null)
            {
                GameObject studentGO = Instantiate(rsvpUserPrefab, studentsContentParent);
            ButtonHandler rsvpButtonHandler = studentGO.GetComponentInChildren<ButtonHandler>();

            if (rsvpButtonHandler != null)
            {
                rsvpButtonHandler.Initialize("data", () => ChatWithFriend(user));
            }
                Text userNameText = studentGO.GetComponentInChildren<Text>(true);
                Image userImage = studentGO.GetComponentInChildren<Image>(true);
                
                if (userNameText != null)
                {
                    userNameText.text = user.name;
                }
                
                if (userImage != null && uiManager != null)
                {
                    var userPoints = await userService.GetUserCoins(userId);
                    var userKnowledgePoints = await userService.GetUserKnowledgePoints(userId);
                    userImage.sprite = uiManager.GetUserAvatarBasedOnPoints(userPoints, userKnowledgePoints);
                }
            }
        }
    }   
    
    private void ChatWithFriend(User user)
    {
        if (user.userId == FirebaseAuth.DefaultInstance.CurrentUser.UserId)
        {
            return;
        }
         ChatManager chatManager = FindObjectOfType<ChatManager>();
        if (chatManager != null)
        {
            chatManager.StartChatWithUser(user);
        }
    }

    private void DisplayBuildingEvents()
    {
        if (eventsContentParent == null || eventPrefab == null)
        {
            Debug.LogError("Events UI components not set up!");
            return;
        }

        foreach (Transform child in eventsContentParent)
        {
            Destroy(child.gameObject);
        }
        if (cachedBuildingEvents != null && cachedBuildingEvents.Count > 0 && buildingEventsPanel != null)
        {
            buildingEventsPanel.SetActive(true);

            SetupEventsScrollAutoScrollTriggers();

            StartCoroutine(uiManager.AnimatePanelPopup(buildingEventsPanel));
            var sortedEvents = new List<BuildingEvent>(cachedBuildingEvents);
            sortedEvents.Sort((a, b) =>
            {
                return b.date.CompareTo(a.date);
            });
            foreach (var evt in sortedEvents)
            {
                GameObject eventGO = Instantiate(eventPrefab, eventsContentParent);

                var eventData = eventGO.GetComponent<EventDisplayData>();
                if (eventData == null)
                {
                    eventData = eventGO.AddComponent<EventDisplayData>();
                }
                eventData.eventId = evt.eventId;

                ButtonHandler rsvpButtonHandler = eventGO.GetComponentInChildren<ButtonHandler>();

                if (rsvpButtonHandler != null)
                {
                    rsvpButtonHandler.Initialize("data", () => RsvpToEvent(evt.eventId));

                }

                StartCoroutine(LoadAndDisplayRsvpList(eventGO, evt.eventId));

                Text[] textComponents = eventGO.GetComponentsInChildren<Text>();
                Text titleText = null;
                Text dateText = null;

                foreach (var text in textComponents)
                {
                    if (text.gameObject.CompareTag("MainText"))
                    {
                        titleText = text;
                    }
                    else if (text.gameObject.CompareTag("SubText"))
                    {
                        dateText = text;
                    }
                }

                if (titleText == null || dateText == null)
                {
                    foreach (var text in textComponents)
                    {
                        if (titleText == null && (text.gameObject.name.Contains("Title") || text.gameObject.name.Contains("Event")))
                        {
                            titleText = text;
                        }
                        else if (dateText == null && (text.gameObject.name.Contains("Date") || text.gameObject.name.Contains("Time")))
                        {
                            dateText = text;
                        }
                    }

                    if ((titleText == null || dateText == null) && textComponents.Length >= 2)
                    {
                        if (titleText == null) titleText = textComponents[0];
                        if (dateText == null) dateText = textComponents[1];
                    }
                    else if (textComponents.Length == 1)
                    {
                        titleText = textComponents[0];
                    }
                }

                string formattedDate = evt.GetDisplayDate();

                if (titleText != null)
                {
                    titleText.text = evt.eventName;

                    if (titleText.supportRichText)
                    {
                        titleText.text = $"<b>{evt.eventName}</b>";
                    }
                }

                if (dateText != null)
                {
                    dateText.text = formattedDate;

                    if (dateText.supportRichText)
                    {
                        dateText.text = $"<color=#888888>{formattedDate}</color>";
                    }
                }
                else if (titleText != null && dateText == null)
                {
                    titleText.text += $"\n{formattedDate}";
                }

                GameObject descObject = eventGO.transform.Find("desc")?.gameObject;
                if (descObject != null)
                {
                    Text descText = descObject.GetComponent<Text>();
                    if (descText != null && !string.IsNullOrEmpty(evt.description))
                    {
                        descText.text = evt.description;
                    }
                    else if (descText != null)
                    {
                        descText.text = "";
                    }
                }

                GameObject gainedCoinsObject = eventGO.transform.Find("gained-coins")?.gameObject;
                if (gainedCoinsObject != null)
                {
                    Text gainedCoinsText = gainedCoinsObject.GetComponent<Text>();
                    if (gainedCoinsText != null)
                    {
                        gainedCoinsText.text = evt.gainedCoins > 0 ? "+" + evt.gainedCoins.ToString() : "+50";
                    }
                }

                GameObject gainedKbObject = eventGO.transform.Find("gained-kb")?.gameObject;
                if (gainedKbObject != null)
                {
                    Text gainedKbText = gainedKbObject.GetComponent<Text>();
                    if (gainedKbText != null)
                    {
                        gainedKbText.text = evt.gainedKb > 0 ? "+" + evt.gainedKb.ToString() : "+50";
                    }
                }

            }
        }
        else if (buildingEventsPanel != null)
        {
            buildingEventsPanel.SetActive(false);
        }
    }
    
    private IEnumerator MovePlayerToBuildingSmooth()
    {
        var lightManager = FindObjectOfType<LightManager>();
        if (lightManager != null)
        {
            lightManager.PerformFlashEffect(1.2f);
        }
        
        GameObject player = GameObject.FindWithTag("Player");
        
        if (player != null)
        {
            Vector3 targetPosition = playerTargetPoint != null ? playerTargetPoint.transform.position : transform.position;
            Vector3 startPosition = player.transform.position;
            float duration = 2.0f;
            float elapsedTime = 0f;
            
            string targetName = playerTargetPoint != null ? playerTargetPoint.name : buildingName;
            Debug.Log($"Starting smooth movement to target '{targetName}' from {startPosition} to {targetPosition}");
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                
                player.transform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
                
                yield return null;
            }
            
            player.transform.position = targetPosition;
            Debug.Log($"Completed smooth movement to target '{targetName}' at position {targetPosition}");
        }
        else
        {
            Debug.LogWarning("Could not find player object with 'Player' tag to move");
        }
    }
    
    #region Auto Scroll Methods
    
    private void SetupEventsScrollAutoScrollTriggers()
    {
        if (eventsScrollView != null)
        {
            var eventTrigger = eventsScrollView.gameObject.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = eventsScrollView.gameObject.AddComponent<EventTrigger>();
            }
            
            eventTrigger.triggers.Clear();

            var pointerEnterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            pointerEnterEntry.callback.AddListener((data) => { OnEventsScrollViewPointerEnter(); });
            eventTrigger.triggers.Add(pointerEnterEntry);

            var pointerExitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExitEntry.callback.AddListener((data) => { OnEventsScrollViewPointerExit(); });
            eventTrigger.triggers.Add(pointerExitEntry);
        }
    }
    
    private IEnumerator ResetEventsScrollPosition()
    {
        if (!isEventsResetting)
        {
            isEventsResetting = true;
            yield return new WaitForSeconds(eventsResetDelay);
            if (eventsScrollView != null)
            {
                eventsScrollView.verticalNormalizedPosition = 1f;
            }
            isEventsResetting = false;
        }
    }
    
    public void OnEventsScrollViewPointerEnter()
    {
        isEventsScrollingPaused = true;
    }
    
    public void OnEventsScrollViewPointerExit()
    {
        isEventsScrollingPaused = false;
    }
    
    #endregion
    
}
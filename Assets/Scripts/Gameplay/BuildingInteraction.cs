using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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


    public bool isPlayerInRange = false;

    private AudioSource audioSource;
    public string buildingName;
    private Material originalMaterial;
    private SpriteRenderer sr;
    private bool lastGpsProximityState = false;
    public GameObject rsvpUserPrefab;
    private List<BuildingEvent> cachedBuildingEvents;
    private bool eventsLoaded = false;
    // Bumped whenever the events popup is rebuilt, so in-flight async RSVP-list
    // population from a previous display pass can detect it's stale and bail out
    // instead of touching destroyed UI.
    private int eventsGeneration = 0;
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
            buildingTitleUnlcoked.gameObject.SetActive(true);
            var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
            buildingTitleUnlcoked.text = buildingInfo.displayName;
        }

        var buildingData = GetComponent<BuildingInteraction>();
        if (showEventsHappening) OnVirtualBuildingEntered?.Invoke(buildingData);

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
        //DisplayBuildingEvents();
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

        buildingsLayerMask = LayerMask.GetMask("Buildings");
    }

    void Start()
    {
        uiManager = UIManager.Instance;
        proximityDetector = FindObjectOfType<BuildingProximityDetector>();
        ramsManager = GetComponent<RamsManager>();

        // var service = new UnlockedBuildingService();

        if(buildingEventsToggle != null)
        {
            buildingEventsToggle.onClick.AddListener(ToggleEventsVisibility);
        }

        _ = FetchBuildingEvents();

        // Subscribe to physical building changes from background location
        if (BackgroundLocationService.Instance != null)
        {
            BackgroundLocationService.Instance.OnPhysicalBuildingChanged += OnPhysicalBuildingChanged;
        }

        // Show RAMs inside building by default on game start
        if (ramsManager != null)
        {
            ramsManager.OnBuildingActivated();
        }
    }


    void OnDestroy()
    {
        if (buildingEventsToggle != null)
        {
            buildingEventsToggle.onClick.RemoveListener(ToggleEventsVisibility);
        }

        if (BackgroundLocationService.Instance != null)
        {
            BackgroundLocationService.Instance.OnPhysicalBuildingChanged -= OnPhysicalBuildingChanged;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnBuildingClicked();
        }

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
            buildingTitleUnlcoked.gameObject.SetActive(!isCurrentlyActive);
        }
    }


    private int buildingsLayerMask;

    private void OnBuildingClicked()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPoint, buildingsLayerMask);

        if (hit == null)
        {
            return;
        }

        if (hit.gameObject == gameObject)
        {
            HandleBuildingClicked();
        }
    }

    private void HandleBuildingClicked()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        StartCoroutine(ShowEventsOnClick());
    }

    private IEnumerator ShowEventsOnClick()
    {
        while (!eventsLoaded)
        {
            yield return null;
        }

        EnterBuildingViewingMode();
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

            
               if (ramsManager != null)
        {
            ramsManager.OnPlayerLeavesBuilding();
        }
        }
    }

    private async Task FetchBuildingEvents()
    {
        if (eventsLoaded) return;
        // No forceRefresh: the shared instance only needs one building to hit the
        // network per session - the rest reuse its in-memory cache.
        var allEvents = await BuildingEventService.Instance.GetBuildingEventsAsync();
        cachedBuildingEvents = allEvents.FindAll(e => e.buildingName == buildingName);

        eventsLoaded = true;
    }

    private async void RsvpToEvent(string eventId)
    {
        string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";

        var eventService = BuildingEventService.Instance;

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
        // cachedBuildingEvents holds references to the same BuildingEvent objects the
        // shared BuildingEventService caches, and RecordInterestAsync/RemoveInterestAsync
        // already update interestedUsers on those objects directly - so this is already
        // up to date without a second per-event Firestore round trip.
        var cachedEvent = cachedBuildingEvents?.FirstOrDefault(e => e.eventId == eventId);
        if (cachedEvent != null)
        {
            eventRsvpLists[eventId] = cachedEvent.interestedUsers ?? new List<string>();
            return;
        }

        // Fallback for an event we don't have cached locally.
        var eventData = await BuildingEventService.Instance.GetBuildingEventByIdAsync(eventId);
        eventRsvpLists[eventId] = eventData?.interestedUsers ?? new List<string>();
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
        if (eventGO == null) return;
        int generation = eventsGeneration;

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
            if (generation != eventsGeneration || studentsContentParent == null) return;
        }

        var rsvpList = eventRsvpLists.ContainsKey(eventId) ? eventRsvpLists[eventId] : new List<string>();

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(rsvpList.Count == 0);
        }

        // Fetch all RSVP'd users concurrently instead of one at a time - for an event
        // with N interested users this turns N sequential round trips into one batch.
        var userService = new UserService();
        var users = await Task.WhenAll(rsvpList.Select(userId => userService.RetrieveUserById(userId)));
        if (generation != eventsGeneration || studentsContentParent == null) return;

        foreach (var user in users)
        {
            if (user == null) continue;

            GameObject studentGO = Instantiate(rsvpUserPrefab, studentsContentParent);
            Text userNameText = studentGO.GetComponentInChildren<Text>(true);
            Image userImage = studentGO.GetComponentInChildren<Image>(true);

            if (userNameText != null)
            {
                userNameText.text = user.name;
            }

            if (userImage != null && uiManager != null)
            {
                // RetrieveUserById already returns coins/knowledgePoints on the User
                // object - no need for two more per-user round trips to fetch them again.
                userImage.sprite = uiManager.GetUserAvatarBasedOnPoints(user.coins, user.knowledgePoints);
            }
        }
    }
    
    private void DisplayBuildingEvents()
    {
        if (eventsContentParent == null || eventPrefab == null)
        {
            Debug.LogError("Events UI components not set up!");
            return;
        }

        // Invalidate any RSVP-list population still in flight from a previous
        // display pass, so it can't touch UI we're about to destroy below.
        eventsGeneration++;

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
    
    private void OnPhysicalBuildingChanged(string changedBuildingName)
    {
        // Only the BuildingInteraction that matches the building name should move the player
        if (!string.Equals(buildingName, changedBuildingName, StringComparison.OrdinalIgnoreCase)) return;

        Debug.Log($"[BuildingInteraction] Physical building changed to {changedBuildingName}, moving player");
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
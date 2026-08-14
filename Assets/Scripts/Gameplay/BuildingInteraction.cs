using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
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
    [SerializeField] private GameObject eventInfoPrefab;
    [SerializeField] private Transform eventInfoParent;


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
    // Bumped whenever the events popup is rebuilt, so an in-flight async image
    // load from a previous display pass can detect it's stale and bail out
    // instead of touching destroyed UI.
    private int eventsGeneration = 0;
    private UIManager uiManager;
    private BuildingProximityDetector proximityDetector;
    private RamsManager ramsManager;

    public delegate void VirtualBuildingEntryEvent(BuildingInteraction buildingData);
    public static event VirtualBuildingEntryEvent OnVirtualBuildingEntered;
    public static event VirtualBuildingEntryEvent OnVirtualBuildingExited;

    private async void EnterBuildingViewingMode(bool showEventsHappening = true)
    {
        await Task.Delay(1000);

        if (buildingTitleUnlcoked != null)
        {
            string displayName = await BuildingDataManager.GetBuildingDisplayNameAsync(buildingName);
            if (displayName != null)
            {
                buildingTitleUnlcoked.gameObject.SetActive(true);
                buildingTitleUnlcoked.text = displayName;
            }
        }

        if (showEventsHappening) OnVirtualBuildingEntered?.Invoke(this);

        if (buildingEventsPanel != null)
        {
            await DisplayBuildingEventsAsync();
        }

        if (ramsManager != null)
        {
            ramsManager.OnBuildingActivated();
        }

        string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
        var userService = new UserService();
        await userService.UpdateCurrentBuilding(userId, buildingName);
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

    private GameObject activeEventInfoGO;

    private void ShowEventInfo(BuildingEvent evt)
    {
        if (eventInfoPrefab == null) return;

        if (activeEventInfoGO != null)
        {
            Destroy(activeEventInfoGO);
        }

        Transform parent = eventInfoParent != null ? eventInfoParent : transform.root;
        GameObject infoGO = Instantiate(eventInfoPrefab, parent);
        activeEventInfoGO = infoGO;

        EventInfoPanel infoPanel = infoGO.GetComponent<EventInfoPanel>();
        if (infoPanel != null)
        {
            infoPanel.ShowEventInfo(evt);
        }
    }

    private async Task DisplayBuildingEventsAsync()
    {
        if (eventsContentParent == null || eventPrefab == null)
        {
            Debug.LogError("Events UI components not set up!");
            return;
        }

        List<BuildingEvent> events;
        try
        {
            events = await BuildingEventService.Instance.GetBuildingEventsForBuildingAsync(buildingName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"BuildingInteraction: Failed to load events for '{buildingName}': {ex.Message}");
            return;
        }

        // Invalidate any RSVP-list population still in flight from a previous
        // display pass, so it can't touch UI we're about to destroy below.
        eventsGeneration++;

        foreach (Transform child in eventsContentParent)
        {
            Destroy(child.gameObject);
        }
        if (events.Count > 0 && buildingEventsPanel != null)
        {
            buildingEventsPanel.SetActive(true);

            SetupEventsScrollAutoScrollTriggers();

            StartCoroutine(uiManager.AnimatePanelPopup(buildingEventsPanel));
            var sortedEvents = new List<BuildingEvent>(events);
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

                ButtonHandler cardButtonHandler = eventGO.GetComponentInChildren<ButtonHandler>();
                if (cardButtonHandler != null)
                {
                    cardButtonHandler.Initialize("data", () => ShowEventInfo(evt));
                }

                string formattedDate = evt.date;

                Text titleText = eventGO.transform.FindDeepChild("title")?.GetComponent<Text>();
                if (titleText != null)
                {
                    titleText.text = titleText.supportRichText ? $"<b>{evt.eventName}</b>" : evt.eventName;
                }

                Text dateText = eventGO.transform.FindDeepChild("date")?.GetComponent<Text>();
                if (dateText != null)
                {
                    dateText.text = dateText.supportRichText ? $"<color=#888888>{formattedDate}</color>" : formattedDate;
                }
                
                Image eventImage = eventGO.transform.FindDeepChild("image")?.GetComponent<Image>();
                if (eventImage != null && !string.IsNullOrEmpty(evt.imageUrl))
                {
                    StartCoroutine(LoadEventImage(evt.imageUrl, eventImage, eventsGeneration));
                }

                // Text descText = eventGO.transform.FindDeepChild("desc")?.GetComponent<Text>();
                // if (descText != null)
                // {
                //     descText.text = evt.description ?? "";
                // }

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

    private IEnumerator LoadEventImage(string url, Image target, int generation)
    {
        using (var request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            // Bail out if the popup was rebuilt (or the image destroyed) while downloading.
            if (generation != eventsGeneration || target == null) yield break;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"BuildingInteraction: Failed to load event image '{url}': {request.error}");
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            target.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
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
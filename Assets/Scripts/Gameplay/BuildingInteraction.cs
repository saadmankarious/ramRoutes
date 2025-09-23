using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using RamRoutes.Model;
using RamRoutes.Services;
using Firebase.Auth;
using System;

public class BuildingInteraction : MonoBehaviour
{

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.J;

    [Header("Popup")]
    [SerializeField] private bool doesTheBuildingHavePopup = false;
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private GameObject popupLocation;


    [Header("Josie Settings")]
    [SerializeField] private AudioClip rewardSound;
    [SerializeField] private int coinPoints = 100;
    [SerializeField] private int knowledgePoints = 250;

    // [Header("Mobile Controls")]
    // [SerializeField] private Button mobileInteractButton;

    [Header("Inactive Display")]
            public Text buildingTitleUnlcoked;      // Moved from BuildingInteraction
    [SerializeField] private GameObject inactivePrefab;
    [SerializeField] private Material lockedMaterial;    [Header("UI Panels")]
    [SerializeField] private GameObject buildingUnlockedPanel;
    [SerializeField] private Button closeUnlockedPanelButton;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private GameObject buildingEventsPanel;
    [SerializeField] private Transform eventsContentParent;
    [SerializeField] private GameObject eventPrefab;
    public string preUnlockMessage;

    [Header("User Location Display")]
    private Dictionary<string, GameObject> activeUserLocations = new Dictionary<string, GameObject>();

    [Header("Debug")]
    [SerializeField] private bool showUnlockedPanelOnStart = false;
    [SerializeField] private bool simulateEntry = false;

    [Header("Gate Integration")]
    [Tooltip("Deprecated: Gate unlocking is now handled by UIManager via mapping.")]
    [SerializeField] private Gate connectedGate;

    [Header("GPS Integration")]
    // [SerializeField] private float gpsUnlockRadius = 50f;
    [SerializeField] public bool bypassGpsCheck = false;

    [Header("Player Movement")]
    [SerializeField] private GameObject playerTargetPoint;

    public bool isPlayerInRange = false;
    private int currentLineIndex = 0;
    private bool extraLineShown = false;
    private AudioSource audioSource;
    private bool dialogActive = false;
    public bool activated = false;
    private bool lastActivated = false;
    public string buildingName;
    private GameObject inactiveInstance;
    private Material originalMaterial;
    private SpriteRenderer sr;
    private bool lastGpsProximityState = false;

    public ScrollRect usersScrollView;
    public Transform usersContentParent;
    public GameObject userPrefab;
    public GameObject rsvpUserPrefab;

    public GameObject usersWhoUnlockedPanel;

    private List<BuildingEvent> cachedBuildingEvents;
    private bool eventsLoaded = false;
    private UIManager uiManager;
    private BuildingProximityDetector proximityDetector;
    private RamsManager ramsManager;
    
    // RSVP tracking
    private Dictionary<string, List<string>> eventRsvpLists = new Dictionary<string, List<string>>();
    
    // Event for virtual building entry/exit
    public delegate void VirtualBuildingEntryEvent(BuildingInteraction buildingData);
    public static event VirtualBuildingEntryEvent OnVirtualBuildingEntered;
    public static event VirtualBuildingEntryEvent OnVirtualBuildingExited;

    // NEW: Switch into building viewing mode (same behavior as when entering an unlocked building)
    private async void EnterBuildingViewingMode(bool showEventsHappening = true)
    {
        // Update title UI
        if (buildingTitleUnlcoked != null)
        {
            var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
            buildingTitleUnlcoked.text = buildingInfo.displayName;
        }

        // Trigger virtual building entry event
        var buildingData = GetComponent<BuildingInteraction>();
        if (showEventsHappening) OnVirtualBuildingEntered?.Invoke(buildingData);

        // Notify UI manager that we are in viewing mode
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetBuildingViewingMode(true, buildingName);
        }

        // Spawn NPCs for this building
        var npcSpawner = FindObjectOfType<NPCSpawner>();
        if (npcSpawner != null)
        {
            npcSpawner.SpawnNPCForBuildingOnEnter(buildingName);
        }

        // Show events panel
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

        // Show current users panel through UIManager
        // if (uiManager != null)
        // {
        //     uiManager.DisplayCurrentUsersForBuilding(buildingName);
        // }
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
        // Initialize AudioSource for SFX only (not background music)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Ensure this AudioSource doesn't interfere with background music
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 0.5f; // Moderate volume for SFX

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
        
        if (inactivePrefab != null)
        {
            Vector3 spawnPos = transform.position;
            spawnPos.z = -1f;
            inactiveInstance = Instantiate(inactivePrefab, spawnPos, Quaternion.identity, transform);
            inactiveInstance.SetActive(true);
        }

        var service = new UnlockedBuildingService();
        StartCoroutine(SetActiveIfEntered(service));

        if (closeUnlockedPanelButton != null && buildingUnlockedPanel != null)
        {
            closeUnlockedPanelButton.onClick.AddListener(() =>
            {
                buildingUnlockedPanel.SetActive(false);

                // Reset AROS visibility when unlock panel is closed
                if (uiManager != null)
                {
                    uiManager.ResetArosVisibility(true);
                }

                // When the panel closes and the player is already in range of this building, enter viewing mode
                if (activated && isPlayerInRange)
                {
                    EnterBuildingViewingMode();
                }

                // Notify UIManager so it can change scene if ready (may load another scene)
                if (uiManager != null)
                {
                    uiManager.OnUnlockPanelClosed();
                }

                // Gate unlock is now handled centrally in UIManager after unlock
            });
        }

        _ = FetchBuildingEvents();
    }

    private IEnumerator SetActiveIfEntered(UnlockedBuildingService service)
    {
        string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
        var task = service.RetrieveUnlockedBuildings(userId);
        while (!task.IsCompleted) yield return null;
        var enteredBuildings = task.Result;
        if (enteredBuildings != null && enteredBuildings.Exists(b => b.buildingName == buildingName && b.userId == userId))
        {
            activated = true;
            
            // Notify RamsManager that building is now activated
            // if (ramsManager != null)
            // {
            //     ramsManager.OnBuildingActivated();
            // }
            
            if (inactiveInstance != null) inactiveInstance.SetActive(false);
            if (sr != null && originalMaterial != null)
            {
                sr.material = originalMaterial;
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }
    }

    void OnEnable()
    {
        BuildingProximityDetector.OnApproachBuilding += HandleApproachBuilding;
        BuildingProximityDetector.OnEnterBuilding += HandleEnteringBuilding;

    }

    void OnDisable()
    {
        BuildingProximityDetector.OnApproachBuilding -= HandleApproachBuilding;
        BuildingProximityDetector.OnEnterBuilding -= HandleEnteringBuilding;
    }

    void Update()
    {
        // if (isPlayerInRange && Input.GetKeyDown(interactKey))
        // {
        //     HandleInteraction();
        // }

        if (simulateEntry && !activated)
        {
            simulateEntry = false;
            Debug.Log($"[DEBUG] Simulating entry for building: {buildingName}");
            var simBuilding = new BuildingProximityDetector.Building { name = buildingName };
            HandleEnteringBuilding(simBuilding);
        }

        if (activated && !lastActivated)
        {
            var c = sr.color;
            c.a = 0.5f;
            sr.color = c;
        }
        else if (!activated && lastActivated)
        {
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
        lastActivated = activated;

        if (inactiveInstance != null)
        {
            inactiveInstance.SetActive(!activated);
        }

        if (sr != null)
        {
            if (!activated && lockedMaterial != null)
            {
                sr.material = lockedMaterial;
            }
            else if (activated && originalMaterial != null)
            {
                sr.material = originalMaterial;
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }
        
        if (!activated && isPlayerInRange)
        {
            bool currentGpsProximity = IsPlayerCloseToBuilding();
            
            if (currentGpsProximity != lastGpsProximityState)
            {
                lastGpsProximityState = currentGpsProximity;
                
                if (uiManager != null && uiManager.IsDialogActive())
                {
                    uiManager.HideDialog();
                    
                    StartCoroutine(ShowUpdatedDialog(currentGpsProximity));
                }
            }
        }
    }
    
    private System.Collections.IEnumerator ShowUpdatedDialog(bool isCloseInRealLife)
    {
        yield return new WaitForEndOfFrame(); // Wait a frame to ensure previous dialog is hidden
        
        if (isCloseInRealLife)
        {
            var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
            string lockedMessage = !string.IsNullOrEmpty(preUnlockMessage) ? preUnlockMessage : 
                $"You're now close to {buildingInfo.displayName}. Press the button below to unlock this building!";
            
            uiManager.ShowDialog(lockedMessage, 0f, "🔓 Unlock", () => {
                // Trigger unlock logic
                UnlockBuilding();
            });
        }
        else
        {
            var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
            string distanceMessage = !string.IsNullOrEmpty(preUnlockMessage) ? preUnlockMessage : 
                $"You've moved away from {buildingInfo.displayName}. You need to be physically close to this location to unlock it.";
            
            // Show message without unlock button since player is not close enough
            uiManager.ShowDialog(distanceMessage, 15f);
        }
    }

    // Combined handler for both mobile and PC interactions

    // Mobile-specific interaction handler
    // private void HandleMobileInteraction()
    // {
    //     if (isPlayerInRange)
    //     {
    //         HandleInteraction();
    //     }
    // }   

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Spaceship"))
        {
            isPlayerInRange = true;

            // if (mobileInteractButton != null)
            // {
            //     mobileInteractButton.gameObject.SetActive(true);
            // }
            
            // Initialize GPS proximity state for locked buildings
            if (!activated)
            {
                lastGpsProximityState = IsPlayerCloseToBuilding();
            }
            
            // Display building events and current users when player is in range
            if (activated)
            {
                // Switch to building viewing mode
                EnterBuildingViewingMode();
            }
            else
            {
                // Show locked building dialog with unlock action button only if player is close in real life
                if (uiManager != null)
                {
                    bool isCloseInRealLife = IsPlayerCloseToBuilding();
                    
                    if (isCloseInRealLife)
                    {
                        var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
                        string lockedMessage = !string.IsNullOrEmpty(preUnlockMessage) ? preUnlockMessage : 
                            $"You're close to {buildingInfo.displayName}. Press the button below to unlock this building!";
                        
                        uiManager.ShowDialog(lockedMessage, 10f, "🔓 Unlock", () => {
                            // Trigger unlock logic
                            UnlockBuilding();
                        }, false, true, buildingName);
                    }
                    else
                    {
                        var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
                        string distanceMessage = !string.IsNullOrEmpty(preUnlockMessage) ? preUnlockMessage : 
                            $"{buildingInfo.displayName} requires GPS to unlock. Walk to the building to unlock it.";
                        
                        // Show message without unlock button since player is not close enough
                        uiManager.ShowDialog(distanceMessage, 15f, false, true, buildingName);
                    }
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Spaceship"))
        {
            Debug.Log($"BuildingInteraction: Player exited building '{buildingName}' trigger area");

            isPlayerInRange = false;
            lastGpsProximityState = false; // Reset GPS proximity state

            // Hide UIManager dialog if it's active
            if (uiManager != null && uiManager.IsDialogActive())
            {
                uiManager.HideDialog();
            }

            // Deactivate building events panel when player leaves
            if (buildingEventsPanel != null)
            {
                buildingEventsPanel.SetActive(false);
            }

            // Trigger virtual building exit event
            OnVirtualBuildingExited?.Invoke(this);

            // Current users panel is handled by UIManager when leaving building view mode

            // Clean up user location indicators
            foreach (var indicator in activeUserLocations.Values)
            {
                Destroy(indicator);
            }
            activeUserLocations.Clear();

            // Despawn NPCs when player leaves the building
            var npcSpawner = FindObjectOfType<NPCSpawner>();
            if (activated && npcSpawner != null)
            {
                Debug.Log($"BuildingInteraction: Player left activated building '{buildingName}', calling despawn NPCs");
                npcSpawner.DespawnNPCsForBuilding(buildingName);
            }
            else if (!activated)
            {
                Debug.Log($"BuildingInteraction: Building '{buildingName}' not activated, skipping NPC despawn");
            }
            else if (npcSpawner == null)
            {
                Debug.LogWarning($"BuildingInteraction: NPCSpawner not found, cannot despawn NPCs");
            }

            // Leaving range exits viewing mode
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

    public void ShowBuildingUnlockedPanel()
    {
        if (buildingUnlockedPanel != null)
        {
            buildingUnlockedPanel.SetActive(true);

            // Apply animation to the panel using UIManager
            if (uiManager != null)
            {
                StartCoroutine(uiManager.AnimatePanelPopup(buildingUnlockedPanel));
            }
        }
    }


    private async void HandleEnteringBuilding(BuildingProximityDetector.Building building)
    {
        if (building.name == buildingName)
        {
            Debug.Log($"Player entered GPS proximity of building: {building.name}");
            
            // Move player to building position smoothly
            StartCoroutine(MovePlayerToBuildingSmooth());
            
            // Check if already unlocked
            var service = new UnlockedBuildingService();
            string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
            var enteredBuildings = await service.RetrieveUnlockedBuildings(userId);
            bool alreadyEntered = enteredBuildings.Exists(b => b.buildingName == buildingName && b.userId == userId);
            
            if (alreadyEntered)
            {
                Debug.Log($"Building {buildingName} already unlocked. No action needed.");
                return;
            }
            
            // Just log that player is now in GPS proximity - don't auto-unlock
            Debug.Log($"Player is now in GPS range of {buildingName}. They can interact to unlock it.");
            
            // If player is currently in interaction range, refresh the dialog to show unlock button
            if (isPlayerInRange && uiManager != null && uiManager.IsDialogActive())
            {
                // Refresh the dialog with updated GPS state
                if (uiManager != null)
                {
                    uiManager.HideDialog();
                    
                    // Wait a frame then show the updated dialog
                    StartCoroutine(ShowUpdatedDialog(true));
                }
            }
        }
    }

    public async void UnlockBuilding()
    {
        closeUnlockedPanelButton.interactable = false; // Prevent multiple clicks
        if (uiManager != null)
        {
            await uiManager.HandleBuildingUnlock(this);
        }
        else
        {
            // Fallback for no UIManager
            await Task.Delay(3000);
            ShowBuildingUnlockedPanel();
            await DisplayUsersWhoUnlocked();
        }

        // Activate building after unlock
        activated = true;

        // // Notify RamsManager that building is now activated
        // if (ramsManager != null)
        // {
        //     ramsManager.OnBuildingActivated();
        // }



        // Play reward sound when building is revealed
        if (rewardSound != null && audioSource != null)
        {
            // Ensure audio source is configured for SFX, not background music
            audioSource.clip = null; // Clear any assigned clip to prevent auto-play
            audioSource.loop = false;
            audioSource.volume = 0.6f; // Good volume for reward SFX
            audioSource.PlayOneShot(rewardSound);
            Debug.Log($"Playing reward sound for building: {buildingName}");
        }

        // Use unified user profile retrieval for points and saving
        var service = new UnlockedBuildingService();
        string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
        var userService = new UserService();

        // Get username from Firestore instead of PlayerPrefs for accuracy
        var user = await userService.RetrieveUserById(userId);
        string userName = user?.name ?? userId;

        // Award points for unlocking the building
        // await userService.AddPoints(userId, 100);
        // Update UI with new points
        // var updatedPoints = await userService.GetPoints(userId);
        // UIManager.Instance.UpdateCoins(updatedPoints);

        // Update user's current building
        // await userService.UpdateCurrentBuilding(userId, buildingName);

        // Save unlock event to Firestore
        var record = new UnlockedBuildingRecord(
            userId,
            userName,
            System.DateTime.UtcNow,
            buildingName,
            buildingName,
            transform.position,
            coinPoints,
            knowledgePoints
        );
        await service.SaveUnlockedBuildingAsync(record);

        // Update coins and knowledge points in Firebase using new utilities
        await userService.UpdateUserCoins(userId, coinPoints);
        await userService.UpdateUserKnowledgePoints(userId, knowledgePoints);

        // Update progress bar when building is revealed
        if (uiManager != null)
        {
            uiManager.UpdateProgressBarOnReveal();
        }
        // If player is already in range, immediately switch to viewing mode
        if (isPlayerInRange)
        {
            EnterBuildingViewingMode(false);
        }

        if (doesTheBuildingHavePopup && popupPrefab != null)
        {
            // Instantiate the popup prefab at the building's position
            GameObject popupInstance = Instantiate(popupPrefab, popupLocation.transform.position, Quaternion.identity);

            // Optionally, set the popup to destroy itself after a few seconds
            // Destroy(popupInstance, 5f); // Destroy after 5 seconds
        }
                closeUnlockedPanelButton.interactable = true; // Prevent multiple clicks

    }
    
    private bool IsPlayerCloseToBuilding()
    {
        // Bypass GPS check if enabled (for testing) - use building-level flag
        if (bypassGpsCheck)
        {
            return true;
        }
        
        // Use cached proximity detector reference
        if (proximityDetector == null)
        {
            Debug.LogWarning("BuildingProximityDetector not found in scene. Cannot check GPS proximity.");
            return false;
        }
        
        // Check if location services are available and running
        if (!Input.location.isEnabledByUser || Input.location.status != LocationServiceStatus.Running)
        {
            return false;
        }
        
        // Find this building in the proximity detector's building list
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
        
        // Use the existing precise distance calculation from BuildingProximityDetector
        float distance = proximityDetector.CalculatePreciseDistance(
            currentLocation.latitude,
            currentLocation.longitude,
            targetBuilding.entranceGPS.x,
            targetBuilding.entranceGPS.y
        );
        
        Debug.Log($"GPS Distance to {buildingName}: {distance:F1}m (threshold: {targetBuilding.detectionRadius}m)");
        
        return distance <= targetBuilding.detectionRadius;
    }

    // Make DisplayUsersWhoUnlocked return a Task for parallel execution
    public async Task DisplayUsersWhoUnlocked()
    {
        // Clear previous entries
        foreach (Transform child in usersContentParent)
        {
            Destroy(child.gameObject);
        }

        var service = new UnlockedBuildingService();
        var usersForBuilding = await service.RetrieveUnlockedBuildingsForBuilding(buildingName);        try
        {
            if (usersForBuilding.Count == 0)
            {
                usersWhoUnlockedPanel.gameObject.SetActive(false);
            }
            else if (usersWhoUnlockedPanel != null)
            {
                // Make sure the panel is active and animate it
                usersWhoUnlockedPanel.gameObject.SetActive(true);
                
                // Apply animation to the panel using UIManager
                if (uiManager != null)
                {
                    StartCoroutine(uiManager.AnimatePanelPopup(usersWhoUnlockedPanel));
                }
            }
            foreach (var record in usersForBuilding)
            {
                // Instantiate the prefab
                GameObject userGO = Instantiate(userPrefab, usersContentParent);
                Text userNameText = userGO.GetComponentInChildren<Text>(true);
                Image userImage = userGO.GetComponentInChildren<Image>(true);

                if (userNameText != null)
                {
                    Debug.Log($"Setting user text: userName={record.userName}, userId={record.userId}");
                    userNameText.text = record.userName;

                    RectTransform textRect = userNameText.GetComponent<RectTransform>();
                    if (textRect != null)
                    {
                        textRect.pivot = new Vector2(0.5f, 0.5f);
                        textRect.anchorMin = new Vector2(0.5f, 0.5f);
                        textRect.anchorMax = new Vector2(0.5f, 0.5f);
                    }
                }
                else
                {
                    Debug.LogError("No Text component found in prefab or its children!");
                }

                // Set user avatar based on points (using UIManager's approach)
                if (userImage != null && uiManager != null)
                {
                    var userService = new UserService();
                    var userPoints = await userService.GetUserCoins(record.userId);
                    var userKnowledgePoints = await userService.GetUserKnowledgePoints(record.userId);
                    userImage.sprite = uiManager.GetUserAvatarBasedOnPoints(userPoints, userKnowledgePoints);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading displaying user who unlcoked: {e.Message}");

        }

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

    /// <summary>
    /// Toggles current user's interest/RSVP for an event
    /// </summary>
    /// <param name="eventId">The event ID to RSVP to</param>
    private async void RsvpToEvent(string eventId)
    {
        string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
        
        var eventService = new BuildingEventService();
        
        // Check if user is already interested
        bool isAlreadyInterested = await eventService.HasPlayerShownInterest(eventId, userId);
        
        if (isAlreadyInterested)
        {
            // Remove interest
            await eventService.RemoveInterestAsync(eventId, userId);
            Debug.Log($"Removed interest for user {userId} from event {eventId}");
            
            // Show brief UI notification for removal
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate("Removed from event");
            }
        }
        else
        {
            // Add interest
            await eventService.RecordInterestAsync(eventId, userId);
            Debug.Log($"Added interest for user {userId} to event {eventId}");
            
            // Show brief UI notification for addition
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate("Added to event!");
            }
        }
        
        // Refresh the RSVP list for this event
        await LoadEventRsvpList(eventId);
        
        // Find and update the event display
        UpdateEventRsvpDisplay(eventId);
    }
    
    /// <summary>
    /// Loads interest/RSVP list for a specific event
    /// </summary>
    /// <param name="eventId">The event ID</param>
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
    
    /// <summary>
    /// Coroutine to load and display RSVP list
    /// </summary>
    private System.Collections.IEnumerator LoadAndDisplayRsvpList(GameObject eventGO, string eventId)
    {
        var loadTask = LoadEventRsvpList(eventId);
        yield return new WaitUntil(() => loadTask.IsCompleted);
        
        PopulateEventRsvpList(eventGO, eventId);
    }
    
    /// <summary>
    /// Updates the RSVP display for a specific event
    /// </summary>
    /// <param name="eventId">The event ID</param>
    private void UpdateEventRsvpDisplay(string eventId)
    {
        // Find the event GameObject by searching through the content parent
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
    
    /// <summary>
    /// Populates the RSVP student list for an event
    /// </summary>
    /// <param name="eventGO">The event GameObject</param>
    /// <param name="eventId">The event ID</param>
    private async void PopulateEventRsvpList(GameObject eventGO, string eventId)
    {
        ScrollRect studentsScrollView = null;
        Transform studentsContentParent = null;
        Text emptyStateText = null;
        
        // Find the students-list scroll view and empty text
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
        
        // Find empty state text (look for a Text component named "empty" or with specific tag)
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
        
        // Clear existing student entries
        foreach (Transform child in studentsContentParent)
        {
            Destroy(child.gameObject);
        }
        
        // Get RSVP list for this event
        if (!eventRsvpLists.ContainsKey(eventId))
        {
            await LoadEventRsvpList(eventId);
        }
        
        var rsvpList = eventRsvpLists.ContainsKey(eventId) ? eventRsvpLists[eventId] : new List<string>();
        
        // Show/hide empty state text
        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(rsvpList.Count == 0);
        }
        
        // Create user entries for each RSVP'd student
        var userService = new UserService();
        foreach (var userId in rsvpList)
        {
            var user = await userService.RetrieveUserById(userId);
            if (user != null)
            {
                GameObject studentGO = Instantiate(rsvpUserPrefab, studentsContentParent);
                
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
    }    private void DisplayBuildingEvents()
    {
        if (eventsContentParent == null || eventPrefab == null)
        {
            Debug.LogError("Events UI components not set up!");
            return;
        }

        // Clear previous entries
        foreach (Transform child in eventsContentParent)
        {
            Destroy(child.gameObject);
        }        if (cachedBuildingEvents != null && cachedBuildingEvents.Count > 0 && buildingEventsPanel != null)
        {
            buildingEventsPanel.SetActive(true);
              // Animate the panel appearing
            StartCoroutine(uiManager.AnimatePanelPopup(buildingEventsPanel));
            // Sort events by date: Most recent events on top
            var sortedEvents = new List<BuildingEvent>(cachedBuildingEvents);
            sortedEvents.Sort((a, b) => {
                // Sort by date (most recent first)
                return b.date.CompareTo(a.date);
            });
              foreach (var evt in sortedEvents)
            {
                GameObject eventGO = Instantiate(eventPrefab, eventsContentParent);
                
                // Add event data component to track event ID
                var eventData = eventGO.GetComponent<EventDisplayData>();
                if (eventData == null)
                {
                    eventData = eventGO.AddComponent<EventDisplayData>();
                }
                eventData.eventId = evt.eventId;
                
                // Setup RSVP button
                ButtonHandler rsvpButtonHandler = eventGO.GetComponentInChildren<ButtonHandler>();
                
                if (rsvpButtonHandler != null)
                {
                            rsvpButtonHandler.Initialize("data", () => RsvpToEvent(evt.eventId));

                }
                
                // Load and populate RSVP list
                StartCoroutine(LoadAndDisplayRsvpList(eventGO, evt.eventId));
                
                // Get separate text components for title and date
                Text[] textComponents = eventGO.GetComponentsInChildren<Text>();
                Text titleText = null;
                Text dateText = null;
                
                // Look specifically for Text components tagged as "MainText" and "SubText"
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
                
                // Fallback logic if tags are not found
                if (titleText == null || dateText == null)
                {
                    // If we have names to identify them
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
                    
                    // If we still don't have both and there are at least 2 components, use the first two
                    if ((titleText == null || dateText == null) && textComponents.Length >= 2)
                    {
                        if (titleText == null) titleText = textComponents[0];
                        if (dateText == null) dateText = textComponents[1];
                    }
                    else if (textComponents.Length == 1)
                    {
                        // Fallback if there's only one text component
                        titleText = textComponents[0];
                    }
                }
                
                // Format and set text using the helper method
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
                    // Fallback if we only have one text component
                    titleText.text += $"\n{formattedDate}";
                }
                
                // Find and populate description text component
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
                        // Hide or set empty if no description
                        descText.text = "";
                    }
                }
                
                // Find and populate gained coins text component
                GameObject gainedCoinsObject = eventGO.transform.Find("gained-coins")?.gameObject;
                if (gainedCoinsObject != null)
                {
                    Text gainedCoinsText = gainedCoinsObject.GetComponent<Text>();
                    if (gainedCoinsText != null)
                    {
                        gainedCoinsText.text = evt.gainedCoins > 0 ? "+" + evt.gainedCoins.ToString() : "+50";
                    }
                }
                
                // Find and populate gained knowledge points text component
                GameObject gainedKbObject = eventGO.transform.Find("gained-kb")?.gameObject;
                if (gainedKbObject != null)
                {
                    Text gainedKbText = gainedKbObject.GetComponent<Text>();
                    if (gainedKbText != null)
                    {
                        gainedKbText.text = evt.gainedKb > 0 ? "+" + evt.gainedKb.ToString() : "+50";
                    }
                }
                
                // Add animation or visual effects if needed
                // StartCoroutine(AnimateEventEntry(eventGO));
            }
        }
        else if (buildingEventsPanel != null)
        {
            buildingEventsPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Smoothly moves the player to the target point near this building
    /// </summary>
    private IEnumerator MovePlayerToBuildingSmooth()
    {
        GameObject player = GameObject.FindWithTag("Player");
        // if (player == null)
        // {
        //     player = GameObject.FindWithTag("Spaceship");
        // }
        
        if (player != null)
        {
            // Check if target point is assigned, otherwise fallback to building position
            Vector3 targetPosition = playerTargetPoint != null ? playerTargetPoint.transform.position : transform.position;
            Vector3 startPosition = player.transform.position;
            float duration = 2.0f; // 2 seconds for smooth movement
            float elapsedTime = 0f;
            
            string targetName = playerTargetPoint != null ? playerTargetPoint.name : buildingName;
            Debug.Log($"Starting smooth movement to target '{targetName}' from {startPosition} to {targetPosition}");
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                
                // Use smooth step for eased movement
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                
                // Interpolate position
                player.transform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
                
                yield return null; // Wait one frame
            }
            
            // Ensure we end exactly at target position
            player.transform.position = targetPosition;
            Debug.Log($"Completed smooth movement to target '{targetName}' at position {targetPosition}");
        }
        else
        {
            Debug.LogWarning("Could not find player object with 'Player' tag to move");
        }
    }
    
}
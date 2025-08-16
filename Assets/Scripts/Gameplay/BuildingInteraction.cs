using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using RamRoutes.Model;
using RamRoutes.Services;
using Firebase.Auth;
using System;

public class BuildingInteraction : MonoBehaviour
{
    [Header("Dialog Settings")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private Text dialogText;
    [SerializeField] private string[] dialogLines;

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.J;
    [SerializeField] private bool hasSapling = false;

    [Header("Reward Settings")]
    [SerializeField] private GameObject saplingPrefab;
    [SerializeField] private AudioClip rewardSound;

    [Header("Mobile Controls")]
    [SerializeField] private Button mobileInteractButton;

    [Header("Inactive Display")]
    [SerializeField] private GameObject inactivePrefab;
    [SerializeField] private Material lockedMaterial;    [Header("UI Panels")]
    [SerializeField] private GameObject buildingUnlockedPanel;
    [SerializeField] private Button closeUnlockedPanelButton;
    [SerializeField] public Text buildingTitle;
    [SerializeField] public Text buildingDescription;
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
    [SerializeField] private Gate connectedGate;

    [Header("Player Teleportation")]
    [SerializeField] private Transform playerTeleportPosition;

    [Header("GPS Integration")]
    [SerializeField] private float gpsUnlockRadius = 50f;
    [SerializeField] private bool bypassGpsCheck = false;

    private bool isPlayerInRange = false;
    private int currentLineIndex = 0;
    private bool saplingSpawned = false;
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
    public GameObject usersWhoUnlockedPanel;

    private List<BuildingEvent> cachedBuildingEvents;
    private bool eventsLoaded = false;
    private UIManager uiManager;
    private bool shouldTeleportOnPanelClose = false;
    private BuildingProximityDetector proximityDetector;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        dialogPanel.SetActive(false);

        if (mobileInteractButton != null)
        {
            mobileInteractButton.onClick.AddListener(HandleMobileInteraction);
            mobileInteractButton.gameObject.SetActive(false);
        }

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

                if (connectedGate != null)
                {
                    connectedGate.UnlockGate();

                    Debug.Log($"Unlocked gate connected to building: {buildingName}");
                }

                if (shouldTeleportOnPanelClose)
                {
                    TeleportPlayerToPosition();
                    
                    StartCoroutine(ActivateBuildingAfterDelay());
                    
                    shouldTeleportOnPanelClose = false;
                }
            });
        }

        // if (showUnlockedPanelOnStart && buildingUnlockedPanel != null)
        // {
        //     buildingUnlockedPanel.SetActive(true);
        //     _ = DisplayUsersWhoUnlocked();
        // }

        // Load building events at start
        _ = FetchBuildingEvents();
    }

    private IEnumerator SetActiveIfEntered(UnlockedBuildingService service)
    {
        string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
        var task = service.RetrieveUnlockedBuildings();
        while (!task.IsCompleted) yield return null;
        var enteredBuildings = task.Result;
        if (enteredBuildings != null && enteredBuildings.Exists(b => b.buildingName == buildingName && b.userId == userId))
        {
            activated = true;
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
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            HandleInteraction();
        }

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
        
        // Monitor GPS proximity changes for locked buildings when player is in range
        if (!activated && isPlayerInRange)
        {
            bool currentGpsProximity = IsPlayerCloseToBuilding();
            
            // If GPS proximity state changed, update the dialog
            if (currentGpsProximity != lastGpsProximityState)
            {
                lastGpsProximityState = currentGpsProximity;
                
                // Refresh the dialog with updated GPS state
                if (uiManager != null && uiManager.IsDialogActive())
                {
                    // Hide current dialog and show updated one
                    uiManager.HideDialog();
                    
                    // Wait a frame then show the updated dialog
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
            string lockedMessage = !string.IsNullOrEmpty(preUnlockMessage) ? preUnlockMessage : 
                $"Great! You're now close to {buildingName}. You can now unlock this building!";
            
            uiManager.ShowDialog(lockedMessage, 0f, "jumping-happy", "", () => {
                // Trigger unlock logic
                UnlockBuilding();
            });
        }
        else
        {
            string distanceMessage = !string.IsNullOrEmpty(preUnlockMessage) ? preUnlockMessage : 
                $"You've moved away from {buildingName}. You need to be physically close to this location to unlock it.";
            
            // Show message without unlock button since player is not close enough
            uiManager.ShowDialog(distanceMessage, 5f, "aros-neutral");
        }
    }

    // Combined handler for both mobile and PC interactions
    private void HandleInteraction()
    {
        if (!dialogActive)
        {
            ShowDialog(dialogLines[currentLineIndex]);
        }
        else
        {
            AdvanceDialog();
        }
    }

    // Mobile-specific interaction handler
    private void HandleMobileInteraction()
    {
        if (isPlayerInRange)
        {
            HandleInteraction();
        }
    }    private void StartDialog()
    {
        dialogActive = true;
        dialogPanel.SetActive(true);
        currentLineIndex = 0;
        extraLineShown = false;
        dialogText.text = dialogLines[currentLineIndex];
        
        // Apply animation to the dialog panel using UIManager
        if (uiManager != null)
        {
            StartCoroutine(uiManager.AnimatePanelPopup(dialogPanel));
        }
    }private void ShowDialog(string message)
    {
        dialogActive = true;
        dialogPanel.SetActive(true);
        currentLineIndex = 0;
        extraLineShown = false;
        dialogText.text = message;
        
        // Apply animation to the dialog panel using UIManager
        if (uiManager != null)
        {
            StartCoroutine(uiManager.AnimatePanelPopup(dialogPanel));
        }
    }

    private void AdvanceDialog()
    {
        currentLineIndex++;

        if (currentLineIndex < dialogLines.Length)
        {
            dialogText.text = dialogLines[currentLineIndex];
        }
        else if (hasSapling && !saplingSpawned &&
                GameManager.Instance.currentTrial.trialNumber == 2 &&
                !extraLineShown)
        {
            GiveSaplingReward();
        }
        else
        {
            CloseDialog();
        }
    }

    private void GiveSaplingReward()
    {
        dialogText.text = "You received a sapling!";
        SpawnSapling();
        saplingSpawned = true;
        extraLineShown = true;
    }

    private void CloseDialog()
    {
        dialogActive = false;
        dialogPanel.SetActive(false);

        if (mobileInteractButton != null)
        {
            mobileInteractButton.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Spaceship"))
        {
            isPlayerInRange = true;

            if (mobileInteractButton != null)
            {
                mobileInteractButton.gameObject.SetActive(true);
            }
            
            // Initialize GPS proximity state for locked buildings
            if (!activated)
            {
                lastGpsProximityState = IsPlayerCloseToBuilding();
            }
            
            // Display building events and current users when player is in range
            if (activated)
            {
                if (buildingEventsPanel != null && eventsLoaded)
                {
                    DisplayBuildingEvents();
                }
            }
            else
            {
                // Show locked building dialog with unlock action button only if player is close in real life
                if (uiManager != null)
                {
                    bool isCloseInRealLife = IsPlayerCloseToBuilding();
                    
                    if (isCloseInRealLife)
                    {
                        string lockedMessage = !string.IsNullOrEmpty(preUnlockMessage) ? preUnlockMessage : 
                            $"Great! You're close to {buildingName}. You can now unlock this building!";
                        
                        uiManager.ShowDialog(lockedMessage, 0f, "jumping-happy", "", () => {
                            // Trigger unlock logic
                            UnlockBuilding();
                        });
                    }
                    else
                    {
                        string distanceMessage = !string.IsNullOrEmpty(preUnlockMessage) ? preUnlockMessage : 
                            $"This building ({buildingName}) is locked. You need to be physically close to this location to unlock it using GPS.";
                        
                        // Show message without unlock button since player is not close enough
                        uiManager.ShowDialog(distanceMessage, 5f, "aros-neutral");
                    }
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Spaceship"))
        {
            isPlayerInRange = false;
            lastGpsProximityState = false; // Reset GPS proximity state
            CloseDialog();
            
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

            // Clean up user location indicators
            foreach (var indicator in activeUserLocations.Values)
            {
                Destroy(indicator);
            }
            activeUserLocations.Clear();
        }
    }

    private void SpawnSapling()
    {
        Vector3 spawnPosition = transform.position + transform.right * 1.5f;
        Instantiate(saplingPrefab, spawnPosition, Quaternion.identity);

        if (rewardSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(rewardSound);
        }
    }

    private void HandleApproachBuilding(BuildingProximityDetector.Building building)
    {
        Debug.Log("Building Interaction:: Approaching building " + building.name);
    }    public void ShowBuildingUnlockedPanel()
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
            // Check if already entered
            var service = new UnlockedBuildingService();
            string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
            var enteredBuildings = await service.RetrieveUnlockedBuildings();
            bool alreadyEntered = enteredBuildings.Exists(b => b.buildingName == buildingName && b.userId == userId);
            if (alreadyEntered)
            {
                Debug.Log($"Building {buildingName} already entered. Skipping unlock logic.");
                return;
            }            Debug.Log("Activating building: " + building.name);
            // Don't set activated = true here, let it happen after teleportation

            // Set flag to indicate this building should teleport when panel closes
            shouldTeleportOnPanelClose = true;

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

            // Use unified user profile retrieval
            var userService = new UserService();
            var userProfile = await userService.GetUserProfileCachedOrRemoteAsync(userId);
            string userName = userProfile != null && !string.IsNullOrEmpty(userProfile.name) ? userProfile.name : userId;
            // Award points for unlocking the building
            await userService.AddPoints(userId, 100);
            // Update UI with new points
            var updatedPoints = await userService.GetPoints(userId);
            UIManager.Instance.UpdateCoins(updatedPoints);

            // Update user's current building
            await userService.UpdateCurrentBuilding(userId, buildingName);

            // Save unlock event to Firestore
            var record = new UnlockedBuildingRecord(
                userId,
                userName,
                System.DateTime.UtcNow,
                buildingName,
                buildingName,
                transform.position
            );
            await service.SaveUnlockedBuildingAsync(record);
        }
    }
    
    public void UnlockBuilding()
    {
        // Create a fake building object to trigger the unlock logic
        var building = new BuildingProximityDetector.Building { name = buildingName };
        HandleEnteringBuilding(building);
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
        
        Debug.Log($"GPS Distance to {buildingName}: {distance:F1}m (threshold: {gpsUnlockRadius}m)");
        
        return distance <= gpsUnlockRadius;
    }

    private IEnumerator ActivateBuildingAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        activated = true;
        
        // Update progress bar when building is revealed
        if (uiManager != null)
        {
            uiManager.UpdateProgressBarOnReveal();
        }
        
        // Play reward sound when building is revealed
        if (rewardSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(rewardSound);
        }
    }

    private void TeleportPlayerToPosition()
    {
        if (playerTeleportPosition != null)
        {
            Debug.Log($"Teleporting player to position: {playerTeleportPosition.position} for building: {buildingName}");
            // Find the player GameObject
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                // Try alternate tag
                player = GameObject.FindGameObjectWithTag("Spaceship");
            }

            if (player != null)
            {
                // Teleport the player to the designated position
                player.transform.position = playerTeleportPosition.position;
                Debug.Log($"Player teleported to position: {playerTeleportPosition.position} for building: {buildingName}");
                
                // Optional: Add some visual effects for teleportation
                if (uiManager != null)
                {
                    StartCoroutine(uiManager.PlayTeleportEffect(player.transform.position));
                }
            }
            else
            {
                Debug.LogWarning("Player GameObject not found! Cannot teleport after building unlock.");
            }
        }
        else
        {
            Debug.LogWarning($"Player teleport position not set for building: {buildingName}. Player will not be moved.");
        }
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
        var unlockedList = await service.RetrieveUnlockedBuildings();
        var usersForBuilding = unlockedList.FindAll(b => b.buildingName == buildingName);        try
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
            // Sort events by date (most recent first)
            var sortedEvents = new List<BuildingEvent>(cachedBuildingEvents);
            sortedEvents.Sort((a, b) => b.date.CompareTo(a.date));
              foreach (var evt in sortedEvents)
            {
                GameObject eventGO = Instantiate(eventPrefab, eventsContentParent);
                
                // Get separate text components for title and date
                Text[] textComponents = eventGO.GetComponentsInChildren<Text>();
                Text titleText = null;
                Text dateText = null;
                
                // Assign the correct text component based on name or order
                if (textComponents.Length >= 2)
                {
                    // If we have names to identify them
                    foreach (var text in textComponents)
                    {
                        if (text.gameObject.name.Contains("Title") || text.gameObject.name.Contains("Event"))
                        {
                            titleText = text;
                        }
                        else if (text.gameObject.name.Contains("Date") || text.gameObject.name.Contains("Time"))
                        {
                            dateText = text;
                        }
                    }
                    
                    // If names don't match, just use the first two
                    if (titleText == null && dateText == null)
                    {
                        titleText = textComponents[0];
                        dateText = textComponents[1];
                    }
                }
                else if (textComponents.Length == 1)
                {
                    // Fallback if there's only one text component
                    titleText = textComponents[0];
                }
                
                // Format and set text
                string formattedDate = evt.date.ToString("MMM dd, yyyy h:mm tt");
                
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
                
                // Add animation or visual effects if needed
                // StartCoroutine(AnimateEventEntry(eventGO));
            }
        }
        else if (buildingEventsPanel != null)
        {
            buildingEventsPanel.SetActive(false);
        }
    }
    
}
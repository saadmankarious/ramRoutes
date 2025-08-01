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
    // Serialized Fields
    [Header("Dialog Settings")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private Text dialogText;
    [SerializeField] private string[] dialogLines;

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.J;
    [SerializeField] private bool isPlanet = false;
    [SerializeField] private bool hasSapling = false;

    [Header("Reward Settings")]
    [SerializeField] private GameObject saplingPrefab;
    [SerializeField] private AudioClip rewardSound;

    [Header("Mobile Controls")]
    [SerializeField] private Button mobileInteractButton;

    [Header("Inactive Display")]
    [SerializeField] private GameObject inactivePrefab;
    [SerializeField] private Material lockedMaterial;

    [Header("UI Panels")]
    [SerializeField] private GameObject buildingUnlockedPanel;
    [SerializeField] private Button closeUnlockedPanelButton;
    [SerializeField] private GameObject buildingEventsPanel; // Add this in Unity Inspector
    [SerializeField] private Transform eventsContentParent; // Add this in Unity Inspector
    [SerializeField] private GameObject eventPrefab; // Add this in Unity Inspector

    [Header("Debug")]
    [SerializeField] private bool showUnlockedPanelOnStart = false;
    [SerializeField] private bool simulateEntry = false; // Add this field

    // Private Variables
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

    public ScrollRect usersScrollView;
    public Transform usersContentParent;
    public GameObject userPrefab;
    public GameObject usersWhoUnlockedPanel;

    private List<BuildingEvent> cachedBuildingEvents;
    private bool eventsLoaded = false;

    void Awake()
    {
        // Initialize components
        audioSource = GetComponent<AudioSource>();

        // Set initial state
        dialogPanel.SetActive(false);

        // Setup mobile button if it exists
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
        if (inactivePrefab != null)
        {
            Vector3 spawnPos = transform.position;
            spawnPos.z = -1f;
            inactiveInstance = Instantiate(inactivePrefab, spawnPos, Quaternion.identity, transform);
            inactiveInstance.SetActive(true);
        }

        // Check if building has already been entered
        var service = new UnlockedBuildingService();
        StartCoroutine(SetActiveIfEntered(service));

        if (closeUnlockedPanelButton != null && buildingUnlockedPanel != null)
        {
            closeUnlockedPanelButton.onClick.AddListener(() => buildingUnlockedPanel.SetActive(false));
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
        // Handle keyboard input
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            HandleInteraction();
        }

        // Add debug simulation
        if (simulateEntry && !activated)
        {
            simulateEntry = false; // Reset flag
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
    }

    private void StartDialog()
    {
        dialogActive = true;
        dialogPanel.SetActive(true);
        currentLineIndex = 0;
        extraLineShown = false;
        dialogText.text = dialogLines[currentLineIndex];
    }

    private void ShowDialog(string message)
    {
        dialogActive = true;
        dialogPanel.SetActive(true);
        currentLineIndex = 0;
        extraLineShown = false;
        dialogText.text = message;
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

            if (!isPlanet && other.CompareTag("Player") &&
                GameManager.Instance.currentTrial.trialNumber == 2)
            {
                dialogText.text = "Press J to interact";
            }

            // Display building events when player is in range
            if (buildingEventsPanel != null && eventsLoaded && activated)
            {
                DisplayBuildingEvents();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Spaceship"))
        {
            isPlayerInRange = false;
            CloseDialog();
            
            // Deactivate building events panel when player leaves
            if (buildingEventsPanel != null)
            {
                buildingEventsPanel.SetActive(false);
            }
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
    }

    private async void HandleEnteringBuilding(BuildingProximityDetector.Building building)
    {
        if (building.name == buildingName)
        {
            // Check if already entered
            var service = new UnlockedBuildingService();
            string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
            var enteredBuildings = await service.RetrieveUnlockedBuildings();
            // await service.ClearUnlockedBuildingsCache();
            bool alreadyEntered = enteredBuildings.Exists(b => b.buildingName == buildingName && b.userId == userId);
            if (alreadyEntered)
            {
                Debug.Log($"Building {buildingName} already entered. Skipping unlock logic.");
                return;
            }

            Debug.Log("Activating building: " + building.name);
            activated = true;

            // Show unlocked panel and related information
            if (buildingUnlockedPanel != null)
            {
                buildingUnlockedPanel.SetActive(true);
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
        var usersForBuilding = unlockedList.FindAll(b => b.buildingName == buildingName);

        try
        {
 if (usersForBuilding.Count == 0)
        {
            usersWhoUnlockedPanel.gameObject.SetActive(false);
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
        } catch (Exception e) {
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

    private void DisplayBuildingEvents()
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
        }

        if (cachedBuildingEvents != null && cachedBuildingEvents.Count > 0 && buildingEventsPanel != null)
        {
            buildingEventsPanel.SetActive(true);
            foreach (var evt in cachedBuildingEvents)
            {
                GameObject eventGO = Instantiate(eventPrefab, eventsContentParent);
                Text eventText = eventGO.GetComponentInChildren<Text>();
                if (eventText != null)
                {
                    eventText.text = $"{evt.eventName}\n{evt.date.ToString("MMM dd, yyyy h:mm tt")}";
                }
            }
        }
        else if (buildingEventsPanel != null)
        {
            buildingEventsPanel.SetActive(false);
        }
    }
}
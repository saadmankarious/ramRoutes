using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Data.SqlClient;
using RamRoutes.Model;
using RamRoutes.Services;
using Firebase.Auth;

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

    [Header("Debug")]
    [SerializeField] private bool showUnlockedPanelOnStart = true;

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

        if (showUnlockedPanelOnStart && buildingUnlockedPanel != null)
        {
            buildingUnlockedPanel.SetActive(true);
            DisplayUsersWhoUnlocked();
        }
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
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Spaceship"))
        {
            isPlayerInRange = false;
            CloseDialog();
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
            bool alreadyEntered = enteredBuildings.Exists(b => b.buildingName == buildingName && b.userId == userId);
            if (alreadyEntered)
            {
                Debug.Log($"Building {buildingName} already entered. Skipping unlock logic.");
                return;
            }

            Debug.Log("Activating building: " + building.name);
            activated = true;
            ShowDialog("Unocked building " + building.name);

            // Show unlocked panel
            if (buildingUnlockedPanel != null)
            {
                buildingUnlockedPanel.SetActive(true);
                DisplayUsersWhoUnlocked();
                // Panel will be hidden by button click
            }

            // Save unlock event to Firestore
            var record = new UnlockedBuildingRecord(
                userId,
                FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.DisplayName : "Unknown User",
                System.DateTime.UtcNow,
                buildingName,
                buildingName,
                transform.position
            );
            await service.SaveUnlockedBuildingAsync(record);
        }
    }

 public async void DisplayUsersWhoUnlocked()
{
    // Clear previous entries
    foreach (Transform child in usersContentParent)
    {
        Destroy(child.gameObject);
    }

    var service = new UnlockedBuildingService();
    var unlockedList = await service.RetrieveUnlockedBuildings();
    var usersForBuilding = unlockedList.FindAll(b => b.buildingName == buildingName);
    
    foreach (var record in usersForBuilding)
    {
        // Instantiate the prefab
        GameObject userGO = Instantiate(userPrefab, usersContentParent);
        
        // Get the Text component from a child object
        Text userNameText = userGO.GetComponentInChildren<Text>(true); // 'true' to include inactive children
        
        if (userNameText != null)
        {
            Debug.Log($"Setting user text: userName={record.userName}, userId={record.userId}");
            userNameText.text =  record.userId;
            
            // Optional: Adjust RectTransform if needed
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
}
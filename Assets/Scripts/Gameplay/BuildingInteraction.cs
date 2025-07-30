using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Data.SqlClient;

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
    }

    void OnEnable()
    {
        BuildingProximityDetector.OnApproachBuilding += HandleApproachBuilding;
    }

    void OnDisable()
    {
        BuildingProximityDetector.OnApproachBuilding -= HandleApproachBuilding;
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
        if (building.name == buildingName)
        {
            Debug.Log("Activating building: " + building.name);
            activated = true;
            ShowDialog("Unocked building " + building.name);
        }
    }
}
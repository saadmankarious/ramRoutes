using UnityEngine;
using UnityEngine.Events;
using RamRoutes.Services;
using RamRoutes.Model;
 
public class Gate : MonoBehaviour
{
    [Header("Gate Settings")]
    public bool isUnlocked = false; // Inspector boolean to set initial state
    public float pushForce = 1000f; // Force to push player back when locked
    public string lockMessage = "This gate is locked. Find a way to unlock it."; // Message to display when locked
    public string unlockedMessage = "The gate is now open!"; // Message to display when unlocked
    
    [Header("Animation")]
    public Animator gateAnimator;
    public string unlockAnimParam = "unlock"; // Animation parameter name
    
    [Header("Arrow Indicator")]
    public GameObject arrowIndicator; // Child arrow sprite to show when gate first opens
    public float beatAnimationScale = 1.2f; // Scale multiplier for beating animation
    public float beatAnimationSpeed = 2f; // Speed of beating animation
    [SerializeField] private Stage stageToRender;
    [Header("Audio")]
    public AudioClip unlockSound;
    public AudioClip blockedSound; // Sound when player tries to pass locked gate
    [Range(0f, 1f)]
    public float unlockVolume = 1f; // Volume for unlock sound
    [Range(0f, 1f)]
    public float blockedVolume = 1f; // Volume for blocked sound
    
    [Header("Events")]
    public UnityEvent OnGateUnlocked; // Event fired when gate is unlocked
    public UnityEvent OnGateLocked; // Event fired when gate is locked
    public UnityEvent OnPlayerBlocked; // Event fired when player is blocked by locked gate
    
    private Collider2D gateCollider;
    private AudioSource audioSource;
    private Rigidbody2D gateRigidbody;
    private bool wasUnlocked; // Track previous state for change detection
    private bool hasShownArrow = false; // Track if arrow has been shown for first unlock
    private Vector3 originalArrowScale; // Store original arrow scale
    
    // UI Manager reference

    // One-shot flag to suppress dialog in UpdateGateState
    private bool suppressDialogOnce = false;
    
    void Start()
    {
        // Get components
        gateCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        gateRigidbody = GetComponent<Rigidbody2D>();
        
        // Get animator if not assigned
        if (gateAnimator == null)
        {
            gateAnimator = GetComponent<Animator>();
        }
        
        // Find UI Manager
        // if (uiManager == null)
        // {
        //     uiManager = FindObjectOfType<UIManager>();
        //     if (uiManager == null)
        //     {
        //         // Try to find by singleton pattern
        //         uiManager = UIManager.Instance;
        //     }
        // }
        
        // if (uiManager == null)
        // {
        //     Debug.LogWarning("UIManager not found. Gate messages will not be displayed.");
        // }
        
        // Create AudioSource if it doesn't exist
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Create and configure Rigidbody2D to keep gate fixed
        if (gateRigidbody == null)
        {
            gateRigidbody = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configure Rigidbody2D to prevent movement
        gateRigidbody.bodyType = RigidbodyType2D.Static; // Static = immovable
        gateRigidbody.simulated = true;
        
        // Initialize arrow indicator
        InitializeArrowIndicator();
        
        // Initialize gate state (locked by default)
        wasUnlocked = isUnlocked;
        UpdateGateState();
        
        // Check if gate should be unlocked based on current stage at scene start
        CheckInitialGateState();
    }
    
    /// <summary>
    /// Checks if gate should be unlocked based on current stage at scene start only
    /// </summary>
    private void CheckInitialGateState()
    {
        var stage = GameStageService.LoadStageFromPrefs();
        if(stage.area == stageToRender)
        {
           UnlockGateSilently();
        }
    }

    void Update()
    {
        // Check if gate state changed in inspector during runtime
        if (wasUnlocked != isUnlocked)
        {
            wasUnlocked = isUnlocked;
            UpdateGateState();
        }
        // Commenting out automatic gate unlock based on stage
        // This prevents gates from automatically unlocking when stage changes
        // var stage = GameStageService.LoadStageFromPrefs();
        // if(stage.area == stageToRender)
        // {
        //    UnlockGateSilently();
        // }
        // else
        // {
        //     gameObject.SetActive(false);
        // }
    }
    
    void UpdateGateState()
    {
        // Switch collider type based on gate state
        if (gateCollider != null)
        {
            gateCollider.isTrigger = isUnlocked; // Trigger when unlocked, solid when locked
        }
        
        // Update animation
        if (gateAnimator != null)
        {
            gateAnimator.SetBool(unlockAnimParam, isUnlocked);
        }
        
        // Capture and clear suppression flag for this update
        bool allowDialog = !suppressDialogOnce;
        suppressDialogOnce = false;
        
        // Fire appropriate event and play sound
        if (isUnlocked)
        {
            OnGateUnlocked?.Invoke();
            PlaySound(unlockSound, unlockVolume);
              // Show unlock message and animate AROS
            // if (allowDialog && uiManager != null && !string.IsNullOrEmpty(unlockedMessage))
            // {
            //     uiManager.ShowDialog(unlockedMessage, 5f, false);
            // }
        }
        else
        {
            OnGateLocked?.Invoke();
            // No sound when locking
        }
    }
    
    void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    void InitializeArrowIndicator()
    {
        if (arrowIndicator != null)
        {
            // Store original scale
            originalArrowScale = arrowIndicator.transform.localScale;
            // Hide arrow initially
            arrowIndicator.SetActive(false);
        }
    }
    
    void ShowArrowWithAnimation()
    {
        if (arrowIndicator != null && !hasShownArrow)
        {
            hasShownArrow = true;
            arrowIndicator.SetActive(true);
            StartCoroutine(BeatingAnimation());
        }
    }
    
    System.Collections.IEnumerator BeatingAnimation()
    {
        if (arrowIndicator == null) yield break;
        
        while (arrowIndicator.activeInHierarchy)
        {
            // Scale up
            float elapsedTime = 0f;
            Vector3 startScale = originalArrowScale;
            Vector3 targetScale = originalArrowScale * beatAnimationScale;
            
            while (elapsedTime < (1f / beatAnimationSpeed))
            {
                if (arrowIndicator == null) yield break;
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (1f / beatAnimationSpeed);
                arrowIndicator.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            
            // Scale down
            elapsedTime = 0f;
            startScale = targetScale;
            targetScale = originalArrowScale;
            
            while (elapsedTime < (1f / beatAnimationSpeed))
            {
                if (arrowIndicator == null) yield break;
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (1f / beatAnimationSpeed);
                arrowIndicator.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            
            // Small pause between beats
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Public methods for other scripts to control the gate
    public void UnlockGate()
    {
        if (!isUnlocked)
        {
            isUnlocked = true;
            wasUnlocked = true;
            UpdateGateState();
            Debug.Log($"Gate {gameObject.name} unlocked!");
        }
    }

    // Unlock without showing dialog once (used for startup restores)
    public void UnlockGateSilently()
    {
        if (!isUnlocked)
        {
            suppressDialogOnce = true;
            isUnlocked = true;
            wasUnlocked = true;
            UpdateGateState();
                        ShowArrowWithAnimation(); // Show arrow with beating animation on first unlock

            Debug.Log($"Gate {gameObject.name} unlocked silently.");
        }
    }
    
    public void LockGate()
    {
        if (isUnlocked)
        {
            isUnlocked = false;
            wasUnlocked = false;
            UpdateGateState();
            Debug.Log($"Gate {gameObject.name} locked!");
        }
    }
    
    public void ToggleGate()
    {
        if (isUnlocked)
        {
            LockGate();
        }
        else
        {
            UnlockGate();
        }
    }
    
    public bool TryUnlockWithKey(string keyName)
    {
        UnlockGate();
        return true;
    }
    
    public bool IsUnlocked()
    {
        return isUnlocked;
    }
    
    // Collision detection for when gate is locked (solid collider)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isUnlocked)
        {
            // Player hit solid locked gate
            OnPlayerBlocked?.Invoke();
            PlaySound(blockedSound, blockedVolume);

            // Show lock message via UI Manager
            
                var notificationManager = FindObjectOfType<NotificationManager>();
                if (notificationManager != null)
                {
                    notificationManager.ShowNotification("Gate Locked", lockMessage);
                }

            
        }
    }
    
    public void HideArrowIndicator()
    {
        if (arrowIndicator != null)
        {
            arrowIndicator.SetActive(false);
        }
    }

    // Trigger detection for when gate is unlocked (trigger collider)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isUnlocked)
        {
            // Hide arrow when player passes through gate
            // HideArrowIndicator();
            Debug.Log("Player passed through unlocked gate.");
        }
    }
}

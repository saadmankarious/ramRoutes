using UnityEngine;
using UnityEngine.Events;

public class Gate : MonoBehaviour
{
    [Header("Gate Settings")]
    public bool isUnlocked = false; // Inspector boolean to set initial state
    public float pushForce = 1000f; // Force to push player back when locked
    
    [Header("Animation")]
    public Animator gateAnimator;
    public string unlockAnimParam = "unlock"; // Animation parameter name
    
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
        
        // Initialize gate state (locked by default)
        wasUnlocked = isUnlocked;
        UpdateGateState();
    }
    
    void Update()
    {
        // Check if gate state changed in inspector during runtime
        if (wasUnlocked != isUnlocked)
        {
            wasUnlocked = isUnlocked;
            UpdateGateState();
        }
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
        
        // Fire appropriate event and play sound
        if (isUnlocked)
        {
            OnGateUnlocked?.Invoke();
            PlaySound(unlockSound, unlockVolume);
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
            Debug.Log("Gate is locked! Find a way to unlock it.");
        }
    }
    
    // Trigger detection for when gate is unlocked (trigger collider)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isUnlocked)
        {
            Debug.Log("Player passed through unlocked gate.");
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isUnlocked)
        {
            Debug.Log("Player exited unlocked gate area.");
        }
    }
}

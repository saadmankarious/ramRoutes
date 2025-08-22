using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NpcAutoMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1f;
    public float moveRadius = 3f; // How far from anchor point NPC can move
    public float waitTime = 2f; // Time to wait at each destination
    public float directionChangeInterval = 3f; // How often to pick new direction
    
    [Header("Building Association")]
    public string associatedBuilding = ""; // Which building this NPC belongs to
    public bool stayNearBuilding = true; // Whether to constrain movement to building area
    public Transform spawnPoint; // Spawn point to return to (usually building or nearby point)
    public NPCSpawner npcSpawner; // Reference to spawner for cleanup
    
    [Header("Conversation")]
    public string[] conversationLines = {
        "Hello there, traveler!",
        "Beautiful day, isn't it?",
        "Safe travels on your journey!"
    };
    public float textDisplaySpeed = 0.05f; // Speed of text appearance
    public KeyCode continueKey = KeyCode.Space; // Key to continue conversation
    
    [Header("UI References")]
    public Image npcPanel; // UI Panel component - assigned by spawner at runtime
    public Text npcNameText; // Text component for NPC name - assigned by spawner at runtime
    public Text conversationText; // Text component for conversation lines - assigned by spawner at runtime
    public Image npcSpriteImage; // Image component for NPC sprite - assigned by spawner at runtime
    
    [Header("NPC Info")]
    public string npcName = "Unknown NPC"; // Will be set by spawner
    
    [Header("Animation")]
    private Animator animator;
    private string movingXParam = "moveX";
    private string movingYParam = "moveY";
    private string idleParam = "idle";
    
    [Header("Audio")]
    public AudioClip npcSound; // Sound to play when spawning and despawning
    public AudioClip typingTickSound; // Sound to play during conversation typing
    private AudioSource audioSource;
    
    [Header("Typing Sound Settings")]
    [SerializeField] private float typingSoundInterval = 0.15f;
    private float lastTypingSoundTime;
    
    [Header("Despawn Settings")]
    [SerializeField] private float maxLifetime = 30f; // 3 minutes maximum lifetime
    private float spawnTime;
    
    private Vector3 anchorPoint;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Vector2 lastMoveDirection;
    private float waitTimer;
    private float directionTimer;
    private bool isWaiting = false;
    
    // NPC State Management
    private enum NPCState { PursuingPlayer, Normal, ReturningToSpawn }
    private NPCState currentState = NPCState.PursuingPlayer; // Start by pursuing player
    private bool playerNearby = false;
    
    // Return to spawn pathfinding state
    private bool returningHorizontalFirst = false;
    private bool hasChosenReturnPath = false;
    
    // Player pursuit pathfinding state
    private bool pursuingHorizontalFirst = false;
    private bool hasChosenPursuitPath = false;
    
    // Conversation variables
    private bool isInConversation = false;
    private int currentLineIndex = 0;
    private string currentDisplayedText = "";
    private float textTimer = 0f;
    private bool isTyping = false;
    
    void Start()
    {
        // Record spawn time for lifetime tracking
        spawnTime = Time.time;
        
        // Set anchor point to starting position
        anchorPoint = transform.position;
        
        // Get animator if not assigned
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Set initial target (for normal movement later)
        ChooseNewTarget();
        
        // Start in pursuit mode - don't wait, immediately begin pursuing
        isWaiting = false;
        
        // Initialize UI
        InitializeUI();
        
        // Play spawn sound
        PlayNPCSound();
    }
    
    void InitializeUI()
    {
        if (npcPanel != null)
        {
            // Set NPC name in the UI
            if (npcNameText != null)
            {
                npcNameText.text = npcName;
            }
            
            // Set NPC sprite in the UI
            if (npcSpriteImage != null)
            {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    npcSpriteImage.sprite = spriteRenderer.sprite;
                    Debug.Log($"NPC {gameObject.name}: Set sprite '{spriteRenderer.sprite.name}' in UI");
                }
                else
                {
                    Debug.LogWarning($"NPC {gameObject.name}: No SpriteRenderer or sprite found for UI");
                }
            }
            
            // Show the panel since NPC is in scene with popup animation
            npcPanel.gameObject.SetActive(true);
            
            // Apply popup animation using UIManager
            if (UIManager.Instance != null)
            {
                StartCoroutine(UIManager.Instance.AnimatePanelPopup(npcPanel.gameObject));
            }
            
            // Initially hide conversation text
            if (conversationText != null)
            {
                conversationText.text = "";
            }
            
            Debug.Log($"NPC {gameObject.name}: UI initialized with name '{npcName}' with popup animation");
        }
        else
        {
            Debug.LogWarning($"NPC {gameObject.name}: No NPC panel assigned in inspector!");
        }
    
    }
    
    void Update()
    {
        // Check if NPC has exceeded maximum lifetime
        if (Time.time - spawnTime > maxLifetime)
        {
            Debug.Log($"NPC {gameObject.name}: Exceeded maximum lifetime ({maxLifetime}s), despawning");
            DespawnNPC();
            return;
        }
        
        if (isInConversation)
        {
            HandleConversation();
        }
        else
        {
            HandleMovement();
        }
        
        // Fallback: Check distance to player if in Normal state
        if (currentState == NPCState.Normal && playerNearby)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
                // If player is too far away, start return to spawn
                if (distanceToPlayer > 5f) // Fallback distance check
                {
                    Debug.Log($"NPC {gameObject.name}: Player too far ({distanceToPlayer:F2}), returning to spawn (fallback)");
                    playerNearby = false;
                    if (isInConversation)
                    {
                        EndConversation();
                    }
                    StartReturnToSpawn();
                }
            }
        }
        
        UpdateAnimationParameters();
    }
    
    void HandleMovement()
    {
        switch (currentState)
        {
            case NPCState.PursuingPlayer:
                HandlePlayerPursuit();
                break;
            case NPCState.Normal:
                HandleNormalMovement();
                break;
            case NPCState.ReturningToSpawn:
                HandleReturnToSpawn();
                break;
        }
    }
    
    void HandlePlayerPursuit()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            // No player found, switch to normal movement
            currentState = NPCState.Normal;
            currentVelocity = Vector3.zero; // Stop movement
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        // If close enough to player, switch to normal movement
        if (distanceToPlayer < 1.5f)
        {
            Debug.Log($"NPC {gameObject.name}: Reached player, switching to normal movement");
            currentState = NPCState.Normal;
            hasChosenPursuitPath = false; // Reset for future use
            currentVelocity = Vector3.zero; // Stop movement
            return;
        }
        
        // Calculate smooth path to player using same logic as return-to-spawn
        Vector3 directionToPlayer = GetDirectionToPlayer(player.transform.position);
        
        if (directionToPlayer != Vector3.zero)
        {
            // Move towards player with proper animation
            currentVelocity = directionToPlayer * moveSpeed;
            transform.position += currentVelocity * Time.deltaTime;
            
            // Ensure NPC is not in waiting state during pursuit
            isWaiting = false;
        }
        else
        {
            // No direction to move, stop
            currentVelocity = Vector3.zero;
        }
    }
    
    Vector3 GetDirectionToPlayer(Vector3 playerPosition)
    {
        Vector3 toPlayer = playerPosition - transform.position;
        
        // If very close, return zero
        if (toPlayer.magnitude < 1.5f)
            return Vector3.zero;
        
        Vector3 direction = Vector3.zero;
        
        // Choose path direction only once when starting pursuit
        if (!hasChosenPursuitPath)
        {
            pursuingHorizontalFirst = Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y);
            hasChosenPursuitPath = true;
            Debug.Log($"NPC {gameObject.name}: Starting pursuit - horizontal first: {pursuingHorizontalFirst}");
        }
        
        // Stick with chosen direction until that axis is complete
        if (pursuingHorizontalFirst)
        {
            // Move horizontally first
            if (Mathf.Abs(toPlayer.x) > 0.1f)
            {
                direction.x = toPlayer.x > 0 ? 1 : -1;
                direction.y = 0;
            }
            else
            {
                // Horizontal movement complete, now move vertically
                direction.x = 0;
                direction.y = toPlayer.y > 0 ? 1 : -1;
            }
        }
        else
        {
            // Move vertically first
            if (Mathf.Abs(toPlayer.y) > 0.1f)
            {
                direction.x = 0;
                direction.y = toPlayer.y > 0 ? 1 : -1;
            }
            else
            {
                // Vertical movement complete, now move horizontally
                direction.x = toPlayer.x > 0 ? 1 : -1;
                direction.y = 0;
            }
        }
        
        return direction;
    }
    
    void HandleNormalMovement()
    {
        // Check if we're waiting
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                ChooseNewTarget();
            }
            currentVelocity = Vector3.zero;
            return;
        }

        // Simple direct movement towards target - only X OR Y can be non-zero
        Vector3 toTarget = targetPosition - transform.position;
        Vector3 direction = Vector3.zero;
        
        // Move only on the axis that has distance remaining
        if (Mathf.Abs(toTarget.x) > 0.1f)
        {
            direction.x = toTarget.x > 0 ? 1 : -1;
            direction.y = 0;
        }
        else if (Mathf.Abs(toTarget.y) > 0.1f)
        {
            direction.x = 0;
            direction.y = toTarget.y > 0 ? 1 : -1;
        }
        
        currentVelocity = direction * moveSpeed;
        transform.position += currentVelocity * Time.deltaTime;
        
        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            StartWaiting();
        }
        
        // Change direction periodically even if not at target
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0)
        {
            ChooseNewTarget();
        }
    }
    
    void HandleReturnToSpawn()
    {
        if (spawnPoint == null) 
        {
            Debug.LogWarning($"NPC {gameObject.name}: No spawn point assigned, despawning immediately");
            DespawnNPC();
            return;
        }
        
        // Calculate direct path to spawn point
        Vector3 directionToSpawn = GetDirectionToSpawn();
        
        if (directionToSpawn != Vector3.zero)
        {
            // Move towards spawn point
            currentVelocity = directionToSpawn * moveSpeed;
            transform.position += currentVelocity * Time.deltaTime;
            
            // Check if close enough to spawn point
            float distanceToSpawn = Vector3.Distance(transform.position, spawnPoint.position);
            Debug.Log($"NPC {gameObject.name}: Moving to spawn, distance: {distanceToSpawn:F2}");
            
            if (distanceToSpawn < 0.2f)
            {
                Debug.Log($"NPC {gameObject.name}: Reached spawn point, despawning");
                DespawnNPC();
            }
        }
        else
        {
            // Already at spawn point
            Debug.Log($"NPC {gameObject.name}: At spawn point, despawning");
            DespawnNPC();
        }
    }
    
    Vector3 GetDirectionToSpawn()
    {
        if (spawnPoint == null) return Vector3.zero;
        
        Vector3 toSpawn = spawnPoint.position - transform.position;
        
        // If very close, return zero (we're at spawn)
        if (toSpawn.magnitude < 0.2f)
            return Vector3.zero;
        
        // Calculate direction with stable pathfinding logic
        Vector3 direction = Vector3.zero;
        
        // Choose path direction only once when starting return
        if (!hasChosenReturnPath)
        {
            returningHorizontalFirst = Mathf.Abs(toSpawn.x) > Mathf.Abs(toSpawn.y);
            hasChosenReturnPath = true;
            Debug.Log($"NPC {gameObject.name}: Chose return path - horizontal first: {returningHorizontalFirst}");
        }
        
        // Stick with chosen direction until that axis is complete
        if (returningHorizontalFirst)
        {
            // Move horizontally first
            if (Mathf.Abs(toSpawn.x) > 0.1f)
            {
                direction.x = toSpawn.x > 0 ? 1 : -1;
                direction.y = 0;
            }
            else
            {
                // Horizontal movement complete, now move vertically
                direction.x = 0;
                direction.y = toSpawn.y > 0 ? 1 : -1;
            }
        }
        else
        {
            // Move vertically first
            if (Mathf.Abs(toSpawn.y) > 0.1f)
            {
                direction.x = 0;
                direction.y = toSpawn.y > 0 ? 1 : -1;
            }
            else
            {
                // Vertical movement complete, now move horizontally
                direction.x = toSpawn.x > 0 ? 1 : -1;
                direction.y = 0;
            }
        }
        
        return direction;
    }    void ChooseNewTarget()
    {
        // Choose one of 4 pure cardinal directions from current position
        int direction = Random.Range(0, 4);
        Vector3 currentPos = transform.position;
        float distance = Random.Range(0.5f, moveRadius);
        
        switch (direction)
        {
            case 0: // Right
                targetPosition = currentPos + Vector3.right * distance;
                break;
            case 1: // Left
                targetPosition = currentPos + Vector3.left * distance;
                break;
            case 2: // Up
                targetPosition = currentPos + Vector3.up * distance;
                break;
            case 3: // Down
                targetPosition = currentPos + Vector3.down * distance;
                break;
        }
        
        // Reset direction timer
        directionTimer = directionChangeInterval + Random.Range(-1f, 1f);
    }
    
    void StartWaiting()
    {
        isWaiting = true;
        waitTimer = waitTime + Random.Range(-0.5f, 0.5f); // Add some randomness
    }
    
    void UpdateAnimationParameters()
    {
        if (animator == null) return;
        
        // Calculate move input from current velocity
        Vector2 moveInput = Vector2.zero;
        
        // Only show movement animation if actually moving (not waiting and has velocity)
        if (!isWaiting && currentVelocity.magnitude > 0.1f)
        {
            // Normalize the velocity to get direction for animation
            moveInput = new Vector2(currentVelocity.x, currentVelocity.y).normalized;
        }
        
        // Update last move direction and set animation parameters
        lastMoveDirection = moveInput;
        
        // Set movement animation parameters
        animator.SetFloat(movingXParam, moveInput.x);
        animator.SetFloat(movingYParam, moveInput.y);
        animator.SetBool(idleParam, moveInput.magnitude < 0.1f);
        
        // Debug animation state during pursuit
        if (currentState == NPCState.PursuingPlayer && currentVelocity.magnitude > 0.1f)
        {
            Debug.Log($"NPC {gameObject.name}: Pursuit animation - X: {moveInput.x:F1}, Y: {moveInput.y:F1}, Idle: {moveInput.magnitude < 0.1f}");
        }
    }
    
    // Conversation System
    void StartConversation()
    {
        if (conversationLines.Length == 0) return;
        if (npcPanel == null || conversationText == null) return;
        
        isInConversation = true;
        currentLineIndex = 0;
        StartTyping();
        
        Debug.Log($"NPC {gameObject.name}: Starting conversation");
    }
    
    void StartTyping()
    {
        if (currentLineIndex >= conversationLines.Length) return;
        
        currentDisplayedText = "";
        textTimer = 0f;
        isTyping = true;
        lastTypingSoundTime = 0f; // Reset typing sound timer for new line
    }
    
    void HandleConversation()
    {
        if (isTyping)
        {
            // Type out text character by character
            textTimer += Time.deltaTime;
            if (textTimer >= textDisplaySpeed)
            {
                textTimer = 0f;
                if (currentDisplayedText.Length < conversationLines[currentLineIndex].Length)
                {
                    currentDisplayedText += conversationLines[currentLineIndex][currentDisplayedText.Length];
                    conversationText.text = currentDisplayedText;
                    
                    // Play typing sound at intervals
                    if (Time.time - lastTypingSoundTime >= typingSoundInterval)
                    {
                        PlayTypingSound();
                        lastTypingSoundTime = Time.time;
                    }
                }
                else
                {
                    isTyping = false;
                }
            }
        }
        
        // Handle input to continue conversation
        if (Input.GetKeyDown(continueKey))
        {
            if (isTyping)
            {
                // Skip typing animation and show full text
                currentDisplayedText = conversationLines[currentLineIndex];
                conversationText.text = currentDisplayedText;
                isTyping = false;
            }
            else
            {
                // Move to next line or end conversation
                currentLineIndex++;
                if (currentLineIndex >= conversationLines.Length)
                {
                    EndConversation();
                }
                else
                {
                    StartTyping();
                }
            }
        }
    }
    
    void EndConversation()
    {
        isInConversation = false;
        currentLineIndex = 0;
        isWaiting = false; // Resume movement when conversation ends
        
        // Clear conversation text but keep panel visible (showing NPC name)
        if (conversationText != null)
        {
            conversationText.text = "";
        }
        
        Debug.Log($"NPC {gameObject.name}: Conversation ended");
    }
    
    // Collision detection for starting conversation
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isInConversation)
        {
            playerNearby = true;
            Debug.Log($"NPC {gameObject.name}: Player entered trigger, starting conversation");
            
            // Switch to normal state if pursuing
            if (currentState == NPCState.PursuingPlayer)
            {
                currentState = NPCState.Normal;
            }
            
            if (animator != null) animator.SetBool(idleParam, true); // Set idle animation
            isWaiting = true; // Stop movement
            StartConversation();
        }
    }
    
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player") && !isInConversation)
        {
            playerNearby = true;
            Debug.Log($"NPC {gameObject.name}: Player collision detected, starting conversation");
            
            // Switch to normal state if pursuing
            if (currentState == NPCState.PursuingPlayer)
            {
                currentState = NPCState.Normal;
            }
            
            StartConversation();
        }
    }
    
    // End conversation when player walks away
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            Debug.Log($"NPC {gameObject.name}: Player left trigger area, ending conversation and returning to spawn");
            
            if (isInConversation)
            {
                EndConversation();
            }
            
            // Start return to spawn sequence
            if (currentState == NPCState.Normal)
            {
                StartReturnToSpawn();
            }
        }
    }
    
    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerNearby = false;
            Debug.Log($"NPC {gameObject.name}: Player collision exit detected, returning to spawn");
            
            if (isInConversation)
            {
                EndConversation();
            }
            
            // Start return to spawn sequence
            if (currentState == NPCState.Normal)
            {
                StartReturnToSpawn();
            }
        }
    }

    void StartReturnToSpawn()
    {
        if (currentState != NPCState.Normal) return;
        
        Debug.Log($"NPC {gameObject.name}: Changing state to ReturningToSpawn");
        currentState = NPCState.ReturningToSpawn;
        isWaiting = false; // Stop any waiting
        
        // Reset pathfinding state for new return journey
        hasChosenReturnPath = false;
        returningHorizontalFirst = false;
    }
    
    // New method for spawner to trigger return-to-spawn for despawn
    public void StartReturnToSpawnForDespawn()
    {
        Debug.Log($"NPC {gameObject.name}: Player left building, starting return to spawn for despawn");
        
        // End any active conversation
        if (isInConversation)
        {
            EndConversation();
        }
        
        // Start return to spawn
        StartReturnToSpawn();
    }
    
    // Public method to check if NPC is currently in conversation
    public bool IsInConversation()
    {
        return isInConversation;
    }
    
    void DespawnNPC()
    {
        Debug.Log($"Despawning NPC {gameObject.name}");
        
        // Hide the NPC panel
        if (npcPanel != null)
        {
            npcPanel.gameObject.SetActive(false);
        }
        
        // Play despawn sound
        PlayNPCSound();
        
        // Notify spawner that this NPC is being removed
        if (npcSpawner != null)
        {
            // Extract NPC name from the gameObject name (remove the building part)
            string npcName = gameObject.name;
            if (npcName.Contains(" (Building:"))
            {
                npcName = npcName.Substring(0, npcName.IndexOf(" (Building:"));
            }
            npcSpawner.OnNPCDespawned(npcName);
        }
        
        // Destroy the NPC GameObject
        Destroy(gameObject);
    }
    
    void PlayNPCSound()
    {
        if (npcSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(npcSound);
            Debug.Log($"NPC {gameObject.name}: Playing NPC sound");
        }
    }
    
    void PlayTypingSound()
    {
        if (typingTickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(typingTickSound, 0.1f); // Volume 3 times lower than UIManager
            // Debug.Log($"NPC {gameObject.name}: Playing typing sound"); // Commented out to avoid spam
        }
    }

    
    // Visualize the movement radius in the scene view
    void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? anchorPoint : transform.position;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, moveRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.1f);
        
        // Draw spawn point if assigned
        if (spawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.2f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
        
        if (Application.isPlaying && (currentState == NPCState.Normal || currentState == NPCState.PursuingPlayer))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.1f);
            Gizmos.DrawLine(transform.position, targetPosition);
            
            // Show player pursuit target
            if (currentState == NPCState.PursuingPlayer)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, player.transform.position);
                }
            }
        }
        
        // Show current state
        if (Application.isPlaying)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"State: {currentState}");
#endif
        }
    }
}

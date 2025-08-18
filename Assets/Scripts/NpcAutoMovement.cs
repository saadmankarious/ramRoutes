using UnityEngine;
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
    
    [Header("Animation")]
    private Animator animator;
    private string movingXParam = "moveX";
    private string movingYParam = "moveY";
    private string idleParam = "idle";
    
    private Vector3 anchorPoint;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Vector2 lastMoveDirection;
    private float waitTimer;
    private float directionTimer;
    private bool isWaiting = false;
    
    // NPC State Management
    private enum NPCState { Normal, ReturningToSpawn }
    private NPCState currentState = NPCState.Normal;
    private bool playerNearby = false;
    
    // Conversation variables
    private bool isInConversation = false;
    private int currentLineIndex = 0;
    private string currentDisplayedText = "";
    private float textTimer = 0f;
    private bool isTyping = false;
    private GameObject conversationUI;
    private UnityEngine.UI.Text conversationText;
    
    void Start()
    {
        // Set anchor point to starting position
        anchorPoint = transform.position;
        
        // Get animator if not assigned
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Set initial target
        ChooseNewTarget();
        
        // Create conversation UI
        CreateConversationUI();
    }
    
    void Update()
    {
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
            case NPCState.Normal:
                HandleNormalMovement();
                break;
            case NPCState.ReturningToSpawn:
                HandleReturnToSpawn();
                break;
        }
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
        
        // Calculate direction with pathfinding logic
        Vector3 direction = Vector3.zero;
        
        // Simple pathfinding: move on strongest axis first
        if (Mathf.Abs(toSpawn.x) > Mathf.Abs(toSpawn.y))
        {
            // Move horizontally first
            direction.x = toSpawn.x > 0 ? 1 : -1;
            direction.y = 0;
        }
        else
        {
            // Move vertically first  
            direction.x = 0;
            direction.y = toSpawn.y > 0 ? 1 : -1;
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
        
        if (!isWaiting && currentVelocity.magnitude > 0.1f)
        {
            // Normalize the velocity to get direction
            moveInput = new Vector2(currentVelocity.x, currentVelocity.y).normalized;
        }
        
        // Update last move direction and set animation parameters
        lastMoveDirection = moveInput;
        
        // Set movement animation parameters
        animator.SetFloat(movingXParam, moveInput.x);
        animator.SetFloat(movingYParam, moveInput.y);
        animator.SetBool(idleParam, moveInput.magnitude < 0.1f);
    }
    
    // Conversation System
    void CreateConversationUI()
    {
        // Create UI Canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("ConversationCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create conversation UI panel
        conversationUI = new GameObject("ConversationPanel");
        conversationUI.transform.SetParent(canvas.transform, false);
        
        // Add background panel
        UnityEngine.UI.Image background = conversationUI.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0, 0, 0, 0.8f);
        
        // Set panel size and position
        RectTransform panelRect = conversationUI.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.3f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create text component
        GameObject textObj = new GameObject("ConversationText");
        textObj.transform.SetParent(conversationUI.transform, false);
        conversationText = textObj.AddComponent<UnityEngine.UI.Text>();
        
        // Configure text
        conversationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        conversationText.fontSize = 18;
        conversationText.color = Color.white;
        conversationText.alignment = TextAnchor.MiddleLeft;
        
        // Set text size
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 20);
        textRect.offsetMax = new Vector2(-20, -20);
        
        // Hide UI initially
        conversationUI.SetActive(false);
    }
    
    void StartConversation()
    {
        if (conversationLines.Length == 0) return;
        
        isInConversation = true;
        currentLineIndex = 0;
        conversationUI.SetActive(true);
        StartTyping();
    }
    
    void StartTyping()
    {
        if (currentLineIndex >= conversationLines.Length) return;
        
        currentDisplayedText = "";
        textTimer = 0f;
        isTyping = true;
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
        conversationUI.SetActive(false);
        currentLineIndex = 0;
        isWaiting = false; // Resume movement when conversation ends
    }
    
    // Collision detection for starting conversation
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isInConversation && currentState == NPCState.Normal)
        {
            playerNearby = true;
            Debug.Log($"NPC {gameObject.name}: Player entered trigger, starting conversation");
            if (animator != null) animator.SetBool(idleParam, true); // Set idle animation
            isWaiting = true; // Stop movement
            StartConversation();
        }
    }
    
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player") && !isInConversation && currentState == NPCState.Normal)
        {
            playerNearby = true;
            Debug.Log($"NPC {gameObject.name}: Player collision detected, starting conversation");
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
    }
    
    void DespawnNPC()
    {
        Debug.Log($"Despawning NPC {gameObject.name}");
        
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
        
        if (Application.isPlaying && currentState == NPCState.Normal)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.1f);
            Gizmos.DrawLine(transform.position, targetPosition);
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

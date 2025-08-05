using UnityEngine;

public class NpcAutoMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1f;
    public float moveRadius = 3f; // How far from anchor point NPC can move
    public float waitTime = 2f; // Time to wait at each destination
    public float directionChangeInterval = 3f; // How often to pick new direction
    
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
        
        UpdateAnimationParameters();
    }
    
    void HandleMovement()
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
        if (other.CompareTag("Player") && !isInConversation)
        {
            animator.SetBool(idleParam, true); // Set idle animation
            isWaiting = true; // Stop movement
            StartConversation();
        }
    }
    
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player") && !isInConversation)
        {
            StartConversation();
        }
    }
    
    // End conversation when player walks away
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isInConversation)
        {
            EndConversation();
        }
    }
    
    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player") && isInConversation)
        {
            EndConversation();
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
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.1f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}

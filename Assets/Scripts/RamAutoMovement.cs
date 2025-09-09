using UnityEngine;

public class RamAutoMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1f;
    public float moveRadius = 3f; // How far from anchor point Ram can move
    public float waitTime = 2f; // Time to wait at each destination
    public float directionChangeInterval = 3f; // How often to pick new direction
    
    [Header("Randomization Ranges")]
    public Vector2 speedRange = new Vector2(0.5f, 2f); // Min and max speed values
    public Vector2 waitTimeRange = new Vector2(1f, 4f); // Min and max wait time values
    
    [Header("Collision Avoidance")]
    public LayerMask obstacleLayerMask = -1; // What layers to consider as obstacles
    public float collisionCheckDistance = 0.5f; // How far ahead to check for obstacles
    public float avoidanceRadius = 0.3f; // Radius for collision detection
    
    [Header("Animation")]
    private Animator animator;
    private string movingXParam = "MoveX";
    private string movingYParam = "MoveY";
    private string idleParam = "idle";
    private string movementMagnitudeParam = "MoveMagnitude"; // For transition control
    
    private Vector3 anchorPoint;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Vector2 lastMoveDirection;
    private float waitTimer;
    private float directionTimer;
    private bool isWaiting = false;
    
    void Start()
    {
        // Set anchor point to starting position
        anchorPoint = transform.position;
        
        // Randomize movement characteristics for variety
        moveSpeed = Random.Range(speedRange.x, speedRange.y);
        waitTime = Random.Range(waitTimeRange.x, waitTimeRange.y);
        
        // Get animator if available
        animator = GetComponent<Animator>();
        
        // Set initial target
        ChooseNewTarget();
    }
    
    void Update()
    {
        HandleMovement();
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
        
        // Check for obstacles before moving
        Vector3 proposedPosition = transform.position + currentVelocity * Time.deltaTime;
        if (!IsPositionBlocked(proposedPosition))
        {
            transform.position = proposedPosition;
        }
        else
        {
            // If blocked, try to find alternative direction or wait
            Vector3 alternativeDirection = FindAlternativeDirection(direction);
            if (alternativeDirection != Vector3.zero)
            {
                Vector3 alternativePosition = transform.position + alternativeDirection * moveSpeed * Time.deltaTime;
                if (!IsPositionBlocked(alternativePosition))
                {
                    transform.position = alternativePosition;
                    currentVelocity = alternativeDirection * moveSpeed;
                }
                else
                {
                    // Can't move anywhere, start waiting
                    StartWaiting();
                }
            }
            else
            {
                // No alternative found, start waiting
                StartWaiting();
            }
        }
        
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
    
    void ChooseNewTarget()
    {
        // Try multiple times to find a valid target
        int maxAttempts = 8;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Choose one of 4 pure cardinal directions from current position
            int direction = Random.Range(0, 4);
            Vector3 currentPos = transform.position;
            float distance = Random.Range(0.5f, moveRadius);
            
            Vector3 potentialTarget = currentPos;
            switch (direction)
            {
                case 0: // Right
                    potentialTarget = currentPos + Vector3.right * distance;
                    break;
                case 1: // Left
                    potentialTarget = currentPos + Vector3.left * distance;
                    break;
                case 2: // Up
                    potentialTarget = currentPos + Vector3.up * distance;
                    break;
                case 3: // Down
                    potentialTarget = currentPos + Vector3.down * distance;
                    break;
            }
            
            // Check if target position is valid (not blocked)
            if (!IsPositionBlocked(potentialTarget))
            {
                targetPosition = potentialTarget;
                break;
            }
        }
        
        // Reset direction timer
        directionTimer = directionChangeInterval + Random.Range(-1f, 1f);
    }
    
    /// <summary>
    /// Check if a position is blocked by obstacles
    /// </summary>
    bool IsPositionBlocked(Vector3 position)
    {
        // Use OverlapCircle to check for collisions at the position
        Collider2D obstacle = Physics2D.OverlapCircle(position, avoidanceRadius, obstacleLayerMask);
        return obstacle != null && obstacle.gameObject != gameObject; // Ignore self
    }
    
    /// <summary>
    /// Find an alternative direction when the primary direction is blocked
    /// </summary>
    Vector3 FindAlternativeDirection(Vector3 blockedDirection)
    {
        // Try perpendicular directions first
        Vector3[] alternatives;
        
        if (Mathf.Abs(blockedDirection.x) > 0.5f) // Moving horizontally
        {
            alternatives = new Vector3[] { Vector3.up, Vector3.down };
        }
        else // Moving vertically
        {
            alternatives = new Vector3[] { Vector3.right, Vector3.left };
        }
        
        // Test each alternative
        foreach (Vector3 alt in alternatives)
        {
            Vector3 testPosition = transform.position + alt * moveSpeed * Time.deltaTime;
            if (!IsPositionBlocked(testPosition))
            {
                return alt;
            }
        }
        
        // No alternatives found
        return Vector3.zero;
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
        
        // Only update direction when actually moving (not waiting and has velocity)
        if (!isWaiting && currentVelocity.magnitude > 0.1f)
        {
            // Normalize the velocity to get direction for animation
            moveInput = new Vector2(currentVelocity.x, currentVelocity.y).normalized;
            // Update last move direction only when moving
            lastMoveDirection = moveInput;
        }
        else
        {
            // When idle, use the last move direction to maintain proper idle animation
            moveInput = lastMoveDirection;
        }
        
        // Calculate movement magnitude for state transitions
        float moveMagnitude = (!isWaiting && currentVelocity.magnitude > 0.1f) ? 1f : 0f;
        
        // Set animation parameters - both trees use same MoveX/MoveY values
        animator.SetFloat(movingXParam, moveInput.x);
        animator.SetFloat(movingYParam, moveInput.y);
        animator.SetBool(idleParam, moveMagnitude < 0.1f);
        // animator.SetFloat(movementMagnitudeParam, moveMagnitude); // For smooth transitions
    }
    
    // Visualize the movement radius in the scene view
    void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? anchorPoint : transform.position;
        
        // Movement radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, moveRadius);
        
        // Anchor point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.1f);
        
        // Current target and path
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.1f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        
        // Collision detection radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
        
        // Show collision check ahead
        if (Application.isPlaying && currentVelocity.magnitude > 0.1f)
        {
            Vector3 checkPosition = transform.position + currentVelocity.normalized * collisionCheckDistance;
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(checkPosition, avoidanceRadius);
            Gizmos.DrawLine(transform.position, checkPosition);
        }
    }
}

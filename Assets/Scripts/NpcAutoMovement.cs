using UnityEngine;

public class NpcAutoMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1f;
    public float moveRadius = 3f; // How far from anchor point NPC can move
    public float waitTime = 2f; // Time to wait at each destination
    public float directionChangeInterval = 3f; // How often to pick new direction
    
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
    
    void Start()
    {
        // Set anchor point to starting position
        anchorPoint = transform.position;
        
        // Get animator if not assigned
        if (animator == null)
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

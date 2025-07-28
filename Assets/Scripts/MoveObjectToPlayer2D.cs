using UnityEngine;

public class FinalSpaceshipControl : MonoBehaviour
{
    [Header("Movement Settings")]
    public KeyCode actionKey = KeyCode.E;
    public Transform spaceship;
    public float moveSpeed = 5f;
    public float alignmentThreshold = 0.1f;
    public float flightSpeed = 8f;
    public float dropDistance = 1f;
    public float boundaryPadding = .1f;
    public Collider2D confiner;

    [Header("Animation Settings")]
    public Animator spaceshipAnimator;
    public string navigateParameter = "navigate";

    [Header("Visual Settings")]
    public SpriteRenderer playerSprite;

    [Header("Offset Settings")]
    public Vector2 playerOffset = Vector2.zero;

    private enum MovementPhase { 
        Idle, 
        MovingToPlayerX, 
        MovingToPlayerY, 
        AscendingToOriginY,
        PlayerControlling,
        DroppingPlayer
    }
    private MovementPhase currentPhase = MovementPhase.Idle;
    
    private Vector2 originalPosition;
    private bool playerOnBoard = false;
    private Rigidbody2D playerRb;
    private Rigidbody2D spaceshipRb;
    private float horizontalInput;
    private float verticalInput;
    private bool wasMovingLastFrame = false;
    private Vector2 dropStartPosition;

    void Start()
    {
        if (spaceship != null)
        {
            originalPosition = spaceship.position;
            spaceshipRb = spaceship.GetComponent<Rigidbody2D>();
            if (spaceshipRb == null)
            {
                spaceshipRb = spaceship.gameObject.AddComponent<Rigidbody2D>();
                spaceshipRb.gravityScale = 0;
                spaceshipRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            if (spaceshipAnimator == null)
            {
                spaceshipAnimator = spaceship.GetComponent<Animator>();
            }
        }

        playerRb = GetComponent<Rigidbody2D>();
        if (playerRb == null)
        {
            playerRb = gameObject.AddComponent<Rigidbody2D>();
        }

        if (playerSprite == null)
        {
            playerSprite = GetComponent<SpriteRenderer>();
        }
    }

  void Update()
{
    int trial = GameManager.Instance.currentTrial.trialNumber;
    if (trial != 3 && trial != 4) return;

    HandleInput();
    UpdateMovement();
    UpdateAnimation();
}

    private void HandleInput()
    {
        if (Input.GetKeyDown(actionKey))
        {
            if (currentPhase == MovementPhase.Idle)
            {
                currentPhase = MovementPhase.MovingToPlayerX;
            }
            else if (currentPhase == MovementPhase.PlayerControlling)
            {
                StartDroppingPlayer();
            }
        }
    }

    private void UpdateMovement()
    {
        if (spaceship == null) return;

        switch (currentPhase)
        {
            case MovementPhase.MovingToPlayerX:
                MoveSpaceshipAlongXToPlayer();
                break;
                
            case MovementPhase.MovingToPlayerY:
                MoveSpaceshipAlongYToPlayer();
                break;
                
            case MovementPhase.AscendingToOriginY:
                AscendToOriginalHeight();
                break;
                
            case MovementPhase.PlayerControlling:
                ControlSpaceshipMovement();
                break;
                
            case MovementPhase.DroppingPlayer:
                DropPlayer();
                break;
        }
    }

    private void UpdateAnimation()
    {
        if (spaceshipAnimator == null) return;

        bool isMoving = currentPhase == MovementPhase.PlayerControlling && 
                      (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f);

        if (isMoving != wasMovingLastFrame)
        {
            spaceshipAnimator.SetBool(navigateParameter, isMoving);
            wasMovingLastFrame = isMoving;
        }
    }

    private void MoveSpaceshipAlongXToPlayer()
    {
        float targetX = transform.position.x + playerOffset.x;
        Vector2 targetPosition = new Vector2(targetX, spaceship.position.y);
        
        spaceship.position = Vector2.MoveTowards(
            spaceship.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Mathf.Abs(spaceship.position.x - targetX) < alignmentThreshold)
        {
            currentPhase = MovementPhase.MovingToPlayerY;
        }
    }

    private void MoveSpaceshipAlongYToPlayer()
    {
        Vector2 targetPosition = new Vector2(
            transform.position.x + playerOffset.x,
            transform.position.y + playerOffset.y
        );
        
        spaceship.position = Vector2.MoveTowards(
            spaceship.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(spaceship.position, targetPosition) < alignmentThreshold)
        {
            BoardSpaceship();
            currentPhase = MovementPhase.AscendingToOriginY;
        }
    }

    private void AscendToOriginalHeight()
    {
        Vector2 targetPosition = new Vector2(spaceship.position.x, originalPosition.y);
        
        spaceship.position = Vector2.MoveTowards(
            spaceship.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Mathf.Abs(spaceship.position.y - originalPosition.y) < alignmentThreshold)
        {
            currentPhase = MovementPhase.PlayerControlling;
        }
    }

    private void BoardSpaceship()
    {
        playerOnBoard = true;
        playerRb.simulated = false;
        transform.SetParent(spaceship);
        transform.localPosition = playerOffset;
        
        if (playerSprite != null)
        {
            playerSprite.enabled = false;
        }
        
        if (spaceshipAnimator != null)
        {
            spaceshipAnimator.SetBool(navigateParameter, false);
            wasMovingLastFrame = false;
        }
    }

    private void StartDroppingPlayer()
    {
        playerOnBoard = false;
        transform.SetParent(null);
        dropStartPosition = transform.position;
        currentPhase = MovementPhase.DroppingPlayer;
        
        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }
    }

    private void DropPlayer()
    {
        Vector2 targetPosition = new Vector2(transform.position.x, dropStartPosition.y - dropDistance);
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, targetPosition) < alignmentThreshold)
        {
            playerRb.simulated = true;
            currentPhase = MovementPhase.Idle;
            
            if (spaceshipRb != null)
            {
                spaceshipRb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void ControlSpaceshipMovement()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        Vector2 velocity = new Vector2(horizontalInput * flightSpeed, verticalInput * flightSpeed);
        
        Vector2 nextPosition = spaceshipRb.position + velocity * Time.deltaTime;
        Bounds bounds = confiner.bounds;
        
        if (nextPosition.x > bounds.min.x + boundaryPadding && 
            nextPosition.x < bounds.max.x - boundaryPadding &&
            nextPosition.y > bounds.min.y + boundaryPadding && 
            nextPosition.y < bounds.max.y - boundaryPadding)
        {
            spaceshipRb.linearVelocity = velocity;
        }
        else
        {
            spaceshipRb.linearVelocity = Vector2.zero;
        }
        
        transform.localPosition = playerOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (spaceship != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector2(
                transform.position.x + playerOffset.x,
                transform.position.y + playerOffset.y
            ), 0.3f);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(originalPosition, 0.3f);
            }
        }
    }
}
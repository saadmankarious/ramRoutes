using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using System.Collections;
using UnityEngine.Tilemaps;
using RamRoutes.Services;
using RamRoutes.Model;

namespace Platformer.Mechanics
{
    public class PlayerController : MonoBehaviour
    {
        // Singleton instance
        public static PlayerController Instance { get; private set; }

        
        // Footstep sounds (assign in Inspector)
        [Header("Footstep Sounds")]
        public AudioClip walkSound1;
        public AudioClip walkSound2;
        [SerializeField] private float stepInterval = 0.35f; // seconds between steps at default speed
        [SerializeField] private float stepVolume = 0.8f;
        private float stepTimer = 0f;
        private bool playFirstStep = true;

        // Movement variables
        public float moveSpeed = 7f;
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;
        private Vector2 moveInput;
        private Vector2 lastMoveDirection; // Store last movement direction for idle
        private Rigidbody2D rb;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        // readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();
        public Bounds Bounds => collider2d.bounds;

        // Trash interaction
        public Transform holdPoint;
        public float pickUpRange = 1.0f;
        public KeyCode interactKey = KeyCode.S;
        private GameObject heldTrash = null;

        // Mobile input variables
        private bool mobileLeftPressed = false;
        private bool mobileRightPressed = false;
        private bool mobileUpPressed = false;
        private bool mobileDownPressed = false;
        public bool mobileInteractPressed = false;
        public Tilemap paintedTilemap; // Assign in inspector - the tilemap with painted areas

        // Idle backflip functionality
        [Header("Idle Backflip")]
        [SerializeField] private float idleBackflipTime = 10f; // Time in seconds before backflip
        [SerializeField] private AudioClip backflipSound; // Optional backflip sound
        private float idleTimer = 0f;
        private bool isPerformingBackflip = false;

        // Add this function to your PlayerController class
        private bool IsSteppingOnPaintedTile()
        {
            if (paintedTilemap == null) return false;

            // Get player's position and movement direction
            Vector3 playerWorldPos = transform.position;
            playerWorldPos.z = 0f;
            Vector3Int cellPosition = paintedTilemap.WorldToCell(playerWorldPos);

            // Check current tile first (must always be valid)
            if (paintedTilemap.GetTile(cellPosition) == null)
                return false;

            // If not moving, only check current tile
            if (moveInput.magnitude < 0.1f)
                return true;

            // Predict next tile based on movement direction
            Vector3Int predictedCell = cellPosition;

            // Get primary movement direction (prioritize horizontal for 4-way movement)
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            {
                predictedCell.x += moveInput.x > 0 ? 1 : -1;
            }
            else if (moveInput.y != 0)
            {
                predictedCell.y += moveInput.y > 0 ? 1 : -1;
            }

            // Check if predicted tile is painted
            return paintedTilemap.GetTile(predictedCell) != null;
        }



        void Awake()
        {

            // Setup click handler — deferred to coroutine so User data can load
                if (GetComponent<RamClickHandler>() == null)
                {
                    gameObject.AddComponent<RamClickHandler>();
                    StartCoroutine(InitializePlayerClickHandler());
                }
            // Removed Box.OnBoxOpened subscription (legacy box feature)
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        private IEnumerator InitializePlayerClickHandler()
        {
            // Wait for Firebase auth and a RamsManager to be available
            yield return new WaitForSeconds(2f);

            var handler = GetComponent<RamClickHandler>();
            if (handler == null) yield break;

            var ramsManager = FindObjectOfType<RamsManager>();
            if (ramsManager == null)
            {
                Debug.LogWarning("PlayerController: No RamsManager found — click handler not initialized.");
                yield break;
            }

            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("PlayerController: No authenticated user — click handler not initialized.");
                yield break;
            }

            var userService = new UserService();
            var task = userService.RetrieveUserById(userId);
            while (!task.IsCompleted) yield return null;

            User user = task.Result;
            if (user != null)
            {
                handler.Initialize(user, ramsManager);
            }
        }

        void Update()
        {
            if (!controlEnabled)
            {
                moveInput = Vector2.zero;
                return;
            }

            // Handle interaction input (keyboard + mobile)
            bool interactInput = Input.GetKeyDown(interactKey) || mobileInteractPressed;
            if (interactInput)
            {
                if (heldTrash == null)
                {
                    TryPickUpTrash();
                }
                else
                {
                    DropTrash();
                }
                mobileInteractPressed = false;
            }

            // Get input from both keyboard and mobile
            float inputX = 0f;
            float inputY = 0f;

            // Keyboard input
            inputX = Input.GetAxisRaw("Horizontal");
            inputY = Input.GetAxisRaw("Vertical");

            // Override with mobile input if pressed
            if (mobileLeftPressed || mobileRightPressed || mobileUpPressed || mobileDownPressed)
            {
                inputX = 0f;
                inputY = 0f;

                if (mobileLeftPressed) inputX = -1f;
                if (mobileRightPressed) inputX = 1f;
                if (mobileDownPressed) inputY = -1f;
                if (mobileUpPressed) inputY = 1f;
            }

            // Prevent diagonal movement by prioritizing the dominant axis
            if (Mathf.Abs(inputX) > Mathf.Abs(inputY))
            {
                inputY = 0f; // Only horizontal movement
            }
            else if (Mathf.Abs(inputY) > Mathf.Abs(inputX))
            {
                inputX = 0f; // Only vertical movement
            }
            else
            {
                // If equal input magnitude (like both at 1), default to horizontal movement
                if (inputX != 0f || inputY != 0f)
                {
                    inputY = 0f;
                }
            }            moveInput = new Vector2(inputX, inputY).normalized;

            // Update animation parameters
            if (moveInput.magnitude > 0.01f)
            {
                // Reset idle timer when moving
                idleTimer = 0f;
                
                // Store direction when moving
                lastMoveDirection = moveInput;
                // Set movement animation parameters
                animator.SetFloat("MoveX", moveInput.x);
                animator.SetFloat("MoveY", moveInput.y);
                animator.SetBool("idle", false);
            }
            else
            {
                // Player is idle - update idle timer
                if (!isPerformingBackflip)
                {
                    idleTimer += Time.deltaTime;
                    
                    // Check if idle time exceeded and trigger backflip
                    if (idleTimer >= idleBackflipTime)
                    {
                        StartCoroutine(PerformIdleBackflip());
                        idleTimer = 0f; // Reset timer
                    }
                }
                
                // Use last direction for idle animation
                animator.SetFloat("MoveX", lastMoveDirection.x);
                animator.SetFloat("MoveY", lastMoveDirection.y);
                animator.SetBool("idle", true);
            }

            // Handle held trash position
            if (heldTrash != null)
            {
                heldTrash.transform.position = holdPoint.position;
            }

            // Update last move direction
            if (moveInput.magnitude > 0.1f)
            {
                lastMoveDirection = moveInput;
            }

            // Handle walking sounds (alternate two steps)
            HandleFootsteps();
        }

        // Play alternating footstep sounds when walking
        private void HandleFootsteps()
        {
            // Only play when actually moving and allowed on painted tiles
            bool isWalking = moveInput.magnitude > 0.1f && IsSteppingOnPaintedTile();

            if (!isWalking)
            {
                // Reset timer when not walking so first step triggers quickly when moving again
                stepTimer = 0f;
                return;
            }

            // Optionally scale step interval by speed (faster speed -> faster steps)
            float speedFactor = Mathf.Max(0.1f, moveSpeed / 7f); // 7f is the default speed in this script
            float effectiveInterval = stepInterval / speedFactor;

            stepTimer += Time.deltaTime;
            if (stepTimer >= effectiveInterval)
            {
                stepTimer = 0f;

                var clip = playFirstStep ? walkSound1 : walkSound2;
                if (clip != null && audioSource != null)
                {
                    audioSource.PlayOneShot(clip, stepVolume);
                }
                playFirstStep = !playFirstStep;
            }
        }

        [SerializeField] private float offTileGracePeriod = 0.5f; // Time allowed off tiles before stopping
        private float timeOffTile = 0f;
        private bool wasOnTileLastFrame = true;

        void FixedUpdate()
        {
            bool isOnTile = IsSteppingOnPaintedTile();

            if (isOnTile)
            {
                rb.linearVelocity = moveInput * moveSpeed;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        // Mobile input methods
        public void OnMobileLeftPressed() { mobileLeftPressed = true; }
        public void OnMobileLeftReleased() { mobileLeftPressed = false; }
        public void OnMobileRightPressed() { mobileRightPressed = true; }
        public void OnMobileRightReleased() { mobileRightPressed = false; }
        public void OnMobileUpPressed() { mobileUpPressed = true; }
        public void OnMobileUpReleased() { mobileUpPressed = false; }
        public void OnMobileDownPressed() { mobileDownPressed = true; }
        public void OnMobileDownReleased() { mobileDownPressed = false; }
        public void OnMobileInteractPressed() { mobileInteractPressed = true; }

        void TryPickUpTrash()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickUpRange);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Trash") || hit.CompareTag("Recyclable") ||
                   hit.CompareTag("Sapling") || hit.CompareTag("Eagle"))
                {
                    TrashItem trashComponent = hit.GetComponent<TrashItem>();
                    if (trashComponent != null)
                    {
                        string itemName = trashComponent.name;
                        Debug.Log($"Picked up: {itemName}");
                        heldTrash = hit.gameObject;
                        heldTrash.GetComponent<Collider2D>().enabled = false;
                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.heldItem.text = itemName;
                        }
                        else
                        {
                            Debug.LogWarning("UIManager instance not found");
                        }
                        return;
                    }
                    else
                    {
                        Debug.LogWarning("Picked up object has Trash tag but no TrashItem component", hit.gameObject);
                    }
                }
            }
        }

        void DropTrash()
        {
            if (heldTrash != null)
            {
                float direction = spriteRenderer.flipX ? -1f : 1f;
                Vector2 dropOffset = new Vector2(direction * 0.6f, 0f);
                Vector2 dropPosition = (Vector2)transform.position + dropOffset;
                heldTrash.transform.position = dropPosition;
                heldTrash.GetComponent<Collider2D>().enabled = true;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.heldItem.text = "";
                }
                else
                {
                    Debug.LogWarning("UIManager instance not found when trying to clear held item text");
                }
                heldTrash = null;
            }
        }

        /// <summary>
        /// Coroutine to perform backflip when player has been idle for too long
        /// </summary>
        private IEnumerator PerformIdleBackflip()
        {
            isPerformingBackflip = true;
            
            // Trigger backflip animation
            if (animator != null)
            {
                animator.SetBool("backflip", true);
                Debug.Log("Player performing idle backflip!");
                
                // Play backflip sound if available
                if (backflipSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(backflipSound, 0.7f);
                }
            }
            
            // Wait for backflip animation duration (adjust based on animation length)
            yield return new WaitForSeconds(0.5f);

            // Reset backflip animation state
            if (animator != null)
            {
                animator.SetBool("backflip", false);
                
            }
            
            isPerformingBackflip = false;
            Debug.Log("Player idle backflip completed");
        }
    }
}
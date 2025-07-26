using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cinemachine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Platformer.Mechanics
{
    public class PlayerController : KinematicObject
    {
        // Singleton instance
        public static PlayerController Instance { get; private set; }

        // Audio clips
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        // Server variables
        private TcpListener server;
        private Thread serverThread;
        private bool isRunning = true;
        public int port = 5005;

        // Movement variables
        public float maxSpeed = 7;
        public float jumpTakeOffSpeed = 7;
        public JumpState jumpState = JumpState.Grounded;
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;
        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();
        public Bounds Bounds => collider2d.bounds;

        // Trash interaction
        public Transform holdPoint;
        public float pickUpRange = 1.0f;
        public KeyCode interactKey = KeyCode.S;
        private GameObject heldTrash = null;
        private bool pickupCommandFromServer = false;
        private bool moveCommandFromServer = false;
        private float moveDirectionFromServer = 0;

        // Camera boundary
        private CinemachineConfiner confiner;
        private Collider2D boundary;

        protected override void Start()
        {
            confiner = FindObjectOfType<CinemachineVirtualCamera>().GetComponent<CinemachineConfiner>();
            if (confiner == null)
            {
                Debug.LogError("No CinemachineConfiner found on virtual camera!");
            }
            boundary = confiner?.m_BoundingShape2D;
        }

        void Awake()
        {
            Box.OnBoxOpened += HandleBoxOpened;
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        void HandleServerInput(string input)
        {
            string[] parts = input.Split(':');
            if (parts.Length == 2)
            {
                string command = parts[0].ToUpper();
                string value = parts[1];
                switch (command)
                {
                    case "MOVE":
                        if (float.TryParse(value, out float direction))
                        {
                            moveDirectionFromServer = direction;
                            moveCommandFromServer = true;
                        }
                        break;
                    case "JUMP":
                        jump = true;
                        break;
                    case "PICKUP":
                        pickupCommandFromServer = true;
                        break;
                    case "DROP":
                        DropTrash();
                        break;
                    default:
                        Debug.Log($"Unknown command: {command}");
                        break;
                }
            }
        }

        protected override void Update()
        {
            // Handle pickup from keyboard or server
            if (Input.GetKeyDown(interactKey) || pickupCommandFromServer)
            {
                if (heldTrash == null)
                {
                    TryPickUpTrash();
                }
                else
                {
                    DropTrash();
                }
                pickupCommandFromServer = false;
            }

            // Handle movement from keyboard or server
            if (controlEnabled)
            {
                float inputX = Input.GetAxis("Horizontal") != 0 ? Input.GetAxis("Horizontal") : moveDirectionFromServer;
                Vector2 movement = new Vector2(inputX * maxSpeed * Time.deltaTime, 0);
                if (boundary != null)
                {
                    Vector2 newPosition = (Vector2)transform.position + movement;
                    if (boundary.OverlapPoint(newPosition))
                    {
                        move.x = inputX;
                    }
                    else
                    {
                        Vector2 edgePosition = boundary.ClosestPoint(newPosition);
                        if (Mathf.Abs(edgePosition.x - transform.position.x) > 0.05f)
                        {
                            move.x = inputX * 0.3f;
                        }
                        else
                        {
                            move.x = 0;
                        }
                    }
                }
                else
                {
                    move.x = inputX;
                }
                // Jump handling - instant jump
                if (jumpState == JumpState.Grounded && (Input.GetButtonDown("Jump") || jump))
                {
                    jumpState = JumpState.PrepareToJump;
                    Debug.Log("jumping rn");
                }
            }
            else
            {
                move.x = 0;
            }

            // Held trash position
            if (heldTrash != null)
            {
                heldTrash.transform.position = holdPoint.position;
            }

            UpdateJumpState();
            base.Update();
        }

        void TryPickUpTrash()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickUpRange);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Trash") || hit.CompareTag("Recyclable") ||
                   hit.CompareTag("Sapling") || hit.CompareTag("Eagle") || hit.CompareTag("Box"))
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
                float direction = transform.localScale.x;
                Vector2 dropOffset = new Vector2(direction * .6f, 1.0f);
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

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;
            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);
            targetVelocity = move * maxSpeed;
        }

        void OnDestroy()
        {
            isRunning = false;
            server?.Stop();
            serverThread?.Abort();
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }

        void HandleBoxOpened()
        {
            Debug.Log("The box was opened! Let's do something!");
            Schedule<PlayerDeath>();
            StartCoroutine(LeaveAfterDelay());
        }

        private IEnumerator LeaveAfterDelay()
        {
            yield return new WaitForSeconds(4f);
            PlayerPrefs.DeleteAll();
            foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>())
            {
                if (obj.scene.name == null) Destroy(obj);
            }
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
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

        /// Max horizontal speed of the player.
        public float maxSpeed = 7;
        /// Initial jump velocity at the start of a jump.
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;

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

        public Transform holdPoint;
        public float pickUpRange = 1.0f;
        public KeyCode interactKey = KeyCode.C;

        private GameObject heldTrash = null;
        private bool pickupCommandFromServer = false;
        private bool moveCommandFromServer = false;
        private float moveDirectionFromServer = 0;

        void Awake()
        {
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

            StartServer();
        }

        void StartServer()
        {
            serverThread = new Thread(ServerLoop);
            serverThread.IsBackground = true;
            serverThread.Start();
            Debug.Log($"Server started on port {port}");
        }

        void ServerLoop()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();

                while (isRunning)
                {
                    using (TcpClient client = server.AcceptTcpClient())
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                        if (!string.IsNullOrEmpty(message))
                        {
                            Debug.Log($"Received: {message}");
                            HandleServerInput(message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Server Error: {e.Message}");
            }
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

        void Update()
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
                pickupCommandFromServer = false; // Reset flag after handling
            }

            // Handle movement from keyboard or server
            if (controlEnabled)
            {
                move.x = Input.GetAxis("Horizontal") != 0 ? Input.GetAxis("Horizontal") : moveDirectionFromServer;
                if (jumpState == JumpState.Grounded && (Input.GetButtonDown("Jump") || jump))
                    jumpState = JumpState.PrepareToJump;
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }

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
                if (hit.CompareTag("Trash") || hit.CompareTag("Recyclable") || hit.CompareTag("Sapling"))
                {
                    heldTrash = hit.gameObject;
                    heldTrash.GetComponent<Collider2D>().enabled = false;
                    Debug.Log("Picked up " + heldTrash.name);
                    return;
                }
            }
        }

        void DropTrash()
        {
            if (heldTrash != null)
            {
                Vector2 dropOffset = new Vector2(0, 1.0f);
                Vector2 dropPosition = (Vector2)transform.position + dropOffset;

                heldTrash.transform.position = dropPosition;
                heldTrash.GetComponent<Collider2D>().enabled = true;

                Debug.Log("Dropped the trash at " + dropPosition);
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
                    stopJump = false;
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
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
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
    }
}

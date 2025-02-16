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
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;
        private UdpClient udpClient;
        private Thread receiveThread;
        private bool isRunning = true;
        private ButtonState buttonState;

        public int port = 5005; // Match this with your Python script

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        /*internal new*/
        public Collider2D collider2d;
        /*internal new*/
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;

        public Transform holdPoint; // The point where the trash will be held (attach an empty GameObject to player)
        public float pickUpRange = 1.0f; // How close player must be to pick up trash
        public KeyCode interactKey = KeyCode.C; // Key to pick up and drop trash

        private GameObject heldTrash = null; // Reference to the trash object the player is holding

        void Start()
        {
            StartUDPServer();
        }


        void OnApplicationQuit()
        {
            StopUDPServer();
        }

        private void StartUDPServer()
        {
            udpClient = new UdpClient(port);
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log("UDP server started on port " + port);
        }

        private void StopUDPServer()
        {
            isRunning = false;

            if (udpClient != null)
            {
                udpClient.Close();
            }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join();
            }
        }

        private void ReceiveData()
        {
            try
            {
                while (isRunning)
                {
                    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
                    byte[] data = udpClient.Receive(ref remoteEndPoint);

                    // Convert byte data to string
                    string message = Encoding.UTF8.GetString(data);
                    Debug.Log("Received message: " + message);

                    // Process the message (if needed)
                    ProcessMessage(message);
                }
            }
            catch (SocketException e)
            {
                if (isRunning)
                {
                    Debug.LogError("Socket exception: " + e.Message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Exception: " + e.Message);
            }
        }

        private void ProcessMessage(string message)
        {
            if(message == "jump")
            {
                buttonState = ButtonState.Jump;
            }
            else if(message == "stop-jump")
            {
                buttonState = ButtonState.None;
            }
            // Add your message handling logic here
        }

      void TryPickUpTrash()
        {
            // Detect trash in the player's range using Physics2D.OverlapCircle
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickUpRange);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Trash") || hit.CompareTag("Recyclable"))
                {
                    heldTrash = hit.gameObject;
                    heldTrash.GetComponent<Collider2D>().enabled = false; // Disable collisions
                    Debug.Log("Picked up " + heldTrash.name);
                    return;
                }
            }
        }


        void DropTrash()
        {
            // Drop the trash at the player's position
            if (heldTrash != null)
            {
                heldTrash.GetComponent<Collider2D>().enabled = true; // Re-enable collisions
                heldTrash = null;
                Debug.Log("Dropped the trash");
            }
        }


        // Visualize the pick-up range in the Scene view
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickUpRange);
        }

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        protected override void Update()
        {
            // Check if player presses the interact key
            if (Input.GetKeyDown(interactKey))
            {
                if (heldTrash == null)
                {
                    // Try to pick up trash
                    TryPickUpTrash();
                }
                else
                {
                    // Drop the trash if already holding it
                    DropTrash();
                }
            }

            // If player is holding trash, keep it at the hold position
            if (heldTrash != null)
            {
                heldTrash.transform.position = holdPoint.position;
            }

            if (controlEnabled)
            {
                move.x = Input.GetAxis("Horizontal");
                if (jumpState == JumpState.Grounded && buttonState == ButtonState.Jump )
                {
                    jumpState = JumpState.PrepareToJump;                
                }
                else if (buttonState == ButtonState.None)
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
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

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
        public enum ButtonState
        {
            None,
            Jump
        }
    }
}
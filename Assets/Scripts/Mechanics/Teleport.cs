using UnityEngine;
using Cinemachine;
using System.Collections;
using UnityEngine.UI; // Required for UI manipulation

public class Teleport : MonoBehaviour
{
    public Transform targetLocation; // The location to teleport to
    public ParticleSystem teleportEffect; // Particle effect for teleportation
    public AudioClip teleportSound; // Sound effect for teleportation
    private KeyCode teleportButton = KeyCode.T; // The button to press for teleportation
    public GameObject player; // Reference to the player GameObject
    public float teleportDelay = 1f; // Delay before teleporting (in seconds)
    public CinemachineImpulseSource impulseSource; // Reference to the Cinemachine Impulse Source
    public SwitchConfiner switchConfiner;
    public GameObject dialogPanel; // The panel where dialog is displayed
    public Text dialogText; // The text field to show the dialog
    [Range(0f, 1f)] public float inactiveAlpha = 0.5f; // Transparency level when inactive

    private enum Stations {Io, Calliston, Ganymede, Europa}; 
    [SerializeField] private Stations teleportStation;
    private Stations? currentCollidedStation;

    public bool active = false; // Initialize as inactive
    private SpriteRenderer spriteRenderer;
    private Color activeColor;
    private Color inactiveColor;

    private void Start()
    {
        // Get the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            activeColor = spriteRenderer.color;
            inactiveColor = new Color(activeColor.r, activeColor.g, activeColor.b, inactiveAlpha);
            UpdateVisualState();
        }
    }

    private void Update()
    {
        // Check if the teleport button is pressed
        if (Input.GetKeyDown(teleportButton) && currentCollidedStation != null)
        {
            if (active)
            {
                StartCoroutine(TeleportWithDelay()); // Start the teleportation process with a delay
            }
            else
            {
                ShowDialog("This teleport station is currently inactive!", player.GetComponent<Collider2D>());
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            Debug.Log("Player collided with " + teleportStation);
            currentCollidedStation = teleportStation;
            string statusMessage = active ? 
                "You are now on " + teleportStation + ". Hit T to time travel!" :
                "This teleport station is currently inactive.";
            ShowDialog(statusMessage, other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            Debug.Log("Player leaving " + teleportStation);
            currentCollidedStation = null;
            HideDialog();
        }
    }

    private IEnumerator TeleportWithDelay()
    {
        Debug.Log("teleporting to location " + targetLocation);
        if (player == null)
        {
            Debug.LogError("Player reference is not set!");
            yield break;
        }

        int switchIndex = GetNextConfinerIndex();
       
        switchConfiner.SwitchToConfiner(switchIndex);

        // Play teleport effect (particles) at the current location
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, player.transform.position, Quaternion.identity);
        }

        // Play teleport sound
        if (teleportSound != null)
        {
            AudioSource.PlayClipAtPoint(teleportSound, player.transform.position);
        }

        // Shake the camera using Cinemachine Impulse
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
            Debug.Log("generated impulse");
        }

        // Wait for the specified delay
        yield return new WaitForSeconds(teleportDelay);

        // Teleport the player to the target location
        player.transform.position = targetLocation.position;
        Debug.Log("teleported to location " + targetLocation.position);
        // Play teleport effect at the new location
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, targetLocation.position, Quaternion.identity);
        }
    }

    private int GetNextConfinerIndex()
    {
        int switchIndex = 0;
        switch (teleportStation)
        {
            case Stations.Io:
                switchIndex = 0;
                break;
            case Stations.Europa:
                switchIndex = 1;
                break;
            case Stations.Ganymede:
                switchIndex = 2;
                break;
            case Stations.Calliston:
                switchIndex = 3;
                break;
        }
        return switchIndex;
    }

    void ShowDialog(string message, Collider2D other)
    {
        if (dialogPanel != null && dialogText != null)
        {
            dialogText.text = message;
            dialogPanel.SetActive(true); // Show the dialog panel
            StartCoroutine(HideDialogAfterDelay());
        }
    }

    void HideDialog()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false); // Hide the dialog panel
        }
    }

    IEnumerator HideDialogAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        HideDialog();
    }

    // Update the visual state based on active status
    private void UpdateVisualState()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = active ? activeColor : inactiveColor;
        }
    }

    // Public method to change active state
    public void SetActiveState(bool isActive)
    {
        active = isActive;
        UpdateVisualState();
    }
}
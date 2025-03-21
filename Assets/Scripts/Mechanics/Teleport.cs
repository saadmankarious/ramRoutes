using UnityEngine;
using Cinemachine;
using System.Collections;

public class Teleport : MonoBehaviour
{
    public Transform targetLocation; // The location to teleport to
    public ParticleSystem teleportEffect; // Particle effect for teleportation
    public AudioClip teleportSound; // Sound effect for teleportation
    public KeyCode teleportButton = KeyCode.T; // The button to press for teleportation
    public GameObject player; // Reference to the player GameObject
    public float teleportDelay = 1f; // Delay before teleporting (in seconds)
    public CinemachineImpulseSource impulseSource; // Reference to the Cinemachine Impulse Source
    public SwitchConfiner switchConfiner;

    private void Update()
    {
        // Check if the teleport button is pressed
        if (Input.GetKeyDown(teleportButton))
        {
            StartCoroutine(TeleportWithDelay()); // Start the teleportation process with a delay
        }
    }

    private IEnumerator TeleportWithDelay()
    {
        if (player == null)
        {
            Debug.LogError("Player reference is not set!");
            yield break;
        }

        switchConfiner.SwitchToNextConfiner();

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

        // Play teleport effect at the new location
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, targetLocation.position, Quaternion.identity);
        }
    }
}
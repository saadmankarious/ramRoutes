using UnityEngine;

public class Teleport : MonoBehaviour
{
    public Transform targetLocation; // The location to teleport to
    public ParticleSystem teleportEffect; // Particle effect for teleportation
    public AudioClip teleportSound; // Sound effect for teleportation
    public KeyCode teleportButton = KeyCode.T; // The button to press for teleportation
    public GameObject player; // Reference to the player GameObject

    private void Update()
    {
        // Check if the teleport button is pressed
        if (Input.GetKeyDown(teleportButton))
        {
            TeleportPlayer(player); // Teleport the player
        }
    }

    private void TeleportPlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("Player reference is not set!");
            return;
        }

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

        // Teleport the player to the target location
        player.transform.position = targetLocation.position;

        // Play teleport effect at the new location
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, targetLocation.position, Quaternion.identity);
        }
    }
}
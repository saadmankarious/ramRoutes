using System.Collections;
using UnityEngine;

public class TrashCan : MonoBehaviour
{
    public int trashCount = 0; // Count of how many trash pieces have been dropped
    public GameObject trashCanGuard; // The hidden object that becomes visible
    public float guardMoveUpDistance = 0.5f; // How much the guard moves up
    public float guardMoveSpeed = 2f; // How fast the guard moves up

    private Vector3 guardOriginalPosition; // Store original position for reset

    void Start()
    {
        if (trashCanGuard != null)
        {
            // Store the original position of the guard
            guardOriginalPosition = trashCanGuard.transform.position;
            trashCanGuard.SetActive(false); // Hide guard at start
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trash"))
        {
            trashCount++;
            Debug.Log("Trash added to trash can. Total: " + trashCount);

            // Activate and move the trash can guard
            if (trashCanGuard != null)
            {
                trashCanGuard.SetActive(true); // Make the guard visible
                StartCoroutine(MoveGuardUpAndDown()); // Smoothly move it up and down
            }

            Destroy(other.gameObject); // Destroy the trash object
        }
    }

    IEnumerator MoveGuardUpAndDown()
    {
        // Move the guard up
        Vector3 targetPosition = guardOriginalPosition + new Vector3(0, guardMoveUpDistance, 0);
        while (Vector3.Distance(trashCanGuard.transform.position, targetPosition) > 0.01f)
        {
            trashCanGuard.transform.position = Vector3.MoveTowards(
                trashCanGuard.transform.position, 
                targetPosition, 
                guardMoveSpeed * Time.deltaTime
            );
            yield return null; // Wait for the next frame
        }

        // Wait for 2 seconds
        yield return new WaitForSeconds(2f);

        // Move the guard back to the original position
        while (Vector3.Distance(trashCanGuard.transform.position, guardOriginalPosition) > 0.01f)
        {
            trashCanGuard.transform.position = Vector3.MoveTowards(
                trashCanGuard.transform.position, 
                guardOriginalPosition, 
                guardMoveSpeed * Time.deltaTime
            );
            yield return null; // Wait for the next frame
        }

        // Optional: Hide the guard after it returns to its original position
        trashCanGuard.SetActive(false); 
    }
}

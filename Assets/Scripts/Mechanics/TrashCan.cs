using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Required for UI manipulation

public class TrashCan : MonoBehaviour
{
    public int trashCount = 0; // Count of how many trash pieces have been dropped
    public int recyclableCount = 0; // Count of how many trash pieces have been dropped
    public int treePlantedCount = 0; // Count of how many trash pieces have been dropped
    public Animator animator;
    private bool planted;

    public GameObject trashCanGuard; // The hidden object that becomes visible
    public float guardMoveUpDistance = 0.5f; // How much the guard moves up
    private float guardMoveSpeed = 2f; // How fast the guard moves up

    public GameObject dialogPanel; // The panel where dialog is displayed
    public Text dialogText; // The text field to show the dialog

    public float dialogDisplayTime = 2f; // How long the dialog stays visible
    public bool isRecycling = false;
    private Vector3 guardOriginalPosition; // Store original position for reset

    void Start()
    {
        if (trashCanGuard != null)
        {
            // Store the original position of the guard
            guardOriginalPosition = trashCanGuard.transform.position;
            trashCanGuard.SetActive(false); // Hide guard at start
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false); // Hide dialog at start
        }
    
        // Initialize the Animator component attached to the same object
        animator = GetComponent<Animator>();
    
}

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trash"))
        {
            if(isRecycling)
            {
                ShowDialog("Wrong place.", other);
            }else if(trashCount < 10)
            {
                trashCount++;
                Destroy(other.gameObject);
                GameManager.Instance.AddTrash(1);
                ShowDialog("Good job!", other);
            }
            else if (recyclableCount == 10)
            {
                //celebrate completing trash
                 trashCount++;
                Destroy(other.gameObject);
                GameManager.Instance.AddTrash(1);
                ShowDialog("Good job! Now you've collected all needed trash", other);
            }
            else{
                ShowDialog("You collected enough trash!", other);
            }
        }
        
        if (other.CompareTag("Recyclable"))
        {
            if(!isRecycling)
            {
                ShowDialog("Wrong place.", other);
            }
            else if(recyclableCount < 10)
            {
                recyclableCount++;
                Destroy(other.gameObject);
                GameManager.Instance.AddBottles(1);
                ShowDialog("Good job!", other);
            }
            else if(recyclableCount == 10)
            {
                //celebrate completing recycling
                recyclableCount++;
                Destroy(other.gameObject);
                GameManager.Instance.AddBottles(1);
                ShowDialog("Good job! Now you've collected all needed recycled items.", other);
            }        
        }

        if (other.CompareTag("Sapling"))
        {
            if (animator != null && !planted)
            { 
                treePlantedCount++;
                Destroy(other.gameObject);
                Debug.Log("trees planted " + treePlantedCount);
                animator.SetTrigger("plant");
                planted = true;
                ShowDialog("Good job!", other);
                GameManager.Instance.PlantTree();

            }
            else 
            {
                ShowDialog("Cannot plant tree here.", other);
            }

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

    void ShowDialog(string message, Collider2D other)
    {
        if (dialogPanel != null && dialogText != null)
        {
            dialogText.text = message;
            dialogPanel.SetActive(true); // Show the dialog panel
            StopAllCoroutines(); // Stop any previous dialog hiding coroutine
            StartCoroutine(HideDialogAfterDelay());
        }
        if (trashCanGuard != null)
        {
            trashCanGuard.SetActive(true); // Make the guard visible
            StartCoroutine(MoveGuardUpAndDown()); // Smoothly move it up and down
        }

    }


    IEnumerator HideDialogAfterDelay()
    {
        yield return new WaitForSeconds(dialogDisplayTime);
        dialogPanel.SetActive(false); // Hide the dialog after the delay
    }
}

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
    public Text trashCountText; // The text field to show the dialog
    public Text bottleCountText; // The text field to show the dialog

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
            }else{
                trashCount++;
                Destroy(other.gameObject);
                Debug.Log("trash count " + trashCount);
                updateCollectableCounts(trashCount, -1);
            }
        }
        
        if (other.CompareTag("Recyclable"))
        {
            if(isRecycling)
            {
               recyclableCount++;
                Destroy(other.gameObject);
                Debug.Log("bottle count " + recyclableCount);
                updateCollectableCounts(-1, recyclableCount);

            }  
            else{
                ShowDialog("Wrong place.", other);

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

            }
            else 
            {
                ShowDialog("Cannot plant tree here.", other);
            }

        }
    }

    void updateCollectableCounts(int trashCount_, int recyclableCount_ ){
        if(trashCount_ != -1)
        {
            trashCountText.text = trashCount + "/12";
        }
        if(recyclableCount_ != -1)
        {
            bottleCountText.text = recyclableCount + "/12";
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

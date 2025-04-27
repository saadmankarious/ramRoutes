using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TrashCan : MonoBehaviour
{
    public int trashCount = 0;
    public int recyclableCount = 0;
    public int treePlantedCount = 0;
    public Animator animator;
    private bool planted;

    public GameObject trashCanGuard;
    public float guardMoveUpDistance = 0.5f;
    private float guardMoveSpeed = 2f;

    public GameObject dialogPanel;
    public Text dialogText;
    public float dialogDisplayTime = 2f;
    public bool isRecycling = false;
    private Vector3 guardOriginalPosition;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip wrongPlaceClip;
    public AudioClip completionClip;
    [SerializeField] private float trashSoundVolume = 0.3f;

    void Start()
    {
        if (trashCanGuard != null)
        {
            guardOriginalPosition = trashCanGuard.transform.position;
            trashCanGuard.SetActive(false);
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    
        animator = GetComponent<Animator>();

        // Ensure we have an AudioSource component
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private bool playerInBound = false;

    void OnTriggerExit2D(Collider2D other)
    {
         if(other.CompareTag("Player"))
        {
            Debug.Log("Player out of bounds");
            playerInBound = false;
        }  
    }

    void OnTriggerEnter2D(Collider2D other)
    {
       
        if(other.CompareTag("Player"))
        {
            Debug.Log("Player in bounds");
            playerInBound = true;
        }
        if(!playerInBound)
            {
                return;
            }
        if (other.CompareTag("Trash"))
        {
           
            if(isRecycling)
            {
                PlaySound(wrongPlaceClip);
                ShowDialog("Wrong place.", other);
            } else if (GameManager.Instance.currentTrial.currentTrash >= 9)
            {
                PlaySound(completionClip);
                ShowDialog("All trash collected. Now collect and deposit all recycle items.", other);
                  trashCount++;
                Destroy(other.gameObject);
                GameManager.Instance.AddTrash(1);
            }
            
            else if(trashCount < 4)
            {
                trashCount++;
                Destroy(other.gameObject);
                GameManager.Instance.AddTrash(1);
                PlaySound(successClip);
                ShowDialog("Good job!", other);
            }
           
            else
            {
                ShowDialog("This bin is full. Use another one to deposit the remaining items.", other);
            }
        }
        
        if (other.CompareTag("Recyclable"))
        {
            if(!isRecycling)
            {
                PlaySound(wrongPlaceClip);
                ShowDialog("Wrong place.", other);
            }
            else if(recyclableCount < 4)
            {
                recyclableCount++;
                Destroy(other.gameObject);
                GameManager.Instance.AddBottles(1);
                PlaySound(successClip);
                ShowDialog("Good job!", other);
            }
            else if(recyclableCount == 10)
            {
                recyclableCount++;
                Destroy(other.gameObject);
                GameManager.Instance.AddBottles(1);
                PlaySound(completionClip);
                ShowDialog("Good job! Now you've collected all needed recycled items.", other);
            }
            else
            {
                                ShowDialog("This bin is full. Use another one to deposit the remaining items.", other);

            }       
        }

        if (other.CompareTag("Sapling"))
        {
            if (animator != null && !planted)
            { 
                treePlantedCount++;
                Destroy(other.gameObject);
                animator.SetTrigger("plant");
                planted = true;
                PlaySound(successClip);
                ShowDialog("Good job!", other);
                GameManager.Instance.PlantTree();
            }
            else 
            {
                PlaySound(wrongPlaceClip);
                ShowDialog("Cannot plant tree here.", other);
            }
        }

        if(other.CompareTag("Spaceship") && GameManager.Instance.currentTrial.trialNumber == 3)
        {
            ShowDialog("Hit 'C' to water tree.", other);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, trashSoundVolume);
        }
    }

    IEnumerator MoveGuardUpAndDown()
    {
        Vector3 targetPosition = guardOriginalPosition + new Vector3(0, guardMoveUpDistance, 0);
        while (Vector3.Distance(trashCanGuard.transform.position, targetPosition) > 0.01f)
        {
            trashCanGuard.transform.position = Vector3.MoveTowards(
                trashCanGuard.transform.position,
                targetPosition,
                guardMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        yield return new WaitForSeconds(2f);

        while (Vector3.Distance(trashCanGuard.transform.position, guardOriginalPosition) > 0.01f)
        {
            trashCanGuard.transform.position = Vector3.MoveTowards(
                trashCanGuard.transform.position,
                guardOriginalPosition,
                guardMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        trashCanGuard.SetActive(false);
    }

    void ShowDialog(string message, Collider2D other)
    {
        if (dialogPanel != null && dialogText != null)
        {
            dialogText.text = message;
            dialogPanel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(HideDialogAfterDelay());
        }
        if (trashCanGuard != null)
        {
            trashCanGuard.SetActive(true);
            StartCoroutine(MoveGuardUpAndDown());
        }
    }

    IEnumerator HideDialogAfterDelay()
    {
        yield return new WaitForSeconds(dialogDisplayTime);
        dialogPanel.SetActive(false);
    }
}
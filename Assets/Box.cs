using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Box : MonoBehaviour
{
    private Animator animator;
   public GameObject panelToShow;
 public Button yesButton;
    public Button noButton;

    public UnityEvent onGameEnd; // Event to be called when game should end

    void Start()
    {
        animator = GetComponent<Animator>();

        if (panelToShow != null)
        {
            panelToShow.SetActive(false);
        }

        // Setup button listeners
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoClicked);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pluto"))
        {
            animator.SetTrigger("boxOpen");
            StartCoroutine(ShowPanelAfterAnimation());
        }
    }

    IEnumerator ShowPanelAfterAnimation()
    {
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }
    }

    private void OnYesClicked()
    {
        // Immediately end the game when Yes is clicked
        EndGame();
    }

    private void OnNoClicked()
    {
        // Wait 2 seconds then end the game when No is clicked
        StartCoroutine(EndGameAfterDelay(2f));
    }

    private void EndGame()
    {
        onGameEnd.Invoke();

        // Optional: Hide the panel when game ends
        if (panelToShow != null)
        {
            panelToShow.SetActive(false);
        }
    }

    private IEnumerator EndGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndGame();
    }

    // Clean up listeners when destroyed
    private void OnDestroy()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(OnNoClicked);
        }
    }
}
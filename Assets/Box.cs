using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;

public class Box : MonoBehaviour
{
    private Animator animator;
   public GameObject panelToShow;
 public Button yesButton;
    public Button noButton;
    public static event Action OnBoxOpened;  // Static makes it accessible globally (optional)

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
         yield return new WaitForSeconds(3);
        panelToShow.SetActive(true); // replace with your actual panel referen
        Debug.Log("panel to show "+ panelToShow);
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
        if (panelToShow != null)
        {
            panelToShow.SetActive(false);
        }
        
        // animator.SetTrigger("ramDeath")
        OnBoxOpened?.Invoke();  // 🚀 Fire the event!


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
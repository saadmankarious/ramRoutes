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

        // // Setup button listeners
        // if (yesButton != null)
        // {
        //     yesButton.onClick.AddListener(OnYesClicked);
        // }

        // if (noButton != null)
        // {
        //     noButton.onClick.AddListener(OnNoClicked);
        // }
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

    public void OnYesClicked()
    {
        // Immediately end the game when Yes is clicked
        if (panelToShow != null)
        {
            panelToShow.SetActive(false);
        }
        StartCoroutine(EndGameAfterDelayYes());
    }

    public void OnNoClicked()
    {
        Debug.Log("no clicked");
        if (panelToShow != null)
        {
            panelToShow.SetActive(false);
        }
        // Wait 2 seconds then end the game when No is clicked
        StartCoroutine(EndGameAfterDelayNo());
    }

    private void EndGame()
    {
      
        
        // animator.SetTrigger("ramDeath")
        OnBoxOpened?.Invoke();  // 🚀 Fire the event!


    }

    private IEnumerator EndGameAfterDelayNo()
    {
        UIManager.Instance.endGamePanelNo.SetActive(true);
        yield return new WaitForSeconds(2f);
        UIManager.Instance.endGamePanelNo.SetActive(false);
        yield return new WaitForSeconds(2f);

        EndGame();
    }
    private IEnumerator EndGameAfterDelayYes()
    {
        UIManager.Instance.endGamePanelYes.SetActive(true);
        yield return new WaitForSeconds(2f);
        UIManager.Instance.endGamePanelYes.SetActive(false);
        yield return new WaitForSeconds(2f);

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
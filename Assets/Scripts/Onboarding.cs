using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class OnboardingManager : MonoBehaviour
{
    public GameObject[] panels;
    public AudioSource backgroundMusic;
    public AudioSource tickSound;
    public float narrationSpeed = 1.0f;

    private int currentPanelIndex = 0;
    public Button advanceButton;
    public Button playButton; // ✅ Added play button reference

    private Text[] panelTexts;
    private Coroutine[] narrationCoroutines;

    private bool isPaused = false;
    public GameObject gamePauseMenu;

    // Call this to toggle pause menu
    public void TogglePauseMenu()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    // Pauses the game and shows the menu
    public void PauseGame()
    {
        Time.timeScale = 0f; // Stop the game
        gamePauseMenu.SetActive(true);
        isPaused = true;
    }

    // Resumes the game and hides the menu
    public void ResumeGame()
    {
        Time.timeScale = 1f; // Resume the game
        gamePauseMenu.SetActive(false);
        isPaused = false;
    }

    // Hides the pause menu, resumes game if needed
    public void hidePauseMenu()
    {
        ResumeGame();
    }

    // Exits to the landing scene, also resumes time
    public void exitPlay()
    {
        Time.timeScale = 1f; // Just in case it was paused
        SceneManager.LoadScene("Landing");
    }

    private void Start()
    {
        panelTexts = new Text[panels.Length];
        narrationCoroutines = new Coroutine[panels.Length];

        for (int i = 0; i < panels.Length; i++)
        {
            panelTexts[i] = panels[i].GetComponentInChildren<Text>();
        }

        ShowPanel(currentPanelIndex);

        if (advanceButton != null)
        {
            advanceButton.onClick.AddListener(AdvanceToNextPanel);
        }

        if (playButton != null)
        {
            playButton.gameObject.SetActive(false); // ✅ Hide Play button at start
            playButton.onClick.AddListener(StartGame); // ✅ Assign click event
        }

        if (backgroundMusic != null)
        {
            backgroundMusic.Play();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    private void ShowPanel(int index)
    {
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }

        if (index >= 0 && index < panels.Length)
        {
            panels[index].SetActive(true);

            if (narrationCoroutines[index] != null)
            {
                StopCoroutine(narrationCoroutines[index]);
            }

            if (panelTexts[index] != null)
            {
                narrationCoroutines[index] = StartCoroutine(NarrateText(panelTexts[index]));
            }

            // ✅ Show play button only on the last panel
            if (playButton != null)
            {
                playButton.gameObject.SetActive(index == panels.Length - 1);
            }
        }
    }

    private IEnumerator NarrateText(Text textComponent)
    {
        string fullText = textComponent.text;
        textComponent.text = "";

        foreach (char c in fullText)
        {
            textComponent.text += c;
            if (tickSound != null)
            {
                tickSound.Play();
            }
            yield return new WaitForSeconds(0.1f / narrationSpeed);
        }
    }

    private void AdvanceToNextPanel()
    {
        currentPanelIndex++;

        if (currentPanelIndex >= panels.Length)
        {
            currentPanelIndex = panels.Length - 1; // Don't wrap around
        }

        ShowPanel(currentPanelIndex);
    }

    public void SetNarrationSpeed(float speed)
    {
        narrationSpeed = speed;
    }

    // ✅ Load the LevelFall scene
    public void StartGame()
    {
        Time.timeScale = 1f; // Just in case it was paused
        SceneManager.LoadScene("LevelFall");
    }
}

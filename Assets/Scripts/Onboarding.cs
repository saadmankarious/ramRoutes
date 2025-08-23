using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using RamRoutes.Services;
using RamRoutes.Model;

public class OnboardingManager : MonoBehaviour
{
    public GameObject[] panels; // Default panels when no stage set

    // New: Stage-specific panel sets (assign in Inspector)
    public GameObject[] panelsEasternCampus;
    public GameObject[] panelsFirstStreet;
    public GameObject[] panelsPedmall;
    public GameObject[] panelsTC;

    // Internals
    private GameObject[] activePanels;

    // Audio
    public AudioSource narrationAudioSource;
    public AudioClip narrationClip;
    public AudioSource sfxAudioSource;
    public AudioClip tickClip;

    // Settings
    public float narrationTypingSpeed = 1.0f;
    public float narrationVolume = 1.0f;
    public float typingSoundInterval = 0.05f;

    private float lastTypingSoundTime = 0f;

    private int currentPanelIndex = 0;
    public Button advanceButton;
    public Button playButton;

    private Text[] panelTexts;
    private Coroutine[] narrationCoroutines;

    private bool isPaused = false;
    public GameObject gamePauseMenu;

    private void Start()
    {
        // Ensure time is running properly when scene starts
        Time.timeScale = 1f;
        Debug.Log($"OnboardingManager Start - Time.timeScale set to: {Time.timeScale}");
        
        // Decide which panels to use based on game stage. If no stage set, use default panels
        var stage = GameStageService.LoadStageFromPrefs();
        if (stage == null)
        {
            activePanels = panels;
            Debug.Log("Onboarding: No stage set. Using default panels.");
        }
        else
        {
            switch (stage.area)
            {
                case Stage.EasternCampus: activePanels = panelsEasternCampus; break;
                case Stage.FirstStreet: activePanels = panelsFirstStreet; break;
                case Stage.Pedmall: activePanels = panelsPedmall; break;
                case Stage.TC: activePanels = panelsTC; break;
                default: activePanels = panels; break;
            }
            if (activePanels == null || activePanels.Length == 0)
            {
                activePanels = panels; // fallback
                Debug.LogWarning($"Onboarding: No panels configured for stage {stage.area}. Falling back to default panels.");
            }
            else
            {
                Debug.Log($"Onboarding: Using panels for stage {stage.area} (count={activePanels.Length}).");
            }
        }

        if (activePanels == null || activePanels.Length == 0)
        {
            Debug.LogError("Onboarding: No panels available to display.");
            return;
        }
        
        panelTexts = new Text[activePanels.Length];
        narrationCoroutines = new Coroutine[activePanels.Length];

        for (int i = 0; i < activePanels.Length; i++)
        {
            panelTexts[i] = activePanels[i].GetComponentInChildren<Text>();
        }

        ShowPanel(currentPanelIndex);

        if (advanceButton != null)
            advanceButton.onClick.AddListener(AdvanceToNextPanel);

        if (playButton != null)
        {
            playButton.gameObject.SetActive(false);
            playButton.onClick.AddListener(StartGame);
        }

        if (narrationAudioSource != null && narrationClip != null)
        {
            narrationAudioSource.clip = narrationClip;
            narrationAudioSource.volume = narrationVolume;
            narrationAudioSource.loop = true;
            narrationAudioSource.Play();
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
        // Hide all panels first
        foreach (var panel in activePanels)
        {
            panel.SetActive(false);
        }

        if (index >= 0 && index < activePanels.Length)
        {
            // Show the current panel
            activePanels[index].SetActive(true);

            // Stop any previously running narration coroutine
            if (narrationCoroutines[index] != null)
            {
                StopCoroutine(narrationCoroutines[index]);
            }

            // Start the narration coroutine for the current panel
            if (panelTexts[index] != null)
            {
                // Disable advance button during narration
                if (advanceButton != null)
                {
                    advanceButton.interactable = false;
                }
                
                // Disable play button during narration if it's visible
                if (playButton != null && index == activePanels.Length - 1)
                {
                    playButton.interactable = false;
                }
                
                narrationCoroutines[index] = StartCoroutine(NarrateText(panelTexts[index]));
            }

            if (playButton != null)
            {
                playButton.gameObject.SetActive(index == activePanels.Length - 1);
            }
        }
    }

    private IEnumerator NarrateText(Text textComponent)
    {
        string fullText = textComponent.text;
        textComponent.text = "";

        lastTypingSoundTime = Time.time;

        foreach (char c in fullText)
        {
            textComponent.text += c;

            // Play tick sound based on interval
            if (sfxAudioSource != null && tickClip != null)
            {
                if (Time.time - lastTypingSoundTime >= typingSoundInterval)
                {
                    sfxAudioSource.PlayOneShot(tickClip);
                    lastTypingSoundTime = Time.time;
                }
            }

            // Check if current character is punctuation that needs longer pause
            float pauseTime = 0.1f / narrationTypingSpeed;
            if (c == '.' || c == '!' || c == '?')
            {
                pauseTime *= 50f; // sentence-ending punctuation
            }
            else if (c == ',' || c == ';' || c == ':')
            {
                pauseTime *= 25f; // mid-sentence punctuation
            }

            yield return new WaitForSeconds(pauseTime);
        }
        
        // Re-enable advance button when narration is complete
        if (advanceButton != null)
        {
            advanceButton.interactable = true;
        }
        
        // Re-enable play button when narration is complete (if it's on the last panel)
        if (playButton != null && currentPanelIndex == activePanels.Length - 1)
        {
            playButton.interactable = true;
        }
    }

    private void AdvanceToNextPanel()
    {
        // Stop any previous tick sounds if switching panels quickly
        if (sfxAudioSource != null && sfxAudioSource.isPlaying)
        {
            sfxAudioSource.Stop();
        }

        currentPanelIndex++;

        // Circular behavior: if past the last panel, loop back to the first
        if (currentPanelIndex >= activePanels.Length)
        {
            currentPanelIndex = 0;
        }

        ShowPanel(currentPanelIndex);
    }

    public void SetNarrationVolume(float volume)
    {
        narrationVolume = volume;
        if (narrationAudioSource != null)
        {
            narrationAudioSource.volume = volume;
        }
    }

    public void SetNarrationTypingSpeed(float speed)
    {
        narrationTypingSpeed = speed;
    }

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

    public void PauseGame()
    {
        Time.timeScale = 0f;
        gamePauseMenu.SetActive(true);
        isPaused = true;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        gamePauseMenu.SetActive(false);
        isPaused = false;
    }

    public void hidePauseMenu()
    {
        ResumeGame();
    }

    public void exitPlay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Landing");
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelRPG");
    }
}

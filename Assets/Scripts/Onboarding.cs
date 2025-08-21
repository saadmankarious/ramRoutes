using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class OnboardingManager : MonoBehaviour
{
    public GameObject[] panels;

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
        
        panelTexts = new Text[panels.Length];
        narrationCoroutines = new Coroutine[panels.Length];

        for (int i = 0; i < panels.Length; i++)
        {
            panelTexts[i] = panels[i].GetComponentInChildren<Text>();
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
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }

        if (index >= 0 && index < panels.Length)
        {
            // Show the current panel
            panels[index].SetActive(true);

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
                if (playButton != null && index == panels.Length - 1)
                {
                    playButton.interactable = false;
                }
                
                narrationCoroutines[index] = StartCoroutine(NarrateText(panelTexts[index]));
            }

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
                pauseTime *= 50f; // Make pause 3x longer for sentence-ending punctuation
            }
            else if (c == ',' || c == ';' || c == ':')
            {
                pauseTime *= 25f; // Make pause 1.5x longer for mid-sentence punctuation
            }

            yield return new WaitForSeconds(pauseTime);
        }
        
        // Re-enable advance button when narration is complete
        if (advanceButton != null)
        {
            advanceButton.interactable = true;
        }
        
        // Re-enable play button when narration is complete (if it's on the last panel)
        if (playButton != null && currentPanelIndex == panels.Length - 1)
        {
            playButton.interactable = true;
        }
    }

    private void AdvanceToNextPanel()
    {
        // Stop any previous tick sounds if switching panels quickly
        if (sfxAudioSource.isPlaying)
        {
            sfxAudioSource.Stop();
        }

        currentPanelIndex++;

        // Circular behavior: if past the last panel, loop back to the first
        if (currentPanelIndex >= panels.Length)
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

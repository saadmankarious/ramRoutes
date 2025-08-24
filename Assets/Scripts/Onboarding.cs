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
    public GameObject[] panelsTerminal; // NEW: Terminal stage panels

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

    // Ambient audio layers
    [Header("Ambient Audio")] 
    public AudioSource ambientCalmSource;   // soft wind
    public AudioSource ambientChatterSource; // distant chatter
    public AudioSource ambientHumSource;    // background hum
    [Range(0f,1f)] public float ambientBaseVolume = 0.6f;
    [Range(0f,1f)] public float ambientLowVolume = 0.15f;
    [SerializeField] private float ambientFadeDuration = 0.75f;

    // Typing fade settings
    [Header("Typing FX")] 
    [Range(0.5f,1f)] public float typeFadeStartAlpha = 0.9f;
    [SerializeField] private float typeFadeDuration = 0.06f;

    private Coroutine typeFlashCoroutine;
    private Coroutine calmFadeCoroutine, chatterFadeCoroutine, humFadeCoroutine;

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
                case Stage.Terminal: activePanels = panelsTerminal; break; // NEW: handle Terminal
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
            // Visibility is controlled by ShowPanel; do not override here.
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

            // Configure buttons visibility for this index
            if (advanceButton != null)
            {
                // Hide advance button on last (or single) panel
                bool showAdvance = index < activePanels.Length - 1;
                advanceButton.gameObject.SetActive(showAdvance);
                advanceButton.interactable = false; // will be re-enabled after narration (if shown)
            }

            if (playButton != null)
            {
                // Only show play on the last panel
                playButton.gameObject.SetActive(index == activePanels.Length - 1);
                if (index == activePanels.Length - 1)
                {
                    // Will be enabled after narration completes
                    playButton.interactable = false;
                }
            }

            // Stop any previously running narration coroutine
            if (narrationCoroutines[index] != null)
            {
                StopCoroutine(narrationCoroutines[index]);
            }

            // Start the narration coroutine for the current panel
            if (panelTexts[index] != null)
            {
                narrationCoroutines[index] = StartCoroutine(NarrateText(panelTexts[index]));
            }

            // Determine ambient tone for this panel from its full text
            if (panelTexts[index] != null)
            {
                UpdateAmbientForText(panelTexts[index].text);
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

            // Subtle per-character fade-in
            TriggerTypeFlash(textComponent);

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
        
        // Re-enable advance button when narration is complete (only if not on last panel)
        if (advanceButton != null && currentPanelIndex < activePanels.Length - 1)
        {
            advanceButton.interactable = true;
        }
        
        // Re-enable play button when narration is complete (if it's on the last panel)
        if (playButton != null && currentPanelIndex == activePanels.Length - 1)
        {
            playButton.interactable = true;
        }
    }

    // Subtle overall text alpha flash per typed character
    private void TriggerTypeFlash(Text t)
    {
        if (t == null) return;
        if (typeFlashCoroutine != null) StopCoroutine(typeFlashCoroutine);
        typeFlashCoroutine = StartCoroutine(FadeTextAlpha(t, typeFadeStartAlpha, typeFadeDuration));
    }

    private IEnumerator FadeTextAlpha(Text t, float startAlpha, float duration)
    {
        if (t == null) yield break;
        Color c = t.color;
        float endAlpha = 1f;
        float origAlpha = c.a;
        c.a = Mathf.Min(startAlpha, endAlpha);
        t.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(c.a, endAlpha, elapsed / duration);
            Color cc = t.color; cc.a = a; t.color = cc;
            yield return null;
        }
        Color final = t.color; final.a = endAlpha; t.color = final;
    }

    // Ambient tone detection and crossfade
    private enum NarrativeTone { Calm, Social, Tense }

    private void UpdateAmbientForText(string fullText)
    {
        var tone = DetectTone(fullText);
        ApplyAmbientTone(tone);
    }

    private NarrativeTone DetectTone(string txt)
    {
        if (string.IsNullOrEmpty(txt)) return NarrativeTone.Calm;
        string t = txt.ToLowerInvariant();

        // Simple heuristics
        if (t.Contains("danger") || t.Contains("hurry") || t.Contains("run") || t.Contains("warning") || t.Contains("!"))
            return NarrativeTone.Tense;
        if (t.Contains("welcome") || t.Contains("together") || t.Contains("friends") || t.Contains("community") || t.Contains("market") || t.Contains("gather"))
            return NarrativeTone.Social;
        return NarrativeTone.Calm;
    }

    private void ApplyAmbientTone(NarrativeTone tone)
    {
        // Ensure sources are ready
        EnsureAmbientPlaying(ambientCalmSource);
        EnsureAmbientPlaying(ambientChatterSource);
        EnsureAmbientPlaying(ambientHumSource);

        float calmTarget = 0f, chatterTarget = 0f, humTarget = 0f;
        switch (tone)
        {
            case NarrativeTone.Calm:
                calmTarget = ambientBaseVolume;
                chatterTarget = 0f;
                humTarget = ambientLowVolume;
                break;
            case NarrativeTone.Social:
                calmTarget = ambientLowVolume;
                chatterTarget = ambientBaseVolume;
                humTarget = 0.1f;
                break;
            case NarrativeTone.Tense:
                calmTarget = 0f;
                chatterTarget = 0f;
                humTarget = ambientBaseVolume;
                break;
        }

        // Crossfade to targets
        if (ambientCalmSource != null)
        {
            if (calmFadeCoroutine != null) StopCoroutine(calmFadeCoroutine);
            calmFadeCoroutine = StartCoroutine(CrossfadeVolume(ambientCalmSource, calmTarget, ambientFadeDuration));
        }
        if (ambientChatterSource != null)
        {
            if (chatterFadeCoroutine != null) StopCoroutine(chatterFadeCoroutine);
            chatterFadeCoroutine = StartCoroutine(CrossfadeVolume(ambientChatterSource, chatterTarget, ambientFadeDuration));
        }
        if (ambientHumSource != null)
        {
            if (humFadeCoroutine != null) StopCoroutine(humFadeCoroutine);
            humFadeCoroutine = StartCoroutine(CrossfadeVolume(ambientHumSource, humTarget, ambientFadeDuration));
        }
    }

    private void EnsureAmbientPlaying(AudioSource src)
    {
        if (src == null) return;
        src.loop = true;
        if (!src.isPlaying)
        {
            src.Play();
        }
    }

    private IEnumerator CrossfadeVolume(AudioSource src, float targetVolume, float duration)
    {
        if (src == null) yield break;
        float start = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(start, targetVolume, elapsed / duration);
            yield return null;
        }
        src.volume = targetVolume;
    }

    private void AdvanceToNextPanel()
    {
        // Stop any previous tick sounds if switching panels quickly
        if (sfxAudioSource != null && sfxAudioSource.isPlaying)
        {
            sfxAudioSource.Stop();
        }

        // If already at the last panel, do not loop back
        if (currentPanelIndex >= activePanels.Length - 1)
        {
            return;
        }

        currentPanelIndex++;
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

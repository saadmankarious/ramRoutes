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

    public AudioClip celebrateClip;
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

    [Header("Background Music Settings")]
    [SerializeField] private AudioClip tcStageMusic;        // Town Center music
    [SerializeField] private AudioClip easternCampusMusic;  // Eastern Campus music
    [SerializeField] private AudioClip firstStreetMusic;    // First Street music
    [SerializeField] private AudioClip pedmallMusic;        // Pedmall music
    [SerializeField] private AudioClip terminalMusic;       // Terminal stage music
    [SerializeField] private float backgroundMusicVolume = 0.3f; // Lower volume to avoid dramatic pulses
    [SerializeField] private float musicFadeInDuration = 2f; // Smooth fade in to avoid harsh starts
    
    private AudioSource backgroundMusicSource;
    [SerializeField] private Stage stageToRender;
    [SerializeField] private bool debugForceStage = false;



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

        // Initialize background music source
        if (backgroundMusicSource == null)
        {
            GameObject musicObject = new GameObject("BackgroundMusic");
            musicObject.transform.SetParent(transform);
            backgroundMusicSource = musicObject.AddComponent<AudioSource>();
            backgroundMusicSource.loop = true;
            backgroundMusicSource.volume = 0f; // Start at 0 for smooth fade-in
            backgroundMusicSource.playOnAwake = false;
        }
        
        // Decide which panels to use based on game stage. If no stage set, use default panels
        var stage = GameStageService.LoadStageFromPrefs();
        if (debugForceStage)
        {
            stage.area = stageToRender;
        }
        if (stage == null)
            {
                activePanels = panels;
                Debug.Log("Onboarding: No stage set. Using default panels.");
                SetBackgroundMusicForStage(Stage.TC); // Default to TC music
            }
            else
            {
                switch (stage.area)
                {
                    case Stage.EasternCampus: activePanels = panelsEasternCampus; break;
                    case Stage.FirstStreet: activePanels = panelsFirstStreet; break;
                    case Stage.Pedmall: activePanels = panelsPedmall; break;
                    case Stage.TC: activePanels = panelsTC; break;
                    case Stage.Terminal: activePanels = panelsTerminal; break;
                    default: activePanels = panels; break;
                }
                SetBackgroundMusicForStage(stage.area);
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

            // Find and animate any objects tagged as "Popup" in the current panel
            StartCoroutine(AnimatePopupElements(activePanels[index]));

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
    private void UpdateAmbientForText(string fullText)
    {
        // No longer needed with new music system
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

    private AudioClip GetMusicForStage(Stage stage)
    {
        return stage switch
        {
            Stage.TC => tcStageMusic,
            Stage.EasternCampus => easternCampusMusic,
            Stage.FirstStreet => firstStreetMusic,
            Stage.Pedmall => pedmallMusic,
            Stage.Terminal => terminalMusic,
            _ => null
        };
    }

    private IEnumerator FadeInMusic()
    {
        float elapsedTime = 0f;
        float startVolume = 0f;
        
        while (elapsedTime < musicFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / musicFadeInDuration;
            backgroundMusicSource.volume = Mathf.Lerp(startVolume, backgroundMusicVolume, progress);
            yield return null;
        }
        
        backgroundMusicSource.volume = backgroundMusicVolume;
    }

    private IEnumerator FadeOutMusic()
    {
        float elapsedTime = 0f;
        float startVolume = backgroundMusicSource.volume;
        
        while (elapsedTime < musicFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / musicFadeInDuration;
            backgroundMusicSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            yield return null;
        }
        
        backgroundMusicSource.volume = 0f;
        backgroundMusicSource.Stop();
    }

    private void SetBackgroundMusicForStage(Stage stage)
    {
        if (backgroundMusicSource == null) return;

        AudioClip stageMusic = GetMusicForStage(stage);
        
        if (stageMusic != null)
        {
            // If music is already playing and it's the same clip, don't restart
            if (backgroundMusicSource.clip == stageMusic && backgroundMusicSource.isPlaying)
            {
                Debug.Log($"Stage music for {stage} is already playing");
                return;
            }
            
            // Stop current music if playing
            if (backgroundMusicSource.isPlaying)
            {
                StartCoroutine(CrossfadeToNewMusic(stageMusic));
            }
            else
            {
                // No music currently playing, start fresh with fade-in
                backgroundMusicSource.clip = stageMusic;
                backgroundMusicSource.Play();
                StartCoroutine(FadeInMusic());
            }
            
            Debug.Log($"Set background music for stage: {stage}");
        }
        else
        {
            // No music assigned for this stage, fade out current music if playing
            if (backgroundMusicSource.isPlaying)
            {
                StartCoroutine(FadeOutMusic());
            }
            Debug.Log($"No background music assigned for stage: {stage}");
        }
    }

    private IEnumerator CrossfadeToNewMusic(AudioClip newMusic)
    {
        // Fade out current music
        float elapsedTime = 0f;
        float startVolume = backgroundMusicSource.volume;
        float halfFadeDuration = musicFadeInDuration * 0.5f;
        
        while (elapsedTime < halfFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfFadeDuration;
            backgroundMusicSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            yield return null;
        }
        
        // Switch to new music
        backgroundMusicSource.clip = newMusic;
        backgroundMusicSource.Play();
        
        // Fade in new music
        elapsedTime = 0f;
        while (elapsedTime < halfFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfFadeDuration;
            backgroundMusicSource.volume = Mathf.Lerp(0f, backgroundMusicVolume, progress);
            yield return null;
        }
        
        backgroundMusicSource.volume = backgroundMusicVolume;
    }

    /// <summary>
    /// Finds and animates all objects tagged as "Popup" and "Fadein" within the given panel
    /// </summary>
    /// <param name="panel">The panel to search for popup and fadein elements</param>
    private IEnumerator AnimatePopupElements(GameObject panel)
    {
        if (panel == null) yield break;

        // Find all objects tagged as "Popup" within this panel (including children)
        GameObject[] popupObjects = GameObject.FindGameObjectsWithTag("Popup");
        
        // Filter to only include popup objects that are children of the current panel
        var panelPopups = new System.Collections.Generic.List<GameObject>();
        foreach (var popup in popupObjects)
        {
            if (popup.transform.IsChildOf(panel.transform))
            {
                panelPopups.Add(popup);
            }
        }

        // Find all objects tagged as "Fadein" within this panel (including children)
        GameObject[] fadeinObjects = GameObject.FindGameObjectsWithTag("Fadein");
        
        // Filter to only include fadein objects that are children of the current panel
        var panelFadeins = new System.Collections.Generic.List<GameObject>();
        foreach (var fadein in fadeinObjects)
        {
            if (fadein.transform.IsChildOf(panel.transform))
            {
                panelFadeins.Add(fadein);
            }
        }

        // Animate each popup object with popup animation
        if (panelPopups.Count > 0)
        {
            foreach (var popup in panelPopups)
            {
                // Start the popup animation (don't wait for it to complete)
                                StartCoroutine(AnimatePopup(popup));

                // Small delay between popup animations for a staggered effect
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Animate each fadein object with fade-in animation sequentially
        if (panelFadeins.Count > 0)
        {
            foreach (var fadein in panelFadeins)
            {
                // Play celebrate clip during each fade-in
                if (narrationAudioSource != null && celebrateClip != null)
                {
                    narrationAudioSource.clip = celebrateClip;
                    narrationAudioSource.volume = narrationVolume;
                    narrationAudioSource.loop = false; // Don't loop for celebrate sound
                    narrationAudioSource.Play();
                }
                
                // Wait for the fade-in animation to complete before starting the next one
                yield return StartCoroutine(AnimateFadein(fadein));

                // Small delay before starting the next fade-in animation
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    /// <summary>
    /// Animates a popup with a smooth bounce effect
    /// </summary>
    /// <param name="popup">The GameObject to animate</param>
    private IEnumerator AnimatePopup(GameObject popup)
    {
        if (popup == null) yield break;

        // Store the original scale to reset to it
        Vector3 originalScale = popup.transform.localScale;
        
        // Start with zero scale
        popup.transform.localScale = Vector3.zero;
        
        // Animate to full size with a stronger overshoot and longer duration
        float duration = .8f; // Increased from 0.4f
        float elapsed = 0f;
        
        // First phase - grow quickly to much larger than target for stronger popup
        while (elapsed < duration * 0.7f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration * 0.7f);
            // Use stronger overshoot for more dramatic effect (1.3x instead of 1.1x)
            float overshoot = Mathf.Lerp(0, 1.3f, progress);
            popup.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale * overshoot, progress);
            yield return null;
        }
        
        // Second phase - settle back to original size
        float secondPhaseDuration = duration * 0.3f;
        elapsed = 0f;
        Vector3 overshotScale = popup.transform.localScale;
        
        while (elapsed < secondPhaseDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / secondPhaseDuration;
            popup.transform.localScale = Vector3.Lerp(overshotScale, originalScale, progress);
            yield return null;
        }
        
        // Ensure we end at exactly the original scale
        popup.transform.localScale = originalScale;
    }

    /// <summary>
    /// Animates a fade-in effect for UI elements
    /// </summary>
    /// <param name="fadeinObject">The GameObject to fade in</param>
    private IEnumerator AnimateFadein(GameObject fadeinObject)
    {
        if (fadeinObject == null) yield break;

        // Get all UI components that can have alpha (Image, Text, CanvasGroup, etc.)
        UnityEngine.UI.Image image = fadeinObject.GetComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.Text text = fadeinObject.GetComponent<UnityEngine.UI.Text>();
        CanvasGroup canvasGroup = fadeinObject.GetComponent<CanvasGroup>();
        
        // Store original alpha values
        float originalImageAlpha = 1f;
        float originalTextAlpha = 1f;
        float originalCanvasGroupAlpha = 1f;
        
        if (image != null)
        {
            originalImageAlpha = image.color.a;
            Color imageColor = image.color;
            imageColor.a = 0f;
            image.color = imageColor;
        }
        
        if (text != null)
        {
            originalTextAlpha = text.color.a;
            Color textColor = text.color;
            textColor.a = 0f;
            text.color = textColor;
        }
        
        if (canvasGroup != null)
        {
            originalCanvasGroupAlpha = canvasGroup.alpha;
            canvasGroup.alpha = 0f;
        }

        // Fade in over time
        float duration = .3f; // Fade-in duration
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Smooth ease-in curve
            float alpha = Mathf.Lerp(0f, 1f, progress);
            
            if (image != null)
            {
                Color imageColor = image.color;
                imageColor.a = Mathf.Lerp(0f, originalImageAlpha, alpha);
                image.color = imageColor;
            }
            
            if (text != null)
            {
                Color textColor = text.color;
                textColor.a = Mathf.Lerp(0f, originalTextAlpha, alpha);
                text.color = textColor;
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, originalCanvasGroupAlpha, alpha);
            }
            
            yield return null;
        }
        
        // Ensure we end at exactly the original alpha values
        if (image != null)
        {
            Color imageColor = image.color;
            imageColor.a = originalImageAlpha;
            image.color = imageColor;
        }
        
        if (text != null)
        {
            Color textColor = text.color;
            textColor.a = originalTextAlpha;
            text.color = textColor;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = originalCanvasGroupAlpha;
        }
    }
}

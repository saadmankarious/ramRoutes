using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Linq;
using System.Threading.Tasks;
using RamRoutes.Services;
using Firebase.Auth;
using System.Collections.Generic;
using RamRoutes.Model;
using Cinemachine;

public class UIManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip collectableSound;

    // Stage-specific time limits (seconds)
    private Dictionary<Stage, int> stageTimeLimits = new Dictionary<Stage, int>
    {
        { Stage.TC, 0 },
        { Stage.EasternCampus, 0 },
        { Stage.FirstStreet, 0 },
        { Stage.Pedmall, 180 },
        { Stage.Terminal, 0 }
    };
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    public Text coinsText;
    public Text usernameAndHallText; // New text field for username and hall

    public Text knowledgePointsText; // New text field for knowledge points
    public ParticleSystem teleportEffect;
    public ParticleSystem celebrationEffect1;
    public ParticleSystem celebrationEffect2;
    public float padding = 2f;
    
    [Header("Building UI")]
    public Text buildingTitle;      // Moved from BuildingInteraction
    public Text buildingDescription; // Moved from BuildingInteraction
    public Text buildingUnlockedMessage; // Moved from BuildingInteraction
    public Image npcImage;          // NPC image in building unlocked dialog
    public Text npcTitle;           // NPC title in building unlocked dialog
    public Text coinsGained;           // NPC title in building unlocked dialog
    public Text kbGained;           // NPC title in building unlocked dialog
    public Text coinsGainedBuildingStats;           // NPC title in building unlocked dialog
    public Text kbGainedBuildingStats;           // NPC title in building unlocked dialog

    [Header("Current Users Display")]
    public GameObject currentUsersPanel;
    public Transform currentUsersContentParent;
    public GameObject currentUserPrefab;

    [Header("Celebration Settings")]
    [SerializeField] private float celebrationPlaybackSpeed = 1f; // 1f = normal speed, 2f = double speed, 0.5f = half speed
    [SerializeField] private float celebrationDuration = 2f; // Total duration of celebration in seconds (controls both sound and particles)

    public Text timerText;
    public Text heldItem;
    public GameObject dialogPanel;
    public GameObject quickUpdatePanel;
    public Text quickUpdateText;
    public GameObject buildingStats;
    public Text dialogText;
    public Button dialogActionButton; // Action button for dialogs
    public Text dialogActionButtonText; // Text component of the action button
    public GameObject timeUpMenu;
    public GameObject trialCompleteMenu;
    public GameObject gamePauseMenu;

    [Header("Timing Settings")]
    [SerializeField] private float typingSpeed = 0.3f;
    [SerializeField] private float objectiveRepeatTime = 60;
    private float currentTime;
    private bool timerRunning;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip trialCompleteSound;
    public AudioClip typingTickSound;
    public AudioClip timeExpiredSound;
    
    [Header("Background Music Settings")]
    [SerializeField] private AudioClip tcStageMusic;        // Town Center music
    [SerializeField] private AudioClip easternCampusMusic; // Eastern Campus music
    [SerializeField] private AudioClip firstStreetMusic;   // First Street music
    [SerializeField] private AudioClip pedmallMusic;       // Pedmall music
    [SerializeField] private AudioClip terminalMusic;      // Terminal stage music
    [SerializeField] private float backgroundMusicVolume = 0.3f; // Lower volume to avoid dramatic pulses
    [SerializeField] private float musicFadeInDuration = 2f; // Smooth fade in to avoid harsh starts
    
    private AudioSource backgroundMusicSource;

    [Header("Typing Sound Settings")]
    [SerializeField] private float typingSoundInterval = 0.15f;
    
    // Static cache for current users per building
    private static Dictionary<string, List<User>> cachedCurrentUsersPerBuilding = new Dictionary<string, List<User>>();
    private static Dictionary<string, bool> currentUsersLoadedPerBuilding = new Dictionary<string, bool>();
    private float lastTypingSoundTime;
    [SerializeField] private float typingSoundVolume = 0.3f;

    [Header("Events")]
    public UnityEvent OnTrialComplete = new UnityEvent();
    public UnityEvent OnTimeExpired = new UnityEvent();
    public UnityEvent<BuildingInteraction> OnBuildingUnlocked = new UnityEvent<BuildingInteraction>();

    [Header("Mobile NPC Interaction")]
    public Button mobileInteractButton; // Mobile button for NPC interactions
    
    [Header("Progress Bar")]
    public GameObject[] progressBarImages; // Array of progress bar images to activate sequentially
    public Text currentStageText; // Text to display current game stage (e.g., TC, CC, SC)
    
    [Header("Building Gates")]
    [Tooltip("Set pairs of BuildingInteraction and its connected Gate. UIManager will unlock the mapped gate when that building is unlocked.")]
    public BuildingGatePair[] buildingGatePairs;

    [Header("Debug / Startup")]
    [Tooltip("If enabled, clears the saved game stage from PlayerPrefs on startup before initialization.")]
    [SerializeField] private bool clearStageOnStart = false;
    
    [Header("Scene Transition Settings")]
    [Tooltip("Delay in seconds before executing scene transition after building unlock conditions are met.")]
    [SerializeField] private float sceneTransitionDelay = 10f;
    
    [Header("Scene Transition Effects")]
    [Tooltip("UI Image component to use as fade overlay during scene transitions.")]
    [SerializeField] private Image fadeOverlay;
    [Tooltip("Duration of the fade effect before scene transition.")]
    [SerializeField] private float fadeEffectDuration = 2f;

    [System.Serializable]
    public class BuildingGatePair
    {
        public BuildingInteraction building;
        public Gate gate;
    }

    private Dictionary<BuildingInteraction, Gate> buildingGateMap;

    private Coroutine typingCoroutine;
    private Coroutine objectiveRepeatCoroutine;
    private Coroutine timerCoroutine;
    public GameObject aros; // Reference to AROS prefab (static display)
    private int buildingsUnlockedCount = 0; // Track number of buildings unlocked
    private Coroutine dialogCoroutine; // Track the entire dialog sequence
    private bool isDialogActive = false; // Flag to prevent overlapping dialogs
    private System.Action currentDialogAction; // Store current dialog action
    private bool keepArosVisible = false; // Flag to keep AROS visible during unlock sequences
    
    // Mobile NPC interaction state
    private NpcAutoMovement currentInteractingNPC; // Currently registered NPC for mobile interaction
    
    private bool isPaused = false;

    // New: defer scene change until building unlock flow completes
    private bool pendingSceneAfterUnlock = false;
    // New: scene change should only occur after progress bar reveal AND unlock panel is closed
    private bool readyToLeaveAfterPanelClose = false;

    // Viewing mode state (prevents scene changes while inspecting a building)
    private bool isInBuildingViewingMode = false;
    private string currentViewedBuilding = null;
    
    // Deferred stage change system
    private Coroutine sceneTransitionDelayCoroutine;

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Initialize background music source separately
        if (backgroundMusicSource == null)
        {
            GameObject musicObject = new GameObject("BackgroundMusic");
            musicObject.transform.SetParent(transform);
            backgroundMusicSource = musicObject.AddComponent<AudioSource>();
            backgroundMusicSource.loop = true;
            backgroundMusicSource.volume = 0f; // Start at 0 for smooth fade-in
            backgroundMusicSource.playOnAwake = false;
        }
        
        // Clean up any conflicting audio settings from other scripts
        CleanupConflictingAudioSettings();
        
        // Load building data from JSON
        BuildingDataManager.LoadBuildingData();
        
        // Initialize AROS as static element
        if (aros != null)
        {
            // Set default scale
            aros.transform.localScale = new Vector3(1f, 1f, 1f);
            
            // Make sure color is fully opaque
            Image image = aros.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = 1f;
                image.color = color;
            }
            
            // Make sure it's hidden initially
            aros.SetActive(false);
        }

        // Initialize progress bar - all images inactive at start
        if (progressBarImages != null)
        {
            foreach (GameObject progressImage in progressBarImages)
            {
                if (progressImage != null)
                {
                    progressImage.SetActive(false);
                }
            }
        }

        // Clear saved stage locally if requested
        if (clearStageOnStart)
        {
            GameStageService.ClearStageFromPrefs();
            if (currentStageText != null)
            {
                currentStageText.text = ""; // clear label; will be set during initialization
            }
        }

        // Initialize progress bar based on unlocked buildings
        _ = InitializeProgressBar();
        
        // Initialize game stage to TC if not already set
        _ = InitializeGameStage();
        
        // Reset fade overlay to be transparent and inactive at start
        ResetFadeOverlay();
    }

    private async Task InitializeGameStage()
    {
        try
        {
            var existing = GameStageService.LoadStageFromPrefs();
            if (existing == null)
            {
                var toSet = GameStage.FromArea(Stage.TC);
                SetCurrentStageText(toSet);
                await GameStageService.SetStage(toSet);
                Debug.Log("UIManager: Initialized game stage to TC");
                
                // Set background music for initial TC stage
                SetBackgroundMusicForStage(toSet.area);
            }
            else
            {
                SetCurrentStageText(existing);
                Debug.Log($"UIManager: Game stage already set to {existing.area}");
                
                // Set background music for existing stage
                SetBackgroundMusicForStage(existing.area);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UIManager: Failed to initialize game stage: {ex.Message}");
        }
    }

    private void SetCurrentStageText(GameStage stage)
    {
        if (currentStageText == null || stage == null) return;
        var label = stage.stageDisplayName ?? GameStage.GetDefaultDisplayName(stage.area);
        currentStageText.text = label;
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

    private void CleanupConflictingAudioSettings()
    {
        // Find all AudioSources in the scene that might interfere with background music
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        
        foreach (AudioSource audioSrc in allAudioSources)
        {
            // Skip our own AudioSources
            if (audioSrc == audioSource || audioSrc == backgroundMusicSource)
                continue;
                
            // Disable playOnAwake for all other AudioSources to prevent auto-playing background music
            if (audioSrc.playOnAwake && audioSrc.clip != null)
            {
                // Check if this looks like background music (long clips that loop)
                bool likelyBackgroundMusic = audioSrc.clip.length > 30f || audioSrc.loop;
                
                if (likelyBackgroundMusic)
                {
                    Debug.Log($"UIManager: Disabled auto-play for potentially conflicting audio source on {audioSrc.gameObject.name}");
                    audioSrc.playOnAwake = false;
                    audioSrc.Stop(); // Stop if currently playing
                }
            }
            
            // Lower volume of any AudioSources that might be playing background-like audio
            if (audioSrc.isPlaying && audioSrc.clip != null && audioSrc.clip.length > 30f)
            {
                float originalVolume = audioSrc.volume;
                if (originalVolume > backgroundMusicVolume)
                {
                    audioSrc.volume = backgroundMusicVolume * 0.5f; // Make it quieter than our background music
                    Debug.Log($"UIManager: Lowered volume of background-like audio on {audioSrc.gameObject.name} from {originalVolume} to {audioSrc.volume}");
                }
            }
        }
        
        Debug.Log("UIManager: Cleaned up conflicting audio settings");
    }

    private void UpdateUsernameAndHall()
    {
        if (usernameAndHallText != null)
        {
            // Simple implementation using PlayerPrefs
            string userName = PlayerPrefs.GetString("UserName", "Anonymous User");
            string hall = PlayerPrefs.GetString("ResidenceHall", "No Hall");
            
            usernameAndHallText.text = $"@{userName} - {hall}";
        }
    }
    
    private async Task GetUserPoints()
    {
        try 
        {
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                var userService = new RamRoutes.Services.UserService();
                int coins = await userService.GetUserCoins(userId);
                UpdateCoins(coins);
                Debug.Log($"Retrieved user coins: {coins}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to get user points: {ex.Message}");
        }
    }

    private async Task GetUserKnowledgePoints()
    {
        try 
        {
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                var userService = new RamRoutes.Services.UserService();
                int points = await userService.GetUserKnowledgePoints(userId);
                UpdateKnowledgePoints(points);
                Debug.Log($"Retrieved user knowledge points: {points}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to get user knowledge points: {ex.Message}");
        }
    }

    private IEnumerator Start()
    {
        while (GameManager.Instance == null || GameManager.Instance.currentTrial == null)
        {
            yield return null;
        }

        OnTrialComplete.AddListener(() => StartCoroutine(CompleteTrial()));
        OnTimeExpired.AddListener(TimeUp);

        // Get and display user points
        _ = GetUserPoints();
        _ = GetUserKnowledgePoints();
        
        // Update username and hall display
        UpdateUsernameAndHall();

        // Start countdown if current stage has a time limit > 0
        var currentStage = GameStageService.LoadStageFromPrefs();
        if (currentStage != null && stageTimeLimits.TryGetValue(currentStage.area, out int stageLimit) && stageLimit > 0)
        {
            StartCountdown(timerText, stageLimit);
        }
    }

    private void ShowObjectsWithTag(string tag)
{
    GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>()
        .Where(go => go.CompareTag(tag) && go.hideFlags == HideFlags.None && go.scene.IsValid()).ToArray();

    foreach (GameObject obj in objs)
    {
        obj.SetActive(true);
    }
}

private void HideObjectsWithTag(string tag)
{
    GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>()
        .Where(go => go.CompareTag(tag) && go.hideFlags == HideFlags.None && go.scene.IsValid()).ToArray();

    foreach (GameObject obj in objs)
    {
        obj.SetActive(false);
    }
}


    private GameObject[] FindObjectsWithTagInactive(string tag)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
                       .Where(go => go.CompareTag(tag)).ToArray();
    }

    private void ResetAllTrialObjects(string exceptTrial)
    {
        for (int i = 1; i <= 4; i++)
        {
            string tag = "Trial " + i;
            GameObject[] objects = FindObjectsWithTagInactive(tag);
            foreach (GameObject o in objects)
            {
                if (o.activeSelf)
                {
                    if(o.tag != exceptTrial)
                    {
                    o.SetActive(false);
                    }else{
                        o.SetActive(true);
                    }
                }
            }
        }
    }


    private IEnumerator CountdownTimer()
    {
        while (timerRunning && currentTime < GameManager.Instance.currentTrial.timeLimit)
        {
            yield return new WaitForSeconds(1f);
            currentTime += 1f;
            UpdateTimerDisplay();

            float timeRemaining = GameManager.Instance.currentTrial.timeLimit - currentTime;
            if (timeRemaining <= 30f)
            {
                timerText.color = Color.red;
            }
        }

        if (timerRunning)
        {
            OnTimeExpired.Invoke();
        }
    }
    
    /// <summary>
    /// Starts a countdown timer with a specified text component and duration
    /// </summary>
    /// <param name="textComponent">The Text component to display the countdown</param>
    /// <param name="seconds">The number of seconds to count down from</param>
    public void StartCountdown(Text textComponent, int seconds)
    {
        if (textComponent == null) return;
        
        // Ensure the timer text component is visible
        if (!textComponent.gameObject.activeInHierarchy)
        {
            textComponent.gameObject.SetActive(true);
        }
        
        // Stop any existing countdown
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        // Use the existing CountdownTimer logic but with new parameters
        timerRunning = true;
        currentTime = 0;
        
        // Store the original timerText reference
        Text originalTimerText = timerText;
        
        // Temporarily set timerText to the provided text component
        timerText = textComponent;
        
        // Set the time limit to the provided seconds
        GameManager.Instance.currentTrial.timeLimit = seconds;
        
        // Start the timer
        timerCoroutine = StartCoroutine(CountdownTimer());
        
        // Reset the timerText reference after the timer completes
        StartCoroutine(ResetTimerAfterCountdown(originalTimerText));
    }
    
    private IEnumerator ResetTimerAfterCountdown(Text originalTimerText)
    {
        // Wait until the timer is no longer running
        yield return new WaitUntil(() => !timerRunning);
        
        // Reset the timer text reference
        timerText = originalTimerText;
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            // Ensure timer text is visible when updating
            if (!timerText.gameObject.activeInHierarchy)
            {
                timerText.gameObject.SetActive(true);
            }
            
            float timeLeft = GameManager.Instance.currentTrial.timeLimit - currentTime;
            timerText.text = FormatTime(timeLeft);
        }
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private void TimeUp()
    {
        var stage = GameStageService.LoadStageFromPrefs();
        if (stage.area == Stage.Pedmall)
        {
            timerRunning = false;
            StopAllCoroutines();
            timeUpMenu.SetActive(true);
            
            // Ensure timer text is visible when showing final time
            if (timerText != null)
            {
                if (!timerText.gameObject.activeInHierarchy)
                {
                    timerText.gameObject.SetActive(true);
                }
                timerText.text = "00:00";
            }
            
            Time.timeScale = 0f;
            PlaySound(timeExpiredSound); 
        }
    }


    public IEnumerator CompleteTrial()
    {
        // Start the Firebase save but don't await it here
        var saveTask = SaveProgressToFirebase();
        
        yield return new WaitForSeconds(2f);
        
        if (teleportEffect != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CelebrationEffect();
            }
        }
        
        PlaySound(trialCompleteSound);
        yield return new WaitForSeconds(2f);

        // Optional: Wait for the save to complete if needed
        while (!saveTask.IsCompleted)
            yield return null;
            
        if (saveTask.IsFaulted)
            Debug.LogError("Save failed: " + saveTask.Exception);

        timerRunning = false;
        StopAllCoroutines();
        trialCompleteMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    // Separate async Task method for Firebase
   private async Task SaveProgressToFirebase()
{
    try 
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Player" + Random.Range(1000, 9999);
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }

        int coins = GameManager.Instance.currentTrial.currentCoins;
        int highestLevel = GameManager.Instance.currentTrial.trialNumber;
        
        await FirestoreUtility.SaveTrialCompletion(playerName, coins, highestLevel);
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Firebase save error: {e.Message}");
    }
}
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            float volume = clip == typingTickSound ? typingSoundVolume : 1f;
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private IEnumerator TypeText(string message, float activeFor, bool playNarration)
    {
        dialogText.text = "";
        lastTypingSoundTime = 0f;

        if (playNarration)
        {


            foreach (char letter in message.ToCharArray())
            {
                dialogText.text += letter;

                if (Time.time - lastTypingSoundTime >= typingSoundInterval)
                {
                    PlaySound(typingTickSound);
                    lastTypingSoundTime = Time.time;
                }

                yield return new WaitForSeconds(typingSpeed);
            }
        }
        else
        {
            dialogText.text = message;
         }

        if (activeFor > 0)
            {
                yield return new WaitForSeconds(activeFor);
                HideDialog();
            }
        
        typingCoroutine = null;
    }

    // Basic dialog - shows message and hides after specified time
    public void ShowDialog(string message, float activeFor = 5f, bool narration = false, bool showBuildingStats = false, string buildingName = null)
    {
        ShowDialog(message, activeFor, null, null, narration, showBuildingStats, buildingName);
    }

    public void ShowQuickUpdate(string message)
    {
        StartCoroutine(ShowQuickUpdateSequence(message));
    }
    
    // Dialog with action button - shows message with button, no auto-hide unless activeFor > 0
    public void ShowDialog(string message, float activeFor, string actionButtonText, System.Action onActionButtonClick, bool narration = false, bool showStats = false, string buildingName = null)
    {
        if (showStats && buildingStats != null && !string.IsNullOrEmpty(buildingName))
        {
            var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
            if (buildingInfo != null)
            {
                // Update coins and knowledge points in building stats
                if (coinsGainedBuildingStats != null)
                {
                    coinsGainedBuildingStats.text = "+" + buildingInfo.coinsGained.ToString();
                }
                if (kbGainedBuildingStats != null)
                {
                    kbGainedBuildingStats.text = "+" + buildingInfo.kbGained.ToString();
                }
            }
            buildingStats.SetActive(true);
        }
        else if (buildingStats != null)
        {
            buildingStats.SetActive(false);
        }

        if (dialogPanel != null && dialogText != null)
        {
            // If dialog is already active, stop the current one
            if (isDialogActive && dialogCoroutine != null)
            {
                StopCoroutine(dialogCoroutine);
                CleanupDialog();
            }

            dialogCoroutine = StartCoroutine(ShowDialogSequence(message, activeFor, actionButtonText, onActionButtonClick, narration));
        }
    }
    
    private IEnumerator ShowQuickUpdateSequence(string message)
    {
        if (quickUpdateText == null) yield break;

        quickUpdateText.text = message;
        quickUpdatePanel.SetActive(true);

        // Play collectable sound
        if (collectableSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectableSound);
        }

        // Animate popup effect
        yield return StartCoroutine(AnimatePanelPopup(quickUpdatePanel));

        yield return new WaitForSeconds(2f);

        quickUpdatePanel.SetActive(false);
    }
    private IEnumerator ShowDialogSequence(string message, float activeFor, string actionButtonText, System.Action onActionButtonClick, bool narration)
    {
        isDialogActive = true;
        currentDialogAction = onActionButtonClick;

        // Reset the keep AROS visible flag for new dialog sequences
        // Unless this is part of an unlock sequence (action button present)
        if (onActionButtonClick == null)
        {
            keepArosVisible = false;
        }

        try
        {
            dialogPanel.SetActive(true);

            // Setup action button if provided
            SetupActionButton(actionButtonText, onActionButtonClick);

            // Show AROS without animation if not already visible
            if (aros != null)
            {
                aros.SetActive(true);
            }

            // Use narration parameter to control whether dialog is narrated
            // When narration is false, skip narration features
            // This is a placeholder - implement actual narration control here

            typingCoroutine = StartCoroutine(TypeText(message, activeFor, narration));
            yield return typingCoroutine;
        }
        finally
        {
            CleanupDialog();
        }
    }
    
    private void SetupActionButton(string buttonText, System.Action onButtonClick)
    {
        if (dialogActionButton != null)
        {
            if (onButtonClick != null) // Show button if action is provided, text can be empty for icon-only buttons
            {
                // Show and setup the action button
                dialogActionButton.gameObject.SetActive(true);
                
                // Set button text (can be empty)
                if (dialogActionButtonText != null)
                {
                    dialogActionButtonText.text = buttonText ?? "";
                }
                
                // Clear any existing listeners and add the new one
                dialogActionButton.onClick.RemoveAllListeners();
                dialogActionButton.onClick.AddListener(() => {
                    onButtonClick?.Invoke();
                    HideDialog(); // Hide dialog after action
                });
            }
            else
            {
                // Hide the action button if no action is provided
                dialogActionButton.gameObject.SetActive(false);
            }
        }
    }
    
    private void CleanupDialog()
    {
        isDialogActive = false;
        dialogCoroutine = null;
        currentDialogAction = null;
        
        // Hide action button and clear listeners
        if (dialogActionButton != null)
        {
            dialogActionButton.gameObject.SetActive(false);
            dialogActionButton.onClick.RemoveAllListeners();
        }
    }
    
    public void HideDialog()
    {
        dialogPanel?.SetActive(false);
        
        // Only hide AROS if we're not in an unlock sequence
        if (!keepArosVisible && aros != null)
        {
            aros.SetActive(false);
        }
        
        // Clean up dialog state
        CleanupDialog();
    }
    
    public bool IsDialogActive()
    {
        return isDialogActive;
    }

    private IEnumerator RepeatObjective()
    {
        while (true)
        {
            yield return new WaitForSeconds(objectiveRepeatTime);
            //ShowObjective();
        }
    }

    public void ShowObjective()
    {
        string objectiveMessage = GameManager.Instance.currentTrial.GetProgressReport();
        ShowDialog(objectiveMessage, 10f);
    }

    private void CelebrationEffect()
    {
        Debug.Log("Playing celebration effect with multiple particle systems");
        if (teleportEffect == null)
        {
            Debug.LogWarning("Teleport effect not set in UIManager!");
            return;
        }

        // Use the unified celebration duration parameter
        float audioDuration = celebrationDuration;

        // Create a parent object to organize all particle effects
        GameObject effectsContainer = new GameObject("CelebrationEffects");
        Transform effectsParent = effectsContainer.transform;
        
        // Auto-cleanup when celebration finishes (add small buffer for particle fadeout)
        Destroy(effectsContainer, audioDuration + 0.5f);
        
        // Create 3 random points around the screen for celebration effects
        Vector3[] celebrationPoints = new Vector3[3];
        
        for (int point = 0; point < 3; point++)
        {
            // Generate random screen positions (viewport coordinates)
            float randomX = Random.Range(0.2f, 0.8f); // Avoid edges
            float randomY = Random.Range(0.2f, 0.8f); // Avoid edges
            celebrationPoints[point] = Camera.main.ViewportToWorldPoint(new Vector3(randomX, randomY, Camera.main.nearClipPlane + 5f));
        }
        
        // Create array of available effects
        ParticleSystem[] effects = { teleportEffect, celebrationEffect1, celebrationEffect2 };
        
        // Distribute effects across the 3 celebration points
        for (int i = 0; i < 3; i++)
        {
            // Use different effect for each celebration point
            ParticleSystem currentEffect = effects[i % effects.Length];
            if (currentEffect == null) continue;
            
            Vector3 basePosition = celebrationPoints[i];
            
            // Spawn multiple instances of each effect around each point
            int effectsPerPoint = i == 0 ? 5 : 4; // 5 + 4 + 4 = 13 total effects
            
            for (int j = 0; j < effectsPerPoint; j++)
            {
                // Add random offset around the celebration point
                Vector3 spawnPos = basePosition + new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    0f
                );
                
                GameObject effect = Instantiate(currentEffect.gameObject, spawnPos, Quaternion.identity, effectsParent);
                
                // Set the particle system to a higher sorting layer to appear above grid/UI
                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    // Stop the particle system first before modifying settings
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingLayerName = "Default";
                        renderer.sortingOrder = 4;
                    }
                    
                    // Adjust particle system duration to match audio length
                    var main = ps.main;
                    main.duration = audioDuration;
                    main.loop = false;
                    main.simulationSpeed = celebrationPlaybackSpeed; // Control playback speed
                    
                    // Start the particle system after configuring it
                    ps.Play();
                }
                
                Debug.Log($"Spawned {currentEffect.name} effect at celebration point {i} - position: {spawnPos}");
            }
        }
        
        Debug.Log($"Celebration effects will play for {audioDuration} seconds to match audio");
    }

    public void UpdateBuildingUI(string buildingName)
    {
        var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);

        if (buildingTitle != null)
        {
            buildingTitle.text = buildingInfo.displayName;
        }
        if (buildingDescription != null)
        {
            buildingDescription.text = buildingInfo.description;
        }
         if (buildingUnlockedMessage != null)
        {
            buildingUnlockedMessage.text = buildingInfo.unlockedMessage;
        }
         if (coinsGained != null)
                {
            coinsGained.text = "+" + buildingInfo.coinsGained.ToString();
                }
                 if (kbGained != null)
                {
            kbGained.text = "+" + buildingInfo.kbGained.ToString();
                }
        
        // Update NPC information from NPCSpawner
        if (NPCSpawner.Instance != null)
        {
            var npcInfo = NPCSpawner.Instance.GetFirstNPCForBuilding(buildingName);
            if (npcInfo != null)
            {
                if (npcTitle != null)
                {
                    npcTitle.text = !string.IsNullOrEmpty(npcInfo.npcName) ? npcInfo.npcName : npcInfo.npcTitle;
                }
                if (npcImage != null && npcInfo.npcImage != null)
                {
                    npcImage.sprite = npcInfo.npcImage;
                }
            }
        }
    }

    public void UpdateCoins(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
        }
    }

    public void UpdateKnowledgePoints(int knowledgePoints)
    {
        if (knowledgePointsText != null)
        {
            knowledgePointsText.text = knowledgePoints.ToString();
        }
    }

    public void PlayBuildingUnlockCelebration()
    {
        if (teleportEffect != null || celebrationEffect1 != null || celebrationEffect2 != null)
        {
            CelebrationEffect();
        }
        else
        {
            Debug.LogWarning("No celebration effects are set in UIManager!");
        }
        
        // Play celebration sound with controlled duration
        if (trialCompleteSound != null && audioSource != null)
        {
            // Stop any previous sound
            audioSource.Stop();
            
            // Play the sound and stop it after celebrationDuration
            audioSource.PlayOneShot(trialCompleteSound);
            StartCoroutine(StopAudioAfterDuration(celebrationDuration));
        }
    }
    
    private IEnumerator StopAudioAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    public float GetCelebrationDuration()
    {
        // Return the unified celebration duration
        return celebrationDuration;
    }

    public async Task UnlockBuilding(BuildingInteraction building)
    {
        OnBuildingUnlocked.Invoke(building);
        PlayBuildingUnlockCelebration();
        
        float celebrationDuration = GetCelebrationDuration();
        int delayMs = Mathf.RoundToInt(celebrationDuration * 1000f);
        await Task.Delay(delayMs);
    }

    public void HandlePreUnlock(BuildingInteraction building)
    {
        if (!string.IsNullOrEmpty(building.preUnlockMessage))
        {
            ShowDialog(building.preUnlockMessage, 5f);
        }
    }

    public async Task<bool> HandleBuildingUnlock(BuildingInteraction building)
    {
        // Set flag to keep AROS visible during unlock sequence
        keepArosVisible = true;
        
        // Play celebration
        OnBuildingUnlocked.Invoke(building);
        PlayBuildingUnlockCelebration();
        
        // Show AROS with jumping happy animation
        if (aros != null)
        {
            aros.SetActive(true);
            PlayArosAnimation("jumping-happy");
        }
        
        float celebrationDuration = GetCelebrationDuration();
        int delayMs = Mathf.RoundToInt(celebrationDuration * 1000f);
        await Task.Delay(delayMs);

        // Update UI elements using building data from JSON
        UpdateBuildingUI(building.buildingName);

        // Make sure AROS is still visible when showing unlock panel
        if (aros != null)
        {
            aros.SetActive(true);
        }

        // Show unlock panel
        building.ShowBuildingUnlockedPanel();
        
        // Unlock mapped gate for this building, if any (may set a pending scene change)
        UnlockGateForBuilding(building);
        
        // Wait for any additional UI (like users list) to finish
        await building.DisplayUsersWhoUnlocked();

        // Do not change scene here anymore; it will be handled on progress bar reveal
        return true;
    }

    public void UnlockGateForBuilding(BuildingInteraction building)
    {
        if (building == null) return;
        if (buildingGateMap == null || buildingGateMap.Count == 0)
        {
            if (buildingGatePairs != null && buildingGatePairs.Length > 0)
            {
                // Build map lazily if needed
                buildingGateMap = new Dictionary<BuildingInteraction, Gate>();
                foreach (var pair in buildingGatePairs)
                {
                    if (pair != null && pair.building != null && pair.gate != null && !buildingGateMap.ContainsKey(pair.building))
                    {
                        buildingGateMap.Add(pair.building, pair.gate);
                    }
                }
            }
        }

        if (buildingGateMap != null && buildingGateMap.TryGetValue(building, out var gate) && gate != null)
        {
            // First: unlock the mapped gate
            //gate.UnlockGate();
            Debug.Log($"UIManager: Unlocked mapped gate '{gate.gameObject.name}' for building '{building.buildingName}'.");

            // Then: switch game stage based on gate name '1','2','3' AFTER unlocking
            var gateName = gate.gameObject.name?.Trim();
            if (!string.IsNullOrEmpty(gateName))
            {
                RamRoutes.Model.Stage? nextStage = null;
                if (gateName == "1") nextStage = RamRoutes.Model.Stage.EasternCampus;
                else if (gateName == "2") nextStage = RamRoutes.Model.Stage.FirstStreet;
                else if (gateName == "3") nextStage = RamRoutes.Model.Stage.Pedmall;

                if (nextStage.HasValue)
                {
                    var current = GameStageService.LoadStageFromPrefs();
                    var gs = GameStage.FromArea(nextStage.Value);

                    // Update UI
                    SetCurrentStageText(gs);

                    // Persist locally immediately and remote in background
                    GameStageService.SaveStageToPrefs(gs);
                    _ = GameStageService.SaveStageToFirestore(gs);

                    // Update background music for the new stage
                    // SetBackgroundMusicForStage(nextStage.Value);

                    // If stage actually changed, mark for scene change after unlock flow completes
                    if (current == null || current.area != nextStage.Value)
                    {
                        Debug.Log($"UIManager: Stage changed {current?.area} -> {gs.area} after gate unlock. Will load Onboarding on progress bar reveal.");
                        pendingSceneAfterUnlock = true;
                    }
                    else
                    {
                        Debug.Log($"UIManager: Stage set to {gs.area} based on gate '{gateName}' after unlocking.");
                    }
                }
            }
        }
        else
        {
            Debug.Log($"UIManager: No mapped gate found for building '{building.buildingName}'.");
        }
    }

    private void UpdateProgressBar()
    {
        if (progressBarImages != null && buildingsUnlockedCount < progressBarImages.Length)
        {
            GameObject progressImage = progressBarImages[buildingsUnlockedCount];
            if (progressImage != null)
            {
                progressImage.SetActive(true);
                Debug.Log($"Activated progress bar image {buildingsUnlockedCount + 1}");
            }
            buildingsUnlockedCount++;
        }
    }

    public async Task UpdateProgressBarOnReveal()
    {
        UpdateProgressBar();

        // Refresh points display when progress bar is updated
        var userService = new UserService();
        string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            int coins = await userService.GetUserCoins(userId);
            int knowledgePoints = await userService.GetUserKnowledgePoints(userId);
            UpdateCoins(coins);
            UpdateKnowledgePoints(knowledgePoints);
        }

        // If the last building was just unlocked, move to Terminal stage
        bool isFinalUnlock = buildingsUnlockedCount >= 7 || (progressBarImages != null && buildingsUnlockedCount >= progressBarImages.Length);
        if (isFinalUnlock)
        {
            var current = GameStageService.LoadStageFromPrefs();
            var target = GameStage.FromArea(Stage.Terminal);

            // Update UI and persist stage to Terminal
            SetCurrentStageText(target);
            GameStageService.SaveStageToPrefs(target);
            _ = GameStageService.SaveStageToFirestore(target);

            // Update background music for Terminal stage
            SetBackgroundMusicForStage(Stage.Terminal);

            // Schedule scene change to Onboarding after unlock panel closes
            pendingSceneAfterUnlock = true;
            readyToLeaveAfterPanelClose = true;
            Debug.Log($"UIManager: Final building unlocked (count={buildingsUnlockedCount}). Stage set to {target.area}. Will load Onboarding after panel closes.");
            return;
        }

        // If a stage change was requested earlier (e.g., via gate mapping), mark ready but wait until unlock panel is closed
        if (pendingSceneAfterUnlock)
        {
            readyToLeaveAfterPanelClose = true;
            Debug.Log("UIManager: Progress bar revealed after unlock. Waiting for unlock panel to close before changing scene.");
        }
    }

    // Call this when the building unlock panel is closed by the user
    public void OnUnlockPanelClosed()
    {
        // Start the delay timer for scene transition
        StartSceneTransitionDelay();

        // If conditions are not yet met, keep flags; we'll try again when they are
        // Avoid resetting readiness here to prevent losing intent
    }

    private void StartSceneTransitionDelay()
    {
        // Cancel any existing delay coroutine
        if (sceneTransitionDelayCoroutine != null)
        {
            StopCoroutine(sceneTransitionDelayCoroutine);
        }
        
        // Start new delay coroutine
        sceneTransitionDelayCoroutine = StartCoroutine(SceneTransitionDelayCoroutine());
    }
    
    private IEnumerator SceneTransitionDelayCoroutine()
    {
        Debug.Log($"UIManager: Starting {sceneTransitionDelay} second delay before scene transition");
        yield return new WaitForSeconds(sceneTransitionDelay);
        
        // After delay, try to proceed with pending scene change
        TryProceedPendingScene();
        
        // Clear the coroutine reference
        sceneTransitionDelayCoroutine = null;
    }

    private async Task InitializeProgressBar()
    {
        try
        {
            var buildingService = new UnlockedBuildingService();
            var unlockedBuildings = await buildingService.RetrieveUnlockedBuildings();
            
            // Get current user's unlocked buildings
            string userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "unknown";
            var userUnlockedBuildings = unlockedBuildings.Where(b => b.userId == userId).ToList();
            
            // Set the buildings unlocked count and activate corresponding progress images
            buildingsUnlockedCount = userUnlockedBuildings.Count;
            
            if (progressBarImages != null)
            {
                for (int i = 0; i < buildingsUnlockedCount && i < progressBarImages.Length; i++)
                {
                    if (progressBarImages[i] != null)
                    {
                        progressBarImages[i].SetActive(true);
                    }
                }
            }
            
            // NEW: Open gates for any buildings already unlocked
            var unlockedNames = new HashSet<string>(userUnlockedBuildings.Select(b => b.buildingName));
            OpenMappedGatesForUnlocked(unlockedNames);
            
            Debug.Log($"Initialized progress bar with {buildingsUnlockedCount} unlocked buildings");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize progress bar: {ex.Message}");
        }
    }

    // Unlock any mapped gates for the provided set of unlocked building names
    private void OpenMappedGatesForUnlocked(HashSet<string> unlockedBuildingNames)
    {
        if (unlockedBuildingNames == null || unlockedBuildingNames.Count == 0) return;
        if (buildingGatePairs == null || buildingGatePairs.Length == 0) return;

        foreach (var pair in buildingGatePairs)
        {
            if (pair == null || pair.building == null || pair.gate == null) continue;

            string bName = pair.building.buildingName;
            if (!string.IsNullOrEmpty(bName) && unlockedBuildingNames.Contains(bName))
            {
                if (!pair.gate.IsUnlocked())
                {
                    // Silent to avoid dialog spam at startup
                    pair.gate.UnlockGateSilently();
                    Debug.Log($"UIManager: Restored gate '{pair.gate.gameObject.name}' for unlocked building '{bName}' (silent).");
                }
            }
        }
    }

    public IEnumerator AnimatePanelPopup(GameObject panel)
    {
        if (panel == null) yield break;

        // Save original scale
        Vector3 originalScale = panel.transform.localScale;
        
        // Start with zero scale
        panel.transform.localScale = Vector3.zero;
        
        // Animate to full size with a slight overshoot
        float duration = 0.4f;
        float elapsed = 0f;
        
        // First phase - grow quickly to slightly larger than original
        while (elapsed < duration * 0.8f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration * 0.8f);
            // Use easeOutBack-like effect for a bouncy feel
            float overshoot = Mathf.Lerp(0, 1.1f, progress);
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale * overshoot, progress);
            yield return null;
        }
        
        // Second phase - settle back to original size
        float secondPhaseDuration = duration * 0.2f;
        elapsed = 0f;
        Vector3 overshotScale = panel.transform.localScale;
        
        while (elapsed < secondPhaseDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / secondPhaseDuration;
            panel.transform.localScale = Vector3.Lerp(overshotScale, originalScale, progress);
            yield return null;
        }
        
        // Ensure we end at exactly the original scale
    }

    // Simple method to hide AROS without animation
    public void HideAros()
    {
        if (aros != null)
        {
            aros.SetActive(false);
        }
    }
    
    // Method to reset AROS visibility control and optionally hide it
    public void ResetArosVisibility(bool hideAros = true)
    {
        keepArosVisible = false;
        if (hideAros && aros != null)
        {
            aros.SetActive(false);
        }
    }
    
    // Method to play AROS animation using Animator
    private void PlayArosAnimation(string animationName)
    {
        if (aros != null)
        {
            Animator animator = aros.GetComponent<Animator>();
            if (animator != null && !string.IsNullOrEmpty(animationName))
            {
                // Play animation directly by state name
                // animator.Play(animationName, 0, 0f);
                animator.SetTrigger("jumping-happy");
                Debug.Log($"Playing AROS animation: {animationName}");
            }
            else
            {
                Debug.LogWarning($"AROS Animator component not found or animation name is empty: {animationName}");
            }
        }
    }

    public IEnumerator PlayTeleportEffect(Vector3 worldPosition)
    {
        if (teleportEffect != null)
        {
            // Position the teleport effect at the specified world position
            teleportEffect.transform.position = worldPosition;
            
            // Play the teleport effect
            teleportEffect.Play();
            
            Debug.Log($"Playing teleport effect at position: {worldPosition}");
            
            // Wait for a short duration to let the effect play
            yield return new WaitForSeconds(2f);
        }
        else
        {
            Debug.LogWarning("Teleport effect not assigned in UIManager!");
        }
    }
    
    // Mobile NPC Interaction Methods
    public void RegisterNPCForMobileInteraction(NpcAutoMovement npc)
    {
        if (npc == null)
        {
            Debug.LogWarning("UIManager: Cannot register null NPC for mobile interaction");
            return;
        }
        
        // Store reference to the current NPC
        currentInteractingNPC = npc;
        
        // Setup mobile button listener if button exists
        if (mobileInteractButton != null)
        {
            // Clear any existing listeners to prevent multiple calls
            mobileInteractButton.onClick.RemoveAllListeners();
            
            // Add listener that sets the mobile interaction flag on the NPC
            mobileInteractButton.onClick.AddListener(() => {
                if (currentInteractingNPC != null)
                {
                    currentInteractingNPC.mobileInteractPressed = true;
                    Debug.Log($"UIManager: Mobile interact pressed for NPC {currentInteractingNPC.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning("UIManager: Mobile interact pressed but no NPC registered");
                }
            });
            
            Debug.Log($"UIManager: Registered NPC {npc.gameObject.name} for mobile interaction");
        }
        else
        {
            Debug.LogWarning("UIManager: Mobile interact button not assigned!");
        }
    }
    
    public void UnregisterNPCForMobileInteraction(NpcAutoMovement npc)
    {
        if (currentInteractingNPC == npc)
        {
            // Clear the NPC reference
            currentInteractingNPC = null;
            
            // Hide the button
            HideMobileInteractButton();
            
            // Clear button listeners
            if (mobileInteractButton != null)
            if (mobileInteractButton != null)
            {
                mobileInteractButton.onClick.RemoveAllListeners();
            }
            
            Debug.Log($"UIManager: Unregistered NPC {npc?.gameObject.name} from mobile interaction");
        }
    }
    
    public void ShowMobileInteractButton()
    {
        if (mobileInteractButton != null)
        {
            mobileInteractButton.gameObject.SetActive(true);
            Debug.Log("UIManager: Showing mobile interact button");
        }
        else
        {
            Debug.LogWarning("UIManager: Cannot show mobile interact button - not assigned!");
        }
    }
    
    public void HideMobileInteractButton()
    {
        if (mobileInteractButton != null)
        {
            mobileInteractButton.gameObject.SetActive(false);
            Debug.Log("UIManager: Hiding mobile interact button");
        }
    }

    // Allows gameplay to announce entering/leaving a building viewing state.
    // While active, pending onboarding scene transitions are deferred.
    public void SetBuildingViewingMode(bool active, string buildingName = null)
    {
        isInBuildingViewingMode = active;
        currentViewedBuilding = active ? buildingName : null;
        if (!active)
        {
            // Hide current users panel when leaving building view
            HideCurrentUsersPanel();
            
            // Reattempt any pending transition when viewing ends (but only if delay has passed)
            if (sceneTransitionDelayCoroutine == null) // Delay has completed
            {
                TryProceedPendingScene();
            }
        }
    }

    // Centralized gate for onboarding transition conditions
    private void TryProceedPendingScene()
    {
        if (pendingSceneAfterUnlock && readyToLeaveAfterPanelClose && !isInBuildingViewingMode)
        {
            pendingSceneAfterUnlock = false;
            readyToLeaveAfterPanelClose = false;
            
            // Play fade transition effect before changing scene
            StartCoroutine(PlaySceneTransitionEffect());
        }
    }
    
    private IEnumerator PlaySceneTransitionEffect()
    {
        Debug.Log("UIManager: Starting scene transition effect");
        
        // Fade in overlay if assigned
        if (fadeOverlay != null)
        {
            // Make sure overlay is active and starts transparent
            fadeOverlay.gameObject.SetActive(true);
            Color startColor = fadeOverlay.color;
            startColor.a = 0f;
            fadeOverlay.color = startColor;
            
            // Fade to opaque
            float elapsedTime = 0f;
            while (elapsedTime < fadeEffectDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeEffectDuration;
                
                Color currentColor = fadeOverlay.color;
                currentColor.a = Mathf.Lerp(0f, 1f, progress);
                fadeOverlay.color = currentColor;
                
                yield return null;
            }
            
            // Ensure fully opaque
            Color finalColor = fadeOverlay.color;
            finalColor.a = 1f;
            fadeOverlay.color = finalColor;
        }
        else
        {
            // If no fade overlay, just wait for the effect duration
            yield return new WaitForSeconds(fadeEffectDuration);
        }
        
        Debug.Log("UIManager: Fade effect complete, loading Onboarding scene");
        SceneManager.LoadScene("Onboarding");
    }
    
    private void ResetFadeOverlay()
    {
        if (fadeOverlay != null)
        {
            // Make overlay transparent and inactive
            Color color = fadeOverlay.color;
            color.a = 0f;
            fadeOverlay.color = color;
            fadeOverlay.gameObject.SetActive(false);
            Debug.Log("UIManager: Reset fade overlay to transparent and inactive");
        }
    }

    // Public method to be called from BuildingInteraction
    public async void DisplayCurrentUsersForBuilding(string buildingName)
    {
        // Only display if we're in building viewing mode for this building
        if (!isInBuildingViewingMode || currentViewedBuilding != buildingName)
        {
            Debug.Log($"Not displaying current users: viewing mode={isInBuildingViewingMode}, current building={currentViewedBuilding}, requested building={buildingName}");
            return;
        }

        // Check if data is already loaded for this building
        bool isLoaded = currentUsersLoadedPerBuilding.ContainsKey(buildingName) && currentUsersLoadedPerBuilding[buildingName];
        
        if (!isLoaded)
        {
            await FetchCurrentUsersForBuilding(buildingName);
        }
        
        DisplayCurrentUsersUI(buildingName);
    }

    private async Task FetchCurrentUsersForBuilding(string buildingName)
    {
        try
        {
            var userService = new UserService();
            
            // Get users currently in this building using a targeted query
            var buildingUsers = await userService.GetUsersInBuilding(buildingName);
            cachedCurrentUsersPerBuilding[buildingName] = buildingUsers;
            Debug.Log($"UIManager: Fetching current users for building {buildingName}: {buildingUsers.Count} users found");

            currentUsersLoadedPerBuilding[buildingName] = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UIManager: Failed to fetch current users for building {buildingName}: {ex.Message}");
            cachedCurrentUsersPerBuilding[buildingName] = new List<User>();
            currentUsersLoadedPerBuilding[buildingName] = true;
        }
    }

    private void DisplayCurrentUsersUI(string buildingName)
    {
        if (currentUsersContentParent == null || currentUserPrefab == null)
        {
            Debug.LogError("UIManager: Current users UI components not set up!");
            return;
        }

        // Clear previous entries
        foreach (Transform child in currentUsersContentParent)
        {
            Destroy(child.gameObject);
        }

        // Get the cached users for this specific building
        List<User> buildingUsers = null;
        if (cachedCurrentUsersPerBuilding.ContainsKey(buildingName))
        {
            buildingUsers = cachedCurrentUsersPerBuilding[buildingName];
        }

        if (buildingUsers != null && buildingUsers.Count > 0 && currentUsersPanel != null)
        {
            currentUsersPanel.SetActive(true);
            // Animate the panel appearing
            StartCoroutine(AnimatePanelPopup(currentUsersPanel));

            foreach (var user in buildingUsers)
            {
                GameObject userGO = Instantiate(currentUserPrefab, currentUsersContentParent);
                
                // Get the single text component for user name
                Text nameText = userGO.GetComponentInChildren<Text>();
                
                if (nameText != null)
                {
                    string displayName = !string.IsNullOrEmpty(user.name) ? user.name : "Anonymous User";
                    nameText.text = displayName;
                    
                    if (nameText.supportRichText)
                    {
                        nameText.text = $"<b>{displayName}</b>";
                    }
                }
                else
                {
                    Debug.LogError("UIManager: No Text component found in current user prefab!");
                }
            }
        }
        else if (currentUsersPanel != null)
        {
            currentUsersPanel.SetActive(false);
        }
    }

    public void HideCurrentUsersPanel()
    {
        if (currentUsersPanel != null)
        {
            currentUsersPanel.SetActive(false);
        }
    }
}
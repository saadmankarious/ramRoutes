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
        { Stage.Pedmall, 0 },
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
    
    [Header("User Avatar")]
    public Image userAvatarImage;   // Reference to the user avatar image component
    public Sprite defaultAvatarSprite; // Default avatar sprite (rank 0 or undefined)
    public Sprite rank1AvatarSprite; // Rank 1 avatar sprite (0-999 combined points)
    public Sprite rank2AvatarSprite; // Rank 2 avatar sprite (1000-1999 combined points)
    public Sprite rank3AvatarSprite; // Rank 3 avatar sprite (2000+ combined points)

    [Header("Current Users Display")]
    public GameObject currentUsersPanel;
    public Transform currentUsersContentParent;
    public GameObject currentUserPrefab;
    public ScrollRect currentUsersScrollRect;

    [Header("Rank Up Panel")]
    public GameObject rankUpPanel;
    public Text rankUpText;
    public Button rankUpCloseButton;

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
    
    // Dictionary to track active popup animations to prevent conflicts
    private Dictionary<GameObject, Coroutine> activePopupAnimations = new Dictionary<GameObject, Coroutine>();
    private Coroutine autoScrollCoroutine;
    
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
    [SerializeField] private float sceneTransitionDelay = 2f;
    
    [Header("Scene Transition Effects")]
    [Tooltip("UI Image component to use as fade overlay during scene transitions.")]
    [SerializeField] private Image fadeOverlay;
    [Tooltip("Duration of the fade effect before scene transition.")]
    [SerializeField] private float fadeEffectDuration = 2f;

    [System.Serializable]
    public class BuildingGatePair
    {
        public BuildingInteraction[] buildings; // Multiple buildings required to unlock this gate
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
            return;
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
        // _ = InitializeProgressBar();
        
        // Initialize game stage to TC if not already set
        _ = InitializeGameStage();
        
        // Reset fade overlay to be transparent and inactive at start
        ResetFadeOverlay();
    }

    /// <summary>
    /// Called when a scene is loaded - reinitialize components if this UIManager persisted from another scene
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only reinitialize if this is an additive load or if we're loading a new scene
        if (mode == LoadSceneMode.Single)
        {
            Debug.Log($"UIManager: Scene '{scene.name}' loaded, checking for component reinitialization");
            
            // Find and reconnect UI components that might have been lost during scene transition
            RefreshUIReferences();
        }
    }

    /// <summary>
    /// Public method to manually refresh UI connections - can be called from Inspector or other scripts
    /// </summary>
    public void RefreshAllUIConnections()
    {
        RefreshUIReferences();
        Debug.Log("UIManager: Manually refreshed all UI connections");
    }

    /// <summary>
    /// Refreshes UI component references in case they were lost during scene transitions
    /// </summary>
    private void RefreshUIReferences()
    {
        // Try to find the pause menu button if it's not connected
        if (gamePauseMenu == null)
        {
            GameObject pauseMenuGO = GameObject.Find("GamePauseMenu");
            if (pauseMenuGO != null)
            {
                gamePauseMenu = pauseMenuGO;
                Debug.Log("UIManager: Reconnected gamePauseMenu reference");
            }
        }
        
        // Look for pause button and reconnect the TogglePauseMenu method if needed
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            // Check if this looks like a pause button (by name or parent name)
            string buttonName = button.gameObject.name.ToLower();
            string parentName = button.transform.parent?.name?.ToLower() ?? "";
            
            if (buttonName.Contains("pause") || parentName.Contains("pause") || 
                buttonName.Contains("menu") || buttonName.Contains("settings"))
            {
                // Check if the button has any listeners for TogglePauseMenu
                bool hasToggleListener = false;
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentMethodName(i) == "TogglePauseMenu")
                    {
                        hasToggleListener = true;
                        break;
                    }
                }
                
                // If no TogglePauseMenu listener found, add it programmatically
                if (!hasToggleListener)
                {
                    // Remove any existing runtime listeners for this method first to avoid duplicates
                    button.onClick.RemoveListener(TogglePauseMenu);
                    // Add the listener
                    button.onClick.AddListener(TogglePauseMenu);
                    Debug.Log($"UIManager: Added TogglePauseMenu listener to button '{button.gameObject.name}'");
                }
            }
        }
    }

    /// <summary>
    /// Cleanup when UIManager is destroyed
    /// </summary>
    private void OnDestroy()
    {
        // Stop all active popup animations to prevent coroutine errors
        if (activePopupAnimations != null)
        {
            foreach (var animationPair in activePopupAnimations)
            {
                if (animationPair.Value != null)
                {
                    StopCoroutine(animationPair.Value);
                }
            }
            activePopupAnimations.Clear();
        }
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

    /// <summary>
    /// Updates the user's avatar sprite based on their rank.
    /// </summary>
    /// <param name="rank">The user's current rank</param>
    public void UpdateUserAvatar(int rank)
    {
        // Verify we have a user avatar image component
        if (userAvatarImage == null)
        {
            Debug.LogWarning("User avatar image component not assigned in UIManager");
            return;
        }

        // Select the appropriate sprite based on rank
        Sprite selectedSprite;

        switch (rank)
        {
            case 1:
                selectedSprite = rank1AvatarSprite;
                break;
            case 2:
                selectedSprite = rank2AvatarSprite;
                break;
            case 3:
                selectedSprite = rank3AvatarSprite ?? rank2AvatarSprite; // Use rank2 sprite if rank3 isn't defined
                break;
            case 0:
            default:
                selectedSprite = defaultAvatarSprite;
                break;
        }

        // If the selected sprite is null, use the default sprite
        if (selectedSprite == null)
        {
            Debug.LogWarning($"Avatar sprite for rank {rank} is not assigned. Using default sprite.");
            selectedSprite = defaultAvatarSprite;
        }

        // Update the avatar image
        userAvatarImage.sprite = selectedSprite;

        Debug.Log($"Updated user avatar to rank {rank} sprite");
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
        var userRank = PlayerPrefs.GetInt("UserRank", 0);
        UpdateUserAvatar(userRank);

    }
    
    /// <summary>
    /// Public method to check for rank increases after user gains points.
    /// This should be called whenever the user's points are updated in the game.
    /// </summary>
    public async Task CheckAndUpdateUserRank()
    {
        try 
        {
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("UIManager: Cannot check rank - no user ID");
                return;
            }

            var userService = new RamRoutes.Services.UserService();
            int coins = await userService.GetUserCoins(userId);
            int knowledgePoints = await userService.GetUserKnowledgePoints(userId);
            int totalPoints = coins + knowledgePoints;
            
            // Update UI with current points
            UpdateCoins(coins);
            UpdateKnowledgePoints(knowledgePoints);
            
            // Get previously saved rank for comparison
            int previousRank = PlayerPrefs.GetInt("UserRank", 1);

            // Calculate current rank and update avatar (this also persists the new rank)
            GetUserAvatarBasedOnPoints(coins, knowledgePoints, true);

            // Get the newly saved rank
            int currentRank = userService.CalculateUserRank(coins, knowledgePoints);

            // Check if rank has increased and show rank up panel if so
            if (currentRank > previousRank)
            {
                ShowRankUpPanel(currentRank);
                Debug.Log($"Rank increased from {previousRank} to {currentRank}! (Total points: {totalPoints})");
            }
            
            // Update the username display which includes the avatar
            UpdateUsernameAndHall();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to check and update user rank: {ex.Message}");
        }
    }

    // private async Task GetUserPoints()
    // {
    //     try 
    //     {
    //         string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
    //         if (!string.IsNullOrEmpty(userId))
    //         {
    //             var userService = new RamRoutes.Services.UserService();
    //             int coins = await userService.GetUserCoins(userId);
    //             int knowledgePoints = await userService.GetUserKnowledgePoints(userId);
                
    //             UpdateCoins(coins);
    //             UpdateKnowledgePoints(knowledgePoints);
                
    //             Debug.Log($"Retrieved user coins: {coins}, knowledge points: {knowledgePoints}");
    //         }
    //     }
    //     catch (System.Exception ex)
    //     {
    //         Debug.LogError($"Failed to get user points: {ex.Message}");
    //     }
    // }

    /// <summary>
    /// Gets user points from Firebase, updates UI, persists current rank, and checks for rank increases
    /// </summary>
    private async Task InitializeUserRankSystem()
    {
        try 
        {
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("UIManager: Cannot initialize rank system - no user ID");
                return;
            }

            var userService = new RamRoutes.Services.UserService();
            int coins = await userService.GetUserCoins(userId);
            int knowledgePoints = await userService.GetUserKnowledgePoints(userId);
            int totalPoints = coins + knowledgePoints;
            
            // Update UI
            UpdateCoins(coins);
            UpdateKnowledgePoints(knowledgePoints);
            
            // Get previously saved rank for comparison
            int previousRank = PlayerPrefs.GetInt("UserRank", 1);

            // Calculate current rank and update avatar (this also persists the new rank)
            if(userAvatarImage != null)
            {
                userAvatarImage.sprite = GetUserAvatarBasedOnPoints(coins, knowledgePoints, true); // reset to default while loading
            }

            // Get the newly saved rank
            int currentRank = userService.CalculateUserRank(coins, knowledgePoints);

            // Check if rank has increased and show rank up panel if so
            if (currentRank > previousRank)
            {
                ShowRankUpPanel(currentRank);
                Debug.Log($"Rank increased from {previousRank} to {currentRank}! (Total points: {totalPoints})");
            }
            else
            {
                Debug.Log($"Rank initialized: {currentRank} (Previous: {previousRank}, Total points: {totalPoints})");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize user rank system: {ex.Message}");
        }
    }

    private IEnumerator Start()
    {
        while (GameManager.Instance == null || GameManager.Instance.currentTrial == null)
        {
            yield return null;
        }

        // Check if in Terminal stage and hide tagged UI elements
        HideTerminalStageElements();

        OnTrialComplete.AddListener(() => StartCoroutine(CompleteTrial()));
        OnTimeExpired.AddListener(TimeUp);

        // Initialize user rank system (gets Firebase data, updates UI, persists rank, checks for increases)
        _ = InitializeUserRankSystem();
        
        // Update username and hall display
        UpdateUsernameAndHall();

        // Start countdown if current stage has a time limit > 0
        var currentStage = GameStageService.LoadStageFromPrefs();
        if (currentStage != null && stageTimeLimits.TryGetValue(currentStage.area, out int stageLimit) && stageLimit > 0)
        {
            StartCountdown(timerText, stageLimit);
        }

        // Initialize progress bar based on unlocked buildings
        // _ = InitializeProgressBar();

        // Setup background music for current stage
        if (currentStage != null)
        {
            SetBackgroundMusicForStage(currentStage.area);
        }

        // Initialize building-gate mapping
        InitializeBuildingGateMapping();
        
        // Reset the fade overlay if it exists
        ResetFadeOverlay();
    }

    /// <summary>
    /// Hides all GameObjects tagged with "GoneOnTerminalStage" when in Terminal stage
    /// </summary>
    private void HideTerminalStageElements()
    {
        try
        {
            // Check if we're in Terminal stage
            var currentStage = GameStageService.LoadStageFromPrefs();
            if (currentStage != null && currentStage.area == Stage.Terminal)
            {
                // Find all GameObjects with the "GoneOnTerminalStage" tag
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("GoneOnTerminalStage");
                
                Debug.Log($"UIManager: Found {taggedObjects.Length} objects with 'GoneOnTerminalStage' tag");
                
                // Hide all found objects
                foreach (GameObject obj in taggedObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                        Debug.Log($"UIManager: Hidden '{obj.name}' due to Terminal stage");
                    }
                }
                
                Debug.Log($"UIManager: Hidden {taggedObjects.Length} UI elements for Terminal stage");
            }
            else
            {
                Debug.Log("UIManager: Not in Terminal stage, keeping all tagged elements visible");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UIManager: Error hiding Terminal stage elements: {ex.Message}");
        }
    }

    /// <summary>
    /// Initializes the building-gate mapping dictionary from the configured pairs
    /// </summary>
    private void InitializeBuildingGateMapping()
    {
        if (buildingGatePairs != null && buildingGatePairs.Length > 0)
        {
            buildingGateMap = new Dictionary<BuildingInteraction, Gate>();
            foreach (var pair in buildingGatePairs)
            {
                if (pair != null && pair.buildings != null && pair.gate != null)
                {
                    // Map each building in the array to the same gate
                    foreach (var building in pair.buildings)
                    {
                        if (building != null && !buildingGateMap.ContainsKey(building))
                        {
                            buildingGateMap.Add(building, pair.gate);
                            Debug.Log($"UIManager: Mapped building '{building.buildingName}' to gate '{pair.gate.gameObject.name}'");
                        }
                    }
                }
            }
            Debug.Log($"UIManager: Initialized building-gate mapping with {buildingGateMap.Count} pairs");
        }
        else
        {
            buildingGateMap = new Dictionary<BuildingInteraction, Gate>();
            Debug.Log("UIManager: No building-gate pairs configured");
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

        // Get NPC spawner in another way
        var npcSpawner = FindObjectOfType<NPCSpawner>();
        if (npcSpawner != null)
        {
            var npcInfo = npcSpawner.GetFirstNPCForBuilding(buildingName);
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

    public async Task UpdateProgressBarOnReveal(string buildingName)
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


    public IEnumerator AnimatePanelPopup(GameObject panel)
    {
        if (panel == null) yield break;

        // Stop any existing animation for this specific panel
        if (activePopupAnimations.ContainsKey(panel))
        {
            if (activePopupAnimations[panel] != null)
            {
                StopCoroutine(activePopupAnimations[panel]);
            }
            activePopupAnimations.Remove(panel);
        }
        
        // Track this animation
        activePopupAnimations[panel] = StartCoroutine(AnimatePanelPopupInternal(panel));
        
        yield return activePopupAnimations[panel];
        
        // Clean up tracking when animation completes
        if (activePopupAnimations.ContainsKey(panel))
        {
            activePopupAnimations.Remove(panel);
        }
    }
    
    private IEnumerator AnimatePanelPopupInternal(GameObject panel)
    {
        if (panel == null) yield break;

        // Always use (1,1,1) as the target scale to prevent accumulating scale issues
        Vector3 targetScale = Vector3.one;
        
        // Start with zero scale
        panel.transform.localScale = Vector3.zero;
        
        // Animate to full size with a slight overshoot
        float duration = 0.4f;
        float elapsed = 0f;
        
        // First phase - grow quickly to slightly larger than target
        while (elapsed < duration * 0.8f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration * 0.8f);
            // Use easeOutBack-like effect for a bouncy feel
            float overshoot = Mathf.Lerp(0, 1.1f, progress);
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale * overshoot, progress);
            yield return null;
        }
        
        // Second phase - settle back to target size
        float secondPhaseDuration = duration * 0.2f;
        elapsed = 0f;
        Vector3 overshotScale = panel.transform.localScale;
        
        while (elapsed < secondPhaseDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / secondPhaseDuration;
            panel.transform.localScale = Vector3.Lerp(overshotScale, targetScale, progress);
            yield return null;
        }
        
        // Ensure we end at exactly the target scale
        panel.transform.localScale = targetScale;
    }

    /// <summary>
    /// Resets a panel's scale to (1,1,1) - useful for fixing scale issues
    /// </summary>
    /// <param name="panel">The panel to reset</param>
    public void ResetPanelScale(GameObject panel)
    {
        if (panel != null)
        {
            // Stop any active animation for this panel first
            if (activePopupAnimations.ContainsKey(panel))
            {
                if (activePopupAnimations[panel] != null)
                {
                    StopCoroutine(activePopupAnimations[panel]);
                }
                activePopupAnimations.Remove(panel);
            }
            
            // Reset to normal scale
            panel.transform.localScale = Vector3.one;
            Debug.Log($"Reset scale for panel: {panel.name}");
        }
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
        
        // Hide/Show objects tagged "StatsToHide" based on building viewing mode
       ToggleStatsToHideObjects(!active); // Hide when in building view mode (active = true)
        
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

    /// <summary>
    /// Toggle visibility of objects tagged "HideOnBuildingView"
    /// </summary>
    /// <param name="show">True to show objects, false to hide them</param>
    private void ToggleStatsToHideObjects(bool show)
    {
        // Use Resources.FindObjectsOfTypeAll to find both active and inactive GameObjects
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        List<GameObject> statsObjects = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.CompareTag("HideOnBuildingView") && obj.scene.IsValid())
            {
                statsObjects.Add(obj);
            }
        }
        
        foreach (GameObject obj in statsObjects)
        {
            if (obj != null)
            {
                obj.SetActive(show);
            }
        }
        
        Debug.Log($"UIManager: {(show ? "Showing" : "Hiding")} {statsObjects.Count} objects tagged 'HideOnBuildingView'");
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

        // Fetch and display live data from Firebase
        await DisplayCurrentUsersUI(buildingName);
    }

    private async Task DisplayCurrentUsersUI(string buildingName)
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

        // Get live user data from Firebase
        List<User> buildingUsers = null;
        try
        {
            var userService = new UserService();
            buildingUsers = await userService.GetUsersInBuildingWithPoints(buildingName);
            Debug.Log($"UIManager: Fetched {buildingUsers.Count} users live from Firebase for building {buildingName}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UIManager: Failed to fetch live users for building {buildingName}: {ex.Message}");
            buildingUsers = new List<User>();
        }

        if (buildingUsers != null && buildingUsers.Count > 0 && currentUsersPanel != null)
        {
            currentUsersPanel.SetActive(true);
            // Animate the panel appearing
            StartCoroutine(AnimatePanelPopup(currentUsersPanel));

            foreach (var user in buildingUsers)
            {
                GameObject userGO = Instantiate(currentUserPrefab, currentUsersContentParent);
                   ButtonHandler rsvpButtonHandler = userGO.GetComponentInChildren<ButtonHandler>();

            if (rsvpButtonHandler != null)
            {
                rsvpButtonHandler.Initialize("data", () => ChatWithFriend(user));
            }
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
                    if (user.userId == FirebaseAuth.DefaultInstance.CurrentUser?.UserId)
                    {
                        nameText.text += " (You)";
                     
                    }
                }
                else
                {
                    Debug.LogError("UIManager: No Text component found in current user prefab!");
                }

                // Display rank frame on the Image component within the "profile" object
                Transform profileTransform = userGO.transform.Find("profile");
                if (profileTransform != null)
                {
                    Image avatarImage = profileTransform.GetComponent<Image>();
                    if (avatarImage != null)
                    {
                        Sprite rankSprite = GetUserAvatarBasedOnPoints(user.coins, user.knowledgePoints);
                        avatarImage.sprite = rankSprite;
                    }
                    else
                    {
                        Debug.LogWarning($"No Image component found in 'profile' object of user prefab for rank display");
                    }
                }
                else
                {
                    Debug.LogWarning($"No 'profile' object found in user prefab for avatar display");
                }
            }
            
            // Start auto-scroll after all users have been instantiated
            if (buildingUsers.Count > 0)
            {
                // Stop any existing auto-scroll first
                if (autoScrollCoroutine != null)
                {
                    StopCoroutine(autoScrollCoroutine);
                }
                autoScrollCoroutine = StartCoroutine(AutoScrollCurrentUsers());
            }
        }
        else if (currentUsersPanel != null)
        {
            currentUsersPanel.SetActive(false);
        }
    }

    
    private void ChatWithFriend(User friend)
    {
        // Assuming there's a ChatManager in the scene that handles chat
        ChatManager chatManager = FindObjectOfType<ChatManager>();
        if (chatManager != null)
        {
            chatManager.StartChatWithUser(friend);
        }
       
    }

    /// <summary>
    /// Auto-scrolls through the current users list
    /// </summary>
    private IEnumerator AutoScrollCurrentUsers()
    {
        if (currentUsersScrollRect == null)
        {
            // Try to find ScrollRect component if not assigned
            if (currentUsersPanel != null)
            {
                currentUsersScrollRect = currentUsersPanel.GetComponentInChildren<ScrollRect>();
            }
        }

        if (currentUsersScrollRect == null)
        {
            Debug.LogWarning("UIManager: No ScrollRect found for auto-scroll functionality");
            yield break;
        }

        // Wait a frame for UI to settle
        yield return null;
        yield return null;

        // Auto-scroll parameters
        float scrollSpeed = 0.5f; // Speed of scrolling (0.5 = moderate speed)
        float scrollInterval = 2f; // Time between scroll movements in seconds
        float scrollAmount = 0.2f; // How much to scroll each time (0.2 = 20% of content)

        while (currentUsersPanel != null && currentUsersPanel.activeInHierarchy)
        {
            // Check if there's content to scroll
            if (currentUsersContentParent != null && currentUsersContentParent.childCount > 0)
            {
                // Get current scroll position
                float currentPos = currentUsersScrollRect.verticalNormalizedPosition;

                // Calculate target position
                float targetPos = currentPos - scrollAmount;

                // If we've reached the bottom, scroll back to top
                if (targetPos <= 0f)
                {
                    targetPos = 1f; // Top of the scroll
                }

                // Smoothly scroll to target position
                float elapsedTime = 0f;
                float startPos = currentPos;

                while (elapsedTime < scrollSpeed && currentUsersPanel != null && currentUsersPanel.activeInHierarchy)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / scrollSpeed;

                    // Use smooth lerping for natural scrolling feel
                    float newPos = Mathf.SmoothStep(startPos, targetPos, progress);
                    currentUsersScrollRect.verticalNormalizedPosition = newPos;

                    yield return null;
                }

                // Ensure we reach the exact target position
                if (currentUsersScrollRect != null)
                {
                    currentUsersScrollRect.verticalNormalizedPosition = targetPos;
                }
            }

            // Wait before next scroll
            yield return new WaitForSeconds(scrollInterval);
        }
    }

    public void HideCurrentUsersPanel()
    {
        if (currentUsersPanel != null)
        {
            currentUsersPanel.SetActive(false);
            
            // Stop any ongoing auto-scroll coroutines when hiding the panel
            if (autoScrollCoroutine != null)
            {
                StopCoroutine(autoScrollCoroutine);
                autoScrollCoroutine = null;
            }
        }
    }

    /// <summary>
    /// Gets the appropriate avatar sprite based on user's points.
    /// </summary>
    /// <param name="points">The user's total points (coins + knowledge points)</param>
    /// <param name="oneself">If true, saves the current rank for rank increase detection</param>
    /// <returns>The appropriate sprite for the user's point level</returns>
    public Sprite GetUserAvatarBasedOnPoints(int coins, int knowledgePoints, bool oneself = false)
    {
        // Determine rank based on points
        var userService = new UserService();
        int rank = userService.CalculateUserRank(coins, knowledgePoints);

        
        // If this is for the current user, save the rank for future comparison
        if (oneself)
        {
            int previousRank = PlayerPrefs.GetInt("UserRank", 1);
            PlayerPrefs.SetInt("UserRank", rank);
            PlayerPrefs.Save();

            Debug.Log($"Updated user rank: {previousRank} -> {rank} (points: {coins}, knowledge: {knowledgePoints})");
        }
        
        // Select the appropriate sprite based on rank
        Sprite selectedSprite;
        
        switch (rank)
        {
            case 1:
                selectedSprite = rank1AvatarSprite;
                break;
            case 2:
                selectedSprite = rank2AvatarSprite;
                break;
            case 3:
                selectedSprite = rank3AvatarSprite ?? rank2AvatarSprite; // Use rank2 sprite if rank3 isn't defined
                break;
            case 0:
            default:
                selectedSprite = defaultAvatarSprite;
                break;
        }
        
        // If the selected sprite is null, use the default sprite
        if (selectedSprite == null)
        {
            Debug.LogWarning($"Avatar sprite for rank {rank} (points: {coins}, knowledge: {knowledgePoints}) is not assigned. Using default sprite.");
            selectedSprite = defaultAvatarSprite;
        }
        
        return selectedSprite;
    }
    

    /// <summary>
    /// Shows the rank up panel with the new rank information
    /// </summary>
    /// <param name="newRank">The new rank achieved</param>
    private void ShowRankUpPanel(int newRank)
    {
        if (rankUpPanel == null)
        {
            Debug.LogWarning("UIManager: Rank up panel not assigned!");
            return;
        }
        
        // Map rank numbers to names (matching the Firebase function)
        string[] rankNames = { "Beginner", "Gold", "Silver", "Platinum" };
        string rankName = rankNames[Mathf.Clamp(newRank, 0, rankNames.Length - 1)];
        
        // Set the rank up text
        if (rankUpText != null)
        {
            rankUpText.text = rankName;
        }
        
        // Setup close button
        if (rankUpCloseButton != null)
        {
            rankUpCloseButton.onClick.RemoveAllListeners();
            rankUpCloseButton.onClick.AddListener(HideRankUpPanel);
        }
        
        // Show the panel
        rankUpPanel.SetActive(true);
        
        // Animate the panel appearing
        StartCoroutine(AnimatePanelPopup(rankUpPanel));
        
        Debug.Log($"Showing rank up panel for {rankName} rank (rank {newRank})");
    }
    
    /// <summary>
    /// Hides the rank up panel
    /// </summary>
    public void HideRankUpPanel()
    {
        if (rankUpPanel != null)
        {
            rankUpPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Clears all static cache data in UIManager upon logout
    /// </summary>
    public static void ClearStaticCache()
    {
        try
        {
            // Clear static cache for current users per building
            cachedCurrentUsersPerBuilding.Clear();
            currentUsersLoadedPerBuilding.Clear();
            
            Debug.Log("UIManager: Cleared static cache data");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UIManager: Failed to clear static cache: {ex.Message}");
        }
    }
}
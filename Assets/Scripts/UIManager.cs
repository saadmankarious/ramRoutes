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
    public Text usernameText;
    public Text statusText;
    public Text knowledgePointsText;
    public ParticleSystem teleportEffect;
    public ParticleSystem celebrationEffect1;
    public ParticleSystem celebrationEffect2;
    public float padding = 2f;
    
    [Header("Building UI")]
    public Text buildingTitle;
    public Text buildingDescription;
    public Text buildingUnlockedMessage;
    public Image npcImage;
    public Text npcTitle;
    public Text coinsGained;
    public Text kbGained;
    public Text coinsGainedBuildingStats;
    public Text kbGainedBuildingStats;
    
    [Header("User Avatar")]
    public Image userAvatarImage;
    public Sprite defaultAvatarSprite;
    public Sprite rank1AvatarSprite;
    public Sprite rank2AvatarSprite;
    public Sprite rank3AvatarSprite;

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
    [SerializeField] private float celebrationPlaybackSpeed = 1f;
    [SerializeField] private float celebrationDuration = 2f;

    public Text timerText;
    public Text heldItem;
    public GameObject dialogPanel;
    public GameObject quickUpdatePanel;
    public Text quickUpdateText;
    public GameObject buildingStats;
    public Text dialogText;
    public Button dialogActionButton;
    public Text dialogActionButtonText;
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
    [SerializeField] private AudioClip tcStageMusic;
    [SerializeField] private AudioClip easternCampusMusic;
    [SerializeField] private AudioClip firstStreetMusic;
    [SerializeField] private AudioClip pedmallMusic;
    [SerializeField] private AudioClip terminalMusic;
    [SerializeField] private float backgroundMusicVolume = 0.3f;
    [SerializeField] private float musicFadeInDuration = 2f;
    
    private AudioSource backgroundMusicSource;

    [Header("Typing Sound Settings")]
    [SerializeField] private float typingSoundInterval = 0.15f;
    
    private static Dictionary<string, List<User>> cachedCurrentUsersPerBuilding = new Dictionary<string, List<User>>();
    private static Dictionary<string, bool> currentUsersLoadedPerBuilding = new Dictionary<string, bool>();
    
    private Dictionary<GameObject, Coroutine> activePopupAnimations = new Dictionary<GameObject, Coroutine>();
    private Coroutine autoScrollCoroutine;
    
    private float lastTypingSoundTime;
    [SerializeField] private float typingSoundVolume = 0.3f;

    [Header("Events")]
    public UnityEvent OnTrialComplete = new UnityEvent();
    public UnityEvent OnTimeExpired = new UnityEvent();
    public UnityEvent<BuildingInteraction> OnBuildingUnlocked = new UnityEvent<BuildingInteraction>();

    [Header("Mobile NPC Interaction")]
    public Button mobileInteractButton;
    
    [Header("Progress Bar")]
    public GameObject[] progressBarImages;
    public Text currentStageText;
    
    [Header("Building Gates")]
    [Tooltip("Set pairs of BuildingInteraction and its connected Gate. UIManager will unlock the mapped gate when that building is unlocked.")]

    [Header("Debug / Startup")]
    [SerializeField] private bool clearStageOnStart = false;
    
    [Header("Scene Transition Settings")]
    [Tooltip("Delay in seconds before executing scene transition after building unlock conditions are met.")]
    [SerializeField] private float sceneTransitionDelay = 2f;
    
    [Header("Scene Transition Effects")]
    [Tooltip("UI Image component to use as fade overlay during scene transitions.")]
    [SerializeField] private Image fadeOverlay;
    [Tooltip("Duration of the fade effect before scene transition.")]
    [SerializeField] private float fadeEffectDuration = 2f;

    [SerializeField] private BuildingInteraction[] buildings;

    private Dictionary<BuildingInteraction, Gate> buildingGateMap;

    private Coroutine typingCoroutine;
    private Coroutine objectiveRepeatCoroutine;
    private Coroutine timerCoroutine;
    public GameObject aros;
    private int buildingsUnlockedCount = 0;
    private Coroutine dialogCoroutine;
    private bool isDialogActive = false;
    private System.Action currentDialogAction;
    private bool keepArosVisible = false;
    
    private NpcAutoMovement currentInteractingNPC;
    
    private bool isPaused = false;

    private bool pendingSceneAfterUnlock = false;
    private bool readyToLeaveAfterPanelClose = false;

    private bool isInBuildingViewingMode = false;
    private string currentViewedBuilding = null;
    
    private Coroutine sceneTransitionDelayCoroutine;

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
        
        if (backgroundMusicSource == null)
        {
            GameObject musicObject = new GameObject("BackgroundMusic");
            musicObject.transform.SetParent(transform);
            backgroundMusicSource = musicObject.AddComponent<AudioSource>();
            backgroundMusicSource.loop = true;
            backgroundMusicSource.volume = 0f;
            backgroundMusicSource.playOnAwake = false;
        }
        
        CleanupConflictingAudioSettings();
        
        BuildingDataManager.LoadBuildingData();
        
        if (aros != null)
        {
            aros.transform.localScale = new Vector3(1f, 1f, 1f);
            
            Image image = aros.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = 1f;
                image.color = color;
            }
            
            aros.SetActive(false);
        }

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

        if (clearStageOnStart)
        {
            GameStageService.ClearStageFromPrefs();
            if (currentStageText != null)
            {
                currentStageText.text = "";
            }
        }

        
        _ = InitializeGameStage();
        
        ResetFadeOverlay();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            Debug.Log($"UIManager: Scene '{scene.name}' loaded, checking for component reinitialization");
            
            RefreshUIReferences();
        }
    }

    public void RefreshAllUIConnections()
    {
        RefreshUIReferences();
        Debug.Log("UIManager: Manually refreshed all UI connections");
    }

    private void RefreshUIReferences()
    {
        if (gamePauseMenu == null)
        {
            GameObject pauseMenuGO = GameObject.Find("GamePauseMenu");
            if (pauseMenuGO != null)
            {
                gamePauseMenu = pauseMenuGO;
                Debug.Log("UIManager: Reconnected gamePauseMenu reference");
            }
        }
        
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            string buttonName = button.gameObject.name.ToLower();
            string parentName = button.transform.parent?.name?.ToLower() ?? "";
            
            if (buttonName.Contains("pause") || parentName.Contains("pause") || 
                buttonName.Contains("menu") || buttonName.Contains("settings"))
            {
                bool hasToggleListener = false;
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentMethodName(i) == "TogglePauseMenu")
                    {
                        hasToggleListener = true;
                        break;
                    }
                }
                
                if (!hasToggleListener)
                {
                    button.onClick.RemoveListener(TogglePauseMenu);
                    button.onClick.AddListener(TogglePauseMenu);
                    Debug.Log($"UIManager: Added TogglePauseMenu listener to button '{button.gameObject.name}'");
                }
            }
        }
    }

    private void OnDestroy()
    {
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
                
                SetBackgroundMusicForStage(toSet.area);
            }
            else
            {
                SetCurrentStageText(existing);
                Debug.Log($"UIManager: Game stage already set to {existing.area}");
                
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
            if (backgroundMusicSource.clip == stageMusic && backgroundMusicSource.isPlaying)
            {
                Debug.Log($"Stage music for {stage} is already playing");
                return;
            }
            
            if (backgroundMusicSource.isPlaying)
            {
                StartCoroutine(CrossfadeToNewMusic(stageMusic));
            }
            else
            {
                backgroundMusicSource.clip = stageMusic;
                backgroundMusicSource.Play();
                StartCoroutine(FadeInMusic());
            }
            
            Debug.Log($"Set background music for stage: {stage}");
        }
        else
        {
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
        
        backgroundMusicSource.clip = newMusic;
        backgroundMusicSource.Play();
        
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
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        
        foreach (AudioSource audioSrc in allAudioSources)
        {
            if (audioSrc == audioSource || audioSrc == backgroundMusicSource)
                continue;
                
            if (audioSrc.playOnAwake && audioSrc.clip != null)
            {
                bool likelyBackgroundMusic = audioSrc.clip.length > 30f || audioSrc.loop;
                
                if (likelyBackgroundMusic)
                {
                    Debug.Log($"UIManager: Disabled auto-play for potentially conflicting audio source on {audioSrc.gameObject.name}");
                    audioSrc.playOnAwake = false;
                    audioSrc.Stop();
                }
            }
            
            if (audioSrc.isPlaying && audioSrc.clip != null && audioSrc.clip.length > 30f)
            {
                float originalVolume = audioSrc.volume;
                if (originalVolume > backgroundMusicVolume)
                {
                    audioSrc.volume = backgroundMusicVolume * 0.5f;
                    Debug.Log($"UIManager: Lowered volume of background-like audio on {audioSrc.gameObject.name} from {originalVolume} to {audioSrc.volume}");
                }
            }
        }
        
        Debug.Log("UIManager: Cleaned up conflicting audio settings");
    }

    public void UpdateUserAvatar(int rank)
    {
        if (userAvatarImage == null)
        {
            Debug.LogWarning("User avatar image component not assigned in UIManager");
            return;
        }

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
                selectedSprite = rank3AvatarSprite ?? rank2AvatarSprite;
                break;
            case 0:
            default:
                selectedSprite = defaultAvatarSprite;
                break;
        }

        if (selectedSprite == null)
        {
            Debug.LogWarning($"Avatar sprite for rank {rank} is not assigned. Using default sprite.");
            selectedSprite = defaultAvatarSprite;
        }

        userAvatarImage.sprite = selectedSprite;

        Debug.Log($"Updated user avatar to rank {rank} sprite");
    }

    private void UpdateUsernameAndHall()
    {
        if (usernameText != null)
        {
            string userName = PlayerPrefs.GetString("UserName", "Anonymous User");
            usernameText.text = $"@{userName}";
        }
        if (statusText != null)        {
            string status = PlayerPrefs.GetString("UserStatus", "--");
            statusText.text = status;
        }
        var userRank = PlayerPrefs.GetInt("UserRank", 0);
        UpdateUserAvatar(userRank);

    }

    public void RefreshStatusText()
    {
        if (statusText != null)
        {
            statusText.text = PlayerPrefs.GetString("UserStatus", "--");
        }
    }
    
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
            
            UpdateCoins(coins);
            UpdateKnowledgePoints(knowledgePoints);
            
            int previousRank = PlayerPrefs.GetInt("UserRank", 1);

            GetUserAvatarBasedOnPoints(coins, knowledgePoints, true);

            int currentRank = userService.CalculateUserRank(coins, knowledgePoints);

            if (currentRank > previousRank)
            {
                ShowRankUpPanel(currentRank);
                Debug.Log($"Rank increased from {previousRank} to {currentRank}! (Total points: {totalPoints})");
            }
            
            UpdateUsernameAndHall();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to check and update user rank: {ex.Message}");
        }
    }

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
            
            UpdateCoins(coins);
            UpdateKnowledgePoints(knowledgePoints);
            
            int previousRank = PlayerPrefs.GetInt("UserRank", 1);

            if(userAvatarImage != null)
            {
                userAvatarImage.sprite = GetUserAvatarBasedOnPoints(coins, knowledgePoints, true);
            }

            int currentRank = userService.CalculateUserRank(coins, knowledgePoints);

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

        HideTerminalStageElements();

        OnTrialComplete.AddListener(() => StartCoroutine(CompleteTrial()));
        OnTimeExpired.AddListener(TimeUp);

        _ = InitializeUserRankSystem();
        
        UpdateUsernameAndHall();

        var currentStage = GameStageService.LoadStageFromPrefs();
        if (currentStage != null && stageTimeLimits.TryGetValue(currentStage.area, out int stageLimit) && stageLimit > 0)
        {
            StartCountdown(timerText, stageLimit);
        }


        if (currentStage != null)
        {
            SetBackgroundMusicForStage(currentStage.area);
        }

        
        ResetFadeOverlay();

        yield return StartCoroutine(MovePlayerToCurrentPhysicalBuilding());
    }

    private IEnumerator MovePlayerToCurrentPhysicalBuilding()
    {
        var firebaseUser = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
        if (firebaseUser == null)
        {
            Debug.Log("[UIManager] No Firebase user logged in — skipping move to physical building");
            yield break;
        }

        var userService = new UserService();
        var task = userService.RetrieveUserById(firebaseUser.UserId);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.Result == null)
        {
            Debug.LogWarning("[UIManager] Could not retrieve user for physical building move");
            yield break;
        }

        string physicalBuilding = task.Result.currentPhysicalBuilding;
        if (string.IsNullOrEmpty(physicalBuilding))
        {
            Debug.Log("[UIManager] No currentPhysicalBuilding set — player stays at default position");
            yield break;
        }

        BuildingInteraction targetBuilding = null;
        if (buildings != null)
        {
            foreach (var building in buildings)
            {
                if (building != null && string.Equals(building.buildingName, physicalBuilding, System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetBuilding = building;
                        break;
                    }
                
                if (targetBuilding != null) break;
            }
        
        }
        if (targetBuilding == null)
        {
            Debug.LogWarning($"[UIManager] BuildingInteraction not found for '{physicalBuilding}' — player stays at default position");
            yield break;
        }

        Debug.Log($"[UIManager] Moving player to currentPhysicalBuilding: {physicalBuilding}");
        yield return StartCoroutine(targetBuilding.MovePlayerToBuildingSmooth());
    }

    private void HideTerminalStageElements()
    {
        try
        {
            var currentStage = GameStageService.LoadStageFromPrefs();
            if (currentStage != null && currentStage.area == Stage.Terminal)
            {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("GoneOnTerminalStage");
                
                Debug.Log($"UIManager: Found {taggedObjects.Length} objects with 'GoneOnTerminalStage' tag");
                
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
    
    public void StartCountdown(Text textComponent, int seconds)
    {
        if (textComponent == null) return;
        
        if (!textComponent.gameObject.activeInHierarchy)
        {
            textComponent.gameObject.SetActive(true);
        }
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        timerRunning = true;
        currentTime = 0;
        
        Text originalTimerText = timerText;
        
        timerText = textComponent;
        
        GameManager.Instance.currentTrial.timeLimit = seconds;
        
        timerCoroutine = StartCoroutine(CountdownTimer());
        
        StartCoroutine(ResetTimerAfterCountdown(originalTimerText));
    }
    
    private IEnumerator ResetTimerAfterCountdown(Text originalTimerText)
    {
        yield return new WaitUntil(() => !timerRunning);
        
        timerText = originalTimerText;
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
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

        while (!saveTask.IsCompleted)
            yield return null;
            
        if (saveTask.IsFaulted)
            Debug.LogError("Save failed: " + saveTask.Exception);

        timerRunning = false;
        StopAllCoroutines();
        trialCompleteMenu.SetActive(true);
        Time.timeScale = 0f;
    }

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

    public void ShowDialog(string message, float activeFor = 5f, bool narration = false, bool showBuildingStats = false, string buildingName = null)
    {
        ShowDialog(message, activeFor, null, null, narration, showBuildingStats, buildingName);
    }

    public void ShowQuickUpdate(string message)
    {
        StartCoroutine(ShowQuickUpdateSequence(message));
    }
    
    public void ShowDialog(string message, float activeFor, string actionButtonText, System.Action onActionButtonClick, bool narration = false, bool showStats = false, string buildingName = null)
    {
        if (showStats && buildingStats != null && !string.IsNullOrEmpty(buildingName))
        {
            var buildingInfo = BuildingDataManager.GetBuildingInfo(buildingName);
            if (buildingInfo != null)
            {
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

        if (collectableSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectableSound);
        }

        yield return StartCoroutine(AnimatePanelPopup(quickUpdatePanel));

        yield return new WaitForSeconds(2f);

        quickUpdatePanel.SetActive(false);
    }
    private IEnumerator ShowDialogSequence(string message, float activeFor, string actionButtonText, System.Action onActionButtonClick, bool narration)
    {
        isDialogActive = true;
        currentDialogAction = onActionButtonClick;

        if (onActionButtonClick == null)
        {
            keepArosVisible = false;
        }

        try
        {
            dialogPanel.SetActive(true);

            SetupActionButton(actionButtonText, onActionButtonClick);

            if (aros != null)
            {
                aros.SetActive(true);
            }


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
            if (onButtonClick != null)
            {
                dialogActionButton.gameObject.SetActive(true);
                
                if (dialogActionButtonText != null)
                {
                    dialogActionButtonText.text = buttonText ?? "";
                }
                
                dialogActionButton.onClick.RemoveAllListeners();
                dialogActionButton.onClick.AddListener(() => {
                    onButtonClick?.Invoke();
                    HideDialog();
                });
            }
            else
            {
                dialogActionButton.gameObject.SetActive(false);
            }
        }
    }
    
    private void CleanupDialog()
    {
        isDialogActive = false;
        dialogCoroutine = null;
        currentDialogAction = null;
        
        if (dialogActionButton != null)
        {
            dialogActionButton.gameObject.SetActive(false);
            dialogActionButton.onClick.RemoveAllListeners();
        }
    }
    
    public void HideDialog()
    {
        dialogPanel?.SetActive(false);
        
        if (!keepArosVisible && aros != null)
        {
            aros.SetActive(false);
        }
        
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

        float audioDuration = celebrationDuration;

        GameObject effectsContainer = new GameObject("CelebrationEffects");
        Transform effectsParent = effectsContainer.transform;
        
        Destroy(effectsContainer, audioDuration + 0.5f);
        
        Vector3[] celebrationPoints = new Vector3[3];
        
        for (int point = 0; point < 3; point++)
        {
            float randomX = Random.Range(0.2f, 0.8f);
            float randomY = Random.Range(0.2f, 0.8f);
            celebrationPoints[point] = Camera.main.ViewportToWorldPoint(new Vector3(randomX, randomY, Camera.main.nearClipPlane + 5f));
        }
        
        ParticleSystem[] effects = { teleportEffect, celebrationEffect1, celebrationEffect2 };
        
        for (int i = 0; i < 3; i++)
        {
            ParticleSystem currentEffect = effects[i % effects.Length];
            if (currentEffect == null) continue;
            
            Vector3 basePosition = celebrationPoints[i];
            
            int effectsPerPoint = i == 0 ? 5 : 4;
            
            for (int j = 0; j < effectsPerPoint; j++)
            {
                Vector3 spawnPos = basePosition + new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    0f
                );
                
                GameObject effect = Instantiate(currentEffect.gameObject, spawnPos, Quaternion.identity, effectsParent);
                
                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingLayerName = "Default";
                        renderer.sortingOrder = 4;
                    }
                    
                    var main = ps.main;
                    main.duration = audioDuration;
                    main.loop = false;
                    main.simulationSpeed = celebrationPlaybackSpeed;
                    
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
        
        if (trialCompleteSound != null && audioSource != null)
        {
            audioSource.Stop();
            
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

        var userService = new UserService();
        string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            int coins = await userService.GetUserCoins(userId);
            int knowledgePoints = await userService.GetUserKnowledgePoints(userId);
            UpdateCoins(coins);
            UpdateKnowledgePoints(knowledgePoints);
        }

        bool isFinalUnlock = buildingsUnlockedCount >= 7 || (progressBarImages != null && buildingsUnlockedCount >= progressBarImages.Length);
        if (isFinalUnlock)
        {
            var current = GameStageService.LoadStageFromPrefs();
            var target = GameStage.FromArea(Stage.Terminal);

            SetCurrentStageText(target);
            GameStageService.SaveStageToPrefs(target);
            _ = GameStageService.SaveStageToFirestore(target);

            SetBackgroundMusicForStage(Stage.Terminal);

            pendingSceneAfterUnlock = true;
            readyToLeaveAfterPanelClose = true;
            Debug.Log($"UIManager: Final building unlocked (count={buildingsUnlockedCount}). Stage set to {target.area}. Will load Onboarding after panel closes.");
            return;
        }

        if (pendingSceneAfterUnlock)
        {
            readyToLeaveAfterPanelClose = true;
            Debug.Log("UIManager: Progress bar revealed after unlock. Waiting for unlock panel to close before changing scene.");
        }
    }

    public void OnUnlockPanelClosed()
    {
        StartSceneTransitionDelay();

    }

    private void StartSceneTransitionDelay()
    {
        if (sceneTransitionDelayCoroutine != null)
        {
            StopCoroutine(sceneTransitionDelayCoroutine);
        }
        
        sceneTransitionDelayCoroutine = StartCoroutine(SceneTransitionDelayCoroutine());
    }
    
    private IEnumerator SceneTransitionDelayCoroutine()
    {
        Debug.Log($"UIManager: Starting {sceneTransitionDelay} second delay before scene transition");
        yield return new WaitForSeconds(sceneTransitionDelay);
        
        TryProceedPendingScene();
        
        sceneTransitionDelayCoroutine = null;
    }


    public IEnumerator AnimatePanelPopup(GameObject panel)
    {
        if (panel == null) yield break;

        if (activePopupAnimations.ContainsKey(panel))
        {
            if (activePopupAnimations[panel] != null)
            {
                StopCoroutine(activePopupAnimations[panel]);
            }
            activePopupAnimations.Remove(panel);
        }
        
        activePopupAnimations[panel] = StartCoroutine(AnimatePanelPopupInternal(panel));
        
        yield return activePopupAnimations[panel];
        
        if (activePopupAnimations.ContainsKey(panel))
        {
            activePopupAnimations.Remove(panel);
        }
    }
    
    private IEnumerator AnimatePanelPopupInternal(GameObject panel)
    {
        if (panel == null) yield break;

        Vector3 targetScale = Vector3.one;
        
        panel.transform.localScale = Vector3.zero;
        
        float duration = 0.4f;
        float elapsed = 0f;
        
        while (elapsed < duration * 0.8f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration * 0.8f);
            float overshoot = Mathf.Lerp(0, 1.1f, progress);
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale * overshoot, progress);
            yield return null;
        }
        
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
        
        panel.transform.localScale = targetScale;
    }

    public void ResetPanelScale(GameObject panel)
    {
        if (panel != null)
        {
            if (activePopupAnimations.ContainsKey(panel))
            {
                if (activePopupAnimations[panel] != null)
                {
                    StopCoroutine(activePopupAnimations[panel]);
                }
                activePopupAnimations.Remove(panel);
            }
            
            panel.transform.localScale = Vector3.one;
            Debug.Log($"Reset scale for panel: {panel.name}");
        }
    }

    public void HideAros()
    {
        if (aros != null)
        {
            aros.SetActive(false);
        }
    }
    
    public void ResetArosVisibility(bool hideAros = true)
    {
        keepArosVisible = false;
        if (hideAros && aros != null)
        {
            aros.SetActive(false);
        }
    }
    
    private void PlayArosAnimation(string animationName)
    {
        if (aros != null)
        {
            Animator animator = aros.GetComponent<Animator>();
            if (animator != null && !string.IsNullOrEmpty(animationName))
            {
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
            teleportEffect.transform.position = worldPosition;
            
            teleportEffect.Play();
            
            Debug.Log($"Playing teleport effect at position: {worldPosition}");
            
            yield return new WaitForSeconds(2f);
        }
        else
        {
            Debug.LogWarning("Teleport effect not assigned in UIManager!");
        }
    }
    
    public void RegisterNPCForMobileInteraction(NpcAutoMovement npc)
    {
        if (npc == null)
        {
            Debug.LogWarning("UIManager: Cannot register null NPC for mobile interaction");
            return;
        }
        
        currentInteractingNPC = npc;
        
        if (mobileInteractButton != null)
        {
            mobileInteractButton.onClick.RemoveAllListeners();
            
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
            currentInteractingNPC = null;
            
            HideMobileInteractButton();
            
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

    public void SetBuildingViewingMode(bool active, string buildingName = null)
    {
        isInBuildingViewingMode = active;
        currentViewedBuilding = active ? buildingName : null;
        
       ToggleStatsToHideObjects(!active);
        
        if (!active)
        {
            HideCurrentUsersPanel();
            
            if (sceneTransitionDelayCoroutine == null)
            {
                TryProceedPendingScene();
            }
        }
    }

    private void ToggleStatsToHideObjects(bool show)
    {
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

    private void TryProceedPendingScene()
    {
        if (pendingSceneAfterUnlock && readyToLeaveAfterPanelClose && !isInBuildingViewingMode)
        {
            pendingSceneAfterUnlock = false;
            readyToLeaveAfterPanelClose = false;
            
            StartCoroutine(PlaySceneTransitionEffect());
        }
    }
    
    private IEnumerator PlaySceneTransitionEffect()
    {
        Debug.Log("UIManager: Starting scene transition effect");
        
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color startColor = fadeOverlay.color;
            startColor.a = 0f;
            fadeOverlay.color = startColor;
            
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
            
            Color finalColor = fadeOverlay.color;
            finalColor.a = 1f;
            fadeOverlay.color = finalColor;
        }
        else
        {
            yield return new WaitForSeconds(fadeEffectDuration);
        }
        
        Debug.Log("UIManager: Fade effect complete, loading Onboarding scene");
        SceneManager.LoadScene("Onboarding");
    }
    
    private void ResetFadeOverlay()
    {
        if (fadeOverlay != null)
        {
            Color color = fadeOverlay.color;
            color.a = 0f;
            fadeOverlay.color = color;
            fadeOverlay.gameObject.SetActive(false);
            Debug.Log("UIManager: Reset fade overlay to transparent and inactive");
        }
    }

    public async void DisplayCurrentUsersForBuilding(string buildingName)
    {
        if (!isInBuildingViewingMode || currentViewedBuilding != buildingName)
        {
            Debug.Log($"Not displaying current users: viewing mode={isInBuildingViewingMode}, current building={currentViewedBuilding}, requested building={buildingName}");
            return;
        }

        await DisplayCurrentUsersUI(buildingName);
    }

    private async Task DisplayCurrentUsersUI(string buildingName)
    {
        if (currentUsersContentParent == null || currentUserPrefab == null)
        {
            Debug.LogError("UIManager: Current users UI components not set up!");
            return;
        }

        foreach (Transform child in currentUsersContentParent)
        {
            Destroy(child.gameObject);
        }

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
            StartCoroutine(AnimatePanelPopup(currentUsersPanel));

            foreach (var user in buildingUsers)
            {
                GameObject userGO = Instantiate(currentUserPrefab, currentUsersContentParent);
                   ButtonHandler rsvpButtonHandler = userGO.GetComponentInChildren<ButtonHandler>();

            if (rsvpButtonHandler != null)
            {
                rsvpButtonHandler.Initialize("data", () => ChatWithFriend(user));
            }
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
            
            if (buildingUsers.Count > 0)
            {
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
        ChatManager chatManager = FindObjectOfType<ChatManager>();
        if (chatManager != null)
        {
            chatManager.StartChatWithUser(friend);
        }
       
    }

    private IEnumerator AutoScrollCurrentUsers()
    {
        if (currentUsersScrollRect == null)
        {
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

        yield return null;
        yield return null;

        float scrollSpeed = 0.5f;
        float scrollInterval = 2f;
        float scrollAmount = 0.2f;

        while (currentUsersPanel != null && currentUsersPanel.activeInHierarchy)
        {
            if (currentUsersContentParent != null && currentUsersContentParent.childCount > 0)
            {
                float currentPos = currentUsersScrollRect.verticalNormalizedPosition;

                float targetPos = currentPos - scrollAmount;

                if (targetPos <= 0f)
                {
                    targetPos = 1f;
                }

                float elapsedTime = 0f;
                float startPos = currentPos;

                while (elapsedTime < scrollSpeed && currentUsersPanel != null && currentUsersPanel.activeInHierarchy)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / scrollSpeed;

                    float newPos = Mathf.SmoothStep(startPos, targetPos, progress);
                    currentUsersScrollRect.verticalNormalizedPosition = newPos;

                    yield return null;
                }

                if (currentUsersScrollRect != null)
                {
                    currentUsersScrollRect.verticalNormalizedPosition = targetPos;
                }
            }

            yield return new WaitForSeconds(scrollInterval);
        }
    }

    public void HideCurrentUsersPanel()
    {
        if (currentUsersPanel != null)
        {
            currentUsersPanel.SetActive(false);
            
            if (autoScrollCoroutine != null)
            {
                StopCoroutine(autoScrollCoroutine);
                autoScrollCoroutine = null;
            }
        }
    }

    public Sprite GetUserAvatarBasedOnPoints(int coins, int knowledgePoints, bool oneself = false)
    {
        var userService = new UserService();
        int rank = userService.CalculateUserRank(coins, knowledgePoints);

        
        if (oneself)
        {
            int previousRank = PlayerPrefs.GetInt("UserRank", 1);
            PlayerPrefs.SetInt("UserRank", rank);
            PlayerPrefs.Save();

            Debug.Log($"Updated user rank: {previousRank} -> {rank} (points: {coins}, knowledge: {knowledgePoints})");
        }
        
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
                selectedSprite = rank3AvatarSprite ?? rank2AvatarSprite;
                break;
            case 0:
            default:
                selectedSprite = defaultAvatarSprite;
                break;
        }
        
        if (selectedSprite == null)
        {
            Debug.LogWarning($"Avatar sprite for rank {rank} (points: {coins}, knowledge: {knowledgePoints}) is not assigned. Using default sprite.");
            selectedSprite = defaultAvatarSprite;
        }
        
        return selectedSprite;
    }
    

    private void ShowRankUpPanel(int newRank)
    {
        if (rankUpPanel == null)
        {
            Debug.LogWarning("UIManager: Rank up panel not assigned!");
            return;
        }
        
        string[] rankNames = { "Beginner", "Gold", "Silver", "Platinum" };
        string rankName = rankNames[Mathf.Clamp(newRank, 0, rankNames.Length - 1)];
        
        if (rankUpText != null)
        {
            rankUpText.text = rankName;
        }
        
        if (rankUpCloseButton != null)
        {
            rankUpCloseButton.onClick.RemoveAllListeners();
            rankUpCloseButton.onClick.AddListener(HideRankUpPanel);
        }
        
        rankUpPanel.SetActive(true);
        
        StartCoroutine(AnimatePanelPopup(rankUpPanel));
        
        Debug.Log($"Showing rank up panel for {rankName} rank (rank {newRank})");
    }
    
    public void HideRankUpPanel()
    {
        if (rankUpPanel != null)
        {
            rankUpPanel.SetActive(false);
        }
    }
    
    public static void ClearStaticCache()
    {
        try
        {
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
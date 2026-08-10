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

    [Header("Progress Bar")]
    public GameObject[] progressBarImages;
    public Text currentStageText;

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

    public GameObject aros;
    private int buildingsUnlockedCount = 0;
    private Coroutine dialogCoroutine;
    private bool isDialogActive = false;
    private System.Action currentDialogAction;
    private bool keepArosVisible = false;

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
            if (currentStageText != null)
            {
                currentStageText.text = "";
            }
        }

        
        // _ = InitializeGameStage();
        
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

    // private async Task InitializeGameStage()
    // {
    //     try
    //     {
    //         var existing = GameStageService.LoadStageFromPrefs();
    //         if (existing == null)
    //         {
    //             var toSet = GameStage.FromArea(Stage.TC);
    //             SetCurrentStageText(toSet);
    //             await GameStageService.SetStage(toSet);
    //             Debug.Log("UIManager: Initialized game stage to TC");
                
    //             SetBackgroundMusicForStage(toSet.area);
    //         }
    //         else
    //         {
    //             SetCurrentStageText(existing);
    //             Debug.Log($"UIManager: Game stage already set to {existing.area}");
                
    //             SetBackgroundMusicForStage(existing.area);
    //         }
    //     }
    //     catch (System.Exception ex)
    //     {
    //         Debug.LogError($"UIManager: Failed to initialize game stage: {ex.Message}");
    //     }
    // }

    // private void SetCurrentStageText(GameStage stage)
    // {
    //     if (currentStageText == null || stage == null) return;
    //     var label = stage.stageDisplayName ?? GameStage.GetDefaultDisplayName(stage.area);
    //     currentStageText.text = label;
    // }


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
        OnTimeExpired.AddListener(TimeUp);

        _ = InitializeUserRankSystem();
        
        UpdateUsernameAndHall();
        
        ResetFadeOverlay();

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
        
        timerRunning = true;
        currentTime = 0;
        
        Text originalTimerText = timerText;
        
        timerText = textComponent;
                       
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

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            float volume = clip == typingTickSound ? typingSoundVolume : 1f;
            audioSource.PlayOneShot(clip, volume);
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
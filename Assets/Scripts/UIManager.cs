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

    public GameObject quickUpdatePanel;
    public Text quickUpdateText;
    public GameObject gamePauseMenu;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip trialCompleteSound;
    public AudioClip typingTickSound;
    public AudioClip timeExpiredSound;
    
    [SerializeField] private float backgroundMusicVolume = 0.3f;
    [SerializeField] private float musicFadeInDuration = 2f;
    
    private AudioSource backgroundMusicSource;

    private static Dictionary<string, List<User>> cachedCurrentUsersPerBuilding = new Dictionary<string, List<User>>();
    private static Dictionary<string, bool> currentUsersLoadedPerBuilding = new Dictionary<string, bool>();
    
    private Dictionary<GameObject, Coroutine> activePopupAnimations = new Dictionary<GameObject, Coroutine>();
    private Coroutine autoScrollCoroutine;

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
    public GameObject aros;

    private bool isPaused = false;

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
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize user rank system: {ex.Message}");
        }
    }

    private void Start()
    {
        _ = InitializeUserRankSystem();
        
        UpdateUsernameAndHall();
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

        // Preserve whatever scale the panel was authored/set at instead of
        // forcing it to Vector3.one, which shrinks panels designed at a
        // different scale (e.g. via RectTransform or a scaled prefab).
        Vector3 targetScale = panel.transform.localScale;
        if (targetScale == Vector3.zero)
        {
            targetScale = Vector3.one;
        }

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
    
    
    public void HideRankUpPanel()
    {
        if (rankUpPanel != null)
        {
            rankUpPanel.SetActive(false);
        }
    }
    public void ShowQuickUpdate(string message)
    {
        StartCoroutine(ShowQuickUpdateSequence(message));
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

}
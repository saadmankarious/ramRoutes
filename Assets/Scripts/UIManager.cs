using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Linq;
using System.Threading.Tasks;
using RamRoutes.Services;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    public Text coinsText;
    public ParticleSystem teleportEffect;
    public ParticleSystem celebrationEffect1;
    public ParticleSystem celebrationEffect2;
    public float padding = 2f;
    
    [Header("Celebration Settings")]
    [SerializeField] private float celebrationPlaybackSpeed = 1f; // 1f = normal speed, 2f = double speed, 0.5f = half speed
    public Text trashText;
    public Text bottlesText;
    public Text treesPlantedText;
    public Text treesWateredText;
    public Text levelText;
    public Text timerText;
    public Text heldItem;
    public GameObject dialogPanel;
    public Text dialogText;
    public GameObject timeUpMenu;
    public GameObject trialCompleteMenu;
    public GameObject gamePauseMenu;

    public GameObject endGamePanelNo;
    public GameObject endGamePanelYes;
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

    [Header("Typing Sound Settings")]
    [SerializeField] private float typingSoundInterval = 0.15f;
    private float lastTypingSoundTime;
    [SerializeField] private float typingSoundVolume = 0.3f;

    [Header("Events")]
    public UnityEvent OnTrialComplete = new UnityEvent();
    public UnityEvent OnTimeExpired = new UnityEvent();

    private Coroutine typingCoroutine;
    private Coroutine objectiveRepeatCoroutine;
    private Coroutine timerCoroutine;
    public GameObject aros; // Reference to AROS prefab for animation

    private bool isPaused = false;

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
    }

    private async Task GetUserPoints()
    {
        try 
        {
            var userService = new UserService();
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                var userProfile = await userService.GetUserProfileCachedOrRemoteAsync(userId);
                if (userProfile != null)
                {
                    UpdateCoins(userProfile.points);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to get user points: {ex.Message}");
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

        StartTrial();
    }

    private void StartTrial()
    {
        if (GameManager.Instance.currentTrial == null)
        {
            Debug.LogError("Cannot start trial - currentTrial is null!");
            return;
        }

        
        int trialNumber = GameManager.Instance.currentTrial.trialNumber;
        switch (trialNumber)
        {
            case 1:
                ShowObjectsWithTag("CEO");

                HideObjectsWithTag("Spaceship");
                HideObjectsWithTag("TreeSpot");
                HideObjectsWithTag("Box");
                HideObjectsWithTag("Eagle");
                HideObjectsWithTag("Trial 2 UI");
                HideObjectsWithTag("Trial 3 UI");
                break;

            case 2:
                HideObjectsWithTag("CEO");
                ShowObjectsWithTag("Trial 2 UI");
                ShowObjectsWithTag("TreeSpot");

                HideObjectsWithTag("Box");
                HideObjectsWithTag("Trial 3 UI");
                HideObjectsWithTag("Eagle");
                break;

            case 3:
                ShowObjectsWithTag("Eagle");
                ShowObjectsWithTag("TreeSpot");
                ShowObjectsWithTag("Spaceship");
                ShowObjectsWithTag("Trial 3 UI");


                HideObjectsWithTag("Box");
                HideObjectsWithTag("Trial 2 UI");

                break;

            case 4:
                ShowObjectsWithTag("Box");
                ShowObjectsWithTag("Spaceship");

                HideObjectsWithTag("Eagle");
                HideObjectsWithTag("Trial 2 UI");
                HideObjectsWithTag("Trial 3 UI");
                break;

            default:
                Debug.LogWarning("Unknown trial number: " + trialNumber);
                break;
        }


        currentTime = 0f;
        timerRunning = true;
        UpdateTimerDisplay();
        
        objectiveRepeatCoroutine = StartCoroutine(RepeatObjective());
        timerCoroutine = StartCoroutine(CountdownTimer());
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

    private void UpdateTimerDisplay()
    {
        float timeLeft = GameManager.Instance.currentTrial.timeLimit - currentTime;
        timerText.text = FormatTime(timeLeft);
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private void TimeUp()
    {
        timerRunning = false;
        StopAllCoroutines();
        timeUpMenu.SetActive(true);
        timerText.text = "00:00";
        Time.timeScale = 0f;
        PlaySound(timeExpiredSound);
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

    private IEnumerator TypeText(string message, float activeFor)
    {
        dialogText.text = "";
        lastTypingSoundTime = 0f;
        
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

        if (activeFor > 0)
        {
            yield return new WaitForSeconds(activeFor);
            HideDialog();
        }
        
        typingCoroutine = null;
    }    public void ShowDialog(string message, float activeFor, string animationTrigger = null)
    {
        if (dialogPanel != null && dialogText != null && typingCoroutine == null)
        {
            dialogPanel.SetActive(true);
            var typeTextCoroutine = StartCoroutine(TypeText(message, activeFor));
            var fadeAnimateCoroutine = aros != null && !string.IsNullOrEmpty(animationTrigger) ? 
                StartCoroutine(FadeInAndAnimate(animationTrigger)) : null;
            
            StartCoroutine(WaitForCoroutines(typeTextCoroutine, fadeAnimateCoroutine, "aros-neutral"));
        }
    }

    private IEnumerator WaitForCoroutines(Coroutine typeText, Coroutine fadeAnimate, string finalAnimation)
    {
        if (typeText != null) yield return typeText;
        if (fadeAnimate != null) yield return fadeAnimate;
        if (aros != null) StartCoroutine(FadeInAndAnimate(finalAnimation));
    }

    public IEnumerator FadeInAndAnimate(string animationTrigger)
    {
        if (aros == null) yield break;
        aros.SetActive(true);
        
        Image image = aros.GetComponent<Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = 0f;
            image.color = color;
            
            float fadeTime = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
                image.color = color;
                yield return null;
            }
            
            color.a = 1f;
            image.color = color;
        }
        
        Animator animator = aros.GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(animationTrigger))
        {
            // Play animation directly by state name
            animator.Play(animationTrigger, 0, 0f);
        }
    }

    private void HideDialog()
    {
        dialogPanel?.SetActive(false);
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

    private void Update()
    {

        var trial = GameManager.Instance.currentTrial;
        trashText.text = $"{trial.currentTrash}/{trial.targetTrash}";
        bottlesText.text = $"{trial.currentRecycling}/{trial.targetRecycling}";
        treesPlantedText.text = $"{trial.currentTreesPlanted}/{trial.targetTreesPlanted}";
        treesWateredText.text = $"{trial.currentTreesWatered}/{trial.targetTreesWatered}";
        levelText.text = trial.trialName;
        // if (Input.GetKeyDown(KeyCode.Escape))
        // {
        //     TogglePauseMenu();
        // }
    }

    public void ContinueToNextTrial()
    {
        Time.timeScale = 1f;
        trialCompleteMenu.SetActive(false);
        GameManager.Instance.SetGameLevel(GameManager.Instance.gameLevel + 1);
        StartTrial();
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        timeUpMenu.SetActive(false);
        GameManager.Instance.ResetLevel();
        StartTrial();
    }

    private void CleanUpLevelObjects()
    {
        DestroyAllWithTag("Trash");
        DestroyAllWithTag("Recyclable");
    }

    private void DestroyAllWithTag(string tag)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag(tag))
        {
            Destroy(obj);
        }
    }

    private void StopAllGameCoroutines()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (objectiveRepeatCoroutine != null) StopCoroutine(objectiveRepeatCoroutine);
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        OnTrialComplete.RemoveAllListeners();
        OnTimeExpired.RemoveAllListeners();
        Time.timeScale = 1f;
    }

    void CelebrationEffect()
    {
        Debug.Log("Playing celebration effect with multiple particle systems");
        if (teleportEffect == null)
        {
            Debug.LogWarning("Teleport effect not set in UIManager!");
            return;
        }

        // Get audio duration to sync effect duration
        float audioDuration = trialCompleteSound != null ? trialCompleteSound.length : 5f;

        // Create a parent object to organize all particle effects
        GameObject effectsContainer = new GameObject("CelebrationEffects");
        Transform effectsParent = effectsContainer.transform;
        
        // Auto-cleanup when audio finishes (add 1 second buffer for particle fadeout)
        Destroy(effectsContainer, audioDuration + 1f);
        
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

    public void UpdateCoins(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
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
        
        // Play celebration sound as part of the celebration
        if (trialCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(trialCompleteSound);
        }
    }
    
    public float GetCelebrationDuration()
    {
        // Return the audio duration that celebration effects are synced to
        return trialCompleteSound != null ? trialCompleteSound.length : 5f;
    }
}
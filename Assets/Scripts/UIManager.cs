using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Linq;
using System.Threading.Tasks;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    public Text coinsText;
    public ParticleSystem teleportEffect;
    public float padding = 2f;
    public Text trashText;
    public Text bottlesText;
    public Text treesText;
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

    private IEnumerator Start()
    {
        while (GameManager.Instance == null || GameManager.Instance.currentTrial == null)
        {
            yield return null;
        }

        OnTrialComplete.AddListener(() => StartCoroutine(CompleteTrial()));
        OnTimeExpired.AddListener(TimeUp);
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
                break;

            case 2:
                ShowObjectsWithTag("Spaceship");
                HideObjectsWithTag("CEO");
                ShowObjectsWithTag("Trial 2 UI");
                ShowObjectsWithTag("TreeSpot");

                HideObjectsWithTag("Box");
                HideObjectsWithTag("Eagle");
                break;

            case 3:
                ShowObjectsWithTag("Eagle");
                ShowObjectsWithTag("TreeSpot");

                HideObjectsWithTag("Box");
                HideObjectsWithTag("Trial 2 UI");


                break;

            case 4:
                ShowObjectsWithTag("Box");
                ShowObjectsWithTag("Spaceship");

                HideObjectsWithTag("Eagle");
                HideObjectsWithTag("Trial 2 UI");
                break;

            default:
                Debug.LogWarning("Unknown trial number: " + trialNumber);
                break;
        }


        currentTime = 0f;
        timerRunning = true;
        UpdateTimerDisplay();
        
        StartCoroutine(ShowInitialObjective());
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

    private IEnumerator ShowInitialObjective()
    {
        yield return new WaitForSeconds(0.5f);
        ShowObjective();
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
    }

    public void ShowDialog(string message, float activeFor)
    {
        if (dialogPanel != null && dialogText != null)
        {
            dialogPanel.SetActive(true);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(message, activeFor));
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
            ShowObjective();
        }
    }

    private void ShowObjective()
    {
        string objectiveMessage = GameManager.Instance.currentTrial.GetProgressReport();
        ShowDialog(objectiveMessage, 10f);
    }

    private void Update()
    {
        var trial = GameManager.Instance.currentTrial;
        coinsText.text = $"{trial.currentCoins}";
        trashText.text = $"{trial.currentTrash}/{trial.targetTrash}";
        bottlesText.text = $"{trial.currentRecycling}/{trial.targetRecycling}";
        treesText.text = $"{trial.currentTreesPlanted}/{trial.targetTreesPlanted}";
        levelText.text = trial.trialName;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
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

    private void StopAllCoroutines()
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
        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 topRight = Camera.main.ViewportToWorldPoint(new Vector2(1, 1));

        bottomLeft += new Vector2(padding, padding);
        topRight -= new Vector2(padding, padding);

        Transform effectsParent = new GameObject("CelebrationEffects").transform;
        for (int i = 0; i < 13; i++)
        {
            Vector2 spawnPos = new Vector2(
                Random.Range(bottomLeft.x, topRight.x),
                Random.Range(bottomLeft.y, topRight.y)
            );
            Instantiate(teleportEffect, spawnPos, Quaternion.identity, effectsParent);
        }


    }






}
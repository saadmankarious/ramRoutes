using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    public Text coinsText;
       public ParticleSystem teleportEffect; // Particle effect for teleportation
    public float padding = 2f; // Optional: Keeps effects from spawning at edges

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

    [Header("Timing Settings")]
    [SerializeField] private float typingSpeed = 0.3f;
    [SerializeField] private float objectiveRepeatTime = 60;
    private float currentTime;
    private bool timerRunning;

    [Header("Events")]
    public UnityEvent OnTrialComplete = new UnityEvent();
    public UnityEvent OnTimeExpired = new UnityEvent();

    private Coroutine typingCoroutine;
    private Coroutine objectiveRepeatCoroutine;
    private Coroutine timerCoroutine;

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
    }

  private IEnumerator Start()
{
    // Wait for GameManager to fully initialize
    while (GameManager.Instance == null || GameManager.Instance.currentTrial == null)
    {
        yield return null;
    }

    // Now safe to setup listeners and start trial
    OnTrialComplete.AddListener(() => StartCoroutine(CompleteTrial()));
    OnTimeExpired.AddListener(TimeUp);
    StartTrial();
}

// Add this helper method to your class
private GameObject[] FindObjectsWithTagInactive(string tag)
{
    return Resources.FindObjectsOfTypeAll<GameObject>()
                   .Where(go => go.CompareTag(tag)).ToArray();
}

private void StartTrial()
{
    // Verify current trial first
    if (GameManager.Instance.currentTrial == null)
    {
        Debug.LogError("Cannot start trial - currentTrial is null!");
        return;
    }

    ResetAllTrialObjects();
    
    int trialNumber = GameManager.Instance.currentTrial.trialNumber;
    string trialTag = "Trial " + trialNumber;
    Debug.Log($"Starting trial {trialNumber} (tag: '{trialTag}')");

    // Find both active and inactive objects
    GameObject[] trialObjects = FindObjectsWithTagInactive(trialTag);
    Debug.Log($"Found {trialObjects.Length} objects for trial {trialNumber}");

    foreach (GameObject o in trialObjects)
    {
        Debug.Log($"Activating: {o.name} (currently active: {o.activeSelf})");
        o.SetActive(true);
    }

    currentTime = 0f;
    timerRunning = true;
    UpdateTimerDisplay();
    
    StartCoroutine(ShowInitialObjective());
    objectiveRepeatCoroutine = StartCoroutine(RepeatObjective());
    timerCoroutine = StartCoroutine(CountdownTimer());
}

private void ResetAllTrialObjects()
{
    Debug.Log("Resetting all trial objects");
    
    for (int i = 1; i <= 4; i++)
    {
        string tag = "Trial " + i;
        GameObject[] objects = FindObjectsWithTagInactive(tag);
        Debug.Log($"Found {objects.Length} objects with tag '{tag}'");

        foreach (GameObject o in objects)
        {
            if (o.activeSelf)
            {
                Debug.Log($"Deactivating: {o.name}");
                o.SetActive(false);
            }
        }
    }
}
private IEnumerator ShowInitialObjective()
{
    // Small delay to ensure all UI elements are ready
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

            // Flash red when 30 seconds remain
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
    }

    public IEnumerator CompleteTrial()
    {
        yield return new WaitForSeconds(2f);
        if (teleportEffect != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CelebrationEffect();
            }
        }
        yield return new WaitForSeconds(2f);

        timerRunning = false;
        StopAllCoroutines();
        trialCompleteMenu.SetActive(true);
        Time.timeScale = 0f;
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

    public void ExitTrial()
    {
        Time.timeScale = 1f;
        CleanUpLevelObjects();
        StopAllCoroutines();
        GameManager.Instance.ResetTemporaryState();
        SceneManager.LoadScene("Landing");
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

    public void ShowDialog(string message, float activeFor)
    {
        if (dialogPanel != null && dialogText != null)
        {
                    Debug.Log("showing dialoge" + message);

            dialogPanel.SetActive(true);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(message, activeFor));
        }
    }

    private IEnumerator TypeText(string message, float activeFor)
    {
        dialogText.text = "";
        foreach (char letter in message.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        if (activeFor > 0)
        {
            yield return new WaitForSeconds(activeFor);
            HideDialog();
        }
    }

    private void HideDialog()
    {
        dialogPanel?.SetActive(false);
    }

    private void Update()
    {
        var trial = GameManager.Instance.currentTrial;
        coinsText.text = $"{trial.currentCoins}/{trial.targetCoins}";
        trashText.text = $"{trial.currentTrash}/{trial.targetTrash}";
        bottlesText.text = $"{trial.currentRecycling}/{trial.targetRecycling}";
        treesText.text = $"{trial.currentTreesPlanted}/{trial.targetTreesPlanted}";
        levelText.text = trial.trialName;
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
        // Get screen corners in world coordinates
        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 topRight = Camera.main.ViewportToWorldPoint(new Vector2(1, 1));

        // Apply padding (optional)
        bottomLeft += new Vector2(padding, padding);
        topRight -= new Vector2(padding, padding);

        // Random position within screen bounds
        for (int i = 0; i < 13; i++)
        {
            Vector2 spawnPos = new Vector2(
                Random.Range(bottomLeft.x, topRight.x),
                Random.Range(bottomLeft.y, topRight.y)
            );
        Transform effectsParent = new GameObject("CelebrationEffects").transform;
        Instantiate(teleportEffect, spawnPos, Quaternion.identity, effectsParent);
        }
    }
}
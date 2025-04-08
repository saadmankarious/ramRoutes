using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Events; // Needed for UnityEvent

public class UIManager : MonoBehaviour
{
    public Text coinsText;
    public Text trashText;
    public Text bottlesText;
    public Text treesText;
    public Text levelText;
    public Text timerText; // New Text element to display the countdown
    public GameObject dialogPanel;
    public Text dialogText;
    public GameObject timeUpMenu; // Reference to the time up menu from Inspector

    [SerializeField] private float typingSpeed = 0.3f; // Speed (lower = faster)
    [SerializeField] private float objectiveRepeatTime = 90f; // Show objective every 90 seconds
    [SerializeField] private float levelDuration = 240f; // 4 minutes in seconds
    private Coroutine typingCoroutine;
    private Coroutine objectiveRepeatCoroutine;
    private Coroutine timerCoroutine;
    private float currentTime;

    public GameObject trialCompleteMenu; // New panel for completed trials
    
    // Event that can be subscribed to from other classes
    public UnityEvent OnTrialComplete = new UnityEvent();

 
    private string GetTrialName(int level)
    {
        switch (level)
        {
            case 0: return "Trial 1: Sorting Trash";
            case 1: return "Trial 2: Tree Planting";
            case 2: return "Trial 3: xxx";
            case 3: return "Trial 4: yyyy";
            default: return "Sorting Trash";
        }
    }
   private void Start()
    {
        // Initialize timer
        currentTime = levelDuration;
        UpdateTimerDisplay();

        // Show immediately at start
        ShowObjective();

        // Start repeating every 90 seconds
        objectiveRepeatCoroutine = StartCoroutine(RepeatObjective());

        // Start the countdown timer
        timerCoroutine = StartCoroutine(CountdownTimer());
        
        // Subscribe to the trial complete event
        OnTrialComplete.AddListener(CompleteTrial);
    }

    // New function to handle trial completion
    public void CompleteTrial()
    {
        // Stop all coroutines
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (objectiveRepeatCoroutine != null) StopCoroutine(objectiveRepeatCoroutine);
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        // Show the trial complete menu
        if (trialCompleteMenu != null)
        {
            trialCompleteMenu.SetActive(true);
        }

        // Pause the game
        Time.timeScale = 0f;
    }

    // New function to continue to next trial
    public void ContinueToNextTrial()
    {
        // Unpause the game
        Time.timeScale = 1f;
        
        // Hide the complete menu
        if (trialCompleteMenu != null)
        {
            trialCompleteMenu.SetActive(false);
        }

        // Reset the timer
        currentTime = levelDuration;
        UpdateTimerDisplay();
        timerText.color = Color.white;

        // Restart coroutines
        objectiveRepeatCoroutine = StartCoroutine(RepeatObjective());
        timerCoroutine = StartCoroutine(CountdownTimer());

        // Increment level (you might want to move this to GameManager)
        GameManager.Instance.gameLevel++;
        
        // Show new objective
        ShowObjective();
    }

    // Modified OnDestroy to clean up event
    private void OnDestroy()
    {
        // Clean up coroutines
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (objectiveRepeatCoroutine != null) StopCoroutine(objectiveRepeatCoroutine);
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        // Remove event listeners
        OnTrialComplete.RemoveAllListeners();

        // Resume time scale
        Time.timeScale = 1f;
    }

    // Countdown timer coroutine
    private IEnumerator CountdownTimer()
    {
        while (currentTime > 0)
        {
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;
            UpdateTimerDisplay();

            // Optional: Add visual/audio effects when time is running low
            if (currentTime <= 30f)
            {
                // Flash timer red or play a sound
                timerText.color = Color.red;
            }
        }

        // Time's up!
        TimeUp();
    }

    // Update the timer text display
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(currentTime);
        }
    }

    // Format time as minutes:seconds
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Called when time runs out
    private void TimeUp()
    {
        // Stop all coroutines
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (objectiveRepeatCoroutine != null) StopCoroutine(objectiveRepeatCoroutine);

        // Show the time up menu
        if (timeUpMenu != null)
        {
            timeUpMenu.SetActive(true);
        }
        timerText.text = "00:00";

        Time.timeScale = 0f; // This pauses the game
    }

    private IEnumerator RepeatObjective()
    {
        while (true) // Infinite loop (safe because of yield)
        {
            yield return new WaitForSeconds(objectiveRepeatTime);
            ShowObjective();
        }
    }

    // Shows the objective message
    private void ShowObjective()
    {
        string objectiveMessage = GetCurrentObjectiveMessage();
        ShowDialog(objectiveMessage, 20f); // Show for 20 seconds
    }

    // Returns the objective message based on current level
    private string GetCurrentObjectiveMessage()
    {
        int level = GameManager.Instance.gameLevel;
        switch (level)
        {
            case 0: return "Objective—Sort 20 pieces of trash and 20 pieces of recycling.";
            case 1: return "Objective—Plant 15 trees across the city.";
            case 2: return "Objective—Complete the water conservation trial.";
            case 3: return "Objective—Reduce carbon emissions by 50%.";
            default: return "Complete the current trial objectives.";
        }
    }

    // Modified ShowDialog with typewriter effect
    public void ShowDialog(string message, float activeFor = 3f)
    {
        if (dialogPanel != null && dialogText != null)
        {
            dialogPanel.SetActive(true);

            // Stop any ongoing typing coroutine
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            // Start new typing effect
            typingCoroutine = StartCoroutine(TypeText(message, activeFor));
        }
    }

    // Typewriter effect Coroutine
    private IEnumerator TypeText(string message, float activeFor)
    {
        dialogText.text = ""; // Clear previous text

        foreach (char letter in message.ToCharArray())
        {
            dialogText.text += letter; // Add one character at a time
            yield return new WaitForSeconds(typingSpeed); // Pause between letters
        }

        // Auto-hide after delay (if activeFor > 0)
        if (activeFor > 0)
        {
            yield return new WaitForSeconds(activeFor);
            HideDialog();
        }
    }

    void HideDialog()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }

    private void Update()
    {
        coinsText.text = "" + GameManager.Instance.coinsCollected;
        trashText.text = "" + GameManager.Instance.trashCollected + "/" + 10;
        bottlesText.text = "" + GameManager.Instance.bottlesCollected + "/" + 10;
        treesText.text = "" + GameManager.Instance.treesPlanted;

        levelText.text = (GameManager.Instance.gameLevel + 1) + " - " + GetTrialName(GameManager.Instance.gameLevel);
    }



    public void RetryLevel()
    {
        // Reset the time scale to unpause the game
        Time.timeScale = 1f;

        // Hide the time up menu
        if (timeUpMenu != null)
        {
            timeUpMenu.SetActive(false);
        }

        // Reset the timer
        currentTime = levelDuration;
        UpdateTimerDisplay();

        // Reset any visual effects on the timer
        if (timerText != null)
        {
            timerText.color = Color.white;
        }

        if (objectiveRepeatCoroutine != null)
        {
            StopCoroutine(objectiveRepeatCoroutine);
        }
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        objectiveRepeatCoroutine = StartCoroutine(RepeatObjective());
        timerCoroutine = StartCoroutine(CountdownTimer());

        GameManager.Instance.ResetLevel();
        
        CleanUpLevelObjects();

        ShowObjective();
    }

    public void ExitTrial()
    {
        Time.timeScale = 1f;

        CleanUpLevelObjects();

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (objectiveRepeatCoroutine != null) StopCoroutine(objectiveRepeatCoroutine);
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        GameManager.Instance.ResetTemporaryState();

        SceneManager.LoadScene("Landing");
    }

    private void CleanUpLevelObjects()
    {
        DestroyAllWithTag("Trash");
        DestroyAllWithTag("Recyclable");
    }

    private void DestroyAllWithTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }
    }
}
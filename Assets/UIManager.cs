using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public Text coinsText;
    public Text trashText;
    public Text bottlesText;
    public Text treesText;
    public Text levelText;
    public GameObject dialogPanel;
    public Text dialogText;
    
    [SerializeField] private float typingSpeed = 0.3f; // Speed (lower = faster)
    [SerializeField] private float objectiveRepeatTime = 90f; // Show objective every 90 seconds
    private Coroutine typingCoroutine;
    private Coroutine objectiveRepeatCoroutine;

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
        // Show immediately at start
        ShowObjective();
        
        // Start repeating every 90 seconds
        objectiveRepeatCoroutine = StartCoroutine(RepeatObjective());
    }

    // Repeats the objective message every 90 seconds
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
        bottlesText.text = "" + GameManager.Instance.bottlesCollected  + "/" + 10;
        treesText.text = "" + GameManager.Instance.treesPlanted;

        levelText.text = (GameManager.Instance.gameLevel + 1) + " - " + GetTrialName(GameManager.Instance.gameLevel);
    }

    private void OnDestroy()
    {
        // Clean up coroutines if the object is destroyed
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (objectiveRepeatCoroutine != null)
            StopCoroutine(objectiveRepeatCoroutine);
    }
}
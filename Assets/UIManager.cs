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
    private Coroutine typingCoroutine; // To track the current typing coroutine

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
        trashText.text = "" + GameManager.Instance.trashCollected;
        bottlesText.text = "" + GameManager.Instance.bottlesCollected;
        treesText.text = "" + GameManager.Instance.treesPlanted;

        levelText.text = (GameManager.Instance.gameLevel + 1) + " - " + GetTrialName(GameManager.Instance.gameLevel);
    }

    private void Start()
    {
        ShowDialog("Objective--sort trash produced by CEOs in the level into appropriate locations.", 20f);
    }
}
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
public class LoginManager : MonoBehaviour
{
    [Header("Login UI")]
    public InputField emailInput;
    public InputField passwordInput;
    public Text statusText;
    public GameObject loginPanel;
    public GameObject welcomePanel;
    public Text welcomeText;

    [Header("Attempts Scroll View")]
    public ScrollRect attemptsScrollView;
    public Transform attemptsContentParent;
    public GameObject attemptTextPrefab;
    public int maxAttemptsToShow = 20;

    private bool isLoggedIn = false;
    private string userEmail = "";

    private async void Start()
    {
        await FirestoreUtility.Initialize();
        await LoadAndDisplayAttempts();
        ConfigureScrollContent();
    }

    private void ConfigureScrollContent()
    {
        // Ensure content has proper layout components
        var layoutGroup = attemptsContentParent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            // layoutGroup = attemptsContentParent.gameObject.AddComponent<VerticalLayoutGroup>();
            // layoutGroup.padding = new RectOffset(20, 20, 10, 10);
            // layoutGroup.spacing = 15f;
            // layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            // layoutGroup.childControlWidth = true;
            // layoutGroup.childControlHeight = false;
        }

        var sizeFitter = attemptsContentParent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = attemptsContentParent.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Configure scroll view
        attemptsScrollView.horizontal = false;
        attemptsScrollView.vertical = true;
        attemptsScrollView.movementType = ScrollRect.MovementType.Clamped;
    }

    private async Task LoadAndDisplayAttempts()
    {
        try
        {
            List<GamePlay> attempts = await FirestoreUtility.GetGameCompletions();
            DisplayAttempts(attempts);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load attempts: {e.Message}");
        }
    }

 private void DisplayAttempts(List<GamePlay> attempts)
{
    // Clear existing items
    foreach (Transform child in attemptsContentParent)
    {
        Destroy(child.gameObject);
    }

    // Define achievement colors
    Color[] achievementColors = new Color[4]
    {
        new Color(0.8f, 0.8f, 0.8f),    // Gray - Trial 1 (Basic)
        new Color(0.2f, 0.8f, 0.2f),    // Green - Trial 2 (Good)
        new Color(0.2f, 0.5f, 0.8f),    // Blue - Trial 3 (Great)
        new Color(0.9f, 0.7f, 0.1f)     // Gold - Trial 4 (Excellent)
    };

    // Show newest first and limit count
    int displayCount = Mathf.Min(attempts.Count, maxAttemptsToShow);
    for (int i = displayCount - 1; i >= 0; i--)
    {
        GamePlay attempt = attempts[i];
        GameObject attemptItem = Instantiate(attemptTextPrefab, attemptsContentParent);
        
        // Configure item layout
        RectTransform itemRect = attemptItem.GetComponent<RectTransform>();
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        
        Text attemptText = attemptItem.GetComponent<Text>();
        string formattedDate = attempt.DateCompleted.ToDateTime().ToString("MMM dd, HH:mm");
        attemptText.text = $"{attempt.PlayerName} - Trial {attempt.TrialNumber}";
        attemptText.alignment = TextAnchor.MiddleCenter;
        
        // Set color based on trial number (clamped between 1-4)
        int colorIndex = Mathf.Clamp(attempt.TrialNumber, 1, 4) - 1;
        attemptText.color = achievementColors[colorIndex];
    }

    // Force layout update
    LayoutRebuilder.ForceRebuildLayoutImmediate(attemptsContentParent as RectTransform);
    Canvas.ForceUpdateCanvases();
    attemptsScrollView.verticalNormalizedPosition = 1f;
}

    public async void Play()
    {
        string playerName = emailInput.text;
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        try
        {
            await FirestoreUtility.SaveGameAttempt(playerName);
            SceneManager.LoadScene("LevelFall");
        }
        catch
        {
            SceneManager.LoadScene("LevelFall");
        }
    }


    private void Update()
    {
        if (isLoggedIn)
        {
            loginPanel.SetActive(false);
            welcomePanel.SetActive(true);
            welcomeText.text = $"Welcome, {userEmail}!";
            isLoggedIn = false;
        }
    }

    public void ViewOnboarding()
    {
        SceneManager.LoadScene("Onboarding");
    }
}
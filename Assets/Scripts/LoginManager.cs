using Firebase.Auth;
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
            layoutGroup = attemptsContentParent.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(20, 20, 10, 10);
            layoutGroup.spacing = 15f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
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
            List<GameAttempt> attempts = await FirestoreUtility.GetGameAttempts();
            DisplayAttempts(attempts);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load attempts: {e.Message}");
        }
    }

    private void DisplayAttempts(List<GameAttempt> attempts)
    {
        // Clear existing items
        foreach (Transform child in attemptsContentParent)
        {
            Destroy(child.gameObject);
        }

        // Show newest first and limit count
        int displayCount = Mathf.Min(attempts.Count, maxAttemptsToShow);
        for (int i = displayCount - 1; i >= 0; i--)
        {
            GameAttempt attempt = attempts[i];
            GameObject attemptItem = Instantiate(attemptTextPrefab, attemptsContentParent);
            
            // Configure item layout
            RectTransform itemRect = attemptItem.GetComponent<RectTransform>();
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.anchorMin = new Vector2(0.5f, 0.5f);
            itemRect.anchorMax = new Vector2(0.5f, 0.5f);
            
            Text attemptText = attemptItem.GetComponent<Text>();
            string formattedDate = attempt.Date.ToDateTime().ToString("MMM dd, HH:mm");
            attemptText.text = $"{attempt.PlayerName} - {formattedDate}";
            attemptText.alignment = TextAnchor.MiddleCenter;
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

    public void LogoutUser()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        loginPanel.SetActive(true);
        welcomePanel.SetActive(false);
        emailInput.text = "";
        passwordInput.text = "";
        statusText.text = "User logged out.";
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
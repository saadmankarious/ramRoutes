using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LoginManager : MonoBehaviour
{
    [Header("Login UI")]
    public InputField emailInput;
    public InputField passwordInput;
    public Button loginButton;
    public Button playButton;
    public Button logoutButton;
    public Text statusText;
    public GameObject loginPanel;
    public GameObject welcomePanel;
    public Text welcomeText;

    [Header("Attempts History")]
    public ScrollRect attemptsScrollView;
    public Transform attemptsContentParent;
    public GameObject attemptTextPrefab;
    public int maxAttemptsToShow = 20;

    private FirebaseAuth auth;
    private string playerName = "";

    private async void Start()
    {
        // Initialize UI
        loginPanel.SetActive(true);
        welcomePanel.SetActive(false);
        playButton.interactable = false;
        
        // Setup button listeners
        loginButton.onClick.AddListener(OnLoginClicked);
        playButton.onClick.AddListener(OnPlayClicked);
        logoutButton.onClick.AddListener(OnLogoutClicked);

        // Initialize Firebase
        await InitializeFirebase();
        CheckAuthState();

        // Load attempts history
        await LoadAndDisplayAttempts();
        ConfigureScrollContent();
    }

    private async Task InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            await FirestoreUtility.Initialize();
            
            // Auth state persistence is now automatic in newer Firebase versions
            auth.StateChanged += AuthStateChanged;
        }
        else
        {
            Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
        }
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != null)
        {
            // User is signed in
            HandleSuccessfulLogin(auth.CurrentUser.Email);
        }
    }

    private void CheckAuthState()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            HandleSuccessfulLogin(auth.CurrentUser.Email);
        }
    }

    private void HandleSuccessfulLogin(string email)
    {
        playerName = email.Split('@')[0];
        PlayerPrefs.SetString("PlayerName", playerName);
        
        loginPanel.SetActive(false);
        welcomePanel.SetActive(true);
        welcomeText.text = $"Welcome, {playerName}!";
        playButton.interactable = true;
        statusText.text = "";
    }

    private async void OnLoginClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter email and password";
            return;
        }

        statusText.text = "Logging in...";
        loginButton.interactable = false;

        try
        {
            await auth.SignInWithEmailAndPasswordAsync(email, password);
            // AuthStateChanged will handle the UI update
        }
        catch (FirebaseException e)
        {
            statusText.text = GetFirebaseErrorMessage(e);
            loginButton.interactable = true;
        }
    }

    private void OnLogoutClicked()
    {
        if (auth != null)
        {
            auth.SignOut();
            PlayerPrefs.DeleteKey("PlayerName");
        }

        loginPanel.SetActive(true);
        welcomePanel.SetActive(false);
        playButton.interactable = false;
        emailInput.text = "";
        passwordInput.text = "";
        statusText.text = "Logged out successfully";
    }

    private string GetFirebaseErrorMessage(FirebaseException e)
    {
        return e.Message switch
        {
            string msg when msg.Contains("INVALID_EMAIL") => "Invalid email format",
            string msg when msg.Contains("EMAIL_NOT_FOUND") => "Account not found",
            string msg when msg.Contains("WRONG_PASSWORD") => "Incorrect password",
            string msg when msg.Contains("TOO_MANY_REQUESTS") => "Too many attempts. Try again later",
            string msg when msg.Contains("USER_DISABLED") => "Account disabled",
            _ => "Login failed: " + e.Message
        };
    }

    private async void OnPlayClicked()
    {
        try
        {
            await FirestoreUtility.SaveGameAttempt(playerName);
            SceneManager.LoadScene("LevelFall");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save attempt: {e.Message}");
            SceneManager.LoadScene("LevelFall");
        }
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
        foreach (Transform child in attemptsContentParent)
        {
            Destroy(child.gameObject);
        }

        Color[] achievementColors = new Color[4]
        {
            new Color(0.8f, 0.8f, 0.8f),
            new Color(0.2f, 0.8f, 0.2f),
            new Color(0.2f, 0.5f, 0.8f),
            new Color(0.9f, 0.7f, 0.1f)
        };

        int displayCount = Mathf.Min(attempts.Count, maxAttemptsToShow);
        for (int i = displayCount - 1; i >= 0; i--)
        {
            GamePlay attempt = attempts[i];
            GameObject attemptItem = Instantiate(attemptTextPrefab, attemptsContentParent);
            
            RectTransform itemRect = attemptItem.GetComponent<RectTransform>();
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.anchorMin = new Vector2(0.5f, 0.5f);
            itemRect.anchorMax = new Vector2(0.5f, 0.5f);
            
            Text attemptText = attemptItem.GetComponent<Text>();
            string formattedDate = attempt.DateCompleted.ToDateTime().ToString("MMM dd, HH:mm");
            attemptText.text = $"{attempt.PlayerName} - Trial {attempt.TrialNumber}";
            attemptText.alignment = TextAnchor.MiddleCenter;
            
            int colorIndex = Mathf.Clamp(attempt.TrialNumber, 1, 4) - 1;
            attemptText.color = achievementColors[colorIndex];
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(attemptsContentParent as RectTransform);
        Canvas.ForceUpdateCanvases();
        attemptsScrollView.verticalNormalizedPosition = 1f;
    }

    private void ConfigureScrollContent()
    {
        var sizeFitter = attemptsContentParent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = attemptsContentParent.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        attemptsScrollView.horizontal = false;
        attemptsScrollView.vertical = true;
        attemptsScrollView.movementType = ScrollRect.MovementType.Clamped;
    }

    public void ViewOnboarding()
    {
        SceneManager.LoadScene("Onboarding");
    }

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }
}
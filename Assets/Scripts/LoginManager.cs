using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System;
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

    [Header("Leaderboard")]
    public Text firstPlaceText;
    public Text secondPlaceText;
    public Text thirdPlaceText;

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

        // Load leaderboard
        await UpdateLeaderboard();
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

    private async void HandleSuccessfulLogin(string email)
    {
        playerName = email.Split('@')[0];
        PlayerPrefs.SetString("PlayerName", playerName);
        loginPanel.SetActive(false);
        welcomePanel.SetActive(true);
        welcomeText.text = $"Welcome, {playerName}!";
        playButton.interactable = true;
        statusText.text = "";

        // Retrieve and cache current user profile
        var userService = new RamRoutes.Services.UserService();
        string userId = auth.CurrentUser != null ? auth.CurrentUser.UserId : "unknown";
        await userService.RetrieveAndCacheCurrentUserProfile(userId);
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
            SceneManager.LoadScene("LevelRPG");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save attempt: {e.Message}");
            SceneManager.LoadScene("LevelRPG");
        }
    }

    private async Task LoadAndDisplayAttempts()
    {
        try
        {
            List<GamePlay> attempts = await FirestoreUtility.GetGameCompletions();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load attempts: {e.Message}");
        }
    }

    private async Task UpdateLeaderboard()
    {
        try
        {
            // Get all users ordered by points
            var db = FirebaseFirestore.DefaultInstance;
            var querySnapshot = await db.Collection("users")
                .OrderByDescending("points")
                .Limit(3)
                .GetSnapshotAsync();

            var leaderboardTexts = new[] { firstPlaceText, secondPlaceText, thirdPlaceText };
            
            // Clear all texts first
            foreach (var text in leaderboardTexts)
            {
                if (text != null) text.text = "";
            }

            int index = 0;
            foreach (var doc in querySnapshot.Documents)
            {
                if (index < leaderboardTexts.Length && leaderboardTexts[index] != null)
                {
                    var userData = doc.ToDictionary();
                    string name = userData.ContainsKey("name") ? userData["name"].ToString() : "Unknown";
                    long points = userData.ContainsKey("points") ? Convert.ToInt64(userData["points"]) : 0;
                    
                    leaderboardTexts[index].text = $"{name}";
                }
                index++;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update leaderboard: {e.Message}");
        }
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
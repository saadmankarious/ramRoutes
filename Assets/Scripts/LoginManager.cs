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

    [Header("Event List")]
    public ScrollRect trialsScrollView;
    public Transform trialContentParent;
    public GameObject unlockPrefab;  // Prefab for unlock events
    public GameObject eventPrefab;    // Prefab for building events

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

        // Load unlocks
        await UpdateUnlockHistory();

        // Load events
        // await UpdateEventList();
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
    }    private async Task UpdateUnlockHistory()
    {
        try
        {
            var db = FirebaseFirestore.DefaultInstance;
            
            // Get both unlocks and events
            var unlocksTask = db.Collection("unlocked-trials")
                .OrderByDescending("unlockTime")
                .GetSnapshotAsync();
                
            var eventsTask = db.Collection("building-events")
                .OrderByDescending("date")
                .GetSnapshotAsync();

            // Wait for both queries to complete
            await Task.WhenAll(unlocksTask, eventsTask);
            
            // Clear existing entries
            if (trialContentParent != null)
            {
                foreach (Transform child in trialContentParent)
                {
                    Destroy(child.gameObject);
                }
            }

            // Combine and sort both types of entries
            var allEntries = new List<(DateTime time, string text)>();

            // Process unlocks
            foreach (var doc in unlocksTask.Result.Documents)
            {
                var data = doc.ToDictionary();
                string userName = data.ContainsKey("userName") ? data["userName"].ToString() : "Unknown";
                string buildingName = data.ContainsKey("buildingName") ? data["buildingName"].ToString() : "Unknown Building";
                var time = data.ContainsKey("unlockTime") && data["unlockTime"] is Timestamp timestamp 
                    ? timestamp.ToDateTime()
                    : DateTime.MinValue;

                allEntries.Add((time, $"{userName} unlocked {buildingName}"));
            }

            // Process events
            foreach (var doc in eventsTask.Result.Documents)
            {
                var data = doc.ToDictionary();
                string buildingName = data.ContainsKey("buildingName") ? data["buildingName"].ToString() : "Unknown Building";
                string eventName = data.ContainsKey("eventName") ? data["eventName"].ToString() : "Unknown Event";
                string eventDetails = data.ContainsKey("details") ? data["details"].ToString() : "";
                var time = data.ContainsKey("date") && data["date"] is Timestamp timestamp
                    ? timestamp.ToDateTime()
                    : DateTime.MinValue;

                // Format the event text with more details if available
                string eventText = string.IsNullOrEmpty(eventDetails)
                    ? $"Event: {eventName} at {buildingName}"
                    : $"Event: {eventName} at {buildingName} - {eventDetails}";
                                    allEntries.Add((time,eventText));

            }

            // Sort all entries by time
            //allEntries.Sort((a, b) => b.time.CompareTo(a.time));

            // Create entries in scroll view
            foreach (var entry in allEntries)
            {
                if (unlockPrefab != null && eventPrefab != null && trialContentParent != null)
                {                GameObject listEntry;
                    // Use appropriate prefab based on event type and ensure prefab exists
                    if (entry.text.Contains("unlocked"))
                    {
                        if (unlockPrefab != null)
                        {
                            listEntry = Instantiate(unlockPrefab, trialContentParent);
                        }
                        else
                        {
                            Debug.LogError("Unlock prefab is missing!");
                            continue;
                        }
                    }
                    else
                    {
                        if (eventPrefab != null)
                        {
                            listEntry = Instantiate(eventPrefab, trialContentParent);
                        }
                        else
                        {
                            Debug.LogError("Event prefab is missing!");
                            continue;
                        }
                    }

                    Text entryText = listEntry.GetComponentInChildren<Text>();
                    
                    if (entryText != null)
                    {
                        entryText.text = $"{entry.text}\n{entry.time.ToLocalTime():MMM dd, yyyy h:mm tt}";
                    }
                }
            }

            // Force layout update
            if (trialsScrollView != null)
            {
                Canvas.ForceUpdateCanvases();
                trialsScrollView.verticalNormalizedPosition = 1f;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update unlock history: {e.Message}");
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
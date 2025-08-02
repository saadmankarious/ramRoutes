using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Collections;  // Add this for IEnumerator

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
    [SerializeField] private float scrollSpeed = 0.1f; // Speed of scroll (0.1 = 10% of the scroll view per second)
    [SerializeField] private float resetDelay = 1f; // Delay before resetting to top
    private bool isScrollingPaused = false;
    private bool isResetting = false;

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
        playButton.interactable = true;
        statusText.text = "";

        // Retrieve and cache current user profile
        var userService = new RamRoutes.Services.UserService();
        string userId = auth.CurrentUser != null ? auth.CurrentUser.UserId : "unknown";
        var user = await userService.RetrieveAndCacheCurrentUserProfile(userId);
                welcomeText.text = $"Welcome, {user.name.Split(" ")[0]}!";

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
    } private async Task UpdateUnlockHistory()
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
                allEntries.Add((time, eventText));

            }

            // Sort all entries by time
            //allEntries.Sort((a, b) => b.time.CompareTo(a.time));
            allEntries.Shuffle();
            // Create entries in scroll view
            foreach (var entry in allEntries)
            {
                if (unlockPrefab != null && eventPrefab != null && trialContentParent != null)
                { GameObject listEntry;
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
                }            }       
            {
                Canvas.ForceUpdateCanvases();

                // Add event triggers for pausing auto-scroll on interaction
                var eventTrigger = trialsScrollView.gameObject.GetComponent<EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = trialsScrollView.gameObject.AddComponent<EventTrigger>();
                }

                var pointerEnterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                pointerEnterEntry.callback.AddListener((data) => { OnScrollViewPointerEnter(); });
                eventTrigger.triggers.Add(pointerEnterEntry);

                var pointerExitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                pointerExitEntry.callback.AddListener((data) => { OnScrollViewPointerExit(); });
                eventTrigger.triggers.Add(pointerExitEntry);
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
    }    void Update()
    {
        // Auto-scroll the events list
        if (trialsScrollView != null && !isScrollingPaused && trialContentParent.childCount > 0)
        {
            // Calculate content height
            float contentHeight = 0;
            foreach (RectTransform child in trialContentParent)
            {
                contentHeight += child.rect.height;
            }

            // Only scroll if there's enough content to scroll
            if (contentHeight > trialsScrollView.viewport.rect.height)
            {
                // float currentPos = Mathf.Clamp01(trialsScrollView.verticalNormalizedPosition);
                float newPosition = trialsScrollView.verticalNormalizedPosition - scrollSpeed;

                if (newPosition <= 0)
                {
                    // When reaching bottom, wait a moment then reset to top
                    StartCoroutine(ResetScrollPosition());
                }
                else
                {
                    trialsScrollView.verticalNormalizedPosition = newPosition;
                }
            }
        }
    }    private IEnumerator ResetScrollPosition()
    {
        if (!isResetting)
        {
            isResetting = true;
            yield return new WaitForSeconds(resetDelay);
            trialsScrollView.verticalNormalizedPosition = 0f;  // Start from the bottom
            isResetting = false;
        }
    }

    // Add pause/resume functionality when user interacts with scroll view
    public void OnScrollViewPointerEnter()
    {
        isScrollingPaused = true;
    }

    public void OnScrollViewPointerExit()
    {
        isScrollingPaused = false;
    }

}


public static class ListExtensions
{
    private static System.Random rng = new System.Random(); // Use a single instance of Random for better randomness

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1); // Get a random index from 0 to n
            T value = list[k];       // Store the value at the random index
            list[k] = list[n];       // Move the value from the current end (n) to the random index (k)
            list[n] = value;         // Move the stored value (from k) to the current end (n)
        }
    }
}
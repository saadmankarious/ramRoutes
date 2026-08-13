using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections;
using RamRoutes.Services;

/// <summary>
/// Simplified guest registration flow: no Cornell/domain email restrictions, no email
/// verification, no email collected at all, no skin/avatar, no leaderboard, no events
/// feed, no background music. Users register with a username that's checked for
/// uniqueness app-side. Every successful login/signup goes straight to the welcome/play
/// screen - there's no separate onboarding scene. All UI fields are found at runtime by
/// GameObject name via Transform.FindDeepChild rather than assigned in the Inspector.
/// </summary>
public class GuestLoginManager : MonoBehaviour
{
    private InputField usernameInput;
    private InputField passwordInput;
    private Button loginButton;
    private Button playButton;
    private Button logoutButton;
    private Button deleteAccountButton;
    private GameObject deleteConfirmationPanel;
    private Text statusText;
    private GameObject loginPanel;
    private GameObject welcomePanel;
    private Text welcomeText;

    private Text usernameText;
    private Text userCoinsText;
    private Text userKBText;

    private InputField signupUsernameInput;
    private InputField signupPasswordInput;
    private Button signupButton;
    private GameObject signupPanel;
    private Text toggleText;
    private Text signupToggleText;

    private InputField bioInputField;
    private Button bioSaveButton;

    // Firebase Auth's email/password provider requires an email-shaped identifier,
    // but this app collects no real email at all - registration is guest/username-only.
    // This suffix builds an internal-only address from the username purely to satisfy
    // that API; it's never shown to the user and nothing is ever sent to it.
    private const string SyntheticEmailDomain = "@guest.ramroutes.app";

    private FirebaseAuth auth;
    private string playerName = "";
    private bool isSignupMode = false;
    private bool isEditingBio = false;

    // Delete account confirmation state
    private bool isAwaitingDeleteConfirmation = false;
    private TaskCompletionSource<bool> deleteConfirmationTask;

    private async void Start()
    {
        BindUI();

        // Initialize UI
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        welcomePanel.SetActive(false);
        playButton.interactable = false;

        // Setup button listeners
        loginButton.onClick.AddListener(OnLoginClicked);
        signupButton.onClick.AddListener(OnSignupClicked);
        playButton.onClick.AddListener(OnPlayClicked);
        logoutButton.onClick.AddListener(OnLogoutClicked);

        if (deleteAccountButton != null)
        {
            deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);
        }

        // Setup toggle text click listener
        if (toggleText != null)
        {
            var toggleButton = toggleText.gameObject.GetComponent<Button>();
            if (toggleButton == null)
            {
                toggleButton = toggleText.gameObject.AddComponent<Button>();
            }
            toggleButton.onClick.AddListener(OnToggleModeClicked);
        }

        // Setup signup toggle text click listener
        if (signupToggleText != null)
        {
            var signupToggleButton = signupToggleText.gameObject.GetComponent<Button>();
            if (signupToggleButton == null)
            {
                signupToggleButton = signupToggleText.gameObject.AddComponent<Button>();
            }
            signupToggleButton.onClick.AddListener(OnToggleModeClicked);
        }

        // Setup bio input and button
        SetupBioComponents();

        // Update toggle text
        UpdateToggleText();

        // Initialize Firebase
        await InitializeFirebase();
        CheckAuthState();
    }

    /// <summary>
    /// Resolves every UI reference by GameObject name via Transform.FindDeepChild
    /// instead of Inspector-assigned fields.
    /// </summary>
    private void BindUI()
    {
        usernameInput = transform.FindDeepChild("username")?.GetComponent<InputField>();
        passwordInput = transform.FindDeepChild("password")?.GetComponent<InputField>();
        loginButton = transform.FindDeepChild("login-button")?.GetComponent<Button>();
        playButton = transform.FindDeepChild("play-button")?.GetComponent<Button>();
        logoutButton = transform.FindDeepChild("logout-button")?.GetComponent<Button>();
        deleteAccountButton = transform.FindDeepChild("delete-account-button")?.GetComponent<Button>();
        deleteConfirmationPanel = transform.FindDeepChild("delete-confirmation-panel")?.gameObject;
        statusText = transform.FindDeepChild("status-text")?.GetComponent<Text>();
        loginPanel = transform.FindDeepChild("login-panel")?.gameObject;
        welcomePanel = transform.FindDeepChild("welcome-panel")?.gameObject;
        welcomeText = transform.FindDeepChild("welcome-text")?.GetComponent<Text>();

        usernameText = transform.FindDeepChild("username-text")?.GetComponent<Text>();
        userCoinsText = transform.FindDeepChild("coins-text")?.GetComponent<Text>();
        userKBText = transform.FindDeepChild("kb-text")?.GetComponent<Text>();

        signupUsernameInput = transform.FindDeepChild("signup-username")?.GetComponent<InputField>();
        signupPasswordInput = transform.FindDeepChild("signup-password")?.GetComponent<InputField>();
        signupButton = transform.FindDeepChild("signup-button")?.GetComponent<Button>();
        signupPanel = transform.FindDeepChild("signup-panel")?.gameObject;
        toggleText = transform.FindDeepChild("toggle-text")?.GetComponent<Text>();
        signupToggleText = transform.FindDeepChild("signup-toggle-text")?.GetComponent<Text>();

        bioInputField = transform.FindDeepChild("bio-input")?.GetComponent<InputField>();
        bioSaveButton = transform.FindDeepChild("bio-save-button")?.GetComponent<Button>();
    }

    private void SetupBioComponents()
    {
        if (bioSaveButton != null)
        {
            bioSaveButton.onClick.AddListener(OnBioButtonClicked);
        }

        // Set initial state - bio is not editable
        if (bioInputField != null)
        {
            bioInputField.interactable = false;
        }

        if (bioSaveButton != null)
        {
            var buttonText = bioSaveButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Edit";
            }
        }
    }

    private async Task InitializeFirebase()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                await FirestoreUtility.Initialize();

                auth.StateChanged += AuthStateChanged;

                Debug.Log("Firebase initialized successfully");
            }
            else
            {
                Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
                statusText.text = "Firebase initialization failed";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Firebase initialization error: {e.Message}");
            statusText.text = "Firebase initialization error";
        }
    }

    private void OnToggleModeClicked()
    {
        isSignupMode = !isSignupMode;
        UpdateToggleText();
        TogglePanels();
    }

    private void UpdateToggleText()
    {
        if (toggleText != null)
        {
            toggleText.text = isSignupMode ? "Login Instead" : "Create Account";
        }

        if (signupToggleText != null)
        {
            signupToggleText.text = "Login Instead";
        }
    }

    private void TogglePanels()
    {
        loginPanel.SetActive(!isSignupMode);
        signupPanel.SetActive(isSignupMode);

        // Clear status text when toggling
        ClearSignupStatus();

        // Reset button states
        loginButton.interactable = true;
        signupButton.interactable = true;
    }

    // ---- Signup status helpers ----
    private void ShowSignupStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void ClearSignupStatus()
    {
        if (statusText != null) statusText.text = "";
    }

    private async Task<bool> IsUsernameTakenAsync(string username)
    {
        var db = FirebaseFirestore.DefaultInstance;
        var snapshot = await db.Collection("users")
            .WhereEqualTo("name", username)
            .Limit(1)
            .GetSnapshotAsync();
        return snapshot.Count > 0;
    }

    private async void OnSignupClicked()
    {
        // Defensive: make sure inputs exist
        if (signupUsernameInput == null || signupPasswordInput == null)
        {
            ShowSignupStatus("Signup form not configured. Please contact support.");
            return;
        }

        string username = signupUsernameInput.text.Trim();
        string password = signupPasswordInput.text;

        ClearSignupStatus();

        if (string.IsNullOrEmpty(username))
        {
            ShowSignupStatus("Please enter a username");
            return;
        }

        if (username.Contains(" ") || username.Contains("@"))
        {
            ShowSignupStatus("Username cannot contain spaces or @ symbol");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowSignupStatus("Please enter a password");
            return;
        }

        if (password.Length < 6)
        {
            ShowSignupStatus("Password must be at least 6 characters long");
            return;
        }

        if (auth == null)
        {
            ShowSignupStatus("Firebase not initialized. Please restart the app.");
            return;
        }

        ShowSignupStatus("Creating account...");
        signupButton.interactable = false;

        try
        {
            if (await IsUsernameTakenAsync(username))
            {
                ShowSignupStatus("That username is already taken.");
                signupButton.interactable = true;
                return;
            }

            string email = $"{username}{SyntheticEmailDomain}";
            var authResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            string userId = authResult.User.UserId;

            var userService = new RamRoutes.Services.UserService();
            await userService.CreateUser(userId, username, email);

            // Clear all existing cache data to ensure fresh start for new user
            RamRoutes.Services.UserService.ClearUserCache();

            // Store user info in PlayerPrefs for easy access
            PlayerPrefs.SetString("UserName", username);
            PlayerPrefs.SetString("UserStatus", "Studying");
            PlayerPrefs.Save();

            // Account creation signs the user in automatically - AuthStateChanged
            // picks this up and shows the welcome panel, same as a normal login.
        }
        catch (FirebaseException e)
        {
            ShowSignupStatus(GetFirebaseErrorMessage(e));
            signupButton.interactable = true;
        }
        catch (System.Exception e)
        {
            ShowSignupStatus("Failed to create account: " + e.Message);
            signupButton.interactable = true;
        }
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != null)
        {
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
        // Extract username from the synthetic email (the part before @)
        playerName = email.Split('@')[0];
        PlayerPrefs.SetString("PlayerName", playerName);

        // Retrieve and cache current user profile
        var userService = new RamRoutes.Services.UserService();
        string userId = auth.CurrentUser != null ? auth.CurrentUser.UserId : "unknown";

        // Fetch user profile and store in PlayerPrefs
        try
        {
            var user = await userService.GetUserProfileCachedOrRemoteAsync(userId);
            if (user != null)
            {
                PlayerPrefs.SetString("UserName", !string.IsNullOrEmpty(user.name) ? user.name : playerName);
                PlayerPrefs.SetString("UserStatus", user.GetStatusAsString());
                PlayerPrefs.Save();
                Debug.Log($"Stored user profile in PlayerPrefs: {user.name}, status={user.GetStatusAsString()}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to retrieve user profile: {e.Message}");
        }

        // Update last login timestamp
        try
        {
            await userService.UpdateLastLogin(userId);
            Debug.Log("Successfully updated last login time");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update last login: {e.Message}");
        }

        // Retrieve user profile after updating login time
        var userProfile = await userService.RetrieveAndCacheCurrentUserProfile(userId);

        // Update welcome panel with user information
        await UpdateWelcomePanelUserInfo(userId, userProfile);

        // Update FCM token in user profile for notifications
        try
        {
            var firebaseMessaging = FindObjectOfType<FirebaseMessagingManager>();
            if (firebaseMessaging != null)
            {
                firebaseMessaging.UpdateCurrentUserToken();
                Debug.Log("Triggered FCM token update for user profile");
            }
            else
            {
                Debug.LogWarning("FirebaseMessagingManager not found, FCM token not updated");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to trigger FCM token update: {e.Message}");
        }

        // Every successful login/signup goes straight to the welcome/play screen -
        // there's no separate onboarding scene anymore.
        if (loginPanel != null) loginPanel.SetActive(false);
        if (signupPanel != null) signupPanel.SetActive(false);
        if (welcomePanel != null) welcomePanel.SetActive(true);
        if (playButton != null) playButton.interactable = true;
        if (statusText != null) statusText.text = "";
    }

    /// <summary>
    /// Updates the welcome panel with detailed user information
    /// </summary>
    private async Task UpdateWelcomePanelUserInfo(string userId, RamRoutes.Model.User userProfile)
    {
        try
        {
            if (this == null) return;

            if (usernameText != null)
            {
                usernameText.text = userProfile.name ?? "Unknown User";
            }

            var userService = new RamRoutes.Services.UserService();

            if (this == null) return;

            int coins = await userService.GetUserCoins(userId);

            if (this == null) return;

            int kb = await userService.GetUserKnowledgePoints(userId);

            if (this == null) return;

            if (userCoinsText != null)
            {
                userCoinsText.text = $"{coins}";
            }

            if (userKBText != null)
            {
                userKBText.text = $"{kb}";
            }

            if (bioInputField != null)
            {
                bioInputField.text = userProfile.bio ?? "";
                bioInputField.interactable = false; // Start in view mode
            }

            if (bioSaveButton != null)
            {
                var buttonText = bioSaveButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Edit";
                }
                isEditingBio = false;
            }

            if (welcomeText != null)
            {
                welcomeText.gameObject.SetActive(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update welcome panel user info: {e.Message}");

            if (this == null) return;

            if (welcomeText != null)
            {
                welcomeText.text = $"Welcome, {userProfile.name?.Split(" ")[0] ?? "User"}!";
                welcomeText.gameObject.SetActive(true);
            }
        }
    }

    private async void OnLoginClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter username and password";
            return;
        }

        statusText.text = "Logging in...";
        loginButton.interactable = false;

        if (auth == null)
        {
            statusText.text = "Firebase not initialized. Please restart the app.";
            loginButton.interactable = true;
            return;
        }

        string email = $"{username}{SyntheticEmailDomain}";

        try
        {
            await auth.SignInWithEmailAndPasswordAsync(email, password);
            // AuthStateChanged handles the UI update.
        }
        catch (FirebaseException e)
        {
            statusText.text = GetFirebaseErrorMessage(e);
            loginButton.interactable = true;
        }
    }

    private async void OnDeleteAccountClicked()
    {
        if (!await ShowDeleteConfirmation())
        {
            return;
        }

        if (auth?.CurrentUser == null)
        {
            statusText.text = "No user logged in";
            return;
        }

        string userId = auth.CurrentUser.UserId;
        string userEmail = auth.CurrentUser.Email;

        statusText.text = "Deleting account...";
        deleteAccountButton.interactable = false;

        try
        {
            var db = FirebaseFirestore.DefaultInstance;

            await DeleteUserDataFromFirestore(userId, db);
            await auth.CurrentUser.DeleteAsync();

            ClearAllUserData();
            ResetToLoginState();

            statusText.text = "Account deleted successfully";

            Debug.Log($"Successfully deleted account for user: {userEmail}");
        }
        catch (FirebaseException e)
        {
            statusText.text = $"Failed to delete account: {GetFirebaseErrorMessage(e)}";
            deleteAccountButton.interactable = true;
            Debug.LogError($"Firebase error deleting account: {e.Message}");
        }
        catch (System.Exception e)
        {
            statusText.text = $"Failed to delete account: {e.Message}";
            deleteAccountButton.interactable = true;
            Debug.LogError($"Error deleting account: {e.Message}");
        }
    }

    private async Task<bool> ShowDeleteConfirmation()
    {
        isAwaitingDeleteConfirmation = true;
        deleteConfirmationTask = new TaskCompletionSource<bool>();

        if (deleteConfirmationPanel != null)
        {
            deleteConfirmationPanel.SetActive(true);
        }

        statusText.text = "Please confirm account deletion in the dialog.";

        bool result = await deleteConfirmationTask.Task;

        if (deleteConfirmationPanel != null)
        {
            deleteConfirmationPanel.SetActive(false);
        }

        isAwaitingDeleteConfirmation = false;
        deleteConfirmationTask = null;

        return result;
    }

    /// <summary>
    /// Call this method when the user confirms deletion in the dialog (attach to Yes button)
    /// </summary>
    public void OnDeleteConfirmationYes()
    {
        if (isAwaitingDeleteConfirmation && deleteConfirmationTask != null)
        {
            statusText.text = "Deletion confirmed. Processing...";
            deleteConfirmationTask.SetResult(true);
        }
        else
        {
            Debug.LogWarning("OnDeleteConfirmationYes called but no deletion is pending");
        }
    }

    /// <summary>
    /// Call this method when the user cancels deletion in the dialog (attach to No button)
    /// </summary>
    public void OnDeleteConfirmationNo()
    {
        if (isAwaitingDeleteConfirmation && deleteConfirmationTask != null)
        {
            statusText.text = "Account deletion cancelled";
            deleteConfirmationTask.SetResult(false);
        }
        else
        {
            Debug.LogWarning("OnDeleteConfirmationNo called but no deletion is pending");
        }
    }

    private async Task DeleteUserDataFromFirestore(string userId, FirebaseFirestore db)
    {
        try
        {
            await db.Collection("users").Document(userId).DeleteAsync();
            Debug.Log("Deleted user profile document");

            var unlocksQuery = await db.Collection("unlocked-trials")
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            foreach (var doc in unlocksQuery.Documents)
            {
                await doc.Reference.DeleteAsync();
            }
            Debug.Log($"Deleted {unlocksQuery.Count} unlock records");

            var eventsQuery = await db.Collection("building-events")
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            foreach (var doc in eventsQuery.Documents)
            {
                await doc.Reference.DeleteAsync();
            }
            Debug.Log($"Deleted {eventsQuery.Count} building event records");

            var sentRequestsQuery = await db.Collection("friend-requests")
                .WhereEqualTo("fromUserId", userId)
                .GetSnapshotAsync();

            foreach (var doc in sentRequestsQuery.Documents)
            {
                await doc.Reference.DeleteAsync();
            }

            var receivedRequestsQuery = await db.Collection("friend-requests")
                .WhereEqualTo("toUserId", userId)
                .GetSnapshotAsync();

            foreach (var doc in receivedRequestsQuery.Documents)
            {
                await doc.Reference.DeleteAsync();
            }
            Debug.Log($"Deleted {sentRequestsQuery.Count + receivedRequestsQuery.Count} friend request records");

            var friendsQuery = await db.Collection("friends")
                .WhereArrayContains("userIds", userId)
                .GetSnapshotAsync();

            foreach (var doc in friendsQuery.Documents)
            {
                await doc.Reference.DeleteAsync();
            }
            Debug.Log($"Deleted {friendsQuery.Count} friendship records");

            await db.Collection("game-stages").Document(userId).DeleteAsync();
            Debug.Log("Deleted game stage document");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error deleting user data from Firestore: {e.Message}");
            throw;
        }
    }

    private void ClearAllUserData()
    {
        RamRoutes.Services.UserService.ClearUserCache();

        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.DeleteKey("UserName");
        PlayerPrefs.Save();

        Debug.Log("Cleared all local user data and cache");
    }

    private void ResetToLoginState()
    {
        isSignupMode = false;
        UpdateToggleText();
        TogglePanels();

        welcomePanel.SetActive(false);
        playButton.interactable = false;
        deleteAccountButton.interactable = true;

        usernameInput.text = "";
        passwordInput.text = "";
        signupUsernameInput.text = "";
        signupPasswordInput.text = "";

        if (bioInputField != null)
        {
            bioInputField.text = "";
            bioInputField.interactable = false;
        }

        if (bioSaveButton != null)
        {
            var buttonText = bioSaveButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Edit";
            }
            isEditingBio = false;
        }

        Debug.Log("Reset UI to login state after account deletion");
    }

    private void OnLogoutClicked()
    {
        if (auth != null)
        {
            auth.SignOut();
            RamRoutes.Services.UserService.ClearUserCache();
        }

        isSignupMode = false;
        UpdateToggleText();
        TogglePanels();

        welcomePanel.SetActive(false);
        playButton.interactable = false;

        usernameInput.text = "";
        passwordInput.text = "";
        signupUsernameInput.text = "";
        signupPasswordInput.text = "";

        statusText.text = "Logged out successfully";
    }

    private async void OnBioButtonClicked()
    {
        if (bioInputField == null || bioSaveButton == null)
        {
            Debug.LogWarning("Bio components not found");
            return;
        }

        var buttonText = bioSaveButton.GetComponentInChildren<Text>();

        if (!isEditingBio)
        {
            isEditingBio = true;
            bioInputField.interactable = true;
            bioInputField.ActivateInputField();

            if (buttonText != null)
            {
                buttonText.text = "Save";
            }
        }
        else
        {
            string newBio = bioInputField.text.Trim();

            if (newBio.Length > 33)
            {
                Debug.LogWarning("Bio too long, maximum 33 characters allowed");
                if (buttonText != null)
                {
                    StartCoroutine(ShowTemporaryButtonText(buttonText, "Too Long", "Save"));
                }
                bioInputField.text = newBio.Substring(0, 33);
                return;
            }

            if (newBio.Length == 0)
            {
                Debug.LogWarning("Bio is empty");
                if (buttonText != null)
                {
                    StartCoroutine(ShowTemporaryButtonText(buttonText, "Too Short", "Save"));
                }
                return;
            }

            if (auth?.CurrentUser != null)
            {
                try
                {
                    var userService = new RamRoutes.Services.UserService();
                    await userService.UpdateUserBio(auth.CurrentUser.UserId, newBio);

                    isEditingBio = false;
                    bioInputField.interactable = false;

                    if (buttonText != null)
                    {
                        buttonText.text = "Edit";
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to update bio: {e.Message}");
                }
            }
        }
    }

    private IEnumerator ShowTemporaryButtonText(Text buttonText, string temporaryText, string originalText, float duration = 2f)
    {
        if (buttonText != null)
        {
            buttonText.text = temporaryText;
            yield return new WaitForSeconds(duration);
            buttonText.text = originalText;
        }
    }

    private string GetFirebaseErrorMessage(FirebaseException e)
    {
        return e.Message switch
        {
            string msg when msg.Contains("EMAIL_ALREADY_IN_USE") => "That username is already taken",
            string msg when msg.Contains("INVALID_EMAIL") => "Invalid username",
            string msg when msg.Contains("EMAIL_NOT_FOUND") => "Account not found",
            string msg when msg.Contains("WRONG_PASSWORD") => "Incorrect password",
            string msg when msg.Contains("TOO_MANY_REQUESTS") => "Too many attempts. Try again later",
            string msg when msg.Contains("USER_DISABLED") => "Account disabled",
            _ => "Login failed: Incorrect Credentials"
        };
    }

    private async void OnPlayClicked()
    {
        try
        {
            await FirestoreUtility.SaveGameAttempt(playerName);
            SceneManager.LoadScene("DCRPG");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save attempt: {e.Message}");
            SceneManager.LoadScene("DCRPG");
        }
    }

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }
}

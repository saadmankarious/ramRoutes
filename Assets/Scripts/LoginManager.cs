using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using System.Collections;  // Add this for IEnumerator
using RamRoutes.Model;
using RamRoutes.Services;
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
    public Text resetPasswordText;

    [Header("Welcome Panel User Info")]
    public Text usernameText;
    public Text userCoinsText;
    public Text userKBText;
    public UnityEngine.UI.Image userAvatarImage;
    public Text userHallText;
    public Text userStageText;

      [Header("User Avatar")]
    public Sprite rank1AvatarSprite; // Rank 1 avatar sprite (0-999 combined points)
    public Sprite rank2AvatarSprite; // Rank 2 avatar sprite (1000-1999 combined points)
    public Sprite rank3AvatarSprite; // Rank 3 avatar sprite (2000+ combined points)


    [Header("Reset Password UI")]
    public InputField resetUsernameInput;
    public Button resetPasswordButton;
    public GameObject resetPasswordPanel;
    public Text backToLoginText;
    public Text resetStatusText;

    [Header("Signup UI")]
    public InputField signupUsernameInput;
    public InputField signupPasswordInput;
    public Dropdown residenceHallDropdown;
    public Button signupButton;
    public GameObject signupPanel;
    public Text toggleText;
    public Text signupToggleText;
    public Text verificationStatusText;
    
    [Header("Domain Settings")]
    public bool allowExternalDomains = false;

    [Header("Leaderboard")]
    [Tooltip("Panel containing the leaderboard with a Vertical Layout Group")]
    public GameObject leaderboardPanel;
    [Tooltip("Transform that acts as parent for leaderboard entries (should have Vertical Layout Group component)")]
    public Transform leaderboardContentParent;
    [Tooltip("Prefab for leaderboard entry. Expected child objects: NameText, PointsText/CoinsText, KBText/KnowledgeText, HallText/ResidenceText, RankImage/AvatarImage, PositionText, RankText/RankNameText")]
    public GameObject leaderboardEntryPrefab;

    [Header("Event List")]
    public ScrollRect trialsScrollView;
    public Transform trialContentParent;
    public GameObject unlockPrefab;  // Prefab for unlock events
    public GameObject eventPrefab;    // Prefab for building events
    [SerializeField] private float scrollSpeed = 0.1f; // Speed of scroll (0.1 = 10% of the scroll view per second)
    [SerializeField] private float resetDelay = 1f; // Delay before resetting to top
    private bool isScrollingPaused = false;
    private bool isResetting = false;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float musicVolume = 0.3f;
    private AudioSource musicSource;

    private FirebaseAuth auth;
    private string playerName = "";
    private bool isSignupMode = false;
    private bool isResetPasswordMode = false;
    
    // Resend verification fields
    private int resendCountdownSeconds = 10;
    private Coroutine resendCountdownCoroutine = null;
    private string lastSignupUserId = null;
    private string lastSignupEmail = null;
    private float countdownTimer = 0f;
    private bool isCountingDown = false;

    private async void Start()
    {
        // Setup background music
        SetupBackgroundMusic();

        // Initialize UI
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        resetPasswordPanel.SetActive(false);
        welcomePanel.SetActive(false);
        playButton.interactable = false;

        // Setup button listeners
        loginButton.onClick.AddListener(OnLoginClicked);
        signupButton.onClick.AddListener(OnSignupClicked);
        playButton.onClick.AddListener(OnPlayClicked);
        logoutButton.onClick.AddListener(OnLogoutClicked);
        resetPasswordButton.onClick.AddListener(OnResetPasswordButtonClicked);
        
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

        // Setup reset password text click listener
        if (resetPasswordText != null)
        {
            var resetPasswordButton = resetPasswordText.gameObject.GetComponent<Button>();
            if (resetPasswordButton == null)
            {
                resetPasswordButton = resetPasswordText.gameObject.AddComponent<Button>();
            }
            resetPasswordButton.onClick.AddListener(OnResetPasswordTextClicked);
        }

        // Setup back to login text click listener
        if (backToLoginText != null)
        {
            var backToLoginButton = backToLoginText.gameObject.GetComponent<Button>();
            if (backToLoginButton == null)
            {
                backToLoginButton = backToLoginText.gameObject.AddComponent<Button>();
            }
            backToLoginButton.onClick.AddListener(OnBackToLoginClicked);
        }

        // Initialize residence hall dropdown
        InitializeResidenceHallDropdown();
        
        // Update toggle text
        UpdateToggleText();
        
        // Update UI based on domain settings
        UpdateDomainUI();

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

    private void SetupBackgroundMusic()
    {
        if (backgroundMusic != null)
        {
            // Create a new GameObject to hold the AudioSource
            GameObject musicObject = new GameObject("BackgroundMusic");
            musicSource = musicObject.AddComponent<AudioSource>();
            
            // Configure the AudioSource
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            
            // Don't destroy the music object when changing scenes
            DontDestroyOnLoad(musicObject);
            
            // Start playing the music
            musicSource.Play();
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

                // Auth state persistence is now automatic in newer Firebase versions
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

    private void InitializeResidenceHallDropdown()
    {
        if (residenceHallDropdown != null)
        {
            residenceHallDropdown.ClearOptions();
            List<string> residenceHalls = new List<string>
            {
                "Select Residence Hall",
                "Tarr Hall",
                "Pfeiffer Hall",
                "Dows Hall",
                "Bowman Carter Hall",
                "Merner Hall",
                "Olin Hall",
                "Pauley-Rorem Hall",
                "Russell Hall",
                "Smith Hall",
                "Wilch Appartments",
                "Other"
            };
            residenceHallDropdown.AddOptions(residenceHalls);
            residenceHallDropdown.value = 0;
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
    
    private void UpdateDomainUI()
    {
        // Update username input placeholder if available
        if (signupUsernameInput != null)
        {
            var placeholder = signupUsernameInput.placeholder as Text;
            if (placeholder != null)
            {
                placeholder.text = allowExternalDomains ? "Enter full email address" : "Enter username (without @cornellcollege.edu)";
            }
        }
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void TogglePanels()
    {
        loginPanel.SetActive(!isSignupMode && !isResetPasswordMode);
        signupPanel.SetActive(isSignupMode && !isResetPasswordMode);
        resetPasswordPanel.SetActive(isResetPasswordMode);
        
        // Clear all status texts when toggling
        ClearSignupStatus();
        
        // Reset button states
        loginButton.interactable = true;
        signupButton.interactable = true;
        resetPasswordButton.interactable = true;
        
        // Reset signup button text and listeners when switching panels
        if (signupButton != null)
        {
            var buttonText = signupButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Sign Up";
            }
            signupButton.onClick.RemoveAllListeners();
            signupButton.onClick.AddListener(OnSignupClicked);
            
            // Stop any running countdown
            if (resendCountdownCoroutine != null)
            {
                StopCoroutine(resendCountdownCoroutine);
                resendCountdownCoroutine = null;
            }
            isCountingDown = false;
        }
    }

    // ---- Signup status helpers ----
    private void ShowSignupStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        // Optionally mirror to verification text if present
        if (verificationStatusText != null && string.IsNullOrEmpty(message) == false)
        {
            // Keep the same message visible in signup panel too
            verificationStatusText.text = message;
        }
    }

    private void ClearSignupStatus()
    {
        if (statusText != null) statusText.text = "";
        if (verificationStatusText != null) verificationStatusText.text = "";
    }

    private async void OnSignupClicked()
    {
        // Defensive: make sure inputs exist
        if (signupUsernameInput == null || signupPasswordInput == null || residenceHallDropdown == null)
        {
            ShowSignupStatus("Signup form not configured. Please contact support.");
            return;
        }

        string username = signupUsernameInput.text;
        string password = signupPasswordInput.text;
        string residenceHall = residenceHallDropdown.options[residenceHallDropdown.value].text;

        // Clear previous status
        ClearSignupStatus();

        // Validate inputs
        if (string.IsNullOrEmpty(username))
        {
            ShowSignupStatus("Please enter a username");
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

        if (residenceHallDropdown.value == 0)
        {
            ShowSignupStatus("Please select a residence hall");
            return;
        }

        // Validate username format
        string email;
        if (allowExternalDomains)
        {
            // When external domains are allowed, treat username as full email
            if (!username.Contains("@"))
            {
                ShowSignupStatus("When external domains are enabled, please enter a full email address");
                return;
            }
            
            // Basic email validation
            if (!IsValidEmail(username))
            {
                ShowSignupStatus("Please enter a valid email address");
                return;
            }
            
            email = username;
        }
        else
        {
            // Original behavior - validate username format (no spaces, special characters)
            if ((username.Contains(" ") || username.Contains("@")))
            {
                ShowSignupStatus("Username cannot contain spaces or @ symbol");
                return;
            }
            // Convert username to Cornell College or RamRoutes email format
            email = $"{username}@cornellcollege.edu";
            if (username.EndsWith(".rr"))
            {
                email = $"{username.Replace(".rr","")}@ramroutes.com";
            }
        }

        ShowSignupStatus("Creating account...");
        signupButton.interactable = false;

        // Check if Firebase Auth is properly initialized
        if (auth == null)
        {
            ShowSignupStatus("Firebase not initialized. Please restart the app.");
            signupButton.interactable = true;
            return;
        }

        try
        {
            // Create Firebase user
            var authResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            var firebaseUser = authResult != null ? authResult.User : null;

            // Resolve a reliable userId (fallback to CurrentUser after reload)
            string userId = firebaseUser != null ? firebaseUser.UserId : null;
            if (string.IsNullOrEmpty(userId))
            {
                if (auth.CurrentUser != null)
                {
                    try { await auth.CurrentUser.ReloadAsync(); } catch { }
                    userId = auth.CurrentUser.UserId;
                }
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new System.Exception("Failed to determine user id for the new account");
            }

            // Send verification email (use whichever user instance is available)
            if (!email.EndsWith("@ramroutes.com"))
            {
                var userToVerify = firebaseUser ?? auth.CurrentUser;
                if (userToVerify == null)
                {
                    throw new System.Exception("Failed to access new user instance to send verification email");
                }
                await userToVerify.SendEmailVerificationAsync();
            }

            // Store for resend
            lastSignupUserId = userId;
            lastSignupEmail = email;
            
            // Create user profile in Firestore using UserService
            var userService = new RamRoutes.Services.UserService();
            await userService.CreateUser(userId, username, email, residenceHall);
            
            // Store user info in PlayerPrefs for easy access
            PlayerPrefs.SetString("UserName", username);
            PlayerPrefs.SetString("ResidenceHall", residenceHall);
            
            // Mark this as a new user for onboarding
            PlayerPrefs.SetInt($"FirstTime_{userId}", 1);
            PlayerPrefs.Save();
            
            // Sign out the user immediately since they need to verify email first
            auth.SignOut();
            
            // Success messaging to status text
            ShowSignupStatus($"Confirmation link sent to {email}. Check your mailbox.");
            
            // Stay on signup panel, start countdown for resend
            if (resendCountdownCoroutine != null)
            {
                Debug.Log("Stopping existing countdown coroutine");
                StopCoroutine(resendCountdownCoroutine);
            }
            Debug.Log("Starting resend countdown after successful signup");
            
            // Start both coroutine and Update-based countdown as backup
            // The Update method will be the primary countdown mechanism
            countdownTimer = resendCountdownSeconds;
            isCountingDown = true;
            Debug.Log($"Started Update-based countdown with timer={countdownTimer}");
            
            // Also start coroutine as backup, but Update method will handle the countdown
            resendCountdownCoroutine = StartCoroutine(ResendCountdownRoutine());
            
            if (resendCountdownCoroutine == null)
            {
                Debug.LogError("Failed to start countdown coroutine! Using Update-based timer.");
            }
            else
            {
                Debug.Log("Countdown coroutine started successfully");
            }
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

    private IEnumerator ResendCountdownRoutine()
    {
        Debug.Log("ResendCountdownRoutine started");
        int secondsLeft = resendCountdownSeconds;
        signupButton.interactable = false;
        var buttonText = signupButton.GetComponentInChildren<Text>();
        
        if (buttonText == null)
        {
            Debug.LogError("Button text component not found!");
            signupButton.interactable = true;
            yield break;
        }
        
        Debug.Log($"Button text component found. Starting countdown from {secondsLeft} seconds");
        
        for (int i = secondsLeft; i > 0; i--)
        {
            if (!isCountingDown) // Check if countdown was cancelled
            {
                Debug.Log("Countdown was cancelled, exiting coroutine");
                yield break;
            }
            
            buttonText.text = $"Resend in {i}s";
            Debug.Log($"Coroutine: Updated button text to: {buttonText.text}, seconds left: {i}");
            
            yield return new WaitForSecondsRealtime(1f); // Use WaitForSecondsRealtime instead
            
            Debug.Log($"Coroutine: After waiting 1 second, continuing loop. Next iteration: {i-1}");
        }
        
        Debug.Log("Coroutine countdown finished, enabling resend");
        isCountingDown = false; // Stop the Update-based timer
        buttonText.text = "Resend Confirmation Email";
        signupButton.interactable = true;
        signupButton.onClick.RemoveAllListeners();
        signupButton.onClick.AddListener(OnResendConfirmationClicked);
        Debug.Log("Coroutine: Resend button is now enabled and listener added");
    }

    private async void OnResendConfirmationClicked()
    {
        Debug.Log("Resend confirmation clicked");
        signupButton.interactable = false;
        var buttonText = signupButton.GetComponentInChildren<Text>();
        
        if (buttonText != null)
        {
            buttonText.text = "Sending...";
        }
        
        ShowSignupStatus($"Resending confirmation email to {lastSignupEmail}...");
        
        try
        {
            // Since user already exists, we can't create again. Instead, provide helpful message.
            ShowSignupStatus($"If you haven't received the verification email, please check your spam folder. The email was sent to {lastSignupEmail}.");
            Debug.Log("Showed message about checking spam folder");
        }
        catch (Exception e)
        {
            ShowSignupStatus("Failed to resend confirmation email: " + e.Message);
            Debug.LogError($"Error in resend: {e.Message}");
        }
        
        // Reset button text and restart countdown
        if (buttonText != null)
        {
            buttonText.text = "Sign Up";
        }
        
        signupButton.onClick.RemoveAllListeners();
        signupButton.onClick.AddListener(OnSignupClicked);
        
        // Restart countdown
        if (resendCountdownCoroutine != null)
        {
            StopCoroutine(resendCountdownCoroutine);
        }
        
        Debug.Log("Restarting countdown coroutine");
        resendCountdownCoroutine = StartCoroutine(ResendCountdownRoutine());
        countdownTimer = resendCountdownSeconds;
        isCountingDown = true;
    }

    private async void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != null)
        {
            await auth.CurrentUser.ReloadAsync(); // Refresh to get latest verification status

            if (auth.CurrentUser.IsEmailVerified || auth.CurrentUser.Email.EndsWith("@ramroutes.com"))
            {
                // User is signed in and email is verified
                HandleSuccessfulLogin(auth.CurrentUser.Email);
            }
            else if (!auth.CurrentUser.Email.EndsWith("@ramroutes.com"))
            {
                // User is signed in but email is not verified
                statusText.text = "Please verify your email before accessing the app.";
                auth.SignOut();
            }
        }
    }

    private async void CheckAuthState()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            await auth.CurrentUser.ReloadAsync(); // Refresh to get latest verification status
            
            if (auth.CurrentUser.IsEmailVerified || auth.CurrentUser.Email.EndsWith("@ramroutes.com"))
            {
                HandleSuccessfulLogin(auth.CurrentUser.Email);
            }
            else if (!auth.CurrentUser.Email.EndsWith("@ramroutes.com"))
            {
                statusText.text = "Please verify your email before accessing the app.";
                auth.SignOut();
            }
        }
    }

    private async void HandleSuccessfulLogin(string email)
    {
        // Extract username from email - use the part before @ for all domains
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
                // Store user name and residence hall in PlayerPrefs for easy access
                PlayerPrefs.SetString("UserName", !string.IsNullOrEmpty(user.name) ? user.name : playerName);
                PlayerPrefs.SetString("ResidenceHall", !string.IsNullOrEmpty(user.residenceHall) ? user.residenceHall : "No Hall");
                PlayerPrefs.Save();
                Debug.Log($"Stored user profile in PlayerPrefs: {user.name}, {user.residenceHall}");
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
        
        // Update welcome panel with user information instead of just welcome text
        await UpdateWelcomePanelUserInfo(userId, userProfile);
        
        FetchAndCacheUserGameStage(userId);

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

        // Check if this is a first-time user
        bool isFirstTime = PlayerPrefs.GetInt($"FirstTime_{userId}", 0) == 1;
        
        if (isFirstTime)
        {
            // First-time user - redirect directly to onboarding
            PlayerPrefs.SetInt($"FirstTime_{userId}", 0); // Mark as no longer first time
            PlayerPrefs.Save();
            SceneManager.LoadScene("Onboarding");
        }
        else
        {
            // Returning user - show welcome screen
            loginPanel.SetActive(false);
            signupPanel.SetActive(false);
            welcomePanel.SetActive(true);
            playButton.interactable = true;
            statusText.text = "";
        }

        // Ensure Firebase Messaging is initialized
    }

    /// <summary>
    /// Updates the welcome panel with detailed user information
    /// </summary>
    private async Task UpdateWelcomePanelUserInfo(string userId, RamRoutes.Model.User userProfile)
    {
        try
        {
            // Set username
            if (usernameText != null)
            {
                usernameText.text = userProfile.name ?? "Unknown User";
            }

            // Set residence hall
            if (userHallText != null)
            {
                userHallText.text = userProfile.residenceHall ?? "No Hall";
            }
                 var userService = new RamRoutes.Services.UserService();

            int coins = await userService.GetUserCoins(userId);
            int kb = await userService.GetUserKnowledgePoints(userId);

            // Set coins and KB
            if (userCoinsText != null)
            {
                userCoinsText.text = $"{coins}";
            }

            if (userKBText != null)
            {
                userKBText.text = $"{kb}";
            }

            // Set user avatar based on total points
            if (userAvatarImage != null)
            {
                int totalPoints = coins + kb;
                Sprite avatarSprite = GetRankSprite(totalPoints);
                if (avatarSprite != null)
                {
                    userAvatarImage.sprite = avatarSprite;
                }
            }

            // Set current stage
            if (userStageText != null)
            {
                try
                {
                    var currentStage = RamRoutes.Services.GameStageService.LoadStageFromPrefs();
                    if (currentStage != null)
                    {
                        userStageText.text = $"{currentStage.area}";
                    }
                    else
                    {
                        userStageText.text = "Thomas Commons";
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to get current stage: {e.Message}");
                    userStageText.text = "Current Stage: Unknown";
                }
            }

            // Hide the old welcome text
            if (welcomeText != null)
            {
                welcomeText.gameObject.SetActive(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update welcome panel user info: {e.Message}");
            
            // Fallback to simple welcome message
            if (welcomeText != null)
            {
                welcomeText.text = $"Welcome, {userProfile.name?.Split(" ")[0] ?? "User"}!";
                welcomeText.gameObject.SetActive(true);
            }
        }
    }
    
    private async void FetchAndCacheUserGameStage(string userId)
    {
        try
        {
            var userStage = await RamRoutes.Services.GameStageService.LoadStageFromFirestore();
            if (userStage != null)
            {
                // Update local cache with stage from Firestore
                RamRoutes.Services.GameStageService.SaveStageToPrefs(userStage);
                Debug.Log($"Successfully loaded and cached user stage: {userStage.area}");
            }
            else
            {
                // Initialize with Thomas Commons stage
                var tcStage = new RamRoutes.Model.GameStage
                {
                    area = Stage.TC,
 
                };
                RamRoutes.Services.GameStageService.SaveStageToPrefs(tcStage);
                Debug.Log("Initialized new user with TC stage");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to fetch user stage from Firestore: {e.Message}");
        }
    }

    private async void OnLoginClicked()
    {
        string usernameOrEmail = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter email and password";
            return;
        }

        statusText.text = "Logging in...";
        loginButton.interactable = false;

        // Check if Firebase Auth is properly initialized
        if (auth == null)
        {
            statusText.text = "Firebase not initialized. Please restart the app.";
            loginButton.interactable = true;
            return;
        }

        // Convert username to full email if needed
        string email;
        if (usernameOrEmail.Contains("@"))
        {
            email = usernameOrEmail;
        }
        else
        {
            email = $"{usernameOrEmail}@cornellcollege.edu";
        }

        if (usernameOrEmail.EndsWith(".rr"))
        {
            email = $"{usernameOrEmail.Replace(".rr", "")}@ramroutes.com";
        }

        try
        {
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            var user = result.User;

            // Check if email is verified
            await user.ReloadAsync(); // Refresh user data to get latest verification status

            if (!user.IsEmailVerified && !email.EndsWith("@ramroutes.com"))
            {
                statusText.text = "Email not verified";
                // Provide option to resend verification email
                var resendButton = GameObject.Find("ResendVerificationButton");
                if (resendButton == null)
                {
                    statusText.text = "Please verify your email before logging in. Check your inbox for the verification link.";
                }

                // Sign out the user
                auth.SignOut();
                loginButton.interactable = true;
                return;
            }

            // Email is verified, proceed with login
            // AuthStateChanged will handle the UI update
        }
        catch (FirebaseException e)
        {
            statusText.text = GetFirebaseErrorMessage(e);
            loginButton.interactable = true;
        }
    }

    private async void OnResetPasswordClicked()
    {
        string usernameOrEmail = emailInput.text;

        if (string.IsNullOrEmpty(usernameOrEmail))
        {
            statusText.text = "Please enter your Cornell username in the email field, then click reset password again.";
            return;
        }

        // Check if Firebase Auth is properly initialized
        if (auth == null)
        {
            statusText.text = "Firebase not initialized. Please restart the app.";
            return;
        }

        string email;
        if (usernameOrEmail.Contains("@"))
        {
            email = usernameOrEmail;
        }
        else
        {
            email = $"{usernameOrEmail}@cornellcollege.edu";
        }

        try
        {
            await auth.SendPasswordResetEmailAsync(email);
            statusText.text = $"If you are registered with {email}, you should get a link to reset your password.";
        }
        catch (FirebaseException e)
        {
            statusText.text = GetFirebaseErrorMessage(e);
        }
    }

    private void OnResetPasswordTextClicked()
    {
        isSignupMode = false;
        isResetPasswordMode = true;
        TogglePanels();
    }

    private async void OnResetPasswordButtonClicked()
    {
        string username = resetUsernameInput.text;

        if (string.IsNullOrEmpty(username))
        {
            if (resetStatusText != null)
                resetStatusText.text = "Please enter your Cornell username.";
            return;
        }

        resetPasswordButton.interactable = false;
        if (resetStatusText != null)
            resetStatusText.text = "Sending reset email...";

        // Check if Firebase Auth is properly initialized
        if (auth == null)
        {
            if (resetStatusText != null)
                resetStatusText.text = "Firebase not initialized. Please restart the app.";
            resetPasswordButton.interactable = true;
            return;
        }

        string email = $"{username}@cornellcollege.edu";

        try
        {
            await auth.SendPasswordResetEmailAsync(email);
            if (resetStatusText != null)
                resetStatusText.text = $"If you are registered with {email}, you should get a link to reset your password.";
        }
        catch (FirebaseException e)
        {
            if (resetStatusText != null)
                resetStatusText.text = GetFirebaseErrorMessage(e);
        }
        finally
        {
            resetPasswordButton.interactable = true;
        }
    }

    private void OnBackToLoginClicked()
    {
        isSignupMode = false;
        isResetPasswordMode = false;
        TogglePanels();
        resetUsernameInput.text = "";
        if (resetStatusText != null)
            resetStatusText.text = "";
    }

    private void OnLogoutClicked()
    {
        if (auth != null)
        {
            auth.SignOut();
            PlayerPrefs.DeleteKey("PlayerName");
        }

        // Reset to login mode
        isSignupMode = false;
        isResetPasswordMode = false;
        UpdateToggleText();
        TogglePanels();
        
        welcomePanel.SetActive(false);
        playButton.interactable = false;
        
        // Clear input fields
        emailInput.text = "";
        passwordInput.text = "";
        signupUsernameInput.text = "";
        signupPasswordInput.text = "";
        resetUsernameInput.text = "";
        if (residenceHallDropdown != null)
        {
            residenceHallDropdown.value = 0;
        }
        if (verificationStatusText != null)
        {
            verificationStatusText.text = "";
        }
        
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
            _ => "Login failed: Incorrect Credentials"
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

    /// <summary>
    /// Calculates user rank based on total points (same logic as UIManager)
    /// </summary>
    private int CalculateRank(int totalPoints)
    {
        if (totalPoints >= 2000)
        {
            return 3;
        }
        else if (totalPoints >= 1000)
        {
            return 2;
        }
        else if (totalPoints > 0)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the appropriate rank sprite based on user's points (same logic as UIManager)
    /// </summary>
    private Sprite GetRankSprite(int points)
    {
        // Implement the same rank logic directly in LoginManager
        // Load rank sprites as public SerializeField references in LoginManager
        if (points >= 2000) return rank3AvatarSprite;
        else if (points >= 1000) return rank2AvatarSprite;
        else  return rank1AvatarSprite;
    }

    /// <summary>
    /// Gets rank name based on rank number for display purposes
    /// </summary>
    private string GetRankName(int rank)
    {
        return rank switch
        {
            0 => "Beginner",
            1 => "Gold",
            2 => "Silver", 
            3 => "Platinum",
            _ => "Unknown"
        };
    }



    private async Task UpdateLeaderboard()
    {
        try
        {
            var db = FirebaseFirestore.DefaultInstance;
            
            // Get all users
            var querySnapshot = await db.Collection("users")
                .Limit(20)  // Get more users initially as we'll need to recalculate points
                .GetSnapshotAsync();

            // List to store user stats with residence hall
            var userStats = new List<(string userId, string name, string residenceHall, int coins, int kb)>();

            // Calculate points for each user from unlocked-trials
            foreach (var doc in querySnapshot.Documents)
            {
                var userData = doc.ToDictionary();
                string userId = doc.Id;
                string name = userData.ContainsKey("name") ? userData["name"].ToString() : "Unknown";
                string residenceHall = userData.ContainsKey("residenceHall") ? userData["residenceHall"].ToString() : "Unknown Hall";

                // Skip users with .rr ending (exclude from leaderboard)
                if (name.EndsWith(".rr"))
                {
                    continue;
                }

                // Get unlocked buildings for this user
                var unlocksSnapshot = await db.Collection("unlocked-trials")
                    .WhereEqualTo("userId", userId)
                    .GetSnapshotAsync();

                int totalCoins = 0;
                int totalKB = 0;

                foreach (var unlockDoc in unlocksSnapshot.Documents)
                {
                    var unlockData = unlockDoc.ToDictionary();
                    if (unlockData.ContainsKey("coinPoints"))
                    {
                        totalCoins += Convert.ToInt32(unlockData["coinPoints"]);
                    }
                    if (unlockData.ContainsKey("knowledgePoints"))
                    {
                        totalKB += Convert.ToInt32(unlockData["knowledgePoints"]);
                    }
                }

                userStats.Add((userId, name, residenceHall, totalCoins, totalKB));
            }

            // Sort by total coins (primary) and knowledge points (secondary)
            userStats.Sort((a, b) => {
                int coinCompare = b.coins.CompareTo(a.coins);
                return coinCompare != 0 ? coinCompare : b.kb.CompareTo(a.kb);
            });

            // Take top 3
            var topThree = userStats.Take(3).ToList();

            // Create leaderboard entries using prefab
            if (leaderboardEntryPrefab != null && leaderboardContentParent != null)
            {
                for (int i = 0; i < topThree.Count; i++)
                {
                    var user = topThree[i];
                    int totalPoints = user.coins + user.kb;
                    int rank = CalculateRank(totalPoints);
                    string rankName = GetRankName(rank);
                    Sprite rankSprite = GetRankSprite(totalPoints);

                    GameObject entryObject = Instantiate(leaderboardEntryPrefab, leaderboardContentParent);
                    
                    // Try to find components with multiple possible names
                    Text nameText = entryObject.transform.Find("name")?.GetComponent<Text>();
                    
                    Text pointsText = entryObject.transform.Find("coins")?.GetComponent<Text>();
                    
                    Text kbText = entryObject.transform.Find("kb")?.GetComponent<Text>();
                    
                    Text hallText = entryObject.transform.Find("hall")?.GetComponent<Text>();
                    
                    UnityEngine.UI.Image rankImage = entryObject.transform.Find("profile")?.GetComponent<UnityEngine.UI.Image>();
                    
                    Text positionText = entryObject.transform.Find("PositionText")?.GetComponent<Text>();

                    Text rankText = entryObject.transform.Find("rank")?.GetComponent<Text>() ;

                    // Set the data
                    if (nameText != null) nameText.text = user.name;
                    if (pointsText != null) pointsText.text = user.coins.ToString();
                    if (kbText != null) kbText.text = user.kb.ToString();
                    if (hallText != null) hallText.text = user.residenceHall;
                    if (rankImage != null && rankSprite != null) rankImage.sprite = rankSprite;
                    if (positionText != null) positionText.text = $"#{i + 1}";
                    if (rankText != null) rankText.text = rankName;

                    Debug.Log($"Created leaderboard entry for {user.name} at position {i + 1} with rank {rankName}");
                }
            }
            else
            {
                Debug.LogWarning("Leaderboard prefab or content parent is not assigned!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update leaderboard: {e.Message}");
        }
    }

    private async Task UpdateUnlockHistory()
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
                    : (DateTime?)null;

                if (time.HasValue)
                {
                    allEntries.Add((time.Value, $"{userName} unlocked {buildingName}"));
                }
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
                    : (DateTime?)null;

                if (time.HasValue)
                {
                    // Format the event text with more details if available
                    string eventText = string.IsNullOrEmpty(eventDetails)
                        ? $"Event: {eventName} at {buildingName}"
                        : $"Event: {eventName} at {buildingName} - {eventDetails}";
                    allEntries.Add((time.Value, eventText));
                }
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
                        // Ensure the date is only displayed if it's not the minimum value
                        string dateText = entry.time != DateTime.MinValue 
                            ? $" at {entry.time.ToLocalTime():MMM dd, yyyy h:mm tt}" 
                            : "";
                        entryText.text = $"{entry.text}{dateText}";
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

        // Clean up background music
        if (musicSource != null)
        {
            musicSource.Stop();
            Destroy(musicSource.gameObject);
        }
    }    void Update()
    {
        // Handle countdown timer as backup to coroutine
        if (isCountingDown)
        {
            countdownTimer -= Time.deltaTime;
            var buttonText = signupButton?.GetComponentInChildren<Text>();
            
            if (buttonText != null)
            {
                int secondsLeft = Mathf.CeilToInt(countdownTimer);
                if (secondsLeft > 0)
                {
                    buttonText.text = $"Resend in {secondsLeft}s";
                    // Debug.Log($"Update: countdownTimer={countdownTimer:F1}, secondsLeft={secondsLeft}");
                }
                else
                {
                    // Countdown finished
                    isCountingDown = false;
                    buttonText.text = "Resend Confirmation Email";
                    signupButton.interactable = true;
                    signupButton.onClick.RemoveAllListeners();
                    signupButton.onClick.AddListener(OnResendConfirmationClicked);
                    Debug.Log("Update-based countdown finished, enabling resend");
                    
                    // Stop the coroutine if it's still running
                    if (resendCountdownCoroutine != null)
                    {
                        StopCoroutine(resendCountdownCoroutine);
                        resendCountdownCoroutine = null;
                    }
                }
            }
            else
            {
                Debug.LogError("Update: Button text component not found during countdown");
            }
        }
        
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
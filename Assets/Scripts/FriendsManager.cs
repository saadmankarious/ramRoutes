using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using RamRoutes.Services;
using RamRoutes.Model;

public class FriendsManager : MonoBehaviour
{
    [Header("UI Navigation")]
    [SerializeField] private Button showRequestsButton;
    [SerializeField] private Button showFriendsButton;
    
    [Header("Friend Requests UI")]
    [SerializeField] private ScrollRect friendRequestsScrollView;
    [SerializeField] private Transform friendRequestsContentParent;
    [SerializeField] private GameObject friendRequestPrefab;
    
    [Header("Friends List UI")]
    [SerializeField] private ScrollRect friendsListScrollView;
    [SerializeField] private Transform friendsListContentParent;
    [SerializeField] private GameObject friendPrefab;
    
    [Header("Empty State UI")]
    [SerializeField] private Text emptyStateText;
    [Tooltip("Text component to show empty state messages for friends/requests")]
    
    [Header("Settings")]
    [SerializeField] private float refreshInterval = 3f;
    
    // Private variables
    private FriendRequestService friendRequestService;
    private UserService userService;
    private Coroutine refreshCoroutine;
    private bool isShowingRequests = true;
    
    // UI state management
    private bool isInitialized = false;

    void Start()
    {
        InitializeServices();
        SetupButtons();
        SetupInitialState();
        LoadFriendData();
    }
    
    /// <summary>
    /// Initialize the services needed for friend management
    /// </summary>
    private void InitializeServices()
    {
        friendRequestService = new FriendRequestService();
        userService = new UserService();
        isInitialized = true;
        Debug.Log("FriendsManager: Services initialized");
    }
    
    /// <summary>
    /// Setup button click handlers
    /// </summary>
    private void SetupButtons()
    {
        if (showRequestsButton != null)
        {
            showRequestsButton.onClick.AddListener(ShowFriendRequests);
        }
        
        if (showFriendsButton != null)
        {
            showFriendsButton.onClick.AddListener(ShowFriendsList);
        }
    }
    
    /// <summary>
    /// Setup the initial UI state
    /// </summary>
    private void SetupInitialState()
    {
        // Just set the initial view, don't load data yet
        // Data will be loaded when LoadFriendData() is called explicitly
        isShowingRequests = false;
        
        // Update UI scroll views without loading data
        if (friendRequestsScrollView != null) friendRequestsScrollView.gameObject.SetActive(false);
        if (friendsListScrollView != null) friendsListScrollView.gameObject.SetActive(true);
        
        // Update button states
        UpdateButtonStates();
        
        Debug.Log("FriendsManager: Initial UI state set (no data loading)");
    }
    
    /// <summary>
    /// Public method to load and display friend data - called from external systems
    /// </summary>
    public async void LoadFriendData()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("FriendsManager: Not initialized, cannot load friend data");
            return;
        }
        
        // Load data for the current view only once
        LoadCurrentViewData();
        
        // Start auto-refresh with a delay to avoid immediate loops
        await System.Threading.Tasks.Task.Delay(1000); // 1 second delay
        StartRefreshCoroutine();
    }
    
    /// <summary>
    /// Switch to showing friend requests
    /// </summary>
    public void ShowFriendRequests()
    {
        isShowingRequests = true;
        
        // Update UI scroll views
        if (friendRequestsScrollView != null) friendRequestsScrollView.gameObject.SetActive(true);
        if (friendsListScrollView != null) friendsListScrollView.gameObject.SetActive(false);
        
        // Update button states
        UpdateButtonStates();
        
        // Load data for this view
        LoadCurrentViewData();
        
        Debug.Log("FriendsManager: Switched to friend requests view");
    }
    
    /// <summary>
    /// Switch to showing friends list
    /// </summary>
    public void ShowFriendsList()
    {
        isShowingRequests = false;
        
        // Update UI scroll views
        if (friendRequestsScrollView != null) friendRequestsScrollView.gameObject.SetActive(false);
        if (friendsListScrollView != null) friendsListScrollView.gameObject.SetActive(true);
        
        // Update button states
        UpdateButtonStates();
        
        // Load data for this view
        LoadCurrentViewData();
        
        Debug.Log("FriendsManager: Switched to friends list view");
    }
    
    /// <summary>
    /// Load data for the currently active view
    /// </summary>
    private void LoadCurrentViewData()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("FriendsManager: Not initialized, cannot load data");
            return;
        }
        
        // Hide empty state while loading
        HideEmptyState();
        
        // Load appropriate data based on current view
        if (isShowingRequests)
        {
            StartCoroutine(LoadFriendRequestsCoroutine());
        }
        else
        {
            StartCoroutine(LoadFriendsListCoroutine());
        }
    }
    
    /// <summary>
    /// Update button visual states based on current view
    /// </summary>
    private void UpdateButtonStates()
    {
        // You can add visual feedback here like changing button colors
        // For now, just ensure buttons are interactable
        if (showRequestsButton != null)
        {
            showRequestsButton.interactable = !isShowingRequests;
        }
        
        if (showFriendsButton != null)
        {
            showFriendsButton.interactable = isShowingRequests;
        }
    }
    
    /// <summary>
    /// Load friend requests from Firebase and display them
    /// </summary>
    private async Task LoadFriendRequests()
    {
        try
        {
            if (friendRequestsScrollView == null || friendRequestsContentParent == null || friendRequestPrefab == null)
            {
                Debug.LogWarning("FriendsManager: Friend requests UI components not assigned");
                return;
            }

            // Clear existing entries
            ClearContentParent(friendRequestsContentParent);

            var requests = await friendRequestService.GetIncomingFriendRequests();

            if (requests.Count == 0)
            {
                // Show empty state for friend requests
                ShowEmptyState("No pending friend requests");
            }
            else
            {
                // Hide empty state and show friend requests
                HideEmptyState();
                
                foreach (var request in requests)
                {
                    CreateFriendRequestEntry(request);
                }
            }

            Debug.Log($"FriendsManager: Loaded {requests.Count} incoming friend requests");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FriendsManager: Failed to load friend requests: {e.Message}");
            // Show error state
            ShowEmptyState("Unable to load friend requests");
        }
    }
    
    /// <summary>
    /// Load friends list and display them
    /// </summary>
    private async Task LoadFriendsList()
    {
        try
        {
            if (friendsListScrollView == null || friendsListContentParent == null || friendPrefab == null)
            {
                Debug.LogWarning("FriendsManager: Friends list UI components not assigned");
                return;
            }

            // Clear existing entries
            ClearContentParent(friendsListContentParent);

            // Get current user's friends list
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {               
                Debug.LogWarning("FriendsManager: No authenticated user, cannot load friends");
                ShowEmptyState("Please log in to view friends");
                return;
            }
            
            var currentUser = await userService.RetrieveUserById(currentUserId);
            if (currentUser?.friends == null || currentUser.friends.Count == 0)
            {
                Debug.Log("FriendsManager: No friends found for current user");
                ShowEmptyState("No friends added yet");
                return;
            }

            // Hide empty state and show friends
            HideEmptyState();
            
            foreach (var friendName in currentUser.friends)
            {
                CreateFriendEntry(friendName);
            }

            Debug.Log($"FriendsManager: Loaded {currentUser.friends.Count} friends");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FriendsManager: Failed to load friends list: {e.Message}");
            ShowEmptyState("Unable to load friends list");
        }
    }
    
    /// <summary>
    /// Create a friend request entry UI element
    /// </summary>
    private void CreateFriendRequestEntry(FriendRequest request)
    {
        GameObject entryObject = Instantiate(friendRequestPrefab, friendRequestsContentParent);
        
        // Find and set the name text
        Text nameText = entryObject.transform.Find("name")?.GetComponent<Text>();
        if (nameText != null)
        {
            string displayText = !string.IsNullOrEmpty(request.fromName) ? request.fromName : "Unknown User";
            if (!string.IsNullOrEmpty(request.fromName) && request.fromName.Contains(" - "))
            {
                displayText = request.fromName; // Already has hall info
            }
            else
            {
                displayText = $"{displayText} - Unknown Hall"; // Add fallback hall
            }
            nameText.text = displayText;
        }

        // Setup accept button
        Button acceptButton = entryObject.transform.Find("accept")?.GetComponent<Button>();
        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(async () => await AcceptFriendRequest(request.requestId, entryObject));
        }

        // Setup delete/reject button
        Button deleteButton = entryObject.transform.Find("delete")?.GetComponent<Button>();
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(async () => await DeleteFriendRequest(request.requestId, entryObject));
        }
    }
    
    /// <summary>
    /// Create a friend entry UI element (display only)
    /// </summary>
    private void CreateFriendEntry(string friendName)
    {
        GameObject entryObject = Instantiate(friendPrefab, friendsListContentParent);
        
        // Find and set the name text - try multiple possible names for the text component
        Text nameText = entryObject.transform.Find("name")?.GetComponent<Text>();
        if (nameText == null)
        {
            nameText = entryObject.transform.Find("friendName")?.GetComponent<Text>();
        }
        if (nameText == null)
        {
            nameText = entryObject.GetComponentInChildren<Text>();
        }
        
        if (nameText != null)
        {
            nameText.text = friendName;
        }
        else
        {
            Debug.LogWarning($"FriendsManager: No text component found in friend prefab for {friendName}");
        }
    }
    
    /// <summary>
    /// Accept a friend request
    /// </summary>
    private async Task AcceptFriendRequest(string requestId, GameObject entryObject)
    {
        try
        {
            bool success = await friendRequestService.AcceptFriendRequest(requestId);
            
            if (success)
            {
                // Remove the entry from UI
                Destroy(entryObject);
                Debug.Log($"FriendsManager: Friend request {requestId} accepted and removed from UI");
                
                // Check for empty state in friend requests
                CheckAndShowEmptyRequestsState();
                
                // Refresh friends list if currently showing it
                if (!isShowingRequests)
                {
                    await LoadFriendsList();
                }
            }
            else
            {
                Debug.LogError($"FriendsManager: Failed to accept friend request {requestId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FriendsManager: Error accepting friend request: {e.Message}");
        }
    }
    
    /// <summary>
    /// Delete/reject a friend request
    /// </summary>
    private async Task DeleteFriendRequest(string requestId, GameObject entryObject)
    {
        try
        {
            bool success = await friendRequestService.DeleteFriendRequest(requestId);
            
            if (success)
            {
                // Remove the entry from UI
                Destroy(entryObject);
                Debug.Log($"FriendsManager: Friend request {requestId} deleted and removed from UI");
                
                // Check for empty state in friend requests
                CheckAndShowEmptyRequestsState();
            }
            else
            {
                Debug.LogError($"FriendsManager: Failed to delete friend request {requestId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FriendsManager: Error deleting friend request: {e.Message}");
        }
    }
    
    /// <summary>
    /// Clear all children from a content parent
    /// </summary>
    private void ClearContentParent(Transform contentParent)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }
    
    /// <summary>
    /// Start the refresh coroutine
    /// </summary>
    private void StartRefreshCoroutine()
    {
        StopRefreshCoroutine();
        refreshCoroutine = StartCoroutine(RefreshCoroutine());
    }
    
    /// <summary>
    /// Stop the refresh coroutine
    /// </summary>
    private void StopRefreshCoroutine()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine that periodically refreshes the current view
    /// </summary>
    private IEnumerator RefreshCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            
            // Only refresh if this GameObject is active
            if (gameObject.activeInHierarchy && isInitialized)
            {
                if (isShowingRequests)
                {
                    StartCoroutine(LoadFriendRequestsCoroutine());
                }
                else
                {
                    StartCoroutine(LoadFriendsListCoroutine());
                }
            }
        }
    }
    
    /// <summary>
    /// Coroutine wrapper for loading friend requests
    /// </summary>
    private IEnumerator LoadFriendRequestsCoroutine()
    {
        var loadTask = LoadFriendRequests();
        
        // Wait for completion with timeout protection
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;
        
        while (!loadTask.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (elapsed >= timeout)
        {
            Debug.LogError("FriendsManager: Friend requests loading timed out after 10 seconds");
        }
        else if (loadTask.Exception != null)
        {
            Debug.LogError($"FriendsManager: Friend requests refresh failed: {loadTask.Exception.Message}");
        }
    }
    
    /// <summary>
    /// Coroutine wrapper for loading friends list
    /// </summary>
    private IEnumerator LoadFriendsListCoroutine()
    {
        var loadTask = LoadFriendsList();
        
        // Wait for completion with timeout protection
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;
        
        while (!loadTask.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (elapsed >= timeout)
        {
            Debug.LogError("FriendsManager: Friends list loading timed out after 10 seconds");
        }
        else if (loadTask.Exception != null)
        {
            Debug.LogError($"FriendsManager: Friends list refresh failed: {loadTask.Exception.Message}");
        }
    }
    
    /// <summary>
    /// Public method to stop refresh when friends manager is no longer needed
    /// </summary>
    public void StopRefresh()
    {
        StopRefreshCoroutine();
        Debug.Log("FriendsManager: Refresh stopped");
    }
    
    /// <summary>
    /// Shows empty state with specified message
    /// </summary>
    private void ShowEmptyState(string message)
    {
        if (emptyStateText != null)
        {
            emptyStateText.text = message;
            emptyStateText.gameObject.SetActive(true);
            Debug.Log($"FriendsManager: Showing empty state: {message}");
        }
    }
    
    /// <summary>
    /// Hides the empty state text
    /// </summary>
    private void HideEmptyState()
    {
        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Checks if there are no friend request entries left and shows empty state
    /// </summary>
    private void CheckAndShowEmptyRequestsState()
    {
        if (friendRequestsContentParent != null)
        {
            // Count the actual friend request entries (exclude destroyed objects)
            int activeChildCount = 0;
            foreach (Transform child in friendRequestsContentParent)
            {
                if (child != null && child.gameObject != null)
                {
                    activeChildCount++;
                }
            }
            
            // Show empty state if no active friend requests and currently showing requests
            if (activeChildCount == 0 && isShowingRequests)
            {
                ShowEmptyState("No pending friend requests");
            }
        }
    }
    
    /// <summary>
    /// Checks if there are no friends left and shows empty state
    /// </summary>
    private void CheckAndShowEmptyFriendsState()
    {
        if (friendsListContentParent != null)
        {
            // Count the actual friend entries (exclude destroyed objects)
            int activeChildCount = 0;
            foreach (Transform child in friendsListContentParent)
            {
                if (child != null && child.gameObject != null)
                {
                    activeChildCount++;
                }
            }
            
            // Show empty state if no active friends and currently showing friends
            if (activeChildCount == 0 && !isShowingRequests)
            {
                ShowEmptyState("No friends added yet");
            }
        }
    }
    
    void OnDestroy()
    {
        StopRefreshCoroutine();
    }
}

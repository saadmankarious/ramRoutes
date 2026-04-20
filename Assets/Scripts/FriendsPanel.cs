using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using RamRoutes.Services;
using Firebase.Auth;
using RamRoutes.Model;

/// <summary>
/// Simple friends panel that displays the current user's friends list in a scroll view
/// </summary>
public class FriendsPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private Transform contentParent; // The "Content" transform nested in viewport
    [SerializeField] private GameObject friendPrefab; // Prefab to instantiate for each friend
    [SerializeField] private Text emptyText; // Text to show when no friends exist
    
    private UserService userService;
    private Coroutine autoRefreshCoroutine;
    private Dictionary<string, GameObject> friendEntries = new Dictionary<string, GameObject>(); // Track existing friend UI entries
    private bool isInitialLoad = true;
    
    void Start()
    {
        userService = new UserService();
        LoadFriends();
        StartAutoRefresh();
    }
    
    void OnDestroy()
    {
        StopAutoRefresh();
    }
    
    /// <summary>
    /// Load and display the current user's friends list
    /// </summary>
    public async void LoadFriends()
    {
        try
        {
            // Get current user ID
            string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                ShowEmptyState("Please log in to view friends");
                return;
            }
            
            // Get current user data
            var currentUser = await userService.RetrieveUserById(currentUserId);
            if (currentUser?.friends == null || currentUser.friends.Count == 0)
            {
                ShowEmptyState("No friends added yet");
                return;
            }
            
            // Hide empty state
            if (emptyText != null)
                emptyText.gameObject.SetActive(false);
            
            // Silent update: only update/create entries as needed
            await UpdateFriendsListSilently(currentUser.friends);
            
            if (isInitialLoad)
            {
                isInitialLoad = false;
            }
        }
        catch (System.Exception e)
        {
            ShowEmptyState("Unable to load friends");
        }
    }
    
    /// <summary>
    /// Silently update friends list without clearing and rebuilding everything
    /// </summary>
    private async Task UpdateFriendsListSilently(List<string> currentFriends)
    {
        // Remove friends that are no longer in the list
        var friendsToRemove = new List<string>();
        foreach (var kvp in friendEntries)
        {
            if (!currentFriends.Contains(kvp.Key))
            {
                friendsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var friendToRemove in friendsToRemove)
        {
            if (friendEntries.ContainsKey(friendToRemove))
            {
                Destroy(friendEntries[friendToRemove]);
                friendEntries.Remove(friendToRemove);
            }
        }
        
        // Update or create entries for current friends
        foreach (string friendUsername in currentFriends)
        {
            if (friendEntries.ContainsKey(friendUsername))
            {
                // Update existing entry silently
                await UpdateExistingFriendEntry(friendUsername, friendEntries[friendUsername]);
            }
            else
            {
                // Create new entry
                var newEntry = await CreateFriendEntry(friendUsername);
                if (newEntry != null)
                {
                    friendEntries[friendUsername] = newEntry;
                }
            }
        }
    }
    
    /// <summary>
    /// Update an existing friend entry with fresh data
    /// </summary>
    private async Task UpdateExistingFriendEntry(string friendUsername, GameObject friendEntry)
    {
        try
        {
            // Get fresh user data
            var friendUser = await userService.RetrieveUserByName(friendUsername);
            if (friendUser != null)
            {
                // Update the UI data silently
                PopulateFriendUI(friendEntry, friendUser);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FriendsPanel: Error updating friend entry for {friendUsername}: {e.Message}");
        }
    }
    
    /// <summary>
    /// Create a UI entry for a friend by fetching their full profile
    /// </summary>
    private async Task<GameObject> CreateFriendEntry(string friendUsername)
    {
        if (friendPrefab == null || contentParent == null)
        {
            return null;
        }
        
        try
        {
            // Get the full user profile by username
            var friendUser = await userService.RetrieveUserByName(friendUsername);
            if (friendUser == null)
            {
                return null;
            }
            
            // Instantiate the friend prefab
            GameObject friendEntry = Instantiate(friendPrefab, contentParent);
            ButtonHandler rsvpButtonHandler = friendEntry.GetComponentInChildren<ButtonHandler>();

            if (rsvpButtonHandler != null)
            {
                rsvpButtonHandler.Initialize("data", () => ChatWithFriend(friendUser));
            }
            // Find and populate the specific GameObjects in the prefab
            PopulateFriendUI(friendEntry, friendUser);
            
            return friendEntry;
        }
        catch (System.Exception e)
        {
            return null;
        }
    }
    
    
    private void ChatWithFriend(User friend)
    {
        // Assuming there's a ChatManager in the scene that handles chat
        ChatManager chatManager = FindObjectOfType<ChatManager>();
        if (chatManager != null)
        {
            chatManager.StartChatWithUser(friend);
        }
       
    }
    /// <summary>
    /// Populate the friend UI components with user data
    /// </summary>
    private void PopulateFriendUI(GameObject friendEntry, RamRoutes.Model.User friendUser)
    {
        // Find and populate the "name" GameObject
        Transform nameTransform = friendEntry.transform.Find("name");
        if (nameTransform != null)
        {
            Text nameText = nameTransform.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = friendUser.name;
            }
        }

        // Find and populate the "profile" GameObject  
        Transform profileTransform = friendEntry.transform.Find("profile");
        if (profileTransform != null)
        {
            Text profileText = profileTransform.GetComponent<Text>();
            if (profileText != null)
            {
                // Display user's profile information (you can customize this)
                profileText.text = $"Coins: {friendUser.coins} | Knowledge: {friendUser.knowledgePoints}";
            }
        }

        // Find and populate the "current-building" GameObject
        Transform buildingTransform = friendEntry.transform.Find("current-building");
        if (buildingTransform != null)
        {
            Text buildingText = buildingTransform.GetComponent<Text>();
            if (buildingText != null)
            {
                string buildingName = !string.IsNullOrEmpty(friendUser.currentBuilding) ?
                    $"Now at: {friendUser.currentBuilding}" : "Not in any building";
                buildingText.text = buildingName;
            }
        }

    }
    
    /// <summary>
    /// Clear all friend entries from the list
    /// </summary>
    private void ClearFriendsList()
    {
        if (contentParent == null) return;
        
        // Clear tracked entries
        foreach (var kvp in friendEntries)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        friendEntries.Clear();
        
        // Clear any remaining children (safety net)
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }
    
    /// <summary>
    /// Show empty state message
    /// </summary>
    private void ShowEmptyState(string message)
    {
        ClearFriendsList();
        
        if (emptyText != null)
        {
            emptyText.text = message;
            emptyText.gameObject.SetActive(true);
        }
        
    }
    
    /// <summary>
    /// Refresh the friends list (can be called from UI buttons)
    /// </summary>
    public void RefreshFriends()
    {
        LoadFriends();
    }
    
    /// <summary>
    /// Start auto-refresh coroutine that silently updates friends list every 3 seconds
    /// </summary>
    private void StartAutoRefresh()
    {
        if (autoRefreshCoroutine == null)
        {
            autoRefreshCoroutine = StartCoroutine(AutoRefreshCoroutine());
        }
    }
    
    /// <summary>
    /// Stop auto-refresh coroutine
    /// </summary>
    private void StopAutoRefresh()
    {
        if (autoRefreshCoroutine != null)
        {
            StopCoroutine(autoRefreshCoroutine);
            autoRefreshCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine that silently refreshes friends list every 3 seconds
    /// </summary>
    private IEnumerator AutoRefreshCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            
            // Silent refresh - only update if the GameObject is still active
            if (gameObject.activeInHierarchy)
            {
                LoadFriends();
            }
        }
    }
}

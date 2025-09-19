using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using RamRoutes.Services;
using RamRoutes.Model;
using Firebase.Auth;
using System.Linq;
using TMPro;
using System;

[System.Serializable]
public class WhisperSprite
{
    public WhisperType whisperType;
    public Sprite sprite;
}

public class ChatManager : MonoBehaviour
{
    [Header("Chat UI")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private ScrollRect chatScrollView;
    [SerializeField] private Transform chatContentParent;
    [SerializeField] private GameObject senderBubblePrefab; // Prefab for messages you send
    [SerializeField] private GameObject receiverBubblePrefab; // Prefab for messages you receive
    [SerializeField] private Button closeChatButton;
    
    [Header("Whisper Selection")]
    [SerializeField] private ScrollRect whisperScrollView;
    [SerializeField] private Transform whisperContentParent;
    [SerializeField] private Button whisperButtonPrefab;
    
    [Header("Whisper Sprites")]
    [SerializeField] private WhisperSprite[] availableWhispers; // List of available whispers with their sprites
    
    private ChatService chatService;
    private UserService userService;
    private ShoutOutService shoutOutService;
    private FriendRequestService friendRequestService;
    private string currentChatTargetId;
    private string currentChatTargetName;
    private User currentChatTargetUser;
    private List<Chat> currentConversation = new List<Chat>();
    private Coroutine refreshCoroutine;
    
    void Start()
    {
        chatService = new ChatService();
        userService = new UserService();
        shoutOutService = new ShoutOutService();
        friendRequestService = new FriendRequestService();
        
        // Setup UI
        if (closeChatButton != null)
        {
            closeChatButton.onClick.AddListener(CloseChatPanel);
        }
        
        // Setup shoutout and friend request buttons
        SetupActionButtons();
        
        // Create whisper buttons
        CreateWhisperButtons();
        
        // Hide chat panel initially
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Create whisper selection buttons
    /// </summary>
    private void CreateWhisperButtons()
    {
        if (whisperContentParent == null || whisperButtonPrefab == null) return;
        
        // Clear existing buttons
        foreach (Transform child in whisperContentParent)
        {
            Destroy(child.gameObject);
        }
        
        // Create button for each available whisper (only those defined in the list)
        if (availableWhispers != null)
        {
            for (int i = 0; i < availableWhispers.Length; i++)
            {
                WhisperSprite whisperSprite = availableWhispers[i];
                if (whisperSprite.sprite == null) continue; // Skip if no sprite assigned
                
                Button whisperBtn = Instantiate(whisperButtonPrefab, whisperContentParent);
                
                // Find the "whisper" child and set sprite
                Transform whisperTransform = FindChildByName(whisperBtn.transform, "whisper");
                Image whisperImage = whisperTransform?.GetComponent<Image>();
                if (whisperImage != null)
                {
                    whisperImage.sprite = whisperSprite.sprite;
                }
                else
                {
                    Debug.LogWarning($"No 'whisper' child found in whisper button prefab or no Image component on whisper child");
                }
                
                // Add click listener
                WhisperType currentWhisper = whisperSprite.whisperType; // Capture for closure
                whisperBtn.onClick.AddListener(() => SendWhisper(currentWhisper));
            }
        }
        else
        {
            Debug.LogWarning("ChatManager: No available whispers defined. Please assign whispers in the availableWhispers array.");
        }
    }
    
    /// <summary>
    /// Setup shoutout and friend request buttons
    /// </summary>
    private void SetupActionButtons()
    {
        if (chatPanel == null) return;
        
        // Find and setup shoutout button
        Transform shoutoutTransform = FindChildByName(chatPanel.transform, "shoutout");
        Button shoutoutButton = shoutoutTransform?.GetComponent<Button>();
        if (shoutoutButton != null)
        {
            shoutoutButton.onClick.AddListener(SendShoutout);
        }
        
        // Find and setup add friend button
        Transform addFriendTransform = FindChildByName(chatPanel.transform, "add-friend");
        Button addFriendButton = addFriendTransform?.GetComponent<Button>();
        if (addFriendButton != null)
        {
            addFriendButton.onClick.AddListener(SendFriendRequest);
        }
    }
    
    /// <summary>
    /// Send a shoutout to the current chat target
    /// </summary>
    private async void SendShoutout()
    {
        if (currentChatTargetUser == null)
        {
            Debug.LogWarning("No chat target selected for shoutout");
            return;
        }
        
        bool success = await shoutOutService.SendShoutOut(currentChatTargetUser.userId);
        if (success)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowQuickUpdate("Shoutout sent to " + currentChatTargetUser.name + "!");
                
                // Update the UI with new player stats after sending shoutout
                await UpdatePlayerStatsInUI();
            }
        }
    }
    
    /// <summary>
    /// Send a friend request to the current chat target
    /// </summary>
    private async void SendFriendRequest()
    {
        if (currentChatTargetUser == null)
        {
            Debug.LogWarning("No chat target selected for friend request");
            return;
        }
        
        var friendRequest = await friendRequestService.SendFriendRequest(currentChatTargetUser.userId);
        if (friendRequest != null)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowQuickUpdate($"Friend request sent to {currentChatTargetUser.name}!");
            }
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowQuickUpdate("You already sent a request");
            }
        }
    }
    
    /// <summary>
    /// Updates the player stats in the UI Manager after a shoutout is sent
    /// </summary>
    private async Task UpdatePlayerStatsInUI()
    {
        try
        {
            // Get current user ID
            string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return;
            }
            
            // Get updated user profile from cache or remote
            var updatedUser = await userService.GetUserProfileCachedOrRemoteAsync(currentUserId);
            
            if (updatedUser != null && UIManager.Instance != null)
            {
                // Update UI with the latest stats
                UIManager.Instance.UpdateCoins(updatedUser.coins);
                UIManager.Instance.UpdateKnowledgePoints(updatedUser.knowledgePoints);
                
                Debug.Log($"Updated UI stats after shoutout - Coins: {updatedUser.coins}, KB: {updatedUser.knowledgePoints}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update player stats in UI after shoutout: {e.Message}");
        }
    }
    
    /// <summary>
    /// Open chat with a specific user
    /// </summary>
    public async void OpenChatWithUser(User user)
    {
        if (user == null) return;
        
        currentChatTargetUser = user;
        currentChatTargetId = user.userId;
        currentChatTargetName = user.name;
        
        // Update profile display
        UpdateProfileDisplay(user);
        
        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
        }
        
        // Load conversation
        await LoadConversation();
        
        // Start auto-refresh
        StartChatRefresh();
    }
    
    /// <summary>
    /// Update the profile display with user information
    /// </summary>
    private void UpdateProfileDisplay(User user)
    {
        if (user == null || chatPanel == null) return;
        
        // Find the "name" text component recursively
        Transform nameTransform = FindChildByName(chatPanel.transform, "name");
        Transform whisperTo = FindChildByName(chatPanel.transform, "whisper-to");
        Text whisperToText = whisperTo?.GetComponent<Text>();
        Text nameText = nameTransform?.GetComponent<Text>();
        if (nameText != null)
        {
            nameText.text = user.name ?? "Unknown";
        }
        
        if (whisperToText != null)
        {
            whisperToText.text = "Whisper to " + (user.name ?? "Unknown");
        }
        
        // Find the "profile" image component recursively and update avatar based on rank using UIManager
        Transform profileTransform = FindChildByName(chatPanel.transform, "profile");
        Image profileImage = profileTransform?.GetComponent<Image>();
        if (profileImage != null && UIManager.Instance != null)
        {
            profileImage.sprite = UIManager.Instance.GetUserAvatarBasedOnPoints(user.coins, user.knowledgePoints);
        }
    }
    
    /// <summary>
    /// Send a whisper to the current chat target
    /// </summary>
    private async void SendWhisper(WhisperType whisperType)
    {
        if (string.IsNullOrEmpty(currentChatTargetId))
        {
            Debug.LogWarning("No chat target selected");
            return;
        }
        
        string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogWarning("User not authenticated");
            return;
        }
        
        // Send the whisper (store as string representation of enum)
        bool success = await chatService.SendChatAsync(currentUserId, currentChatTargetId, whisperType.ToString());
        
        if (success)
        {
            UIManager.Instance.ShowQuickUpdate("Whisper sent to " + currentChatTargetUser.name + "!");

            // Refresh conversation to show the new message
            await LoadConversation();
        }
    }
    
    /// <summary>
    /// Load and display the conversation with current target
    /// </summary>
    private async Task LoadConversation()
    {
        if (string.IsNullOrEmpty(currentChatTargetId)) return;
        
        string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(currentUserId)) return;
        
        // Get conversation
        currentConversation = await chatService.GetConversationAsync(currentUserId, currentChatTargetId);
        
        // Display messages
        DisplayMessages();
    }
    
    /// <summary>
    /// Display chat messages in the UI
    /// </summary>
    private void DisplayMessages()
    {
        if (chatContentParent == null || senderBubblePrefab == null || receiverBubblePrefab == null) return;
        
        // Clear existing messages
        foreach (Transform child in chatContentParent)
        {
            Destroy(child.gameObject);
        }
        
        string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        
        // Create message bubbles
        foreach (var chat in currentConversation)
        {
            bool isMyMessage = chat.fromId == currentUserId;
            
            // Choose the appropriate prefab based on sender
            GameObject prefabToUse = isMyMessage ? senderBubblePrefab : receiverBubblePrefab;
            GameObject bubble = Instantiate(prefabToUse, chatContentParent);
            
            // Remove ButtonHandler if no Button component exists
            ButtonHandler buttonHandler = bubble.GetComponent<ButtonHandler>();
            Button button = bubble.GetComponent<Button>();
            if (buttonHandler != null && button == null)
            {
                Destroy(buttonHandler);
            }
            
            // Find the "whisper" child and set sprite
            Transform whisperTransform = FindChildByName(bubble.transform, "whisper");
            Image whisperImage = whisperTransform?.GetComponent<Image>();
            if (whisperImage != null)
            {
                WhisperType whisperType = chat.GetWhisperType();
                
                // Find the sprite for this whisper type from available whispers
                Sprite whisperSprite = GetSpriteForWhisperType(whisperType);
                if (whisperSprite != null)
                {
                    whisperImage.sprite = whisperSprite;
                }
                else
                {
                    Debug.LogWarning($"No sprite found for whisper type: {whisperType}");
                }
            }
            else
            {
                Debug.LogWarning($"No 'whisper' child found in chat bubble prefab or no Image component on whisper child");
            }
            
            // Hide any text components since we're using sprites now
            TextMeshProUGUI messageText = bubble.GetComponentInChildren<TextMeshProUGUI>();
            if (messageText != null)
            {
                messageText.gameObject.SetActive(false);
            }
            
            // Also hide regular Text components
            Text regularText = bubble.GetComponentInChildren<Text>();
            if (regularText != null)
            {
                regularText.gameObject.SetActive(false);
            }
        }
        
        // Scroll to bottom to show most recent messages
        if (chatScrollView != null)
        {
            // Force canvas update first
            Canvas.ForceUpdateCanvases();
            
            // Use a small delay to ensure layout is complete
            StartCoroutine(ScrollToBottomDelayed());
        }
    }
    
    /// <summary>
    /// Coroutine to scroll to bottom with a small delay to ensure layout is complete
    /// </summary>
    private IEnumerator ScrollToBottomDelayed()
    {
        // Wait one frame for layout to update
        yield return null;
        
        if (chatScrollView != null)
        {
            // Scroll to bottom (0 = bottom for vertical scroll)
            chatScrollView.normalizedPosition = new Vector2(0, 0);
        }
    }
    
    /// <summary>
    /// Close the chat panel
    /// </summary>
    public void CloseChatPanel()
    {
        // Stop auto-refresh
        StopChatRefresh();
        
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
        
        currentChatTargetId = "";
        currentChatTargetName = "";
        currentChatTargetUser = null;
        currentConversation.Clear();
    }
    
    /// <summary>
    /// Get recent chats for current user (for notifications or chat list)
    /// </summary>
    public async Task<List<Chat>> GetRecentChatsAsync()
    {
        string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return new List<Chat>();
        }
        
        return await chatService.GetChatsForUserAsync(currentUserId);
    }
    
    /// <summary>
    /// Public method to open chat - can be called from other scripts
    /// </summary>
    public void StartChatWithUser(User user)
    {
        if (user != null)
        {
            OpenChatWithUser(user);
        }
    }
    
    void OnDestroy()
    {
        // Stop auto-refresh
        StopChatRefresh();
        
        if (closeChatButton != null)
        {
            closeChatButton.onClick.RemoveListener(CloseChatPanel);
        }
        
        // Remove action button listeners
        if (chatPanel != null)
        {
            Transform shoutoutTransform = FindChildByName(chatPanel.transform, "shoutout");
            Button shoutoutButton = shoutoutTransform?.GetComponent<Button>();
            if (shoutoutButton != null)
            {
                shoutoutButton.onClick.RemoveListener(SendShoutout);
            }
            
            Transform addFriendTransform = FindChildByName(chatPanel.transform, "add-friend");
            Button addFriendButton = addFriendTransform?.GetComponent<Button>();
            if (addFriendButton != null)
            {
                addFriendButton.onClick.RemoveListener(SendFriendRequest);
            }
        }
    }
    
    /// <summary>
    /// Start the chat refresh coroutine
    /// </summary>
    private void StartChatRefresh()
    {
        StopChatRefresh(); // Stop any existing coroutine
        refreshCoroutine = StartCoroutine(RefreshChatPeriodically());
    }
    
    /// <summary>
    /// Stop the chat refresh coroutine
    /// </summary>
    private void StopChatRefresh()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine to refresh chat every 3 seconds
    /// </summary>
    private IEnumerator RefreshChatPeriodically()
    {
        while (!string.IsNullOrEmpty(currentChatTargetId))
        {
            yield return new WaitForSeconds(3f);
            
            // Only refresh if chat is still open
            if (!string.IsNullOrEmpty(currentChatTargetId) && chatPanel != null && chatPanel.activeInHierarchy)
            {
                // Start the async operation and wait for it to complete
                var loadTask = LoadConversation();
                yield return new WaitUntil(() => loadTask.IsCompleted);
            }
        }
    }
    
    /// <summary>
    /// Recursively find a child transform by name
    /// </summary>
    private Transform FindChildByName(Transform parent, string name)
    {
        // Check direct children first
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.ToLower().Contains(name.ToLower()))
            {
                return child;
            }
        }
        
        // If not found in direct children, search recursively
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Get the sprite for a specific whisper type from the available whispers list
    /// </summary>
    /// <param name="whisperType">The whisper type to find a sprite for</param>
    /// <returns>The sprite for the whisper type, or null if not found</returns>
    private Sprite GetSpriteForWhisperType(WhisperType whisperType)
    {
        if (availableWhispers == null) return null;
        
        foreach (var whisperSprite in availableWhispers)
        {
            if (whisperSprite.whisperType == whisperType && whisperSprite.sprite != null)
            {
                return whisperSprite.sprite;
            }
        }
        
        return null;
    }
}

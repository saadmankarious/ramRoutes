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
    // [SerializeField] private WhisperSprite[] availableWhispers; // List of available whispers with their sprites
    
    // Cache for downloaded whisper sprites
    private Dictionary<string, Sprite> downloadedSpriteCache = new Dictionary<string, Sprite>();
    
    private ChatService chatService;
    private UserService userService;
    private ShoutOutService shoutOutService;
    private FriendRequestService friendRequestService;
    private InventoryService inventoryService;
    private string currentChatTargetId;
    private string currentChatTargetName;
    private User currentChatTargetUser;
    private List<Chat> currentConversation = new List<Chat>();
    private Coroutine refreshCoroutine;
    private Coroutine whisperRefreshCoroutine;
    
    void Start()
    {
        chatService = new ChatService();
        userService = new UserService();
        shoutOutService = new ShoutOutService();
        friendRequestService = new FriendRequestService();
        inventoryService = new InventoryService();
        
        // Setup UI
        if (closeChatButton != null)
        {
            closeChatButton.onClick.AddListener(CloseChatPanel);
        }
        
        // Setup shoutout and friend request buttons
        SetupActionButtons();
        
        // Initialize whisper buttons with user's purchased whispers
        StartCoroutine(InitializeWhisperButtonsCoroutine());
        
        // Hide chat panel initially
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Initialize whisper buttons with user's purchased whispers on start
    /// </summary>
    private IEnumerator InitializeWhisperButtonsCoroutine()
    {
        // Wait a frame to ensure other components are initialized
        yield return null;
        
        // Get current user's purchased whispers from inventory
        string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            // If user not authenticated yet, just create basic whispers
            CreateWhisperButtons(new List<InventoryItem>());
            yield break;
        }
        
        // Start the async task to get user's whisper inventory items
        var inventoryService = new InventoryService();
        var getWhisperInventoryTask = inventoryService.GetInventoryByCategory("Whisper", currentUserId);
        
        // Wait for the task to complete
        yield return new WaitUntil(() => getWhisperInventoryTask.IsCompleted);
        
        if (getWhisperInventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to get user whisper inventory during initialization: {getWhisperInventoryTask.Exception.Message}");
            // Fallback to basic whispers
            CreateWhisperButtons(new List<InventoryItem>());
            yield break;
        }
        
        // Create whisper buttons with purchased whispers (full inventory items with image URLs)
        var whisperInventory = getWhisperInventoryTask.Result;
        CreateWhisperButtons(whisperInventory.Where(w => w.equipped).ToList());
        Debug.Log($"ChatManager initialized with {whisperInventory.Count} purchased whisper items");
    }
    
    /// <summary>
    /// Coroutine to refresh whisper buttons including purchased whispers
    /// </summary>
    private IEnumerator RefreshWhisperButtonsCoroutine()
    {
        // Get current user's purchased whispers from inventory
        string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogWarning("Cannot refresh whispers - user not authenticated");
            yield break;
        }
        
        // Start the async task to get user's whisper inventory items
        var inventoryService = new InventoryService();
        var getWhisperInventoryTask = inventoryService.GetInventoryByCategory("Whisper", currentUserId);
        
        // Wait for the task to complete
        yield return new WaitUntil(() => getWhisperInventoryTask.IsCompleted);
        
        if (getWhisperInventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to get user whisper inventory: {getWhisperInventoryTask.Exception.Message}");
            yield break;
        }
        
        // Recreate whisper buttons with updated inventory (full inventory items with image URLs)
        var whisperInventory = getWhisperInventoryTask.Result;
        CreateWhisperButtons(whisperInventory.Where(w => w.equipped).ToList());
        Debug.Log($"Refreshed ChatManager with {whisperInventory.Count} purchased whisper items");
    }
    
    /// <summary>
    /// Create whisper selection buttons
    /// </summary>
    private void CreateWhisperButtons()
    {
        // Call the overloaded method with empty user whispers list for initial setup
        CreateWhisperButtons(new List<InventoryItem>());
    }
    
    /// <summary>
    /// Create whisper selection buttons including user's purchased whispers
    /// </summary>
    private void CreateWhisperButtons(List<InventoryItem> userPurchasedWhispers)
    {
        if (whisperContentParent == null || whisperButtonPrefab == null) return;
        
        // Clear existing buttons
        foreach (Transform child in whisperContentParent)
        {
            Destroy(child.gameObject);
        }
        
        var createdWhisperTypes = new HashSet<WhisperType>(); // Track created whispers to avoid duplicates
        
        // First, create buttons for purchased whispers (ordered by purchase date, newest first)
        if (userPurchasedWhispers != null && userPurchasedWhispers.Count > 0)
        {
            var orderedPurchasedWhispers = userPurchasedWhispers
                .Where(w => Enum.IsDefined(typeof(WhisperType), w.whisperType))
                .OrderByDescending(w => w.purchaseDate) // Newest first
                .ToList();
            
            foreach (var purchasedWhisper in orderedPurchasedWhispers)
            {
                WhisperType whisperType = (WhisperType)purchasedWhisper.whisperType;
                
                // Skip if we already created a button for this whisper type
                // if (createdWhisperTypes.Contains(whisperType)) continue;
                
                CreateWhisperButton(whisperType, purchasedWhisper, userPurchasedWhispers);
                createdWhisperTypes.Add(whisperType);
            }
        }
        
        // Then, create buttons for hardcoded whispers that haven't been created yet
        // if (availableWhispers != null)
        // {
        //     foreach (var whisperSprite in availableWhispers)
        //     {
        //         if (whisperSprite.sprite != null && !createdWhisperTypes.Contains(whisperSprite.whisperType))
        //         {
        //             CreateWhisperButton(whisperSprite.whisperType, null, userPurchasedWhispers);
        //             createdWhisperTypes.Add(whisperSprite.whisperType);
        //         }
        //     }
        // }
        
        if (createdWhisperTypes.Count == 0)
        {
            var noWhispersBoughtText = FindChildByName(chatPanel.transform, "no-whispers-bought")?.GetComponent<Text>();
            if (noWhispersBoughtText != null)
            {
                noWhispersBoughtText.gameObject.SetActive(true);
            }
            Debug.LogWarning("ChatManager: No whispers available (neither hardcoded nor purchased).");
        }
        else
        {
            var noWhispersBoughtText = FindChildByName(chatPanel.transform, "no-whispers-bought")?.GetComponent<Text>();
            if (noWhispersBoughtText != null)
            {
                noWhispersBoughtText.gameObject.SetActive(false);
            }
            Debug.Log($"ChatManager: Created {createdWhisperTypes.Count} whisper buttons (purchased + hardcoded)");
        }
    }
    
    /// <summary>
    /// Create a single whisper button
    /// </summary>
    private void CreateWhisperButton(WhisperType whisperType, InventoryItem purchasedWhisper, List<InventoryItem> userPurchasedWhispers)
    {
        Button whisperBtn = Instantiate(whisperButtonPrefab, whisperContentParent);
        
        // Find the "whisper" child
        Transform whisperTransform = FindChildByName(whisperBtn.transform, "whisper");
        Image whisperImage = whisperTransform?.GetComponent<Image>();
        
        if (whisperImage != null)
        {
            // If we have a purchased whisper with image URL, use it
            if (purchasedWhisper != null && !string.IsNullOrEmpty(purchasedWhisper.imageUrl))
            {
                // Download sprite from URL for purchased whisper
                StartCoroutine(DownloadSpriteFromUrl(purchasedWhisper.imageUrl, (downloadedSprite) =>
                {
                    if (downloadedSprite != null && whisperImage != null)
                    {
                        whisperImage.sprite = downloadedSprite;
                    }
                }));
            }
            // else
            // {
            //     // Try to get sprite from hardcoded list or find purchased whisper with URL
            //     Sprite whisperSprite = GetSpriteForWhisperType(whisperType);
                
            //     if (whisperSprite != null)
            //     {
            //         // Use hardcoded sprite
            //         whisperImage.sprite = whisperSprite;
            //     }
            //     else
            //     {
            //         // Try to find matching purchased whisper with image URL
            //         var matchingPurchased = userPurchasedWhispers?.FirstOrDefault(w => 
            //             Enum.IsDefined(typeof(WhisperType), w.whisperType) && 
            //             (WhisperType)w.whisperType == whisperType);
                    
            //         if (matchingPurchased != null && !string.IsNullOrEmpty(matchingPurchased.imageUrl))
            //         {
            //             // Download sprite from URL
            //             StartCoroutine(DownloadSpriteFromUrl(matchingPurchased.imageUrl, (downloadedSprite) =>
            //             {
            //                 if (downloadedSprite != null && whisperImage != null)
            //                 {
            //                     whisperImage.sprite = downloadedSprite;
            //                 }
            //             }));
            //         }
            //         else
            //         {
            //             Debug.LogWarning($"No sprite or image URL found for whisper type {whisperType}");
            //         }
            //     }
            // }
        }
        else
        {
            Debug.LogWarning($"No 'whisper' child found in whisper button prefab or no Image component on whisper child");
        }
        
        // Find and set the "count" text component
        Transform countTransform = FindChildByName(whisperBtn.transform, "count");
        if (countTransform != null)
        {
            // Try TextMeshProUGUI first
            TextMeshProUGUI countTextTMP = countTransform.GetComponent<TextMeshProUGUI>();
            if (countTextTMP != null)
            {
                int quantity = purchasedWhisper?.quantity ?? 0;
                countTextTMP.text = quantity.ToString();
            }
            else
            {
                // Try regular Text component
                Text countText = countTransform.GetComponent<Text>();
                if (countText != null)
                {
                    int quantity = purchasedWhisper?.quantity ?? 0;
                    countText.text = quantity.ToString();
                }
            }
        }
        else
        {
            Debug.LogWarning($"No 'count' child found in whisper button prefab");
        }
        
        // Add click listener
        WhisperType currentWhisper = whisperType; // Capture for closure
        whisperBtn.onClick.AddListener(() => SendWhisper(purchasedWhisper));
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
    
    public void OnUserWhisperChanged()
    {
        
        // Clear sprite cache to ensure new whispers are downloaded fresh
        downloadedSpriteCache.Clear();
        
        // Refresh the whisper buttons to include newly purchased whispers
        StartCoroutine(RefreshWhisperButtonsCoroutine());
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
        
        // Check if we're switching to a different user
        bool isDifferentUser = currentChatTargetUser == null || currentChatTargetUser.userId != user.userId;
        
        currentChatTargetUser = user;
        currentChatTargetId = user.userId;
        currentChatTargetName = user.name;
        
        // Update profile display
        UpdateProfileDisplay(user);
        
        if (chatPanel != null)
        {
            // Start the slide-up animation
            StartCoroutine(AnimateChatPanelOpen());
        }
        
        // If switching to a different user, clear existing conversation UI
        if (isDifferentUser)
        {
            ClearConversationDisplay();
        }
        
        // Load conversation
        await LoadConversation();
        
        // Start auto-refresh for both chat messages and whisper buttons
        StartChatRefresh();
        StartWhisperRefresh();
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
    private async void SendWhisper(InventoryItem whisper)
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
        bool success = await chatService.SendChatAsync(currentUserId, currentChatTargetId, whisper.imageUrl);
        
        if (success)
        {
            UIManager.Instance.ShowQuickUpdate("Whisper sent to " + currentChatTargetUser.name + "!");

            // Decrease the whisper quantity in inventory
            bool quantityDecreased = await inventoryService.DecreaseItemQuantity(currentUserId, whisper.itemId);
            if (quantityDecreased)
            {
                Debug.Log($"Decreased quantity for whisper type {whisper}");
                
                // Refresh whisper buttons to reflect updated quantities
                StartCoroutine(RefreshWhisperButtonsCoroutine());
            }
            else
            {
                Debug.LogWarning($"Failed to decrease quantity for whisper type {whisper}");
            }

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
        var newConversation = await chatService.GetConversationAsync(currentUserId, currentChatTargetId);

        currentConversation = newConversation;
        
        if (newConversation.Count == 0)
        {
            chatContentParent.gameObject.SetActive(false);
            var noWhispersText = FindChildByName(chatPanel.transform, "no-whispers")?.GetComponent<Text>();
            if (noWhispersText != null)
            {
                noWhispersText.gameObject.SetActive(true);
            }
        }
        else
        {
            chatContentParent.gameObject.SetActive(true);
            var noWhispersText = FindChildByName(chatPanel.transform, "no-whispers")?.GetComponent<Text>();
            if (noWhispersText != null)
            {
                noWhispersText.gameObject.SetActive(false);
            }
            DisplayMessagesSmartly();
        }
    }
    
    /// <summary>
    /// Check if there are new messages compared to current conversation
    /// </summary>
    private bool HasNewMessages(List<Chat> newConversation)
    {
        // If we have no current conversation, then we have new messages
        if (currentConversation == null || currentConversation.Count == 0)
        {
            return newConversation != null && newConversation.Count > 0;
        }
        
        // If new conversation has more messages, we have new messages
        if (newConversation.Count > currentConversation.Count)
        {
            return true;
        }
        
        // If same count, check if the messages are different (compare by timestamp and content)
        if (newConversation.Count == currentConversation.Count)
        {
            for (int i = 0; i < newConversation.Count; i++)
            {
                if (newConversation[i].timestamp != currentConversation[i].timestamp ||
                    newConversation[i].fromId != currentConversation[i].fromId ||
                    newConversation[i].toId != currentConversation[i].toId ||
                    newConversation[i].imageUrl != currentConversation[i].imageUrl)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Clear all existing message bubbles from the conversation display
    /// </summary>
    private void ClearConversationDisplay()
    {
        if (chatContentParent == null) return;
        
        // Destroy all existing message bubbles
        for (int i = chatContentParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(chatContentParent.GetChild(i).gameObject);
        }
        
        // Clear the current conversation list
        currentConversation.Clear();
        
        Debug.Log("Cleared conversation display for new user");
    }
    
    /// <summary>
    /// Display chat messages smartly - only add new messages to preserve existing images
    /// </summary>
    private void DisplayMessagesSmartly()
    {
        if (chatContentParent == null || senderBubblePrefab == null || receiverBubblePrefab == null) return;
        
        string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        
        // Get count of existing message bubbles
        int existingMessageCount = chatContentParent.childCount;
        
        // If we have fewer UI messages than conversation messages, add the missing ones
        for (int i = existingMessageCount; i < currentConversation.Count; i++)
        {
            var chat = currentConversation[i];
            CreateMessageBubble(chat, currentUserId);
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
    /// Create a single message bubble for a chat message
    /// </summary>
    private void CreateMessageBubble(Chat chat, string currentUserId)
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
            
            if (!string.IsNullOrEmpty(chat.imageUrl))
            {
                // Download sprite from URL
                StartCoroutine(DownloadSpriteFromUrl(chat.imageUrl, (downloadedSprite) =>
                {
                    if (downloadedSprite != null && whisperImage != null)
                    {
                        whisperImage.sprite = downloadedSprite;
                    }
                }));
            }
            else
            {
                Debug.LogWarning("Chat message has no imageUrl for whisper");
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
    
    /// <summary>
    /// Display chat messages in the UI (full refresh - used for initial load)
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
        
        // Create message bubbles for all messages
        foreach (var chat in currentConversation)
        {
            CreateMessageBubble(chat, currentUserId);
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
        // Stop auto-refresh for both chat messages and whisper buttons
        StopChatRefresh();
        StopWhisperRefresh();
        
        if (chatPanel != null)
        {
            // Start the slide-down animation
            StartCoroutine(AnimateChatPanelClose());
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
        // Stop auto-refresh for both chat messages and whisper buttons
        StopChatRefresh();
        StopWhisperRefresh();
        
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
            
            // // Only refresh if chat is still open
            // if (!string.IsNullOrEmpty(currentChatTargetId) && chatPanel != null && chatPanel.activeInHierarchy)
            // {
            //     // Start the async operation and wait for it to complete
            //     var loadTask = LoadConversation();
            //     yield return new WaitUntil(() => loadTask.IsCompleted);
            // }
        }
    }
    
    /// <summary>
    /// Start the whisper buttons refresh coroutine
    /// </summary>
    private void StartWhisperRefresh()
    {
        StopWhisperRefresh(); // Stop any existing coroutine
        whisperRefreshCoroutine = StartCoroutine(RefreshWhisperButtonsPeriodically());
    }
    
    /// <summary>
    /// Stop the whisper buttons refresh coroutine
    /// </summary>
    private void StopWhisperRefresh()
    {
        if (whisperRefreshCoroutine != null)
        {
            StopCoroutine(whisperRefreshCoroutine);
            whisperRefreshCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine to refresh whisper buttons every 5 seconds
    /// </summary>
    private IEnumerator RefreshWhisperButtonsPeriodically()
    {
        while (!string.IsNullOrEmpty(currentChatTargetId))
        {
            yield return new WaitForSeconds(5f);
            
            // Only refresh if chat is still open
            if (!string.IsNullOrEmpty(currentChatTargetId) && chatPanel != null && chatPanel.activeInHierarchy)
            {
                // Start the whisper refresh and wait for it to complete
                var refreshTask = StartCoroutine(RefreshWhisperButtonsCoroutine());
                yield return refreshTask;
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
    // private Sprite GetSpriteForWhisperType(WhisperType whisperType)
    // {
    //     if (availableWhispers == null) return null;
        
    //     foreach (var whisperSprite in availableWhispers)
    //     {
    //         if (whisperSprite.whisperType == whisperType && whisperSprite.sprite != null)
    //         {
    //             return whisperSprite.sprite;
    //         }
    //     }
        
    //     return null;
    // }
    
    /// <summary>
    /// Download and cache a sprite from a URL
    /// </summary>
    /// <param name="url">The URL to download the sprite from</param>
    /// <returns>Coroutine that downloads the sprite</returns>
    private IEnumerator DownloadSpriteFromUrl(string url, System.Action<Sprite> onComplete)
    {
        // Check cache first
        if (downloadedSpriteCache.ContainsKey(url))
        {
            onComplete?.Invoke(downloadedSpriteCache[url]);
            yield break;
        }
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                
                // Cache the downloaded sprite
                downloadedSpriteCache[url] = sprite;
                
                onComplete?.Invoke(sprite);
            }
            else
            {
                Debug.LogError($"ChatManager: Failed to load image from {url}: {www.error}");
                onComplete?.Invoke(null);
            }
        }
    }
    
    /// <summary>
    /// Load whisper sprite for a chat message from user's inventory
    /// </summary>
    /// <param name="whisperImage">The image component to set the sprite on</param>
    /// <param name="whisperType">The whisper type to load</param>
    /// <param name="senderId">The ID of the user who sent the whisper</param>
    /// <returns>Coroutine that loads the whisper sprite</returns>
    private IEnumerator LoadWhisperSpriteForMessage(Image whisperImage, WhisperType whisperType, string senderId)
    {
        // Get sender's whisper inventory
        var inventoryService = new InventoryService();
        var getWhisperInventoryTask = inventoryService.GetInventoryByCategory("Whisper", senderId);
        
        // Wait for the task to complete
        yield return new WaitUntil(() => getWhisperInventoryTask.IsCompleted);
        
        if (getWhisperInventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to get sender's whisper inventory: {getWhisperInventoryTask.Exception.Message}");
            yield break;
        }
        
        // Find the matching whisper item
        var whisperInventory = getWhisperInventoryTask.Result;
        var matchingWhisper = whisperInventory.FirstOrDefault(w => 
            Enum.IsDefined(typeof(WhisperType), w.whisperType) && 
            (WhisperType)w.whisperType == whisperType);
        
        if (matchingWhisper != null && !string.IsNullOrEmpty(matchingWhisper.imageUrl))
        {
            // Download sprite from URL
            yield return StartCoroutine(DownloadSpriteFromUrl(matchingWhisper.imageUrl, (downloadedSprite) =>
            {
                if (downloadedSprite != null && whisperImage != null)
                {
                    whisperImage.sprite = downloadedSprite;
                }
            }));
        }
        else
        {
            Debug.LogWarning($"No inventory item with image URL found for whisper type {whisperType} from sender {senderId}");
        }
    }
    
    #region Chat Panel Animation
    
    /// <summary>
    /// Animate the chat panel sliding up from the bottom
    /// </summary>
    private IEnumerator AnimateChatPanelOpen()
    {
        chatPanel.SetActive(true);
        
        RectTransform chatRect = chatPanel.GetComponent<RectTransform>();
        Vector3 originalPosition = chatRect.anchoredPosition;
        Vector3 startPosition = originalPosition + Vector3.down * chatRect.rect.height;
        
        // Set starting position (below screen)
        chatRect.anchoredPosition = startPosition;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Simple ease-out animation
            progress = 1f - Mathf.Pow(1f - progress, 3f);
            
            chatRect.anchoredPosition = Vector3.Lerp(startPosition, originalPosition, progress);
            yield return null;
        }
        
        chatRect.anchoredPosition = originalPosition;
    }
    
    /// <summary>
    /// Animate the chat panel sliding down to the bottom
    /// </summary>
    private IEnumerator AnimateChatPanelClose()
    {
        RectTransform chatRect = chatPanel.GetComponent<RectTransform>();
        Vector3 originalPosition = chatRect.anchoredPosition;
        Vector3 endPosition = originalPosition + Vector3.down * chatRect.rect.height;
        
        float duration = 0.25f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Simple ease-in animation
            progress = Mathf.Pow(progress, 2f);
            
            chatRect.anchoredPosition = Vector3.Lerp(originalPosition, endPosition, progress);
            yield return null;
        }
        
        chatPanel.SetActive(false);
        chatRect.anchoredPosition = originalPosition; // Reset position for next time
    }
    
    #endregion
}

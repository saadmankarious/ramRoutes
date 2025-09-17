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
public class ChatManager : MonoBehaviour
{
    [Header("Chat UI")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private ScrollRect chatScrollView;
    [SerializeField] private Transform chatContentParent;
    [SerializeField] private GameObject senderBubblePrefab; // Prefab for messages you send
    [SerializeField] private GameObject receiverBubblePrefab; // Prefab for messages you receive
    [SerializeField] private Button closeChatButton;
    
    [Header("Emoji Selection")]
    [SerializeField] private Transform emojiButtonsParent;
    [SerializeField] private Button emojiButtonPrefab;
    
    [Header("Available Emojis")]
    [SerializeField] private string[] availableEmojis = { "😀", "😎", "👍", "❤️", "😂", "🔥", "💯", "🎉", "👋", "🤔" };
    
    private ChatService chatService;
    private UserService userService;
    private string currentChatTargetId;
    private string currentChatTargetName;
    private User currentChatTargetUser;
    private List<Chat> currentConversation = new List<Chat>();
    private Coroutine refreshCoroutine;
    
    void Start()
    {
        chatService = new ChatService();
        userService = new UserService();
        
        // Setup UI
        if (closeChatButton != null)
        {
            closeChatButton.onClick.AddListener(CloseChatPanel);
        }
        
        // Create emoji buttons
        CreateEmojiButtons();
        
        // Hide chat panel initially
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Create emoji selection buttons
    /// </summary>
    private void CreateEmojiButtons()
    {
        if (emojiButtonsParent == null || emojiButtonPrefab == null) return;
        
        // Clear existing buttons
        foreach (Transform child in emojiButtonsParent)
        {
            Destroy(child.gameObject);
        }
        
        // Create button for each emoji
        foreach (string emoji in availableEmojis)
        {
            Button emojiBtn = Instantiate(emojiButtonPrefab, emojiButtonsParent);
            
            // Set emoji text
            TextMeshProUGUI emojiText = emojiBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (emojiText != null)
            {
                emojiText.text = emoji;
            }
            
            // Add click listener
            string currentEmoji = emoji; // Capture for closure
            emojiBtn.onClick.AddListener(() => SendEmoji(currentEmoji));
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
        Text nameText = nameTransform?.GetComponent<Text>();
        if (nameText != null)
        {
            nameText.text = user.name ?? "Unknown";
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
    /// Send an emoji to the current chat target
    /// </summary>
    private async void SendEmoji(string emoji)
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
        
        // Send the emoji
        bool success = await chatService.SendChatAsync(currentUserId, currentChatTargetId, emoji);
        
        if (success)
        {
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
            
            // Find text component and set emoji only
            TextMeshProUGUI messageText = bubble.GetComponentInChildren<TextMeshProUGUI>();
            if (messageText != null)
            {
                // Display only the emoji, no additional text
                messageText.text = chat.chatEmojies;
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
}

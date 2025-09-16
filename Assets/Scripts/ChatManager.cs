using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private GameObject chatBubblePrefab;
    [SerializeField] private Button closeChatButton;
    
    [Header("Emoji Selection")]
    [SerializeField] private Transform emojiButtonsParent;
    [SerializeField] private Button emojiButtonPrefab;
    
    [Header("Available Emojis")]
    [SerializeField] private string[] availableEmojis = { "😀", "😎", "👍", "❤️", "😂", "🔥", "💯", "🎉", "👋", "🤔" };
    
    private ChatService chatService;
    private string currentChatTargetId;
    private string currentChatTargetName;
    private List<Chat> currentConversation = new List<Chat>();
    
    void Start()
    {
        chatService = new ChatService();
        
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
    public async void OpenChatWithUser(string userId, string userName)
    {
        currentChatTargetId = userId;
        currentChatTargetName = userName;
        
        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
        }
        
        // Load conversation
        await LoadConversation();
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
        if (chatContentParent == null || chatBubblePrefab == null) return;
        
        // Clear existing messages
        foreach (Transform child in chatContentParent)
        {
            Destroy(child.gameObject);
        }
        
        string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        
        // Create message bubbles
        foreach (var chat in currentConversation)
        {
            GameObject bubble = Instantiate(chatBubblePrefab, chatContentParent);
            
            // Find text component and set emoji
            TextMeshProUGUI messageText = bubble.GetComponentInChildren<TextMeshProUGUI>();
            if (messageText != null)
            {
                bool isMyMessage = chat.fromId == currentUserId;
                string prefix = isMyMessage ? "You: " : $"{currentChatTargetName}: ";
                messageText.text = prefix + chat.chatEmojies;
                
                // Optional: Color code messages
                messageText.color = isMyMessage ? Color.blue : Color.gray;
            }
        }
        
        // Scroll to bottom
        if (chatScrollView != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollView.normalizedPosition = new Vector2(0, 0);
        }
    }
    
    /// <summary>
    /// Close the chat panel
    /// </summary>
    public void CloseChatPanel()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
        
        currentChatTargetId = "";
        currentChatTargetName = "";
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
            OpenChatWithUser(user.userId, user.name);
        }
    }
    
    void OnDestroy()
    {
        if (closeChatButton != null)
        {
            closeChatButton.onClick.RemoveListener(CloseChatPanel);
        }
    }
}

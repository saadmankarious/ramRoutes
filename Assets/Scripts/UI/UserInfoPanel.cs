using UnityEngine;
using UnityEngine.UI;
using RamRoutes.Model;
using RamRoutes.Services;
using System.Collections;
using System.Threading.Tasks;

public class UserInfoPanel : MonoBehaviour
{
    public static UserInfoPanel Instance { get; private set; }
    
    [Header("User Info Panel Components")]
    public Button closeButton;
    public Button shoutOutButton;
    public Button friendRequestButton;
    
    [Header("User Info Display")]
    public Text usernameText;
    public Text coinsText;
    public Text knowledgePointsText;
    public Text residenceHallText;
    public Image rankImage;
    
    [Header("Rank Sprites")]
    public Sprite defaultRankSprite;
    public Sprite rank1Sprite;
    public Sprite rank2Sprite;
    public Sprite rank3Sprite;
    
    private UserService userService;
    private ShoutOutService shoutOutService;
    private FriendRequestService friendRequestService;
    private Coroutine autoHideCoroutine;
    private User currentUser;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        userService = new UserService();
        shoutOutService = new ShoutOutService();
        friendRequestService = new FriendRequestService();
    }
    
    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
        
        if (shoutOutButton != null)
        {
            shoutOutButton.onClick.AddListener(SendShoutout);
        }
        
        
        if (friendRequestButton != null)
        {
            friendRequestButton.onClick.AddListener(SendFriendRequest);
        }
    }
    
    public void ShowUserInfo(User user)
    {
        if (user == null)
        {
            return;
        }
        
        if (userService == null)
        {
            userService = new UserService();
        }
        
        currentUser = user;
        
        if (usernameText != null)
        {
            usernameText.text = user.name ?? "Unknown";
        }
        
        if (coinsText != null)
        {
            coinsText.text = user.coins.ToString();
        }
        
        if (knowledgePointsText != null)
        {
            knowledgePointsText.text = user.knowledgePoints.ToString();
        }
        
        if (residenceHallText != null)
        {
            residenceHallText.text = user.residenceHall ?? "No Hall Set";
        }
        
        if (rankImage != null)
        {
            int userRank = userService.CalculateUserRank(user.coins, user.knowledgePoints);
            rankImage.sprite = GetRankSprite(userRank);
        }
        
        gameObject.SetActive(true);
        
        if (UIManager.Instance != null)
        {
            StartCoroutine(UIManager.Instance.AnimatePanelPopup(gameObject));
        }
        
        StartAutoHideTimer();
    }
    
    public void HidePanel()
    {
        StopAutoHideTimer();
        gameObject.SetActive(false);
    }
    
    private void StartAutoHideTimer()
    {
        StopAutoHideTimer();
        autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(5f));
    }
    
    private void StopAutoHideTimer()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }
    
    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePanel();
    }
    
    private async void SendShoutout()
    {
        if (currentUser == null)
        {
            return;
        }
        
        bool success = await shoutOutService.SendShoutOut(currentUser.userId);
        if (success)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowQuickUpdate("Shoutout sent to " + currentUser.name + "!");

                // Update the UI with new player stats after sending shoutout
                await UpdatePlayerStatsInUI();
            }

            HidePanel();
        }
    }
    
    private async void SendFriendRequest()
    {
        if (currentUser == null)
        {
            return;
        }
        
        var friendRequest = await friendRequestService.SendFriendRequest(currentUser.userId);
        if (friendRequest != null)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowQuickUpdate($"Friend request sent to {currentUser.name}!");
            }

            HidePanel();
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowQuickUpdate("Failed to send friend request. You may have already sent one.");
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
            var userService = new UserService();
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
    
    private Sprite GetRankSprite(int rank)
    {
        switch (rank)
        {
            case 1:
                return rank1Sprite ?? defaultRankSprite;
            case 2:
                return rank2Sprite ?? defaultRankSprite;
            case 3:
                return rank3Sprite ?? defaultRankSprite;
            default:
                return defaultRankSprite;
        }
    }
    
    void OnDestroy()
    {
        StopAutoHideTimer();
        
        if (Instance == this)
        {
            Instance = null;
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HidePanel);
        }
        
        if (shoutOutButton != null)
        {
            shoutOutButton.onClick.RemoveListener(SendShoutout);
        }
        
        if (friendRequestButton != null)
        {
            friendRequestButton.onClick.RemoveListener(SendFriendRequest);
        }
    }
}

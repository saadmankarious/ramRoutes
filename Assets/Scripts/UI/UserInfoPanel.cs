using UnityEngine;
using UnityEngine.UI;
using RamRoutes.Model;
using RamRoutes.Services;
using System.Collections;

public class UserInfoPanel : MonoBehaviour
{
    public static UserInfoPanel Instance { get; private set; }
    
    [Header("User Info Panel Components")]
    public GameObject panelContainer;
    public Button closeButton;
    
    [Header("User Info Display")]
    public Text usernameText;
    public Text coinsText;
    public Text knowledgePointsText;
    public Image rankImage;
    
    [Header("Rank Sprites")]
    public Sprite defaultRankSprite;
    public Sprite rank1Sprite;
    public Sprite rank2Sprite;
    public Sprite rank3Sprite;
    
    private UserService userService;
    private Coroutine autoHideCoroutine;
    
    void Awake()
    {
        // Set up singleton in Awake (called even if GameObject is inactive)
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("UserInfoPanel: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
            return;
        }
        
        // Initialize UserService immediately
        userService = new UserService();
    }
    
    void Start()
    {
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
        
        // Don't auto-hide panel - let it maintain its initial state from Inspector
    }
    
    /// <summary>
    /// Shows the user info panel with the provided user data
    /// </summary>
    public void ShowUserInfo(User user)
    {
        if (user == null)
        {
            Debug.LogWarning("UserInfoPanel: Cannot show info for null user");
            return;
        }
        
        // Ensure userService is initialized
        if (userService == null)
        {
            userService = new UserService();
        }
        
        // Update UI elements with user data
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
        
        // Update rank sprite
        if (rankImage != null)
        {
            int userRank = userService.CalculateUserRank(user.coins, user.knowledgePoints);
            rankImage.sprite = GetRankSprite(userRank);
        }
        
        // Show the panel
        if (panelContainer != null)
        {
            panelContainer.SetActive(true);
            
            // Add popup animation if UIManager has it
            if (UIManager.Instance != null)
            {
                StartCoroutine(UIManager.Instance.AnimatePanelPopup(panelContainer));
            }
            
            // Start auto-hide timer
            StartAutoHideTimer();
        }
        
        panelContainer.SetActive(true);
        Debug.Log($"UserInfoPanel: Showing info for user {user.name}");
    }
    
    /// <summary>
    /// Hides the user info panel
    /// </summary>
    public void HidePanel()
    {
        // Stop auto-hide timer if running
        StopAutoHideTimer();
        
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
    }
    
    /// <summary>
    /// Starts the auto-hide timer
    /// </summary>
    private void StartAutoHideTimer()
    {
        // Stop any existing timer
        StopAutoHideTimer();
        
        // Start new auto-hide coroutine
        autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(5f));
    }
    
    /// <summary>
    /// Stops the auto-hide timer
    /// </summary>
    private void StopAutoHideTimer()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine that hides the panel after a delay
    /// </summary>
    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePanel();
    }
    
    /// <summary>
    /// Gets the appropriate rank sprite based on user rank
    /// </summary>
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
        // Stop auto-hide timer
        StopAutoHideTimer();
        
        // Clean up singleton
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Clean up button listeners
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HidePanel);
        }
    }
}

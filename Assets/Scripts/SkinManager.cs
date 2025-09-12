using UnityEngine;
using RamRoutes.Model;
using RamRoutes.Services;
using Firebase.Auth;

public class SkinManager : MonoBehaviour
{
    public static SkinManager Instance { get; private set; }
    
    [Header("Skin Animator Controllers")]
    public RuntimeAnimatorController defaultSkinAnimator;
    public RuntimeAnimatorController rainbowSkinAnimator;
    public RuntimeAnimatorController summerSkinAnimator;
    public RuntimeAnimatorController winterSkinAnimator;
    
    [Header("Player Reference")]
    private GameObject player;
    private Animator playerAnimator;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Find and cache player reference
        FindPlayer();
        
        // Initialize player skin from Firebase
        _ = InitializePlayerSkin();
    }
    
    /// <summary>
    /// Fetches the player's current skin from Firebase and initializes the animator
    /// </summary>
    private async System.Threading.Tasks.Task InitializePlayerSkin()
    {
        try
        {
            // Get current user ID
            string userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("SkinManager: No logged-in user found, using default skin");
                SetPlayerSkin(EquippedSkin.Default);
                return;
            }
            
            // Fetch equipped skin from UserService
            var userService = new UserService();
            EquippedSkin currentSkin = await userService.GetEquippedSkin(userId);
            
            Debug.Log($"SkinManager: Retrieved equipped skin from Firebase: {currentSkin}");
            
            // Apply the skin to the player
            OnUserSkinChanged(currentSkin);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SkinManager: Failed to initialize player skin: {ex.Message}");
            Debug.LogWarning("SkinManager: Falling back to default skin");
            
            // Fallback to default skin if there's an error
            SetPlayerSkin(EquippedSkin.Default);
        }
    }
    
    /// <summary>
    /// Finds the player GameObject and caches its Animator
    /// </summary>
    private void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogWarning("SkinManager: Player GameObject found but no Animator component detected");
            }
            else
            {
                Debug.Log("SkinManager: Successfully found and cached player Animator");
            }
        }
        else
        {
            Debug.LogWarning("SkinManager: No GameObject with 'Player' tag found in scene");
        }
    }
    
    /// <summary>
    /// Called when user's equipped skin changes - updates player animator
    /// </summary>
    /// <param name="newSkin">The new equipped skin</param>
    public void OnUserSkinChanged(EquippedSkin newSkin)
    {
        // Ensure we have a valid player reference
        if (player == null || playerAnimator == null)
        {
            FindPlayer(); // Try to find player again
        }
        
        if (playerAnimator == null)
        {
            Debug.LogWarning("SkinManager: Cannot update skin - no player Animator available");
            return;
        }
        
        RuntimeAnimatorController skinAnimator = GetAnimatorForSkin(newSkin);
        if (skinAnimator != null)
        {
            playerAnimator.runtimeAnimatorController = skinAnimator;
            Debug.Log($"SkinManager: Updated player skin animator to {newSkin}");
        }
        else
        {
            Debug.LogWarning($"SkinManager: No animator assigned for skin {newSkin}");
        }
    }
    
    /// <summary>
    /// Gets the appropriate animator controller for the given skin
    /// </summary>
    /// <param name="skin">The skin enum value</param>
    /// <returns>The corresponding animator controller, or default if not found</returns>
    private RuntimeAnimatorController GetAnimatorForSkin(EquippedSkin skin)
    {
        return skin switch
        {
            EquippedSkin.Rainbow => rainbowSkinAnimator ?? defaultSkinAnimator,
            EquippedSkin.Summer => summerSkinAnimator ?? defaultSkinAnimator,
            EquippedSkin.Winter => winterSkinAnimator ?? defaultSkinAnimator,
            EquippedSkin.Default => defaultSkinAnimator,
            _ => defaultSkinAnimator
        };
    }
    
    /// <summary>
    /// Manually refresh player reference (useful when player is spawned dynamically)
    /// </summary>
    public void RefreshPlayerReference()
    {
        FindPlayer();
    }
    
    /// <summary>
    /// Manually refresh player skin from Firebase (useful after login or scene changes)
    /// </summary>
    public async System.Threading.Tasks.Task RefreshPlayerSkinFromFirebase()
    {
        await InitializePlayerSkin();
    }
    
    /// <summary>
    /// Set player skin directly without going through skin change event
    /// </summary>
    /// <param name="skin">The skin to apply</param>
    public void SetPlayerSkin(EquippedSkin skin)
    {
        OnUserSkinChanged(skin);
    }
    
    /// <summary>
    /// Get the current animator controller assigned to the player
    /// </summary>
    /// <returns>The current RuntimeAnimatorController, or null if no player found</returns>
    public RuntimeAnimatorController GetCurrentPlayerAnimator()
    {
        if (playerAnimator == null)
        {
            FindPlayer();
        }
        
        return playerAnimator?.runtimeAnimatorController;
    }
}

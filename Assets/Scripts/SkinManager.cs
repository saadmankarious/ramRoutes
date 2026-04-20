using UnityEngine;
using UnityEngine.Rendering.Universal;
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
    public RuntimeAnimatorController catSkinAnimator;

    [Header("Accessory Prefabs")]
    [SerializeField] private Light2D torchLightPrefab;
    [SerializeField] private Light2D hornsLightPrefab;
    
    [Header("Player Reference")]
    private GameObject player;
    private Animator playerAnimator;
    private Light2D currentAccessoryLight;
    
    void Awake()
    {
        // // Singleton pattern
        // if (Instance == null)
        // {
        //     Instance = this;
        //     DontDestroyOnLoad(gameObject);
        // }
        // else
        // {
        //     Destroy(gameObject);
        //     return;
        // }
    }
    
    void Start()
    {
        // Find and cache player reference
        FindPlayer();
        
        // Initialize player skin and accessory from Firebase
        _ = InitializePlayerSkin();
        _ = InitializePlayerAccessory();
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
    /// Fetches the player's current accessory from Firebase and initializes the Light2D
    /// </summary>
    private async System.Threading.Tasks.Task InitializePlayerAccessory()
    {
        try
        {
            // Get current user ID
            string userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("SkinManager: No logged-in user found, using no accessory");
                SetPlayerAccessory(EquippedAccessory.None);
                return;
            }
            
            // Fetch equipped accessory from UserService
            var userService = new UserService();
            EquippedAccessory currentAccessory = await userService.GetEquippedAccessory(userId);
            
            Debug.Log($"SkinManager: Retrieved equipped accessory from Firebase: {currentAccessory}");
            
            // Apply the accessory to the player
            OnUserAccessoryChanged(currentAccessory);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SkinManager: Failed to initialize player accessory: {ex.Message}");
            Debug.LogWarning("SkinManager: Falling back to no accessory");
            
            // Fallback to no accessory if there's an error
            SetPlayerAccessory(EquippedAccessory.None);
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
        try
        {
            // Ensure we're in the correct scene for player updates
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            Debug.Log($"SkinManager: Updating player skin to: {newSkin}");
            
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
                Debug.Log($"SkinManager: Successfully updated player skin animator to {newSkin}");
            }
            else
            {
                Debug.LogWarning($"SkinManager: No animator assigned for skin {newSkin}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SkinManager: Failed to update skin to {newSkin}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Called when user's equipped accessory changes - updates player Light2D
    /// </summary>
    /// <param name="newAccessory">The new equipped accessory</param>
    public void OnUserAccessoryChanged(EquippedAccessory newAccessory)
    {
        try
        {
            // Ensure we're in the correct scene for player updates
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            Debug.Log($"SkinManager: Updating player accessory to: {newAccessory}");
            
            // Ensure we have a valid player reference
            if (player == null)
            {
                FindPlayer(); // Try to find player again
            }
            
            if (player == null)
            {
                Debug.LogWarning("SkinManager: Cannot update accessory - no player GameObject available");
                return;
            }
            
            // Remove existing accessory light if any
            if (currentAccessoryLight != null)
            {
                DestroyImmediate(currentAccessoryLight.gameObject);
                currentAccessoryLight = null;
                Debug.Log("SkinManager: Removed previous accessory light");
            }
            
            // Add new accessory light if not None
            Light2D accessoryLightPrefab = GetLightPrefabForAccessory(newAccessory);
            if (accessoryLightPrefab != null)
            {
                GameObject lightObj = Instantiate(accessoryLightPrefab.gameObject, player.transform);
                currentAccessoryLight = lightObj.GetComponent<Light2D>();
                Debug.Log($"SkinManager: Successfully equipped accessory light for {newAccessory}");
            }
            else
            {
                Debug.Log($"SkinManager: No accessory light for {newAccessory}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SkinManager: Failed to update accessory to {newAccessory}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the appropriate animator controller for the given skin
    /// </summary>
    /// <param name="skin">The skin enum value</param>
    /// <returns>The corresponding animator controller, or default if not found</returns>
    public RuntimeAnimatorController GetAnimatorForSkin(EquippedSkin skin)
    {
        return skin switch
        {
            EquippedSkin.Rainbow => rainbowSkinAnimator ?? defaultSkinAnimator,
            EquippedSkin.Summer => summerSkinAnimator ?? defaultSkinAnimator,
            EquippedSkin.Winter => winterSkinAnimator ?? defaultSkinAnimator,
            EquippedSkin.Cat => catSkinAnimator ?? defaultSkinAnimator,

            EquippedSkin.Default => defaultSkinAnimator,
            _ => defaultSkinAnimator
        };
    }
    
    /// <summary>
    /// Gets the appropriate Light2D prefab for the given accessory
    /// </summary>
    /// <param name="accessory">The accessory enum value</param>
    /// <returns>The corresponding Light2D prefab, or null if none or not found</returns>
    public Light2D GetLightPrefabForAccessory(EquippedAccessory accessory)
    {
        return accessory switch
        {
            EquippedAccessory.Torch => torchLightPrefab,
            EquippedAccessory.Horns => hornsLightPrefab,
            EquippedAccessory.None => null,
            _ => null
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
    
    /// <summary>
    /// Manually refresh player accessory from Firebase (useful after login or scene changes)
    /// </summary>
    public async System.Threading.Tasks.Task RefreshPlayerAccessoryFromFirebase()
    {
        await InitializePlayerAccessory();
    }
    
    /// <summary>
    /// Set player accessory directly without going through accessory change event
    /// </summary>
    /// <param name="accessory">The accessory to apply</param>
    public void SetPlayerAccessory(EquippedAccessory accessory)
    {
        OnUserAccessoryChanged(accessory);
    }
    
    /// <summary>
    /// Get the currently equipped accessory Light2D component
    /// </summary>
    /// <returns>The current Light2D component, or null if no accessory equipped</returns>
    public Light2D GetCurrentAccessoryLight()
    {
        return currentAccessoryLight;
    }
}

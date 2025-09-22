using UnityEngine;
using UnityEngine.EventSystems;
using RamRoutes.Model;

public class RamClickHandler : MonoBehaviour, IPointerClickHandler
{
    private User user;
    private RamsManager ramsManager;
    private bool isInitialized = false;

    void Awake()
    {
        // SetupClickHandling();
    }

    void Start()
    {
        // Re-setup in Start() in case components were added after Awake
        if (!isInitialized)
        {
            // SetupClickHandling();
        }
    }

    // private void SetupClickHandling()
    // {
    //     // Ensure we have a collider for click detection
    //     BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
    //     if (boxCollider == null)
    //     {
    //         boxCollider = gameObject.AddComponent<BoxCollider2D>();
    //         Debug.Log($"RamClickHandler: Added BoxCollider2D to {gameObject.name}");
    //     }
        
    //     // Ensure the collider is enabled
    //     boxCollider.enabled = true;
        
    //     // Setup button click handling if there's a button component
    //     UnityEngine.UI.Button button = GetComponent<UnityEngine.UI.Button>();
    //     if (button != null)
    //     {
    //         button.onClick.RemoveAllListeners();
    //         button.onClick.AddListener(() => {
    //             if (user != null && ramsManager != null)
    //             {
    //                 ramsManager.HandleRamClick(user, gameObject);
    //             }
    //         });
    //         Debug.Log($"RamClickHandler: Setup button click listener for {gameObject.name}");
    //     }
        
    //     isInitialized = true;
    // }

    public void Initialize(User user, RamsManager ramsManager)
    {
        this.user = user;
        this.ramsManager = ramsManager;
        
        // Ensure click handling is set up after initialization
        // if (!isInitialized)
        // {
        //     SetupClickHandling();
        // }
        
        Debug.Log($"RamClickHandler: Initialized {gameObject.name} for user {user.name}");
    }

    void OnMouseDown()
    {
        // Handle direct mouse clicks on the ram sprite
        if (user != null && ramsManager != null)
        {
            ramsManager.HandleRamClick(user, gameObject);
        }
    }

    // Handle UI clicks (this is the one that was working)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (user != null && ramsManager != null)
        {
            ramsManager.HandleRamClick(user, gameObject);
        }
    }

    // Handle collision detection for chat initiation
    void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the player
        if (IsPlayer(other) && user != null && ramsManager != null)
        {
            Debug.Log($"Player collided with ram for user: {user.name}");
            ramsManager.HandleRamClick(user, gameObject);
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || 
               other.GetComponent<CharacterController>() != null ||
               other.gameObject.name.ToLower().Contains("player");
    }
}
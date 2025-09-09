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
        SetupClickHandling();
    }

    void Start()
    {
        // Re-setup in Start() in case components were added after Awake
        if (!isInitialized)
        {
            SetupClickHandling();
        }
    }

    private void SetupClickHandling()
    {
        // Ensure we have a collider for click detection
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            // Check for CircleCollider2D first (rams already have this)
            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider == null)
            {
                BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                Debug.Log($"RamClickHandler: Added BoxCollider2D to {gameObject.name}");
            }
        }
        
        Debug.Log($"RamClickHandler: Set up ram click for {gameObject.name}");
        isInitialized = true;
    }

    public void Initialize(User user, RamsManager ramsManager)
    {
        this.user = user;
        this.ramsManager = ramsManager;
        
        // Ensure click handling is set up after initialization
        if (!isInitialized)
        {
            SetupClickHandling();
        }
        
        Debug.Log($"RamClickHandler: Initialized {gameObject.name} for user {user.name}");
    }

    void OnMouseDown()
    {
        // Handle direct mouse clicks on the ram sprite
        if (user != null && ramsManager != null)
        {
            Debug.Log($"RamClickHandler: Mouse down on ram {user.name}");
            ramsManager.HandleRamClick(user);
        }
    }

    // Handle UI clicks (this is the one that was working)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (user != null && ramsManager != null)
        {
            Debug.Log($"RamClickHandler: Pointer click on ram {user.name}");
            ramsManager.HandleRamClick(user);
        }
    }
}
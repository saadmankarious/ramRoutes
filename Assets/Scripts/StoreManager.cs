using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RamRoutes.Model;
using RamRoutes.Services;

/// <summary>
/// StoreManager handles the display and interaction with store items.
/// 
/// Hierarchy Understanding:
/// - "items" container: The main container that gets hidden/shown when store opens/closes
/// - Content container: Where items are actually instantiated (ScrollView content or items container)
/// - ScrollView: Optional scrollable area within the items container hierarchy
/// 
/// Expected Hierarchy:
/// items (visibility toggle) → ... → ScrollView → content (item instantiation)
/// OR
/// items (both visibility and instantiation)
/// 
/// Setup:
/// 1. Assign the "items" container that should be hidden/shown to itemsContainer field
/// 2. Optionally assign ScrollRect to storeScrollView for scrollable display
/// 3. Items will be instantiated in ScrollView content if available, otherwise in items container
/// 
/// Usage:
/// - Opening store: Shows the "items" container (revealing entire store UI)
/// - Closing store: Hides the "items" container (hiding entire store UI)
/// - ScrollView provides scrolling functionality within the visible store area
/// </summary>
public class StoreManager : MonoBehaviour
{
    [Header("Store UI References")]
    public GameObject itemPrefab;
    public Button openStoreButton;
    public Button closeStoreButton;
    public ScrollRect storeScrollView; // ScrollView reference
    public Transform itemsContainer; // The "items" container that gets hidden/shown
    
    [Header("Store Settings")]
    public bool useTestData = false;
    public Color overlayColor = new Color(0, 0, 0, 0.5f); // Semi-transparent black
    
    private StoreService storeService;
    private List<GameObject> spawnedItems = new List<GameObject>();
    private Transform contentContainer; // Where items are actually instantiated (ScrollView content or fallback)
    private bool isStoreOpen = false;
    private SpriteRenderer overlaySprite;
    
    void Start()
    {
        storeService = new StoreService();
        InitializeStoreManager();
        
        // Configure ScrollView if available
        if (storeScrollView != null)
        {
            ConfigureScrollView();
        }
        
        // Don't load items immediately - wait for store to be opened
    }
    
    private void InitializeStoreManager()
    {
        // Find or assign the "items" container (the one that gets hidden/shown)
        if (itemsContainer == null)
        {
            itemsContainer = FindChildByName(transform, "items");
            if (itemsContainer == null)
            {
                Debug.LogError("StoreManager: No 'items' container found. Please assign the 'items' container that should be hidden/shown.");
                return;
            }
            else
            {
                Debug.Log("StoreManager: Found and using 'items' container for visibility toggle");
            }
        }
        else
        {
            Debug.Log("StoreManager: Using directly assigned 'items' container for visibility toggle");
        }
        
        // Determine content container (where items are actually instantiated)
        if (storeScrollView != null && storeScrollView.content != null)
        {
            contentContainer = storeScrollView.content.transform;
            Debug.Log("StoreManager: Using ScrollView content for item instantiation");
        }
        else
        {
            // Fallback to items container or find a suitable child
            contentContainer = itemsContainer;
            Debug.Log("StoreManager: Using items container directly for item instantiation");
        }
        
        // Initially hide the items container (this hides the entire store UI)
        if (itemsContainer != null)
        {
            itemsContainer.gameObject.SetActive(false);
        }
        
        // Set up the open store button if assigned
        if (openStoreButton != null)
        {
            openStoreButton.onClick.RemoveAllListeners();
            openStoreButton.onClick.AddListener(ToggleStore);
            Debug.Log("StoreManager: Set up store toggle button");
        }
        else
        {
            Debug.LogWarning("StoreManager: No openStoreButton assigned. Please assign a Button to toggle store visibility");
        }
        
        // Set up the close store button if assigned
        if (closeStoreButton != null)
        {
            closeStoreButton.onClick.RemoveAllListeners();
            closeStoreButton.onClick.AddListener(CloseStore);
            Debug.Log("StoreManager: Set up store close button");
        }
        else
        {
            Debug.LogWarning("StoreManager: No closeStoreButton assigned. Optionally assign a Button to close the store");
        }
        
        // Create overlay for when store is open
    }
    

    
    private void ToggleStore()
    {
        isStoreOpen = !isStoreOpen;
        
        // Use the new method to properly handle UI activation
        SetStoreUIActive(isStoreOpen);
        
        // Load items if opening store for the first time
        if (isStoreOpen && spawnedItems.Count == 0)
        {
            LoadStoreItems();
        }
        
        Debug.Log($"StoreManager: Store {(isStoreOpen ? "opened" : "closed")}");
    }
    
    // Public method to open/close store programmatically
    public void OpenStore()
    {
        if (!isStoreOpen)
        {
            ToggleStore();
        }
    }
    
    public void CloseStore()
    {
        if (isStoreOpen)
        {
            ToggleStore();
        }
    }
    
    /// <summary>
    /// Configure the ScrollView for optimal store item display
    /// Call this method if you want to customize ScrollView settings
    /// </summary>
    public void ConfigureScrollView()
    {
        if (storeScrollView != null)
        {
            // Enable vertical scrolling
            storeScrollView.vertical = true;
            storeScrollView.horizontal = false;
            
            // Configure scroll sensitivity
            storeScrollView.scrollSensitivity = 20f;
            
            // Enable inertia for smooth scrolling
            storeScrollView.inertia = true;
            storeScrollView.decelerationRate = 0.135f;
            
            // Ensure content has proper layout components
            if (storeScrollView.content != null)
            {
                // Add VerticalLayoutGroup if not present
                if (storeScrollView.content.GetComponent<VerticalLayoutGroup>() == null)
                {
                    VerticalLayoutGroup layoutGroup = storeScrollView.content.gameObject.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.spacing = 10f;
                    layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childControlWidth = true;
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = true;
                }
                
                // Add ContentSizeFitter if not present
                if (storeScrollView.content.GetComponent<ContentSizeFitter>() == null)
                {
                    ContentSizeFitter sizeFitter = storeScrollView.content.gameObject.AddComponent<ContentSizeFitter>();
                    sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
            }
            
            Debug.Log("StoreManager: ScrollView configured for optimal store display");
        }
    }
    
    /// <summary>
    /// Properly activates/deactivates the store UI
    /// Toggles the "items" container which contains the entire store UI
    /// </summary>
    private void SetStoreUIActive(bool active)
    {
        // Toggle the main items container (this shows/hides the entire store)
        if (itemsContainer != null)
        {
            itemsContainer.gameObject.SetActive(active);
            Debug.Log($"StoreManager: Items container '{itemsContainer.name}' set to {(active ? "active" : "inactive")}");
        }
        else
        {
            Debug.LogError("StoreManager: Items container is null, cannot toggle store visibility");
        }
    }
    
    /// <summary>
    /// Debug method to log the store UI hierarchy
    /// Useful for troubleshooting activation issues
    /// </summary>
    [ContextMenu("Debug Store Hierarchy")]
    public void DebugStoreHierarchy()
    {
        Debug.Log("=== Store UI Hierarchy Debug ===");
        
        if (itemsContainer != null)
        {
            Debug.Log($"Items Container (visibility toggle): {itemsContainer.name} (Active: {itemsContainer.gameObject.activeSelf})");
            Transform parent = itemsContainer.parent;
            int level = 1;
            while (parent != null && level < 5)
            {
                Debug.Log($"  Items Parent {level}: {parent.name} (Active: {parent.gameObject.activeSelf})");
                parent = parent.parent;
                level++;
            }
        }
        else
        {
            Debug.Log("Items Container: Not assigned");
        }
        
        if (contentContainer != null && contentContainer != itemsContainer)
        {
            Debug.Log($"Content Container (item instantiation): {contentContainer.name} (Active: {contentContainer.gameObject.activeSelf})");
            Transform parent = contentContainer.parent;
            int level = 1;
            while (parent != null && level < 5)
            {
                Debug.Log($"  Content Parent {level}: {parent.name} (Active: {parent.gameObject.activeSelf})");
                parent = parent.parent;
                level++;
            }
        }
        else if (contentContainer == itemsContainer)
        {
            Debug.Log("Content Container: Same as Items Container");
        }
        else
        {
            Debug.Log("Content Container: Not assigned");
        }
        
        if (storeScrollView != null)
        {
            Debug.Log($"ScrollView: {storeScrollView.name} (Active: {storeScrollView.gameObject.activeSelf})");
        }
        else
        {
            Debug.Log("ScrollView: Not assigned");
        }
        
        Debug.Log($"Store Open: {isStoreOpen}, Spawned Items: {spawnedItems.Count}");
        Debug.Log("================================");
    }
    
    private async void LoadStoreItems()
    {
        try
        {
            List<StoreItem> items;
            
            if (useTestData)
            {
                items = await storeService.GetAllStoreItemsWithTestData();
            }
            else
            {
                items = await storeService.GetAllStoreItems();
            }
            
            DisplayStoreItems(items);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"StoreManager.LoadStoreItems: Error loading items: {ex.Message}");
            
            // Fallback to test items if loading fails
            if (useTestData)
            {
                var testItems = storeService.InitializeTestItems();
                DisplayStoreItems(testItems);
            }
        }
    }
    
    private void DisplayStoreItems(List<StoreItem> items)
    {
        // Clear existing items
        ClearSpawnedItems();
        
        if (contentContainer == null)
        {
            Debug.LogError("StoreManager: Cannot display items - no content container found");
            return;
        }
        
        foreach (var item in items)
        {
            GameObject itemObject = Instantiate(itemPrefab, contentContainer);
            
            // Ensure the instantiated item is active
            itemObject.SetActive(true);
            
            SetupItemUI(itemObject, item);
            spawnedItems.Add(itemObject);
        }
        
        // If using ScrollView, ensure content size is updated
        if (storeScrollView != null && storeScrollView.content != null)
        {
            // Force rebuild layout to ensure proper content size
            LayoutRebuilder.ForceRebuildLayoutImmediate(storeScrollView.content);
            
            // Reset scroll position to top
            storeScrollView.verticalNormalizedPosition = 1f;
        }
        
        Debug.Log($"Displayed {items.Count} store items in content container '{contentContainer.name}'");
    }
    
    /// <summary>
    /// Refresh the store items - useful for updating content dynamically
    /// </summary>
    public void RefreshStoreItems()
    {
        if (isStoreOpen)
        {
            Debug.Log("StoreManager: Refreshing store items");
            LoadStoreItems();
        }
        else
        {
            // Clear items and they will be reloaded when store is opened next time
            ClearSpawnedItems();
            Debug.Log("StoreManager: Cleared store items (will reload when store is opened)");
        }
    }

    private void SetupItemUI(GameObject itemObject, StoreItem item)
    {
        // Find UI components by name
        Transform nameTransform = FindChildByName(itemObject.transform, "name");
        Transform descTransform = FindChildByName(itemObject.transform, "desc");
        Transform kbTransform = FindChildByName(itemObject.transform, "kb");
        Transform coinsTransform = FindChildByName(itemObject.transform, "coins");
        Transform buyButtonTransform = FindChildByName(itemObject.transform, "buy-button");
        Transform imageTransform = FindChildByName(itemObject.transform, "image");
        
        // Set item name
        if (nameTransform != null)
        {
            SetTextComponent(nameTransform, item.name);
        }
        
        // Set item description
        if (descTransform != null)
        {
            SetTextComponent(descTransform, item.description);
        }
        
        // Set KB price
        if (kbTransform != null)
        {
            SetTextComponent(kbTransform, item.priceKb.ToString());
        }
        
        // Set coins price
        if (coinsTransform != null)
        {
            SetTextComponent(coinsTransform, item.priceCoins.ToString());
        }
        
        // Load and set item image
        if (imageTransform != null && !string.IsNullOrEmpty(item.imageUrl))
        {
            LoadItemImage(imageTransform, item.imageUrl);
        }
        
        // Set up buy button and check affordability
        if (buyButtonTransform != null)
        {
            var buyButton = buyButtonTransform.GetComponent<Button>();
            if (buyButton != null)
            {
                // Remove existing listeners to avoid duplicates
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => OnBuyButtonClicked(item));
                
                // Check if user can afford the item and enable/disable button accordingly
                CheckAffordabilityAndSetButton(buyButton, item.itemId, itemObject);
            }
        }
    }
    
    private void SetTextComponent(Transform textTransform, string text)
    {
        // Try TextMeshProUGUI first (preferred)
        var tmpText = textTransform.GetComponent<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = text;
            return;
        }
        
        // Fallback to legacy Text component
        var legacyText = textTransform.GetComponent<Text>();
        if (legacyText != null)
        {
            legacyText.text = text;
        }
    }
    
    private async void CheckAffordabilityAndSetButton(Button buyButton, string itemId, GameObject itemObject)
    {
        try
        {
            bool canAfford = await storeService.CanAffordItem(itemId);
            buyButton.interactable = canAfford;
            
            // Add or remove overlay based on affordability
            AddOverlayToUnaffordableItem(itemObject, canAfford);
            
            // Optional: Change button appearance based on affordability
            var buttonColors = buyButton.colors;
            if (canAfford)
            {
                buttonColors.normalColor = Color.white;
                buttonColors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            }
            else
            {
                buttonColors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            buyButton.colors = buttonColors;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"StoreManager.CheckAffordabilityAndSetButton: Error checking affordability: {ex.Message}");
            // If error occurs, disable the button and add overlay as a safety measure
            buyButton.interactable = false;
            AddOverlayToUnaffordableItem(itemObject, false);
        }
    }
    
    private void LoadItemImage(Transform imageTransform, string imageUrl)
    {
        // Get Image component from the transform
        var imageComponent = imageTransform.GetComponent<UnityEngine.UI.Image>();
        if (imageComponent != null)
        {
            StartCoroutine(LoadImageFromUrl(imageComponent, imageUrl));
        }
        else
        {
            Debug.LogWarning($"StoreManager: No Image component found on 'image' GameObject");
        }
    }
    
    private System.Collections.IEnumerator LoadImageFromUrl(UnityEngine.UI.Image imageComponent, string url)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                imageComponent.sprite = sprite;
            }
            else
            {
                Debug.LogError($"StoreManager: Failed to load image from {url}: {www.error}");
            }
        }
    }
    
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
    
    private async void OnBuyButtonClicked(StoreItem item)
    {
        Debug.Log($"Attempting to buy item: {item.name}");
        
        bool success = await storeService.BuyItem(item.itemId);
        
        if (success)
        {
            Debug.Log($"Successfully purchased {item.name}!");
            
            // Close the store
            CloseStore();
            
            // Show success message using UIManager
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate($"Purchased {item.name}!");

                // Get updated user values and update UI display
                var userService = new UserService();
                string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    UpdateUserDisplayValues(userService, currentUserId, uiManager);
                }
            }
        }
        else
        {
            Debug.Log($"Failed to purchase {item.name}. Check console for details.");
            
            // Show error message using UIManager
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate($"Cannot afford {item.name}");
            }
        }
    }
    
    private async void UpdateUserDisplayValues(UserService userService, string userId, UIManager uiManager)
    {
        try
        {
            int coins = await userService.GetUserCoins(userId);
            int knowledgePoints = await userService.GetUserKnowledgePoints(userId);
            
            uiManager.UpdateCoins(coins);
            uiManager.UpdateKnowledgePoints(knowledgePoints);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"StoreManager.UpdateUserDisplayValues: Error updating display values: {ex.Message}");
        }
    }
    
    private void ClearSpawnedItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                // Remove any overlays before destroying the item
                RemoveOverlayFromItem(item);
                DestroyImmediate(item);
            }
        }
        spawnedItems.Clear();
    }
    
    private void AddOverlayToUnaffordableItem(GameObject itemObject, bool canAfford)
    {
        // Only add overlay to unaffordable items
        if (canAfford)
        {
            // Item is affordable, remove overlay if it exists
            RemoveOverlayFromItem(itemObject);
            return;
        }
        
        // Check if overlay already exists to avoid duplicates
        Transform existingOverlay = itemObject.transform.Find("UnaffordableOverlay");
        if (existingOverlay != null)
        {
            return; // Overlay already exists
        }
        
        // Create overlay GameObject as child of the item
        GameObject overlayObject = new GameObject("UnaffordableOverlay");
        overlayObject.transform.SetParent(itemObject.transform, false);
        
        // Add Image component for the overlay
        var overlayImage = overlayObject.AddComponent<UnityEngine.UI.Image>();
        overlayImage.color = new Color(0, 0, 0, 0.6f); // Semi-transparent black overlay
        overlayImage.raycastTarget = false; // Don't block interactions
        
        // Add RectTransform and set it to cover the entire item
        var rectTransform = overlayObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Set as last sibling to appear on top
        overlayObject.transform.SetAsLastSibling();
        
        Debug.Log($"Added unaffordable overlay to store item: {itemObject.name}");
    }
    
    private void RemoveOverlayFromItem(GameObject itemObject)
    {
        // Find and destroy the overlay GameObject
        Transform overlayTransform = itemObject.transform.Find("UnaffordableOverlay");
        if (overlayTransform != null)
        {
            DestroyImmediate(overlayTransform.gameObject);
            Debug.Log($"Removed unaffordable overlay from store item: {itemObject.name}");
        }
    }
    
    // Public method to refresh store items
    public void RefreshStore()
    {
        LoadStoreItems();
    }
    
    // Method to toggle between test data and Firebase data
    public void SetUseTestData(bool useTest)
    {
        useTestData = useTest;
        LoadStoreItems();
    }
    
    void OnDestroy()
    {
        ClearSpawnedItems();
        
        // Clean up overlay sprite
        if (overlaySprite != null && overlaySprite.sprite != null)
        {
            DestroyImmediate(overlaySprite.sprite.texture);
            DestroyImmediate(overlaySprite.sprite);
        }
    }
}

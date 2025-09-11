using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RamRoutes.Model;
using RamRoutes.Services;

public class StoreManager : MonoBehaviour
{
    [Header("Store UI References")]
    public GameObject itemPrefab;
    public Button openStoreButton;
    public Button closeStoreButton;
    
    [Header("Store Settings")]
    public bool useTestData = true;
    public Color overlayColor = new Color(0, 0, 0, 0.5f); // Semi-transparent black
    
    private StoreService storeService;
    private List<GameObject> spawnedItems = new List<GameObject>();
    private Transform itemsContainer;
    private bool isStoreOpen = false;
    private SpriteRenderer overlaySprite;
    
    void Start()
    {
        storeService = new StoreService();
        InitializeStoreManager();
        // Don't load items immediately - wait for store to be opened
    }
    
    private void InitializeStoreManager()
    {
        // Look for a child named "items"
        // find the object recursievly
        itemsContainer = FindChildByName(transform, "whereitemslive");
        if (itemsContainer == null)
        {
            Debug.LogError("StoreManager: No 'items' child found. Please create a child GameObject named 'items'.");
        }
        else
        {
            Debug.Log("StoreManager: Found and using 'items' child GameObject");
            // Hide items container initially
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
        
        if (itemsContainer != null)
        {
            itemsContainer.gameObject.SetActive(isStoreOpen);
            
            // Toggle overlay sprite visibility
            if (overlaySprite != null)
            {
                overlaySprite.enabled = isStoreOpen;
            }
            
            // Only activate the parent when opening the store, don't deactivate when closing
            if (itemsContainer.parent.parent != null)
            {
                itemsContainer.parent.parent.gameObject.SetActive(isStoreOpen);
            }
            
            if (isStoreOpen && spawnedItems.Count == 0)
            {
                // Load items only when opening the store for the first time
                LoadStoreItems();
            }
            
            Debug.Log($"StoreManager: Store {(isStoreOpen ? "opened" : "closed")}");
        }
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
        
        if (itemsContainer == null)
        {
            Debug.LogError("StoreManager: Cannot display items - no 'items' container found");
            return;
        }
        
        foreach (var item in items)
        {
            GameObject itemObject = Instantiate(itemPrefab, itemsContainer);
            SetupItemUI(itemObject, item);
            spawnedItems.Add(itemObject);
        }
        
        Debug.Log($"Displayed {items.Count} store items");
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
        
        // Set up buy button
        if (buyButtonTransform != null)
        {
            var buyButton = buyButtonTransform.GetComponent<Button>();
            if (buyButton != null)
            {
                // Remove existing listeners to avoid duplicates
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => OnBuyButtonClicked(item));
                
                // Check if user can afford the item and enable/disable button accordingly
                CheckAffordabilityAndSetButton(buyButton, item.itemId);
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
    
    private async void CheckAffordabilityAndSetButton(Button buyButton, string itemId)
    {
        try
        {
            bool canAfford = await storeService.CanAffordItem(itemId);
            buyButton.interactable = canAfford;
            
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
            // If error occurs, disable the button as a safety measure
            buyButton.interactable = false;
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
                DestroyImmediate(item);
            }
        }
        spawnedItems.Clear();
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

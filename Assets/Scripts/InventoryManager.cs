using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RamRoutes.Model;
using RamRoutes.Services;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory UI References")]
    public GameObject itemPrefab;
    public Button openInventoryButton;
    public Button closeInventoryButton;
    
    [Header("Inventory Settings")]
    public Color overlayColor = new Color(0, 0, 0, 0.5f);
    
    private InventoryService inventoryService;
    private List<GameObject> spawnedItems = new List<GameObject>();
    private Transform itemsContainer;
    private GameObject emptyText; // Reference to the "empty" text child
    private bool isInventoryOpen = false;
    
    void Start()
    {
        inventoryService = new InventoryService();
        InitializeInventoryManager();
    }
    
    private void InitializeInventoryManager()
    {
        // Find the items container recursively
        itemsContainer = FindChildByName(transform, "whereitemslive");
        
        if (itemsContainer == null)
        {
            Debug.LogError("InventoryManager: No 'whereitemslive' child found. Please create a child GameObject named 'whereitemslive'.");
        }
        else
        {
            // Hide items container initially
            itemsContainer.gameObject.SetActive(false);
        }
        
        // Find the empty text child
        Transform emptyTransform = FindChildByName(transform, "empty");
        if (emptyTransform != null)
        {
            emptyText = emptyTransform.gameObject;
            emptyText.SetActive(false); // Hide initially
        }
        else
        {
            Debug.LogWarning("InventoryManager: No 'empty' child found. Please create a child GameObject named 'empty' to display when inventory is empty.");
        }
        
        // Set up the open inventory button if assigned
        if (openInventoryButton != null)
        {
            openInventoryButton.onClick.RemoveAllListeners();
            openInventoryButton.onClick.AddListener(ToggleInventory);
        }
        else
        {
            Debug.LogWarning("InventoryManager: No openInventoryButton assigned. Please assign a Button to toggle inventory visibility");
        }
        
        // Set up the close inventory button if assigned
        if (closeInventoryButton != null)
        {
            closeInventoryButton.onClick.RemoveAllListeners();
            closeInventoryButton.onClick.AddListener(CloseInventory);
        }
        else
        {
            Debug.LogWarning("InventoryManager: No closeInventoryButton assigned. Optionally assign a Button to close the inventory");
        }
    }
    
    private void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        
        if (itemsContainer != null)
        {
            itemsContainer.gameObject.SetActive(isInventoryOpen);
            
            // Show/hide the parent container accordingly
            if (itemsContainer.parent.parent.parent.parent != null)
            {
                itemsContainer.parent.parent.parent.parent.gameObject.SetActive(isInventoryOpen);
            }
            
            if (isInventoryOpen)
            {
                // Always reload items when opening the inventory to ensure counts are updated
                LoadInventoryItems();
            }
        }
    }
    
    // Public method to open/close inventory programmatically
    public void OpenInventory()
    {
        if (!isInventoryOpen)
        {
            ToggleInventory();
        }
    }
    
    public void CloseInventory()
    {
        if (isInventoryOpen)
        {
            ToggleInventory();
        }
    }
    
    private async void LoadInventoryItems()
    {
        try
        {
            List<InventoryItem> items;
            
         
                // Load all inventory items from Firebase
                items = await inventoryService.GetUserInventory();
          
            
            DisplayInventoryItems(items);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"InventoryManager.LoadInventoryItems: Error loading items: {ex.Message}");
            
        }
    }
    
    private void DisplayInventoryItems(List<InventoryItem> items)
    {
        // Clear existing items
        ClearSpawnedItems();
        
        if (itemsContainer == null)
        {
            Debug.LogError("InventoryManager: Cannot display items - no 'whereitemslive' container found");
            return;
        }
        
        // Check if inventory is empty
        bool isEmpty = items == null || items.Count == 0;
        
        // Show/hide empty text based on inventory state
        if (emptyText != null)
        {
            emptyText.SetActive(isEmpty);
        }
        
        if (isEmpty)
        {
            return;
        }
        
        // Display items directly using their quantity property instead of grouping
        var sortedItems = items
            .OrderBy(item => item.itemName)
            .ToList();
        
        foreach (var item in sortedItems)
        {
            GameObject itemObject = Instantiate(itemPrefab, itemsContainer);
            SetupItemUI(itemObject, item, item.quantity, item.equipped, new List<InventoryItem> { item });
            spawnedItems.Add(itemObject);
        }
    }
    
    private void SetupItemUI(GameObject itemObject, InventoryItem item, int count, bool hasEquipped, List<InventoryItem> allItems)
    {
        // Find UI components by name
        Transform nameTransform = FindChildByName(itemObject.transform, "name");
        Transform descTransform = FindChildByName(itemObject.transform, "desc");
        Transform imageTransform = FindChildByName(itemObject.transform, "image");
        Transform countTransform = FindChildByName(itemObject.transform, "count");
        Transform equipButtonTransform = FindChildByName(itemObject.transform, "equip-button");
        Transform sellButtonTransform = FindChildByName(itemObject.transform, "sell-button");
        
        // Set item name
        if (nameTransform != null)
        {
            SetTextComponent(nameTransform, item.itemName);
        }
        
        // Set item description
        if (descTransform != null)
        {
            SetTextComponent(descTransform, item.description);
        }
        
        // Set item count
        if (countTransform != null)
        {
            SetTextComponent(countTransform, count.ToString());
        }
        
        // Load and set item image
        if (imageTransform != null && !string.IsNullOrEmpty(item.imageUrl))
        {
            LoadItemImage(imageTransform, item.imageUrl);
        }
        
        // Add overlay to unequipped items
        AddOverlayToUnequippedItem(itemObject, item.equipped);
        
        // Set up equip/unequip button
        if (equipButtonTransform != null)
        {
            var equipButton = equipButtonTransform.GetComponent<Button>();
            if (equipButton != null)
            {
                equipButton.onClick.RemoveAllListeners();
                
                // Set button text based on equipped status
                var buttonText = equipButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (buttonText == null) buttonText = equipButton.GetComponentInChildren<UnityEngine.UI.Text>();

                if (item.equipped)
                {
                    if (buttonText != null) buttonText.text = "Unequip";
                    equipButton.onClick.AddListener(() => OnUnequipButtonClicked(allItems));
                }
                else
                {
                    if (buttonText != null) buttonText.text = "Equip";
                    equipButton.onClick.AddListener(() => OnEquipButtonClicked(allItems));
                }
            }
        }
        
        // Set up sell button
        if (sellButtonTransform != null)
        {
            var sellButton = sellButtonTransform.GetComponent<Button>();
            if (sellButton != null)
            {
                sellButton.onClick.RemoveAllListeners();
                sellButton.onClick.AddListener(() => OnSellButtonClicked(allItems));
            }
        }
    }
    
    private void SetTextComponent(Transform textTransform, string text)
    {
        // Try TextMeshProUGUI first (preferred)
        var tmpText = textTransform.GetComponent<UnityEngine.UI.Text>();
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
    
    private void LoadItemImage(Transform imageTransform, string imageUrl)
    {
        // Get Image component from the transform
        var imageComponent = imageTransform.GetComponent<UnityEngine.UI.Image>();
        if (imageComponent != null)
        {
            StartCoroutine(LoadImageFromUrl(imageComponent, imageUrl));
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
                Debug.LogError($"InventoryManager: Failed to load image from {url}: {www.error}");
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
    
    private async void OnEquipButtonClicked(List<InventoryItem> items)
    {
        var firstItem = items.FirstOrDefault();
        if (firstItem == null)
        {
            Debug.LogError("No items available for equipping");
            return;
        }
        
        // Use the first unequipped item for equipping
        var itemToEquip = items.FirstOrDefault(item => !item.equipped);
        if (itemToEquip == null)
        {
            Debug.LogError("No unequipped items available for equipping");
            return;
        }
        
        bool success = await inventoryService.EquipItem(itemToEquip.inventoryId);
        
        if (success)
        {
            // Show success message using UIManager
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate($"Equipped {firstItem.itemName}!");
            }
            
            // Refresh inventory to update button states
            LoadInventoryItems();
        }
        else
        {
            // Show error message using UIManager
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate($"Failed to equip {firstItem.itemName}");
            }
        }
    }
    
    private async void OnUnequipButtonClicked(List<InventoryItem> items)
    {
        var firstItem = items.FirstOrDefault();
        if (firstItem == null)
        {
            Debug.LogError("No items available for unequipping");
            return;
        }
        
        // Use the equipped item for unequipping
        var itemToUnequip = items.FirstOrDefault(item => item.equipped);
        if (itemToUnequip == null)
        {
            Debug.LogError("No equipped items available for unequipping");
            return;
        }
        
        bool success = await inventoryService.UnequipItem(itemToUnequip.inventoryId);
        
        if (success)
        {
            // Show success message using UIManager
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate($"Unequipped {firstItem.itemName}!");
            }
            
            // Refresh inventory to update button states
            LoadInventoryItems();
        }
        else
        {
            // Show error message using UIManager
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate($"Failed to unequip {firstItem.itemName}");
            }
        }
    }
    
    private async void OnSellButtonClicked(List<InventoryItem> items)
    {
        var firstItem = items.FirstOrDefault();
        if (firstItem == null)
        {
            Debug.LogError("No items available for selling");
            return;
        }
        
        // Use the first available item for selling
        var itemToSell = items.FirstOrDefault();
        if (itemToSell == null)
        {
            Debug.LogError("No items available for selling");
            return;
        }
        
        // Calculate sell price (50% of original price)
        int sellPriceCoins = itemToSell.pricePaidCoins / 2;
        int sellPriceKb = itemToSell.pricePaidKb / 2;
        
        bool success = await inventoryService.SellItem(itemToSell.inventoryId, sellPriceCoins, sellPriceKb);
        
        if (success)
        {
            // Show success message using UIManager
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate($"Sold {firstItem.itemName} for {sellPriceCoins} coins and {sellPriceKb} KB!");
                
                // Update user display values
                var userService = new UserService();
                string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    UpdateUserDisplayValues(userService, currentUserId, uiManager);
                }
            }
            
            // Refresh inventory to remove sold item
            LoadInventoryItems();
        }
        else
        {
            // Show error message using UIManager
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowQuickUpdate($"Failed to sell {firstItem.itemName}");
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
            Debug.LogError($"InventoryManager.UpdateUserDisplayValues: Error updating display values: {ex.Message}");
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
    
    private void AddOverlayToUnequippedItem(GameObject itemObject, bool hasEquipped)
    {
        // Only add overlay to unequipped items
        if (hasEquipped)
        {
            return; // Item is equipped, no overlay needed
        }
        
        // Create overlay GameObject as child of the item
        GameObject overlayObject = new GameObject("UnequippedOverlay");
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
    }
    
    private void RemoveOverlayFromItem(GameObject itemObject)
    {
        // Find and destroy the overlay GameObject
        Transform overlayTransform = itemObject.transform.Find("UnequippedOverlay");
        if (overlayTransform != null)
        {
            DestroyImmediate(overlayTransform.gameObject);
        }
    }
    
    
 
    
    // Public method to refresh inventory items (useful after purchasing items)
    public void RefreshInventory()
    {
        if (isInventoryOpen)
        {
            LoadInventoryItems();
        }
    }
    
    void OnDestroy()
    {
        ClearSpawnedItems();
    }
}

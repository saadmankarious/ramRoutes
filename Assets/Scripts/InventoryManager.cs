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
            Debug.Log("InventoryManager: Found and using 'whereitemslive' child GameObject");
            // Hide items container initially
            itemsContainer.gameObject.SetActive(false);
        }
        
        // Set up the open inventory button if assigned
        if (openInventoryButton != null)
        {
            openInventoryButton.onClick.RemoveAllListeners();
            openInventoryButton.onClick.AddListener(ToggleInventory);
            Debug.Log("InventoryManager: Set up inventory toggle button");
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
            Debug.Log("InventoryManager: Set up inventory close button");
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
            if (itemsContainer.parent.parent != null)
            {
                itemsContainer.parent.parent.gameObject.SetActive(isInventoryOpen);
            }
            
            if (isInventoryOpen)
            {
                // Always reload items when opening the inventory to ensure counts are updated
                LoadInventoryItems();
            }
            
            Debug.Log($"InventoryManager: Inventory {(isInventoryOpen ? "opened" : "closed")}");
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
        
        // Group items by itemId and display each unique item once with count
        var groupedItems = items
            .GroupBy(item => item.itemId)
            .Select(group => new {
                Item = group.First(), // Take first item for display info
                Count = group.Count(), // Count how many of this item
                HasEquipped = group.Any(item => item.equipped), // Check if any is equipped
                AllItems = group.ToList() // Keep all items for operations
            })
            .OrderBy(group => group.Item.itemName)
            .ToList();
        
        foreach (var group in groupedItems)
        {
            GameObject itemObject = Instantiate(itemPrefab, itemsContainer);
            SetupItemUI(itemObject, group.Item, group.Count, group.HasEquipped, group.AllItems);
            spawnedItems.Add(itemObject);
        }
        
        Debug.Log($"Displayed {groupedItems.Count} unique inventory items (total {items.Count} items)");
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

                if (hasEquipped)
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
        else
        {
            Debug.LogWarning($"InventoryManager: No Image component found on 'image' GameObject");
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
        
        Debug.Log($"Attempting to equip item: {firstItem.itemName}");
        
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
            Debug.Log($"Successfully equipped {firstItem.itemName}!");
            
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
            Debug.Log($"Failed to equip {firstItem.itemName}. Check console for details.");
            
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
        
        Debug.Log($"Attempting to unequip item: {firstItem.itemName}");
        
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
            Debug.Log($"Successfully unequipped {firstItem.itemName}!");
            
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
            Debug.Log($"Failed to unequip {firstItem.itemName}. Check console for details.");
            
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
        
        Debug.Log($"Attempting to sell item: {firstItem.itemName}");
        
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
            Debug.Log($"Successfully sold {firstItem.itemName}!");
            
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
            Debug.Log($"Failed to sell {firstItem.itemName}. Check console for details.");
            
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
                DestroyImmediate(item);
            }
        }
        spawnedItems.Clear();
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

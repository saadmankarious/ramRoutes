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
    
    [Header("Store Settings")]
    public bool useTestData = true;
    
    private StoreService storeService;
    private List<GameObject> spawnedItems = new List<GameObject>();
    private Transform itemsContainer;
    private bool isStoreOpen = false;
    
    void Start()
    {
        storeService = new StoreService();
        InitializeStoreManager();
        // Don't load items immediately - wait for store to be opened
    }
    
    private void InitializeStoreManager()
    {
        // Look for a child named "items"
        itemsContainer = transform.Find("items");
        
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
    }
    
    private void ToggleStore()
    {
        isStoreOpen = !isStoreOpen;
        
        if (itemsContainer != null)
        {
            itemsContainer.gameObject.SetActive(isStoreOpen);
            
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
            SetTextComponent(kbTransform, item.priceKb.ToString() + " KB");
        }
        
        // Set coins price
        if (coinsTransform != null)
        {
            SetTextComponent(coinsTransform, item.priceCoins.ToString() + " Coins");
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
            // You can add UI feedback here (e.g., show success message, update UI)
            
            // Optionally refresh the store items to update availability
            LoadStoreItems();
        }
        else
        {
            Debug.Log($"Failed to purchase {item.name}. Check console for details.");
            // You can add UI feedback here (e.g., show error message)
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
    }
}

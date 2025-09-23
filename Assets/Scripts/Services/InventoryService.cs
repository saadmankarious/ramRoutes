using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    public class InventoryService
    {
        private FirebaseFirestore db;
        private const string USER_INVENTORY_COLLECTION = "user-inventory";

        public InventoryService()
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        public async Task<List<InventoryItem>> GetUserInventory(string userId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("InventoryService.GetUserInventory: No user ID provided");
                return new List<InventoryItem>();
            }

            try
            {
                var query = db.Collection(USER_INVENTORY_COLLECTION).WhereEqualTo("userId", userId);
                var snapshot = await query.GetSnapshotAsync();

                var inventory = new List<InventoryItem>();
                foreach (var doc in snapshot.Documents)
                {
                    var item = ConvertDocumentToInventoryItem(doc);
                    if (item != null)
                    {
                        inventory.Add(item);
                    }
                }

                return inventory.OrderByDescending(i => i.purchaseDate).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"InventoryService.GetUserInventory: Error getting inventory: {ex.Message}");
                return new List<InventoryItem>();
            }
        }

        public async Task<List<InventoryItem>> GetInventoryByCategory(string category, string userId = null)
        {
            var allItems = await GetUserInventory(userId);
            return allItems.Where(item => item.category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<InventoryItem>> GetEquippedItems(string userId = null)
        {
            var allItems = await GetUserInventory(userId);
            return allItems.Where(item => item.equipped).ToList();
        }

        /// <summary>
        /// Safely converts a Firestore document to InventoryItem with proper type handling
        /// </summary>
        private InventoryItem ConvertDocumentToInventoryItem(DocumentSnapshot doc)
        {
            try
            {
                var data = doc.ToDictionary();
                var item = new InventoryItem();
                
                item.inventoryId = doc.Id;
                item.userId = data.ContainsKey("userId") ? data["userId"].ToString() : "";
                item.itemId = data.ContainsKey("itemId") ? data["itemId"].ToString() : "";
                item.itemName = data.ContainsKey("itemName") ? data["itemName"].ToString() : "";
                item.description = data.ContainsKey("description") ? data["description"].ToString() : "";
                item.category = data.ContainsKey("category") ? data["category"].ToString() : "general";
                item.imageUrl = data.ContainsKey("imageUrl") ? data["imageUrl"].ToString() : "";
                item.quantity = data.ContainsKey("quantity") && int.TryParse(data["quantity"].ToString(), out int qty) ? qty : 1;
                
                // Handle numeric fields with safe conversion
                if (data.ContainsKey("pricePaidCoins"))
                {
                    if (int.TryParse(data["pricePaidCoins"].ToString(), out int coins))
                        item.pricePaidCoins = coins;
                }
                
                if (data.ContainsKey("pricePaidKb"))
                {
                    if (int.TryParse(data["pricePaidKb"].ToString(), out int kb))
                        item.pricePaidKb = kb;
                }
                
                // Handle boolean field
                if (data.ContainsKey("equipped"))
                {
                    if (bool.TryParse(data["equipped"].ToString(), out bool equipped))
                        item.equipped = equipped;
                }
                
                // Handle whisperType with safe conversion (string to int)
                if (data.ContainsKey("whisperType"))
                {
                    var whisperTypeValue = data["whisperType"];
                    if (int.TryParse(whisperTypeValue.ToString(), out int whisperType))
                    {
                        item.whisperType = whisperType;
                    }
                    else
                    {
                        item.whisperType = 0; // Default to Greeting
                        Debug.LogWarning($"Could not parse whisperType '{whisperTypeValue}' for item {item.itemName}, defaulting to 0");
                    }
                }
                
                // Handle timestamp
                if (data.ContainsKey("purchaseDate") && data["purchaseDate"] is Timestamp timestamp)
                {
                    item.purchaseDate = timestamp;
                }
                else
                {
                    item.purchaseDate = Timestamp.GetCurrentTimestamp();
                }
                
                return item;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting document {doc.Id} to InventoryItem: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> EquipItem(string inventoryId)
        {
            string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("InventoryService.EquipItem: No logged-in user");
                return false;
            }

            try
            {
                var inventoryDoc = await db.Collection(USER_INVENTORY_COLLECTION).Document(inventoryId).GetSnapshotAsync();
                if (!inventoryDoc.Exists)
                {
                    Debug.LogError("InventoryService.EquipItem: Inventory item not found");
                    return false;
                }

                var item = ConvertDocumentToInventoryItem(inventoryDoc);
                if (item == null)
                {
                    Debug.LogError("InventoryService.EquipItem: Failed to convert inventory item");
                    return false;
                }
                
                if (item.userId != currentUserId)
                {
                    Debug.LogError("InventoryService.EquipItem: Item doesn't belong to current user");
                    return false;
                }

                await db.Collection(USER_INVENTORY_COLLECTION).Document(inventoryId).UpdateAsync(new Dictionary<string, object>
                {
                    { "equipped", true }
                });

                // If the item is clothing, update the user's equipped skin
                if (item.category.Equals("Clothing", StringComparison.OrdinalIgnoreCase))
                {
                    // Map item name to skin enum
                    EquippedSkin newSkin = MapItemNameToSkin(item.itemName);

                    var userService = new UserService();
                    bool skinUpdated = await userService.UpdateEquippedSkin(currentUserId, newSkin);

                    if (skinUpdated)
                    {

                        // Notify UIManager to refresh user avatar/appearance
                        NotifyUIManagerSkinChanged(newSkin);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to update equipped skin for clothing item: {item.itemName}");
                    }
                }

                // If item is an accessory, update the user's equipped accessory
                if (item.category.Equals("Accessories", StringComparison.OrdinalIgnoreCase))
                {
                    // Map item name to accessory enum
                    EquippedAccessory newAccessory = MapItemNameToAccessory(item.itemName);

                    var userService = new UserService();
                    bool accessoryUpdated = await userService.UpdateEquippedAccessory(currentUserId, newAccessory);

                    if (accessoryUpdated)
                    {

                        // Notify UIManager to refresh user avatar/appearance
                        NotifyUIManagerAccessoryChanged(newAccessory);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to update equipped accessory for accessory item: {item.itemName}");
                    }
                }

                if (item.category.Equals("Whisper", StringComparison.OrdinalIgnoreCase))
                {
                    // For whisper items, just mark as equipped (no need to update user profile)
                    WhisperType whisperType = MapWhisperNameToType(item.whisperType);
                    

                    // Notify managers about the whisper change
                    NotifyManagersWhisperChanged(whisperType);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"InventoryService.EquipItem: Error equipping item: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnequipItem(string inventoryId)
        {
            string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("InventoryService.UnequipItem: No logged-in user");
                return false;
            }

            try
            {
                var inventoryDoc = await db.Collection(USER_INVENTORY_COLLECTION).Document(inventoryId).GetSnapshotAsync();
                if (!inventoryDoc.Exists)
                {
                    Debug.LogError("InventoryService.UnequipItem: Inventory item not found");
                    return false;
                }

                InventoryItem item = null;
                try
                {
                    item = ConvertDocumentToInventoryItem(inventoryDoc);
                }
                catch (Exception conversionEx)
                {
                    return false;
                }
                
                if (item == null)
                {
                    return false;
                }
                
                if (item.userId != currentUserId)
                {
                    return false;
                }

                await db.Collection(USER_INVENTORY_COLLECTION).Document(inventoryId).UpdateAsync(new Dictionary<string, object>
                {
                    { "equipped", false }
                });

                if (item.category.Equals("Clothing", StringComparison.OrdinalIgnoreCase))
                {
                    // Map item name to skin enum
                    EquippedSkin newSkin = EquippedSkin.Default; // Revert to default on unequip

                    var userService = new UserService();
                    bool skinUpdated = await userService.UpdateEquippedSkin(currentUserId, newSkin);

                    if (skinUpdated)
                    {

                        // Notify UIManager to refresh user avatar/appearance
                        NotifyUIManagerSkinChanged(newSkin);
                    }
                   
                }

                // If item is an accessory, revert to no accessory
                if (item.category.Equals("Accessories", StringComparison.OrdinalIgnoreCase))
                {
                    EquippedAccessory newAccessory = EquippedAccessory.None; // Revert to none on unequip

                    var userService = new UserService();
                    bool accessoryUpdated = await userService.UpdateEquippedAccessory(currentUserId, newAccessory);

                    if (accessoryUpdated)
                    {

                        // Notify UIManager to refresh user avatar/appearance
                        NotifyUIManagerAccessoryChanged(newAccessory);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to update equipped accessory for accessory item: {item.itemName}");
                    }
                }

                // If item is a whisper, notify ChatManager to refresh whisper buttons
                if (item.category.Equals("Whisper", StringComparison.OrdinalIgnoreCase))
                {
                    if (Enum.IsDefined(typeof(WhisperType), item.whisperType))
                    {
                        WhisperType whisperType = (WhisperType)item.whisperType;
                        NotifyManagersWhisperChanged(whisperType);
                        Debug.Log($"Successfully unequipped whisper: {whisperType} for item: {item.itemName}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"InventoryService.UnequipItem: Error unequipping item: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SellItem(string inventoryId, int sellPriceCoins, int sellPriceKb)
        {
            string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("InventoryService.SellItem: No logged-in user");
                return false;
            }

            try
            {
                var inventoryDoc = await db.Collection(USER_INVENTORY_COLLECTION).Document(inventoryId).GetSnapshotAsync();
                if (!inventoryDoc.Exists)
                {
                    Debug.LogError("InventoryService.SellItem: Inventory item not found");
                    return false;
                }

                var item = ConvertDocumentToInventoryItem(inventoryDoc);
                if (item == null)
                {
                    Debug.LogError("InventoryService.SellItem: Failed to convert inventory item");
                    return false;
                }
                
                if (item.userId != currentUserId)
                {
                    Debug.LogError("InventoryService.SellItem: Item doesn't belong to current user");
                    return false;
                }

                // Update user's coins and KB
                var userService = new UserService();
                int userCoins = await userService.GetUserCoins(currentUserId);
                int userKb = await userService.GetUserKnowledgePoints(currentUserId);

                var userDoc = db.Collection("users").Document(currentUserId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "coins", userCoins + sellPriceCoins },
                    { "knowledgePoints", userKb + sellPriceKb }
                });

                // Remove item from inventory
                await db.Collection(USER_INVENTORY_COLLECTION).Document(inventoryId).DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"InventoryService.SellItem: Error selling item: {ex.Message}");
                return false;
            }
        }

        public async Task<int> GetInventoryCount(string userId = null)
        {
            var inventory = await GetUserInventory(userId);
            return inventory.Count;
        }

        public async Task<int> GetEquippedCount(string userId = null)
        {
            var equippedItems = await GetEquippedItems(userId);
            return equippedItems.Count;
        }

        // Utility method for testing - initializes and returns test inventory items
        public List<InventoryItem> InitializeTestInventory(string userId)
        {
            var testItems = new List<InventoryItem>();

            // Initialize test inventory item
            var testItem = new InventoryItem
            {
                inventoryId = "test-inventory-001",
                userId = userId,
                itemId = "test-hat-001",
                itemName = "Cool Hat",
                description = "A stylish hat that makes you look smart",
                category = "hat",
                pricePaidCoins = 120,
                pricePaidKb = 50,
                imageUrl = "https://via.placeholder.com/150x150/4CAF50/FFFFFF?text=Hat",
                purchaseDate = Timestamp.GetCurrentTimestamp(),
                equipped = false
            };

            testItems.Add(testItem);

            return testItems;
        }

        /// <summary>
        /// Maps clothing item names to corresponding skin enums
        /// </summary>
        /// <param name="itemName">The name of the clothing item</param>
        /// <returns>The corresponding EquippedSkin enum value</returns>
        private EquippedSkin MapItemNameToSkin(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return EquippedSkin.Default;
            }

            string lowerItemName = itemName.ToLower();

            // Map based on item name keywords
            if (lowerItemName.Contains("rainbow"))
            {
                return EquippedSkin.Rainbow;
            }
            else if (lowerItemName.Contains("summer") || lowerItemName.Contains("beach") || lowerItemName.Contains("tropical"))
            {
                return EquippedSkin.Summer;
            }
            else if (lowerItemName.Contains("winter") || lowerItemName.Contains("snow") || lowerItemName.Contains("cold"))
            {
                return EquippedSkin.Winter;
            }
            else
            {
                return EquippedSkin.Default;
            }
        }

        /// <summary>
        /// Maps an accessory item name to the corresponding EquippedAccessory enum value
        /// </summary>
        /// <param name="itemName">The name of the accessory item</param>
        /// <returns>The corresponding EquippedAccessory enum value</returns>
        private EquippedAccessory MapItemNameToAccessory(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return EquippedAccessory.None;
            }

            string lowerItemName = itemName.ToLower();

            // Map based on item name keywords
            if (lowerItemName.Contains("torch") || lowerItemName.Contains("light") || lowerItemName.Contains("flame"))
            {
                return EquippedAccessory.Torch;
            }
            else if (lowerItemName.Contains("horns") || lowerItemName.Contains("horn") || lowerItemName.Contains("devil"))
            {
                return EquippedAccessory.Horns;
            }
            else
            {
                return EquippedAccessory.None;
            }
        }

        private WhisperType MapWhisperNameToType(int whisperTypeInt)
        {
            if (Enum.IsDefined(typeof(WhisperType), whisperTypeInt))
            {
                return (WhisperType)whisperTypeInt;
            }
            else
            {
                return WhisperType.Greeting;
            }
        }

        /// <summary>
        /// Notifies the SkinManager and UIManager that a skin has been changed
        /// </summary>
        /// <param name="newSkin">The new equipped skin</param>
        private void NotifyUIManagerSkinChanged(EquippedSkin newSkin)
        {
            try
            {
                // Only attempt notifications if we're in the game scene
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentScene != "LevelRPG")
                {
                    return;
                }

                // Find SkinManager in current scene (no singleton dependency)
                var skinManager = UnityEngine.Object.FindObjectOfType<SkinManager>();
                if (skinManager != null)
                {
                    skinManager.OnUserSkinChanged(newSkin);
                }
                else
                {
                    Debug.LogWarning("SkinManager not found in current scene - cannot update player skin");
                }

                // Find UIManager in the scene for UI notifications
                var uiManager = UnityEngine.Object.FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    // Show a quick notification about the skin change
                    uiManager.ShowQuickUpdate($"Equipped {newSkin} skin!");
                }
                else
                {
                    Debug.LogWarning("UIManager not found in scene - cannot notify of skin change");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to notify managers of skin change: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifies the UIManager that an accessory has been changed
        /// </summary>
        /// <param name="newAccessory">The new equipped accessory</param>
        private void NotifyUIManagerAccessoryChanged(EquippedAccessory newAccessory)
        {
            try
            {
                // Only attempt notifications if we're in the game scene
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentScene != "LevelRPG")
                {
                    return;
                }

                // Find SkinManager in current scene (no singleton dependency)
                var skinManager = UnityEngine.Object.FindObjectOfType<SkinManager>();
                if (skinManager != null)
                {
                    skinManager.OnUserAccessoryChanged(newAccessory);
                }
                else
                {
                    Debug.LogWarning("SkinManager not found in current scene - cannot update player accessory");
                }

                // Find UIManager in the scene for UI notifications
                var uiManager = UnityEngine.Object.FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    // Show a quick notification about the accessory change
                    string accessoryDisplayName = newAccessory == EquippedAccessory.None ? "No accessory" : newAccessory.ToString();
                    uiManager.ShowQuickUpdate($"Equipped {accessoryDisplayName}!");

                }
                else
                {
                    Debug.LogWarning("UIManager not found in scene - cannot notify of accessory change");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to notify managers of accessory change: {ex.Message}");
            }
        }

        private void NotifyManagersWhisperChanged(WhisperType newWhisper)
        {
            try
            {
                // Only attempt notifications if we're in the game scene
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentScene != "LevelRPG")
                {
                    return;
                }

                // Find UIManager in the scene for UI notifications
                var uiManager = UnityEngine.Object.FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    // Show a quick notification about the whisper change
                    string whisperDisplayName = newWhisper.ToString();
                    uiManager.ShowQuickUpdate($"Equipped {whisperDisplayName} whisper!");

                }
               
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to notify managers of whisper change: {ex.Message}");
            }

            // Notify chat manager of new message whisper added
            try
            {
                var chatManager = UnityEngine.Object.FindObjectOfType<ChatManager>();
                if (chatManager != null)
                {
                    chatManager.OnUserWhisperChanged();
                }
               
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to notify ChatManager of whisper change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Decrease the quantity of an inventory item by 1. If quantity reaches 0, remove the item.
        /// </summary>
        public async Task<bool> DecreaseItemQuantity(string userId, string whisperInventoryItemId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("InventoryService.DecreaseItemQuantity: User ID is null or empty");
                return false;
            }

            try
            {
                // Find the inventory item with the matching whisper type
                var inventory = await GetUserInventory(userId);
                var whisperItem = inventory.FirstOrDefault(item => item.itemId == whisperInventoryItemId && item.quantity > 0);
                
                if (whisperItem == null)
                {
                    Debug.LogWarning($"InventoryService.DecreaseItemQuantity: No whisper item found with type {whisperInventoryItemId} or quantity is 0");
                    return false;
                }

                var inventoryDoc = db.Collection(USER_INVENTORY_COLLECTION).Document(whisperItem.inventoryId);
                
                if (whisperItem.quantity <= 1)
                {
                    // Remove the item if quantity would reach 0 or below
                    await inventoryDoc.DeleteAsync();
                    Debug.Log($"InventoryService.DecreaseItemQuantity: Removed whisper item {whisperInventoryItemId} as quantity reached 0");
                }
                else
                {
                    // Decrease quantity by 1
                    await inventoryDoc.UpdateAsync(new Dictionary<string, object>
                    {
                        { "quantity", whisperItem.quantity - 1 }
                    });
                    Debug.Log($"InventoryService.DecreaseItemQuantity: Decreased whisper {whisperInventoryItemId} quantity to {whisperItem.quantity - 1}");
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"InventoryService.DecreaseItemQuantity: Error decreasing item quantity: {ex.Message}");
                return false;
            }
        }
    }
}

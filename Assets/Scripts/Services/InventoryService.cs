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
                    var item = doc.ConvertTo<InventoryItem>();
                    item.inventoryId = doc.Id;
                    inventory.Add(item);
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

                var item = inventoryDoc.ConvertTo<InventoryItem>();
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
                        Debug.Log($"Successfully updated equipped skin to: {newSkin} for clothing item: {item.itemName}");
                        
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
                        Debug.Log($"Successfully updated equipped accessory to: {newAccessory} for accessory item: {item.itemName}");
                        
                        // Notify UIManager to refresh user avatar/appearance
                        NotifyUIManagerAccessoryChanged(newAccessory);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to update equipped accessory for accessory item: {item.itemName}");
                    }
                }

                Debug.Log($"Successfully equipped item: {item.itemName}");
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

                var item = inventoryDoc.ConvertTo<InventoryItem>();
                if (item.userId != currentUserId)
                {
                    Debug.LogError("InventoryService.UnequipItem: Item doesn't belong to current user");
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
                        Debug.Log($"Successfully updated equipped skin to: {newSkin} for clothing item: {item.itemName}");
                        
                        // Notify UIManager to refresh user avatar/appearance
                        NotifyUIManagerSkinChanged(newSkin);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to update equipped skin for clothing item: {item.itemName}");
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
                        Debug.Log($"Successfully updated equipped accessory to: {newAccessory} for accessory item: {item.itemName}");
                        
                        // Notify UIManager to refresh user avatar/appearance
                        NotifyUIManagerAccessoryChanged(newAccessory);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to update equipped accessory for accessory item: {item.itemName}");
                    }
                }

                Debug.Log($"Successfully unequipped item: {item.itemName}");
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

                var item = inventoryDoc.ConvertTo<InventoryItem>();
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

                Debug.Log($"Successfully sold item {item.itemName} for {sellPriceCoins} coins and {sellPriceKb} KB");
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

            Debug.Log($"Initialized {testItems.Count} test inventory items");
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
                    Debug.Log($"Skipping skin change notification - not in game scene (current: {currentScene})");
                    return;
                }

                // Find SkinManager in current scene (no singleton dependency)
                var skinManager = UnityEngine.Object.FindObjectOfType<SkinManager>();
                if (skinManager != null)
                {
                    skinManager.OnUserSkinChanged(newSkin);
                    Debug.Log($"Notified SkinManager of skin change to: {newSkin}");
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
                    
                    Debug.Log($"Notified UIManager of skin change to: {newSkin}");
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
                    Debug.Log($"Skipping accessory change notification - not in game scene (current: {currentScene})");
                    return;
                }

                // Find SkinManager in current scene (no singleton dependency)
                var skinManager = UnityEngine.Object.FindObjectOfType<SkinManager>();
                if (skinManager != null)
                {
                    skinManager.OnUserAccessoryChanged(newAccessory);
                    Debug.Log($"Notified SkinManager of accessory change to: {newAccessory}");
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
                    
                    Debug.Log($"Notified UIManager of accessory change to: {newAccessory}");
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
    }
}

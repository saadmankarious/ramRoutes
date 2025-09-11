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
    }
}

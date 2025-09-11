using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    public class StoreService
    {
        private FirebaseFirestore db;
        private const string STORE_COLLECTION = "store-items";
        private const string USER_INVENTORY_COLLECTION = "user-inventory";

        public StoreService()
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        public async Task<List<StoreItem>> GetAllStoreItems()
        {
            try
            {
                var query = db.Collection(STORE_COLLECTION).WhereEqualTo("available", true);
                var snapshot = await query.GetSnapshotAsync();

                var items = new List<StoreItem>();
                foreach (var doc in snapshot.Documents)
                {
                    var item = doc.ConvertTo<StoreItem>();
                    item.itemId = doc.Id;
                    items.Add(item);
                }

                return items;
            }
            catch (Exception ex)
            {
                Debug.LogError($"StoreService.GetAllStoreItems: Error getting items: {ex.Message}");
                return new List<StoreItem>();
            }
        }

        public async Task<bool> BuyItem(string itemId)
        {
            string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("StoreService.BuyItem: No logged-in user");
                return false;
            }

            try
            {
                var itemDoc = await db.Collection(STORE_COLLECTION).Document(itemId).GetSnapshotAsync();
                if (!itemDoc.Exists)
                {
                    Debug.LogError("StoreService.BuyItem: Item not found");
                    return false;
                }

                var item = itemDoc.ConvertTo<StoreItem>();
                if (!item.available)
                {
                    Debug.LogError("StoreService.BuyItem: Item not available");
                    return false;
                }

                var userService = new UserService();
                int userCoins = await userService.GetUserCoins(currentUserId);
                int userKb = await userService.GetUserKnowledgePoints(currentUserId);

                if (userCoins < item.priceCoins || userKb < item.priceKb)
                {
                    Debug.LogError($"StoreService.BuyItem: Insufficient funds. Need {item.priceCoins} coins (have {userCoins}) and {item.priceKb} KB (have {userKb})");
                    return false;
                }

                var userDoc = db.Collection("users").Document(currentUserId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "coins", userCoins - item.priceCoins },
                    { "knowledgePoints", userKb - item.priceKb }
                });

                var inventoryData = new Dictionary<string, object>
                {
                    { "userId", currentUserId },
                    { "itemId", itemId },
                    { "itemName", item.name },
                    { "purchaseDate", Timestamp.GetCurrentTimestamp() },
                    { "pricePaidCoins", item.priceCoins },
                    { "pricePaidKb", item.priceKb }
                };

                await db.Collection(USER_INVENTORY_COLLECTION).AddAsync(inventoryData);

                Debug.Log($"Successfully purchased item {item.name}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"StoreService.BuyItem: Error purchasing item: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SellItem(string inventoryItemId, int sellPriceCoins, int sellPriceKb)
        {
            string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("StoreService.SellItem: No logged-in user");
                return false;
            }

            try
            {
                var inventoryDoc = await db.Collection(USER_INVENTORY_COLLECTION).Document(inventoryItemId).GetSnapshotAsync();
                if (!inventoryDoc.Exists)
                {
                    Debug.LogError("StoreService.SellItem: Inventory item not found");
                    return false;
                }

                var inventoryData = inventoryDoc.ToDictionary();
                if (inventoryData["userId"].ToString() != currentUserId)
                {
                    Debug.LogError("StoreService.SellItem: Item doesn't belong to current user");
                    return false;
                }

                var userService = new UserService();
                int userCoins = await userService.GetUserCoins(currentUserId);
                int userKb = await userService.GetUserKnowledgePoints(currentUserId);

                var userDoc = db.Collection("users").Document(currentUserId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
                {
                    { "coins", userCoins + sellPriceCoins },
                    { "knowledgePoints", userKb + sellPriceKb }
                });

                await db.Collection(USER_INVENTORY_COLLECTION).Document(inventoryItemId).DeleteAsync();

                Debug.Log($"Successfully sold item for {sellPriceCoins} coins and {sellPriceKb} KB");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"StoreService.SellItem: Error selling item: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Dictionary<string, object>>> GetUserInventory(string userId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("StoreService.GetUserInventory: No user ID provided");
                return new List<Dictionary<string, object>>();
            }

            try
            {
                var query = db.Collection(USER_INVENTORY_COLLECTION).WhereEqualTo("userId", userId);
                var snapshot = await query.GetSnapshotAsync();

                var inventory = new List<Dictionary<string, object>>();
                foreach (var doc in snapshot.Documents)
                {
                    var item = doc.ToDictionary();
                    item["inventoryId"] = doc.Id;
                    inventory.Add(item);
                }

                return inventory;
            }
            catch (Exception ex)
            {
                Debug.LogError($"StoreService.GetUserInventory: Error getting inventory: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        // Utility method for testing - initializes and returns test store items
        public List<StoreItem> InitializeTestItems()
        {
            var testItems = new List<StoreItem>();

            // Initialize hat item worth 50 KB and 120 coins
            var hatItem = new StoreItem
            {
                itemId = "test-hat-001",
                name = "Cool Hat",
                description = "A stylish hat that makes you look smart",
                category = "hat",
                priceKb = 50,
                priceCoins = 120,
                available = true,

            };

            testItems.Add(hatItem);

            Debug.Log($"Initialized {testItems.Count} test store items");
            return testItems;
        }

        // Modified GetAllStoreItems to return test items when in development mode
        public async Task<List<StoreItem>> GetAllStoreItemsWithTestData()
        {
            try
            {
                var query = db.Collection(STORE_COLLECTION).WhereEqualTo("available", true);
                var snapshot = await query.GetSnapshotAsync();

                var items = new List<StoreItem>();
                
                // Add items from Firebase
                foreach (var doc in snapshot.Documents)
                {
                    var item = doc.ConvertTo<StoreItem>();
                    item.itemId = doc.Id;
                    items.Add(item);
                }

                // Add test items for development/testing
                // var testItems = InitializeTestItems();
                // items.AddRange(testItems);

                return items;
            }
            catch (Exception ex)
            {
                Debug.LogError($"StoreService.GetAllStoreItemsWithTestData: Error getting items: {ex.Message}");
                // Return test items even if Firebase fails (useful for offline testing)
                return InitializeTestItems();
            }
        }

        // Check if the current user can afford a specific item
        public async Task<bool> CanAffordItem(string itemId)
        {
            string currentUserId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return false;
            }

            try
            {
                var itemDoc = await db.Collection(STORE_COLLECTION).Document(itemId).GetSnapshotAsync();
                if (!itemDoc.Exists)
                {
                    return false;
                }

                var item = itemDoc.ConvertTo<StoreItem>();
                if (!item.available)
                {
                    return false;
                }

                var userService = new UserService();
                int userCoins = await userService.GetUserCoins(currentUserId);
                int userKb = await userService.GetUserKnowledgePoints(currentUserId);

                return userCoins >= item.priceCoins && userKb >= item.priceKb;
            }
            catch (Exception ex)
            {
                Debug.LogError($"StoreService.CanAffordItem: Error checking affordability: {ex.Message}");
                return false;
            }
        }

        // Check if the current user can afford a specific item (overload for StoreItem object)

    }
}

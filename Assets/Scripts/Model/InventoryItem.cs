using System;
using Firebase.Firestore;

namespace RamRoutes.Model
{
    [FirestoreData]
    [Serializable]
    public class InventoryItem
    {
        [FirestoreProperty]
        public string inventoryId { get; set; }
        
        [FirestoreProperty]
        public string userId { get; set; }
        
        [FirestoreProperty]
        public string itemId { get; set; }
        
        [FirestoreProperty]
        public string itemName { get; set; }
        
        [FirestoreProperty]
        public string description { get; set; }
        
        [FirestoreProperty]
        public string category { get; set; }
        
        [FirestoreProperty]
        public string imageUrl { get; set; }
        
        [FirestoreProperty]
        public int pricePaidCoins { get; set; }
        
        [FirestoreProperty]
        public int pricePaidKb { get; set; }
        
        [FirestoreProperty]
        public Timestamp purchaseDate { get; set; }
        
        [FirestoreProperty]
        public bool equipped { get; set; } = false;

        public InventoryItem()
        {
        }

        public InventoryItem(string userId, string itemId, string itemName, string description, 
            int pricePaidCoins, int pricePaidKb, string category = "general", string imageUrl = "")
        {
            this.userId = userId;
            this.itemId = itemId;
            this.itemName = itemName;
            this.description = description;
            this.pricePaidCoins = pricePaidCoins;
            this.pricePaidKb = pricePaidKb;
            this.category = category;
            this.imageUrl = imageUrl;
            this.purchaseDate = Timestamp.GetCurrentTimestamp();
            this.equipped = false;
        }
    }
}

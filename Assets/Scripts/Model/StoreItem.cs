using System;
using Firebase.Firestore;

namespace RamRoutes.Model
{
    [FirestoreData]
    [Serializable]
    public class StoreItem
    {
        [FirestoreProperty]
        public string itemId { get; set; }
        
        [FirestoreProperty]
        public string name { get; set; }
        
        [FirestoreProperty]
        public string description { get; set; }
        
        [FirestoreProperty]
        public int priceCoins { get; set; }
        
        [FirestoreProperty]
        public int priceKb { get; set; }
        
        [FirestoreProperty]
        public string category { get; set; }
        
        [FirestoreProperty]
        public string imageUrl { get; set; }
        
        [FirestoreProperty]
        public bool available { get; set; } = true;
        
        [FirestoreProperty]
        public string whisperType { get; set; } = "0";
        
        [FirestoreProperty]
        public Firebase.Firestore.Timestamp createdAt { get; set; }

        public StoreItem()
        {
        }

        public StoreItem(string itemId, string name, string description, int priceCoins, int priceKb, string category = "general", string imageUrl = "")
        {
            this.itemId = itemId;
            this.name = name;
            this.description = description;
            this.priceCoins = priceCoins;
            this.priceKb = priceKb;
            this.category = category;
            this.imageUrl = imageUrl;
            this.available = true;
        }
    }
}

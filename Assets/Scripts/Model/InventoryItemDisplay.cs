using System;
using System.Collections.Generic;

namespace RamRoutes.Model
{
    /// <summary>
    /// Represents a grouped inventory item for display purposes with count
    /// </summary>
    [Serializable]
    public class InventoryItemDisplay
    {
        public string itemId { get; set; }
        public string itemName { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public string imageUrl { get; set; }
        public int pricePaidCoins { get; set; }
        public int pricePaidKb { get; set; }
        public bool hasEquippedItem { get; set; } = false;
        public int quantity { get; set; } = 0;
        
        // List of individual inventory IDs for this item (for operations like equip/sell)
        public List<string> inventoryIds { get; set; } = new List<string>();
        
        // First equipped item ID (if any)
        public string equippedInventoryId { get; set; }

        public InventoryItemDisplay()
        {
        }

        public InventoryItemDisplay(string itemId, string itemName, string description, 
            string category = "general", string imageUrl = "")
        {
            this.itemId = itemId;
            this.itemName = itemName;
            this.description = description;
            this.category = category;
            this.imageUrl = imageUrl;
        }
    }
}

using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.Shops
{
    public class ShopItem
    {
        private InventoryItem m_item;
        private int m_availableStock;
        private int m_price;
        private int m_quantityToBuy;

        public Sprite Icon => m_item.Icon;
        public string Name => m_item.Name;
        public int AvailableStock => m_availableStock;
        public int Price => m_price;
        public int QuantityToBuy => m_quantityToBuy;

        public InventoryItem Item => m_item;

        /*----------------------------------------------------------------------
        | --- ShopItem: Constructor to initialize a shop m_item with details --- |
        ----------------------------------------------------------------------*/
        public ShopItem(InventoryItem item, int availableStock, int price, int quantityToBuy)
        {
            this.m_item = item;
            this.m_availableStock = availableStock;
            this.m_price = price;
            this.m_quantityToBuy = quantityToBuy;
        }
    }
}
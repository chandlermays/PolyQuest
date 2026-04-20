/*---------------------------
File: ShopConfig.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.Shops
{
    [CreateAssetMenu(menuName = "PolyQuest/Shops/New Shop Config", fileName = "ShopConfig")]
    public class ShopConfig : ScriptableObject
    {
        [Range(0.0f, 100.0f)]
        [SerializeField] private float m_sellPricePercentage = 50.0f;
        [SerializeField] private ShopItemEntry[] m_shopInventory;

        public float SellPricePercentage => m_sellPricePercentage;
        public ShopItemEntry[] ShopInventory => m_shopInventory;

        [System.Serializable]
        public class ShopItemEntry
        {
            [SerializeField] private InventoryItem m_item;
            [SerializeField] private int m_initialStock;

            public InventoryItem Item => m_item;
            public int InitialStock => m_initialStock;
        }
    }
}
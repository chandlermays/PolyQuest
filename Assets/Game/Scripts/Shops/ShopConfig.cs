/*---------------------------
| File: ShopConfig.cs
| Author: Chandler Mays
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

#if UNITY_EDITOR
        /*-----------------------------------------------------------------------------------------
        | --- OnValidate: Editor warning for an entry with no item, which the shop leaves out --- |
        -----------------------------------------------------------------------------------------*/
        private void OnValidate()
        {
            if (m_shopInventory == null)
                return;

            for (int i = 0; i < m_shopInventory.Length; ++i)
            {
                if (m_shopInventory[i] == null || m_shopInventory[i].Item == null)
                    Debug.LogWarning($"ShopConfig '{name}': entry #{i} has no item, so the shop leaves it out. Assign an item or remove the entry.", this);
            }
        }
#endif
    }
}
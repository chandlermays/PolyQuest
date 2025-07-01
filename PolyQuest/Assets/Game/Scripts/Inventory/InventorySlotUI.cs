using UnityEngine;

namespace PolyQuest
{
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private InventoryItemIcon m_itemIcon;

        private int m_index;
        private Inventory m_inventory;

        public InventoryItem GetItem() => m_inventory.GetItemInSlot(m_index);
        public int GetQuantity() => m_inventory.GetQuantityInSlot(m_index);

        /*---------------------------------------------------------------------------
        | --- Setup: Initialize the InventorySlotUI with an Inventory and index --- |
        ---------------------------------------------------------------------------*/
        public void Setup(Inventory inventory, int index)
        {
            m_inventory = inventory;
            m_index = index;
            m_itemIcon.SetItem(m_inventory.GetItemInSlot(m_index));
        }

        /*----------------------------------------------------------------------------------
        | --- MaxQuantity: Determine the maximum quantity of an item that can be added --- |
        ----------------------------------------------------------------------------------*/
        public int MaxQuantity(InventoryItem item)
        {
            if (m_inventory.HasSpaceFor(item))
            {
                return int.MaxValue;
            }
            return 0;
        }

        /*-----------------------------------------------------------------------------
        | --- AddItems: Add a specified quantity of an item to the inventory slot --- |
        -----------------------------------------------------------------------------*/
        public void AddItems(InventoryItem item, int quantity)
        {
            m_inventory.AddItemToSlot(m_index, item, quantity);
        }

        /*-------------------------------------------------------------------------------------
        | --- RemoveItems: Remove a specified quantity of an item from the inventory slot --- |
        -------------------------------------------------------------------------------------*/
        public void RemoveItems(int quantity)
        {
            m_inventory.RemoveItemFromSlot(m_index, quantity);
        }
    }
}
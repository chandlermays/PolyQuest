using UnityEngine;
//---------------------------------
using PolyQuest.UI.Dragging;

namespace PolyQuest.Inventories
{
    /* --------------------------------------------------------------------------------------------
     * Role: Represents a single inventory slot in the UI, displaying item and quantity.           *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Displays the item icon and quantity for its assigned inventory slot.                 *
     *      - Handles adding and removing items from its slot.                                     *
     *      - Supports drag-and-drop operations for inventory items.                               *
     *      - Updates its display when the underlying inventory data changes.                      *
     * ------------------------------------------------------------------------------------------- */
    public class InventorySlotUI : MonoBehaviour, IItemHolder, IDragContainer<InventoryItem>
    {
        [SerializeField] private InventoryItemIcon m_itemIcon;

        private int m_index;
        private Inventory m_inventory;

        public InventoryItem GetItem() => m_inventory.GetItemAtSlot(m_index);
        public int GetQuantity() => m_inventory.GetQuantityAtSlot(m_index);

        /*---------------------------------------------------------------------------
        | --- Setup: InitializeDecorationArray the InventorySlotUI with an Inventory and index --- |
        ---------------------------------------------------------------------------*/
        public void Setup(Inventory inventory, int index)
        {
            m_inventory = inventory;
            m_index = index;
            m_itemIcon.SetItem(m_inventory.GetItemAtSlot(m_index), m_inventory.GetQuantityAtSlot(index));
        }

        /*-------------------------------------------------------------------------------------
        | --- GetMaxQuantity: Determine the maximum quantity of an item that can be added --- |
        -------------------------------------------------------------------------------------*/
        public int GetMaxQuantity(InventoryItem item)
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
            m_inventory.TryAddItemToSlot(m_index, item, quantity);
        }

        /*-------------------------------------------------------------------------------------
        | --- RemoveItems: Remove a specified quantity of an item from the inventory slot --- |
        -------------------------------------------------------------------------------------*/
        public void RemoveItems(int quantity)
        {
            m_inventory.RemoveItemsFromSlot(m_index, quantity);
        }

        /*--------------------------------------------------------------------------------------
        | --- GetMaxItemsCapacity: Determine the maximum number of items that can be added --- |
        --------------------------------------------------------------------------------------*/
        public int GetMaxItemsCapacity(InventoryItem item)
        {
            if (m_inventory.HasSpaceFor(item))
            {
                return int.MaxValue;
            }
            return 0;
        }
    }
}
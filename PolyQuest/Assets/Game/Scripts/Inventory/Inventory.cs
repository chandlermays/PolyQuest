using System;
using System.Collections.Generic;
using UnityEngine;
using PolyQuest.Saving;

namespace PolyQuest
{
    public class Inventory : MonoBehaviour, ISaveable
    {
        private struct InventorySlot
        {
            public InventoryItem m_item;
            public int m_quantity;
        }

        [System.Serializable]
        private struct InventorySlotRecord
        {
            public string m_itemID;
            public int m_quantity;
        }

        [SerializeField] private GameObject m_player;
        [SerializeField] private int m_inventorySize = 20;
        public Inventory GetPlayerInventory() => m_player.GetComponent<Inventory>();
        private InventorySlot[] m_inventorySlots;

        public int GetSize() => m_inventorySlots.Length;
        private Dictionary<string, int> m_stackableItemSlots = new();
        private int m_firstEmptySlot = 0;       // Keep track of the first empty slot
        private int FindEmptySlot() => m_firstEmptySlot;

        public event Action OnInventoryChanged;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_inventorySlots = new InventorySlot[m_inventorySize];
            UpdateFirstEmptySlot();
        }

        /*----------------------------------------------------------------------------
        | --- HasSpaceFor: Check if there is space for the item in the inventory --- |
        ----------------------------------------------------------------------------*/
        public bool HasSpaceFor(InventoryItem item)
        {
            return FindSlot(item) >= 0;
        }

        /*-----------------------------------------------------------------------------------------
        | --- HasSpaceFor: Check if there is space for a collection of items in the inventory --- |
        -----------------------------------------------------------------------------------------*/
        public bool HasSpaceFor(IEnumerable<InventoryItem> items)
        {
            int emptySlots = CountEmptySlots();
            HashSet<InventoryItem> stackedItems = new();

            foreach (InventoryItem item in items)
            {
                if (item.IsStackable() && !stackedItems.Add(item) && HasItem(item))
                    continue;

                if (emptySlots <= 0)
                    return false;

                --emptySlots;
            }

            return true;
        }

        /*----------------------------------------------------------------------------
        | --- AddItemToNextAvailableSlot: Add an item to the next available slot --- |
        ----------------------------------------------------------------------------*/
        public bool AddItemToNextAvailableSlot(InventoryItem item, int quantity)
        {
            if (quantity <= 0)
                return true;
            
            int i = FindSlot(item);

            if (i < 0)
                return false;

            m_inventorySlots[i].m_item = item;
            m_inventorySlots[i].m_quantity = quantity;
            OnInventoryChanged?.Invoke();

            return true;
        }

        /*------------------------------------------------------------------
        | --- HasItem: Check if the inventory contains a specific item --- |
        ------------------------------------------------------------------*/
        public bool HasItem(InventoryItem item)
        {
            for (int i = 0; i < m_inventorySlots.Length; ++i)
            {
                if (m_inventorySlots[i].m_item == item)
                {
                    return true;
                }
            }
            return false;
        }

        /*-----------------------------------------------------------------------
        | --- GetItemInSlot: Retrieve the item in a specific inventory slot --- |
        -----------------------------------------------------------------------*/
        public InventoryItem GetItemInSlot(int slot)
        {
            return m_inventorySlots[slot].m_item;
        }

        /*--------------------------------------------------------------------------------
        | --- GetQuantityInSlot: Retrieve the quantity of an item in a specific slot --- |
        --------------------------------------------------------------------------------*/
        public int GetQuantityInSlot(int slot)
        {
            return m_inventorySlots[slot].m_quantity;
        }

        /*------------------------------------------------------------------------
        | --- AddItemToSlot: Add an item to a specific slot in the inventory --- |
        ------------------------------------------------------------------------*/
        public bool AddItemToSlot(int slot, InventoryItem item, int quantity)
        {
            Debug.Log("Adding " + quantity + " item(s) to slot " + slot);

            if (m_inventorySlots[slot].m_item != null)
            {
                return AddItemToNextAvailableSlot(item, quantity);
            }

            var i = FindStack(item);
            if (i >= 0)
            {
                slot = i;
            }

            m_inventorySlots[slot].m_item = item;
            m_inventorySlots[slot].m_quantity += quantity;
            UpdateFirstEmptySlot();
            UpdateStackableItemSlot(item, slot);
            OnInventoryChanged?.Invoke();

            return true;
        }

        /*--------------------------------------------------------------------------------
        | --- RemoveItemFromSlot: Remove a specified quantity of an item from a slot --- |
        --------------------------------------------------------------------------------*/
        public void RemoveItemFromSlot(int slot, int quantity)
        {
            Debug.Log("Removing " + quantity + " item(s) from slot " + slot);

            m_inventorySlots[slot].m_quantity -= quantity;
            if (m_inventorySlots[slot].m_quantity <= 0)
            {
                m_inventorySlots[slot].m_quantity = 0;
                m_inventorySlots[slot].m_item = null;
            }
            UpdateFirstEmptySlot();
            UpdateStackableItemSlot(m_inventorySlots[slot].m_item, slot);
            OnInventoryChanged?.Invoke();
        }

        /*---------------------------------------------------------------------------
        | --- CountEmptySlots: Count the number of empty slots in the inventory --- |
        ---------------------------------------------------------------------------*/
        public int CountEmptySlots()
        {
            int count = 0;
            for (int i = 0; i < m_inventorySlots.Length; ++i)
            {
                if (m_inventorySlots[i].m_quantity == 0)
                {
                    ++count;
                }
            }
            return count;
        }

        /*-----------------------------------------------------------------------------
        | --- CaptureState: Capture the current state of the inventory for saving --- |
        -----------------------------------------------------------------------------*/
        public object CaptureState()
        {
            InventorySlotRecord[] slotRecords = new InventorySlotRecord[m_inventorySize];
            for (int i = 0; i < m_inventorySize; ++i)
            {
                if (m_inventorySlots[i].m_item != null)
                {
                    slotRecords[i].m_itemID = m_inventorySlots[i].m_item.GetID();
                    slotRecords[i].m_quantity = m_inventorySlots[i].m_quantity;
                }
            }
            return slotRecords;
        }

        /*-----------------------------------------------------------------------------
        | --- RestoreState: Restore the inventory state from a saved state object --- |
        -----------------------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            InventorySlotRecord[] slotRecords = (InventorySlotRecord[])state;
            for (int i = 0; i < m_inventorySize; ++i)
            {
                m_inventorySlots[i].m_item = InventoryItem.GetByID(slotRecords[i].m_itemID);
                m_inventorySlots[i].m_quantity = slotRecords[i].m_quantity;
            }
            UpdateFirstEmptySlot();

            m_stackableItemSlots.Clear();
            for (int i = 0; i < m_inventorySize; ++i)
            {
                var item = m_inventorySlots[i].m_item;
                if (item != null && item.IsStackable() && m_inventorySlots[i].m_quantity > 0)
                {
                    m_stackableItemSlots[item.GetID()] = i;
                }
            }

            OnInventoryChanged?.Invoke();
        }

        /*-----------------------------------------------------------------------------------------
        | --- UpdateFirstEmptySlot: Update the index of the first empty slot in the inventory --- |
        -----------------------------------------------------------------------------------------*/
        private void UpdateFirstEmptySlot()
        {
            for (int i = 0; i < m_inventorySlots.Length; ++i)
            {
                if (m_inventorySlots[i].m_item == null)
                {
                    m_firstEmptySlot = i;
                    return;
                }
            }
            m_firstEmptySlot = -1;
        }

        /*----------------------------------------------------------------------------------------
        | --- UpdateStackableItemSlot: Update the slot for a stackable item in the inventory --- |
        ----------------------------------------------------------------------------------------*/
        private void UpdateStackableItemSlot(InventoryItem item, int slot)
        {
            if (item.IsStackable())
            {
                if (m_inventorySlots[slot].m_item == item && m_inventorySlots[slot].m_quantity > 0)
                {
                    m_stackableItemSlots[item.GetID()] = slot;
                }
                else
                {
                    m_stackableItemSlots.Remove(item.GetID());
                }
            }
        }

        /*------------------------------------------------------------------------------
        | --- FindSlot: Find the first available slot for an item in the inventory --- |
        ------------------------------------------------------------------------------*/
        private int FindSlot(InventoryItem item)
        {
            int i = FindStack(item);
            if (i < 0)
            {
                i = FindEmptySlot();
            }
            return i;
        }

        /*--------------------------------------------------------------------------------
        | --- FindStack: Find the first stackable item in the inventory that matches --- |
        --------------------------------------------------------------------------------*/
        private int FindStack(InventoryItem item)
        {
            if (!item.IsStackable()) return -1;
            return m_stackableItemSlots.TryGetValue(item.GetID(), out int slot) ? slot : -1;
        }
    }
}
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;
using PolyQuest.Tools;

namespace PolyQuest.Inventories
{
    /* --------------------------------------------------------------------------------------------
     * Role: Manages the player's collection of items in the game.                                 *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores and organizes items and their quantities.                                     *
     *      - Handles adding, removing, and querying items.                                        *
     *      - Manages inventory slot availability and stackable items.                             *
     *      - Provides save/load functionality for inventory slots.                                *
     *      - Notifies listeners when the inventory changes.                                       *
     * ------------------------------------------------------------------------------------------- */
    public class Inventory : MonoBehaviour, ISaveable, IConditionChecker, IJsonSaveable
    {
        private struct InventorySlot
        {
            public InventoryItem m_item;
            public int m_quantity;
        }

        [System.Serializable]
        private struct InventorySlotData
        {
            [SerializeField] private string m_itemID;
            [SerializeField] private int m_quantity;

            public string ItemID
            { readonly get => m_itemID; set => m_itemID = value; }

            public int Quantity
            { readonly get => m_quantity; set => m_quantity = value; }
        }

        [SerializeField] private int m_inventorySize = 20;
        public int Size => m_inventorySlots.Length;
        private InventorySlot[] m_inventorySlots;       
                                                        
        // NOTE: I'm still uncertain whether it's necessary to have an array AND a dictionary for the inventory.
        // I'm concerned that a complication may arise at some point which causes a disconnect between the two containers.

        private readonly Dictionary<InventoryItem, List<int>> m_itemSlotMapping = new();
        private readonly SortedSet<int> m_emptySlots = new();

        public event Action OnInventoryChanged;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_inventorySlots = new InventorySlot[m_inventorySize];
            InitializeEmptySlots();
        }

        /*---------------------------------------------------------------------------
        | --- InitializeEmptySlots: InitializeDecorationArray the empty slots in the inventory --- |
        ---------------------------------------------------------------------------*/
        private void InitializeEmptySlots()
        {
            m_emptySlots.Clear();
            for (int i = 0; i < m_inventorySize; i++)
            {
                m_emptySlots.Add(i);
            }
        }

        /*----------------------------------------------------------------
        | --- HasSpaceFor: Check if there is space for a single item --- |
        ----------------------------------------------------------------*/
        public bool HasSpaceFor(InventoryItem item)
        {
            if (item.IsStackable && m_itemSlotMapping.ContainsKey(item))
            {
                return true;
            }

            return m_emptySlots.Count > 0;
        }

        /*-----------------------------------------------------------------
        | --- HasSpaceFor: Check if there is space for multiple items --- |
        -----------------------------------------------------------------*/
        public bool HasSpaceFor(IEnumerable<InventoryItem> items)
        {
            int requiredSlots = 0;
            HashSet<InventoryItem> processedStackableItems = new();

            foreach (InventoryItem item in items)
            {
                if (item.IsStackable)
                {
                    if (processedStackableItems.Contains(item) || m_itemSlotMapping.ContainsKey(item))
                        continue;

                    processedStackableItems.Add(item);
                }
                ++requiredSlots;
            }

            return m_emptySlots.Count >= requiredSlots;
        }

        /*------------------------------------------------------------------------
        | --- TryAddToAvailableSlot: Try to add an item to an available slot --- |
        ------------------------------------------------------------------------*/
        public bool TryAddToAvailableSlot(InventoryItem item, int quantity = 1)
        {
            int slotIndex = FindSlotForItem(item);
            if (slotIndex < 0)
                return false;

            AddItemToSlotInternal(slotIndex, item, quantity);
            return true;
        }

        /*-----------------------------------------------------------------------
        | --- ContainsItem: Check if the inventory contains a specific item --- |
        -----------------------------------------------------------------------*/
        public bool ContainsItem(InventoryItem item)
        {
            return m_itemSlotMapping.ContainsKey(item);
        }

        /*-------------------------------------------------------------
        | --- GetItemAtSlot: Retrieve the item at a specific slot --- |
        -------------------------------------------------------------*/
        public InventoryItem GetItemAtSlot(int slot)
        {
            return m_inventorySlots[slot].m_item;
        }

        /*------------------------------------------------------------------------------
        | --- GetQuantityAtSlot: Retrieve the quantity of items at a specific slot --- |
        ------------------------------------------------------------------------------*/
        public int GetQuantityAtSlot(int slot)
        {
            return m_inventorySlots[slot].m_quantity;
        }

        /*-----------------------------------------------------------------
        | --- TryAddItemToSlot: Try to add an item to a specific slot --- |
        -----------------------------------------------------------------*/
        public bool TryAddItemToSlot(int slot, InventoryItem item, int quantity)
        {
            if (m_inventorySlots[slot].m_item != null)
            {
                return TryAddToAvailableSlot(item, quantity);
            }

            if (item.IsStackable && m_itemSlotMapping.ContainsKey(item))
            {
                var existingSlots = m_itemSlotMapping[item];
                if (existingSlots.Count > 0)
                {
                    slot = existingSlots[0];
                }
            }

            AddItemToSlotInternal(slot, item, quantity);
            return true;
        }

        /*------------------------------------------------------------------------------
        | --- RemoveItemsFromSlot: Remove a specific quantity of items from a slot --- |
        ------------------------------------------------------------------------------*/
        public void RemoveItemsFromSlot(int slot, int quantity)
        {
            var currentItem = m_inventorySlots[slot].m_item;
            if (currentItem == null)
                return;

            m_inventorySlots[slot].m_quantity -= quantity;

            if (m_inventorySlots[slot].m_quantity <= 0)
            {
                RemoveItemFromSlotInternal(slot);
            }

            OnInventoryChanged?.Invoke();
        }

        /*--------------------------------------------------------------
        | --- GetEmptySlotCount: Retrieve the count of empty slots --- |
        --------------------------------------------------------------*/
        public int GetEmptySlotCount()
        {
            return m_emptySlots.Count;
        }

        /*--------------------------------------------------------------------
        | --- CaptureState: Capture the current inventory state (Saving) --- |
        --------------------------------------------------------------------*/
        public object CaptureState()
        {
            var slotRecords = new InventorySlotData[m_inventorySize];

            for (int i = 0; i < m_inventorySize; ++i)
            {
                if (m_inventorySlots[i].m_item != null)
                {
                    slotRecords[i].ItemID = m_inventorySlots[i].m_item.ID;
                    slotRecords[i].Quantity = m_inventorySlots[i].m_quantity;
                }
            }

            return slotRecords;
        }

        /*------------------------------------------------------------------------
        | --- RestoreState: Restore the inventory from saved state (Loading) --- |
        ------------------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            // Clear existing mappings
            m_itemSlotMapping.Clear();
            m_emptySlots.Clear();

            var slotRecords = (InventorySlotData[])state;

            for (int i = 0; i < m_inventorySize; ++i)
            {
                var item = InventoryItem.FindByID(slotRecords[i].ItemID);
                var quantity = slotRecords[i].Quantity;

                m_inventorySlots[i].m_item = item;
                m_inventorySlots[i].m_quantity = quantity;

                if (item != null && quantity > 0)
                {
                    AddItemToMapping(item, i);
                }
                else
                {
                    m_emptySlots.Add(i);
                }
            }

            OnInventoryChanged?.Invoke();
        }

        /*---------------------------------------------------------------------
        | --- Evaluate: Check if the inventory meets a specific condition --- |
        ---------------------------------------------------------------------*/
        public bool? Evaluate(PredicateType predicate, string[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return null;

            switch (predicate)
            {
                // single item
                case PredicateType.kHasItem:
                    return ContainsItem(InventoryItem.FindByID(parameters[0]));

                // stackable items only
                case PredicateType.kHasItems:
                    InventoryItem item = InventoryItem.FindByID(parameters[0]);
                    if (item == null || !item.IsStackable)
                        return false;

                    if (!int.TryParse(parameters[1], out int requiredQuantity) || requiredQuantity <= 0)
                        return false;

                    // If the item is not in the mapping, inventory has none.
                    if (!m_itemSlotMapping.TryGetValue(item, out var slots) || slots == null || slots.Count == 0)
                        return false;

                    int totalQuantity = 0;
                    foreach (int slotIndex in slots)
                    {
                        // Defensive: ensure slot index is valid.
                        if (slotIndex < 0 || slotIndex >= m_inventorySlots.Length)
                            continue;

                        totalQuantity += m_inventorySlots[slotIndex].m_quantity;
                        if (totalQuantity >= requiredQuantity)
                            return true;
                    }

                    return false;

                default:
                    break;
            }

            return null;
        }

        /*-----------------------------------------------------------------------
        | --- AddItemToSlotInternal: Helper method to add an item to a slot --- |
        -----------------------------------------------------------------------*/
        private void AddItemToSlotInternal(int slot, InventoryItem item, int quantity)
        {
            bool wasEmpty = m_inventorySlots[slot].m_item == null;

            m_inventorySlots[slot].m_item = item;
            m_inventorySlots[slot].m_quantity += quantity;

            if (wasEmpty)
            {
                m_emptySlots.Remove(slot);
                AddItemToMapping(item, slot);
            }

            OnInventoryChanged?.Invoke();
        }

        /*---------------------------------------------------------------------------------
        | --- RemoveItemFromSlotInternal: Helper method to remove an item from a slot --- |
        ---------------------------------------------------------------------------------*/
        private void RemoveItemFromSlotInternal(int slot)
        {
            var item = m_inventorySlots[slot].m_item;

            m_inventorySlots[slot].m_quantity = 0;
            m_inventorySlots[slot].m_item = null;

            m_emptySlots.Add(slot);
            RemoveItemFromMapping(item, slot);
        }

        /*------------------------------------------------------------------------
        | --- AddItemToMapping: Add an item to the mapping of items to slots --- |
        ------------------------------------------------------------------------*/
        private void AddItemToMapping(InventoryItem item, int slot)
        {
            if (!m_itemSlotMapping.ContainsKey(item))
            {
                m_itemSlotMapping[item] = new List<int>();
            }
            m_itemSlotMapping[item].Add(slot);
        }

        /*----------------------------------------------------------------------------------
        | --- RemoveItemFromMapping: Remove an item from the mapping of items to slots --- |
        ----------------------------------------------------------------------------------*/
        private void RemoveItemFromMapping(InventoryItem item, int slot)
        {
            if (m_itemSlotMapping.ContainsKey(item))
            {
                m_itemSlotMapping[item].Remove(slot);
                if (m_itemSlotMapping[item].Count == 0)
                {
                    m_itemSlotMapping.Remove(item);
                }
            }
        }

        /*----------------------------------------------------------------
        | --- FindSlotForItem: Find the appropriate slot for an item --- |
        ----------------------------------------------------------------*/
        private int FindSlotForItem(InventoryItem item)
        {
            if (item.IsStackable && m_itemSlotMapping.ContainsKey(item))
            {
                return m_itemSlotMapping[item][0];
            }

            return FindFirstAvailableSlot();
        }

        /*---------------------------------------------------------------------
        | --- FindFirstAvailableSlot: Find the first available empty slot --- |
        ---------------------------------------------------------------------*/
        private int FindFirstAvailableSlot()
        {
            if (m_emptySlots.Count == 0)
                return -1;

            foreach (int slot in m_emptySlots)
            {
                return slot;
            }

            return -1;
        }

        public JToken CaptureJToken()
        {
            JObject state = new();
            IDictionary<string, JToken> stateDict = state;

            for (int i = 0; i < m_inventorySize; ++i)
            {
                if (m_inventorySlots[i].m_item != null)
                {
                    JObject itemState = new();
                    IDictionary<string, JToken> itemStateDict = itemState;
                    itemState["item"] = JToken.FromObject(m_inventorySlots[i].m_item.ID);
                    itemState["quantity"] = JToken.FromObject(m_inventorySlots[i].m_quantity);
                    stateDict[i.ToString()] = itemState;
                }
            }
            return state;
        }

        public void RestoreJToken(JToken state)
        {
            if (state is JObject stateObject)
            {
                m_inventorySlots = new InventorySlot[m_inventorySize];
                IDictionary<string, JToken> stateDict = stateObject;

                for (int i = 0; i < m_inventorySize; ++i)
                {
                    if (stateDict.ContainsKey(i.ToString()) && stateDict[i.ToString()] is JObject itemState)
                    {
                        IDictionary<string, JToken> itemStateDict = itemState;
                        m_inventorySlots[i].m_item = InventoryItem.FindByID(itemStateDict["item"].ToObject<string>());
                        m_inventorySlots[i].m_quantity = itemStateDict["quantity"].ToObject<int>();
                    }
                }
                OnInventoryChanged?.Invoke();
            }
        }
    }
}
/*---------------------------
| File: Inventory.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;
using PolyQuest.Tools;

namespace PolyQuest.Inventories
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Manages the player's collection of items in the game.                                 *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores and organizes items and their quantities.                                     *
     *      - Handles adding, removing, and querying items.                                        *
     *      - Manages inventory slot availability and stackable items.                             *
     *      - Provides save/load functionality for inventory slots.                                *
     *      - Notifies listeners when the inventory changes.                                       *
     * ------------------------------------------------------------------------------------------- */
    public class Inventory : MonoBehaviour, ISaveable, IConditionChecker
    {
        private struct InventorySlot
        {
            public InventoryItem m_item;
            public int m_quantity;
        }

        [SerializeField] private int m_inventorySize = 20;
        private InventorySlot[] m_inventorySlots;

        private readonly Dictionary<InventoryItem, List<int>> m_itemSlotMapping = new();
        private readonly SortedSet<int> m_emptySlots = new();

        public int Size => m_inventorySlots.Length;

        public event Action OnInventoryChanged;
        public event Action<InventoryItem, int> OnItemAdded;
        public Func<InventoryItem, int, bool> OnBeforeAddItem;

        public bool SuppressItemAddedNotif { get; set; } = false;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_inventorySlots = new InventorySlot[m_inventorySize];
            InitializeEmptySlots();
        }

        /*---------------------------------------------------------------------------
        | --- InitializeEmptySlots: Initialize the empty slots in the inventory --- |
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
            // Adding nothing adds nothing: no slot is taken and no event is raised
            if (quantity <= 0)
                return false;

            if (item.IsStackable && OnBeforeAddItem != null && OnBeforeAddItem(item, quantity))
            {
                if (!SuppressItemAddedNotif)
                {
                    OnItemAdded?.Invoke(item, quantity);
                }

                return true;
            }

            if (!item.IsStackable)
                return TryAddNonStackable(-1, item, quantity);

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
            // Adding nothing adds nothing: no slot is taken and no event is raised
            if (quantity <= 0)
                return false;

            // A different item is there, or a copy of a non-stackable that can't share its slot: use the first available slot
            InventoryItem current = m_inventorySlots[slot].m_item;
            if (current != null && (current != item || !item.IsStackable))
            {
                return TryAddToAvailableSlot(item, quantity);
            }

            if (!item.IsStackable)
            {
                return TryAddNonStackable(slot, item, quantity);
            }

            // The same stackable item stacks there; in an empty slot it starts a new stack (drag-and-drop rearranging)
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

        /*------------------------------------------------------------------
        | --- CaptureState: Capture the current state of the inventory --- |
        ------------------------------------------------------------------*/
        public JToken CaptureState()
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

        /*---------------------------------------------------------------------------------------------
        | --- RestoreState: Restores the saved slots; a slot that can't be restored is left empty --- |
        ---------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            // The whole save is the wrong shape: ignore it, keep what's there, and raise nothing
            if (state is not JObject stateObject)
                return;

            m_inventorySlots = new InventorySlot[m_inventorySize];
            m_itemSlotMapping.Clear();
            InitializeEmptySlots();

            for (int i = 0; i < m_inventorySize; ++i)
            {
                JToken entry = stateObject[i.ToString()];
                if (entry == null)
                    continue;

                // One bad slot is skipped and stays empty; the other slots load
                if (!TryReadEntry(entry, out string itemID, out int quantity))
                {
                    WarnSkippedSlot(i, entry.ToString(Formatting.None), "the entry can't be read");
                    continue;
                }

                InventoryItem item = InventoryItem.FindByID(itemID);
                if (item == null)
                {
                    WarnSkippedSlot(i, itemID, "no item has that ID");
                    continue;
                }

                PlaceInSlot(i, item, quantity);
            }

            OnInventoryChanged?.Invoke();
        }

        /*-------------------------------------------------------------------------------------------------
        | --- TryReadEntry: Reads a saved slot; false unless it has an item ID and a quantity above 0 --- |
        -------------------------------------------------------------------------------------------------*/
        private static bool TryReadEntry(JToken entry, out string itemID, out int quantity)
        {
            itemID = null;
            quantity = 0;
            if (entry is not JObject entryObject)
                return false;

            JToken itemToken = entryObject["item"];
            JToken quantityToken = entryObject["quantity"];
            if (itemToken == null || itemToken.Type != JTokenType.String || quantityToken == null || quantityToken.Type != JTokenType.Integer)
                return false;

            itemID = itemToken.Value<string>();
            quantity = quantityToken.Value<int>();
            return !string.IsNullOrWhiteSpace(itemID) && quantity > 0;
        }

        /*------------------------------------------------------------------------------------------------
        | --- WarnSkippedSlot: Editor warning naming a saved slot and the item ID that wasn't loaded --- |
        ------------------------------------------------------------------------------------------------*/
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void WarnSkippedSlot(int slot, string itemID, string reason)
        {
            Debug.LogWarning($"Inventory on '{name}': skipped saved slot {slot} with item ID '{itemID}' because {reason}. The other slots loaded.", this);
        }

        /*--------------------------------------------------------------------------------------------------------
        | --- Evaluate: Check if the inventory meets a condition; unknown items or quantities are unanswered --- |
        --------------------------------------------------------------------------------------------------------*/
        public bool? Evaluate(ConditionType predicate, string[] parameters)
        {
            if (parameters == null || parameters.Length == 0 || string.IsNullOrWhiteSpace(parameters[0]))
                return null;

            switch (predicate)
            {
                // single item
                case ConditionType.kHasItem:
                    {
                        // An ID no item has can't be answered; the condition then fails closed
                        InventoryItem item = InventoryItem.FindByID(parameters[0]);
                        if (item == null)
                            return null;

                        return ContainsItem(item);
                    }

                // stackable items only
                case ConditionType.kHasItems:
                    {
                        InventoryItem item = InventoryItem.FindByID(parameters[0]);
                        if (item == null)
                            return null;

                        // A missing, unreadable, zero or negative quantity can't be answered either
                        if (parameters.Length < 2 || !int.TryParse(parameters[1], out int requiredQuantity) || requiredQuantity <= 0)
                            return null;

                        if (!item.IsStackable)
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
                    }

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
            PlaceInSlot(slot, item, quantity);
            NotifyAdded(item, quantity);
        }

        /*------------------------------------------------------------------------------------------------------
        | --- TryAddNonStackable: Each copy takes its own slot; without room for every copy, none is added --- |
        ------------------------------------------------------------------------------------------------------*/
        private bool TryAddNonStackable(int preferredSlot, InventoryItem item, int quantity)
        {
            if (m_emptySlots.Count < quantity)
                return false;

            int remaining = quantity;
            if (preferredSlot >= 0 && m_inventorySlots[preferredSlot].m_item == null)
            {
                PlaceInSlot(preferredSlot, item, 1);
                --remaining;
            }

            for (; remaining > 0; --remaining)
            {
                PlaceInSlot(FindFirstAvailableSlot(), item, 1);
            }

            NotifyAdded(item, quantity);
            return true;
        }

        /*------------------------------------------------------------------------------
        | --- PlaceInSlot: Puts items in a slot and updates the lookups, no events --- |
        ------------------------------------------------------------------------------*/
        private void PlaceInSlot(int slot, InventoryItem item, int quantity)
        {
            bool wasEmpty = m_inventorySlots[slot].m_item == null;

            m_inventorySlots[slot].m_item = item;
            m_inventorySlots[slot].m_quantity += quantity;

            if (wasEmpty)
            {
                m_emptySlots.Remove(slot);
                AddItemToMapping(item, slot);
            }
        }

        /*-----------------------------------------------------------------------------------------
        | --- NotifyAdded: Raises OnItemAdded (unless suppressed) and OnInventoryChanged once --- |
        -----------------------------------------------------------------------------------------*/
        private void NotifyAdded(InventoryItem item, int quantity)
        {
            if (!SuppressItemAddedNotif)
            {
                OnItemAdded?.Invoke(item, quantity);
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
    }
}
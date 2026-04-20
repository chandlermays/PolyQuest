/*---------------------------
File: Actions.cs
Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Inventories
{
    [RequireComponent(typeof(Inventory))]
    public class Actions : MonoBehaviour, ISaveable
    {
        private class ActionSlot
        {
            private ActionItem m_item;
            private int m_quantity;

            public ActionItem Item => m_item;
            public int Quantity => m_quantity;

            public ActionSlot(ActionItem item, int quantity)
            {
                m_item = item;
                m_quantity = quantity;
            }

            public void AddQuantity(int amount) => m_quantity += amount;
            public void SubtractQuantity(int amount) => m_quantity -= amount;
        }

        [System.Serializable]
        private class ActionSlotData
        {
            private string m_itemID;
            private int m_quantity;

            public string ItemID => m_itemID;
            public int Quantity => m_quantity;

            public ActionSlotData(string itemID, int quantity)
            {
                m_itemID = itemID;
                m_quantity = quantity;
            }
        }

        private readonly Dictionary<int, ActionSlot> m_actionSlots = new();
        private Inventory m_inventory;

        public event Action OnActionTabUpdated;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_inventory = GetComponent<Inventory>();
            Utilities.CheckForNull(m_inventory, nameof(m_inventory));
            m_inventory.OnBeforeAddItem += TryStackInActionSlot;
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            if (m_inventory != null)
            {
                m_inventory.OnBeforeAddItem -= TryStackInActionSlot;
            }
        }

        /*-----------------------------------------------------------------------------------
        | --- TryStackInActionSlot: Stack a stackable item into an existing action slot --- |
        -----------------------------------------------------------------------------------*/
        private bool TryStackInActionSlot(InventoryItem item, int quantity)
        {
            var actionItem = item as ActionItem;
            if (actionItem == null)
                return false;

            foreach (var pair in m_actionSlots)
            {
                if (ReferenceEquals(pair.Value.Item, actionItem))
                {
                    pair.Value.AddQuantity(quantity);
                    OnActionTabUpdated?.Invoke();
                    return true;
                }
            }

            return false;
        }

        /*-----------------------------------------------------------------------------
        | --- GetActionItem: Retrieve the item in the specified action slot index --- |
        -----------------------------------------------------------------------------*/
        public ActionItem GetActionItem(int index)
        {
            if (m_actionSlots.TryGetValue(index, out var slot))
            {
                return slot.Item;
            }
            return null;
        }

        /*-------------------------------------------------------------------------------------
        | --- Quantity: Retrieve the quantity of items in the specified action slot index --- |
        -------------------------------------------------------------------------------------*/
        public int GetQuantity(int index)
        {
            if (m_actionSlots.TryGetValue(index, out var slot))
            {
                return slot.Quantity;
            }
            return 0;
        }

        /*-----------------------------------------------------------------------
        | --- AddActionItem: Add an item to the specified action slot index --- |
        -----------------------------------------------------------------------*/
        public void AddActionItem(InventoryItem item, int index, int quantity)
        {
            var actionItem = item as ActionItem;
            if (actionItem == null) return;

            if (m_actionSlots.TryGetValue(index, out var slot))
            {
                if (ReferenceEquals(actionItem, slot.Item))
                {
                    slot.AddQuantity(quantity);
                }
            }
            else
            {
                m_actionSlots[index] = new ActionSlot(actionItem, quantity);
            }

            OnActionTabUpdated?.Invoke();
        }

        /*------------------------------------------------------------------------------------------------
        | --- RemoveItems: Remove a specified quantity of items from the specified action slot index --- |
        ------------------------------------------------------------------------------------------------*/
        public void RemoveItems(int index, int quantity)
        {
            if (m_actionSlots.TryGetValue(index, out var slot))
            {
                slot.SubtractQuantity(quantity);

                if (slot.Quantity <= 0)
                {
                    m_actionSlots.Remove(index);
                }

                OnActionTabUpdated?.Invoke();
            }
        }

        /*------------------------------------------------------------------------------------------------------------
        | --- MaxAcceptable: Determine the maximum number of items acceptable in the specified action slot index --- |
        ------------------------------------------------------------------------------------------------------------*/
        public int MaxAcceptable(InventoryItem item, int index)
        {
            var actionItem = item as ActionItem;
            if (actionItem == null)
                return 0;

            if (m_actionSlots.TryGetValue(index, out var slot) && !ReferenceEquals(actionItem, slot.Item))
                return 0;

            if (actionItem.IsConsumable)
                return int.MaxValue;

            if (m_actionSlots.ContainsKey(index))
                return 0;

            return 1;
        }

        /*--------------------------------------------------------
        | --- Use: Use the item in the specified action slot --- |
        --------------------------------------------------------*/
        public bool Use(GameObject user, int index)
        {
            if (m_actionSlots.TryGetValue(index, out var slot))
            {
                bool used = slot.Item.Use(user);
                if (used && slot.Item.IsConsumable)
                {
                    RemoveItems(index, 1);
                }
                return true;
            }
            return false;
        }

        /*-----------------------------------------------------------------
        | --- CaptureState: Capture the current state of action slots --- |
        -----------------------------------------------------------------*/
        public JToken CaptureState()
        {
            JObject state = new();

            foreach (var pair in m_actionSlots)
            {
                ActionSlotData slotData = new(pair.Value.Item.ID, pair.Value.Quantity);
                state[pair.Key.ToString()] = JToken.FromObject(slotData);
            }

            return state;
        }

        /*---------------------------------------------------------------------
        | --- RestoreState: Restore the action slots from the given state --- |
        ---------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state is JObject stateObject)
            {
                m_actionSlots.Clear();

                foreach (var pair in stateObject)
                {
                    int key = int.Parse(pair.Key);
                    ActionSlotData slotData = pair.Value.ToObject<ActionSlotData>();

                    InventoryItem item = InventoryItem.FindByID(slotData.ItemID);
                    AddActionItem(item, key, slotData.Quantity);
                }
            }
        }
    }
}
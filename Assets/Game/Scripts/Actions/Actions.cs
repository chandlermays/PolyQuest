/*---------------------------
| File: Actions.cs
| Author: Chandler Mays
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

            // An ability (anything not consumable) lives in one slot only: placing it again moves it here
            if (!actionItem.IsConsumable)
            {
                foreach (int otherIndex in new List<int>(m_actionSlots.Keys))
                {
                    if (otherIndex != index && ReferenceEquals(m_actionSlots[otherIndex].Item, actionItem))
                    {
                        m_actionSlots.Remove(otherIndex);
                    }
                }
            }

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
                ActionItem item = slot.Item;

                // A consumable is used up when the use actually happens (after any targeting), never if it's cancelled
                item.Use(user, () =>
                {
                    if (item.IsConsumable && ReferenceEquals(GetActionItem(index), item))
                    {
                        RemoveItems(index, 1);
                    }
                });
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

                // A slot that can't be read, or holds an unknown item, is skipped and the rest load
                foreach (var pair in stateObject)
                {
                    if (!int.TryParse(pair.Key, out int key))
                        continue;

                    if (pair.Value is not JObject slotObject)
                        continue;

                    JToken itemID = slotObject["ItemID"];
                    JToken quantity = slotObject["Quantity"];
                    if (itemID?.Type != JTokenType.String || quantity?.Type != JTokenType.Integer)
                        continue;

                    int amount = quantity.Value<int>();
                    if (amount <= 0)
                        continue;

                    InventoryItem item = InventoryItem.FindByID(itemID.Value<string>());
                    if (item == null)
                        continue;

                    AddActionItem(item, key, amount);
                }
            }
        }
    }
}
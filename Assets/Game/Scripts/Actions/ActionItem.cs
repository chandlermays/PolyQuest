/*---------------------------
| File: ActionItem.cs
| Author: Chandler Mays
----------------------------*/
using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Inventories
{
    [CreateAssetMenu(menuName = "PolyQuest/Items/Action Item", fileName = "New Action Item")]
    public class ActionItem : InventoryItem
    {
        // NOTE: InventoryItem -> ActionItem vs. Ability

        [SerializeField] private bool m_isConsumable = false;
        public bool IsConsumable => m_isConsumable;

        /*-----------------------------------------------------------------
        | --- Use: Defines the action performed when the item is used --- |
        -----------------------------------------------------------------*/
        public bool Use(GameObject user)
        {
            return Use(user, null);
        }

        /*-----------------------------------------------------------------------------------------------------------------
        | --- Use: Starts using the item; onUsed runs once the use actually happens (never if targeting is cancelled) --- |
        -----------------------------------------------------------------------------------------------------------------*/
        public virtual bool Use(GameObject user, Action onUsed)
        {
            onUsed?.Invoke();
            return true;
        }

        /*------------------------------------------------------------------------------------------
        | --- OnUse: Uses the item from the inventory; a consumable is used up once it is used --- |
        ------------------------------------------------------------------------------------------*/
        public override void OnUse(Inventory inventory, int slotIndex)
        {
            Use(inventory.gameObject, () =>
            {
                if (!m_isConsumable)
                    return;

                // The player may have moved the item while targeting; only take it from the slot it was used from
                if (inventory.GetItemAtSlot(slotIndex) == this)
                {
                    inventory.RemoveItemsFromSlot(slotIndex, 1);
                }
            });
        }
    }
}
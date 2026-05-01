/*---------------------------
File: ActionItem.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.Inventories
{
    [CreateAssetMenu(menuName = "PolyQuest/Items/Action Item", fileName = "New Action Item")]
    public class ActionItem : InventoryItem
    {
        // NOTE: Not sure if I even need this subclass since consumables are treated like abilities.
        // This used to be a design-choice where ActionItems would be consumables, but I found that
        // configuring them as abilities worked better with the strategy pattern that I built.

        // April 20th: Same concern as noted in Cooldowns.cs with InventoryItem vs. ActionItem

        [SerializeField] private bool m_isConsumable = false;
        public bool IsConsumable => m_isConsumable;

        /*-----------------------------------------------------------------
        | --- Use: Defines the action performed when the item is used --- |
        -----------------------------------------------------------------*/
        public virtual bool Use(GameObject user)
        {
            return true;
        }

        public override void OnUse(Inventory inventory, int slotIndex)
        {
            if (Use(inventory.gameObject))
            {
                inventory.RemoveItemsFromSlot(slotIndex, 1);
            }
        }
    }
}
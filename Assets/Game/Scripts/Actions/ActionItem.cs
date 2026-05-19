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
        // NOTE: InventoryItem -> ActionItem vs. Ability

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
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

        [SerializeField] private bool m_isConsumable = false;
        public bool IsConsumable => m_isConsumable;

        /*-----------------------------------------------------------------
        | --- Use: Defines the action performed when the item is used --- |
        -----------------------------------------------------------------*/
        public virtual bool Use(GameObject user)
        {
            return true;
        }
    }
}
using UnityEngine;
//---------------------------------

namespace PolyQuest.Inventories
{
    [CreateAssetMenu(fileName = "New Action Item", menuName = "PolyQuest/Items/Action Item", order = 0)]
    public class ActionItem : InventoryItem
    {
        [SerializeField] private bool m_isConsumable = false;
        public bool IsConsumable => m_isConsumable;

        /*-----------------------------------------------------------------
        | --- Use: Defines the action performed when the item is used --- |
        -----------------------------------------------------------------*/
        public virtual bool Use(GameObject user)
        {
            //...
            return true;
        }
    }
}
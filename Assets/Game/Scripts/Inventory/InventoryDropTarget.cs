using UnityEngine;
//---------------------------------
using PolyQuest.UI.Dragging;

namespace PolyQuest.Inventories
{
    /* --------------------------------------------------------------------------------------------
     * Role: Acts as a drop destination for inventory items, handling their removal from the UI    *
     *       and spawning them into the game world as pickups.                                     *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Receives items dropped from the inventory UI via drag-and-drop.                      *
     *      - Uses the ItemDropper component to spawn dropped items into the world.                *
     *      - Determines the maximum number of items that can be dropped.                          *
     * ------------------------------------------------------------------------------------------- */
    public class InventoryDropTarget : MonoBehaviour, IDragDestination<InventoryItem>
    {
        [SerializeField] private GameObject m_player;

        private ItemDropper m_itemDropper;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_player, nameof(m_player));

            m_itemDropper = m_player.GetComponent<ItemDropper>();
            Utilities.CheckForNull(m_itemDropper, nameof(m_itemDropper));
        }

        /*---------------------------------------------------------------------------------------------
        | --- AddItems: Drops the specified m_item(s) into the world, "adding" to the "destination" --- |
        ---------------------------------------------------------------------------------------------*/
        public void AddItems(InventoryItem item, int quantity)
        {
            m_itemDropper.DropItem(item, quantity);
        }

        /*--------------------------------------------------------------------------------------
        | --- GetMaxItemsCapacity: Determine the maximum number of items that can be added --- |
        --------------------------------------------------------------------------------------*/
        public int GetMaxItemsCapacity(InventoryItem item)
        {
            return int.MaxValue;
        }
    }
}
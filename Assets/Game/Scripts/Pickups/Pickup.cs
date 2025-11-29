using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.Pickups
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Represents the actual collectible item in the game world.                             *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Holds data about the item and quantity.                                              *
     *      - Handles player interaction (i.e. adding the item to the inventory when picked up).   *
     *      - Contains logic for whether it can be picked up and what happens on pickup.           *
     * ------------------------------------------------------------------------------------------- */
    public class Pickup : MonoBehaviour
    {
        private Inventory m_inventory;
        private InventoryItem m_item;
        private int m_quantity = 1;

        public InventoryItem Item => m_item;
        public int Quantity => m_quantity;

        private const string kPlayerTag = "Player";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            // NOTE:
            // Don't use Find(). Retrieve the Player by other means.
            // I'm doing this just because the Pickup is instantiated upon Play
            // so the Inventory cannot be assigned in the Inspector. Fix this asap.
            var player = GameObject.FindWithTag(kPlayerTag);
            m_inventory = player?.GetComponent<Inventory>();
            Utilities.CheckForNull(m_inventory, nameof(m_inventory));
        }

        /*----------------------------------------------------------------
        | --- Setup: InitializeDecorationArray the Pickup with an item and quantity --- |
        ----------------------------------------------------------------*/
        public void Setup(InventoryItem item, int quantity)
        {
            m_item = item;
            if (!item.IsStackable)
            {
                m_quantity = 1;
            }
            m_quantity = quantity;
        }

        /*--------------------------------------------------------------------------
        | --- PickupItem: Add the item to the inventory and destroy the Pickup --- |
        --------------------------------------------------------------------------*/
        public void PickupItem()
        {
            bool slotAvailable = m_inventory.TryAddToAvailableSlot(m_item, m_quantity);
            if (slotAvailable)
            {
                Destroy(gameObject);
            }
        }

        /*---------------------------------------------------------------------------------------------
        | --- CanBePickedUp: Check if there's space in the inventory for the item to be picked up --- |
        ---------------------------------------------------------------------------------------------*/
        public bool CanBePickedUp()
        {
            if (m_inventory == null)
            {
                Debug.Log("Pickup: Inventory is not assigned.");
            }
            return m_inventory.HasSpaceFor(m_item);
        }
    }
}
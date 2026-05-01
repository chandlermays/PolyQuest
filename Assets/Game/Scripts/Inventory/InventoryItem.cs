/*---------------------------
File: InventoryItem.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Pickups;

namespace PolyQuest.Inventories
{
    /* --------------------------------------------------------------------------------------------
     * Role: Represents a data asset for an item that can be stored in the player's inventory.     *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores item data such as ID, name, description, icon, stackability, and category.    *
     *      - Provides access to item data for inventory and UI systems.                           *
     *      - Supports item lookup by ID and item instantiation as pickups in the world.           *
     *      - Ensures each item has a unique identifier for saving/loading and referencing.        *
     * ------------------------------------------------------------------------------------------- */
    public abstract class InventoryItem : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private string m_itemID;
        [SerializeField] private string m_itemName;
        [SerializeField][TextArea] private string m_itemDescription;
        [SerializeField] private Sprite m_itemIcon;
        [SerializeField] private Pickup m_pickup;
        [SerializeField] private bool m_isStackable = false;
        [SerializeField] private ItemCategory m_category = ItemCategory.kNone;
        [SerializeField] private int m_itemPrice;

        private static Dictionary<string, InventoryItem> m_itemRegistry;

        public string ID                    => m_itemID;                // Get the m_item ID
        public string Name                  => m_itemName;              // Get the m_item name
        public string Description           => m_itemDescription;       // Get the m_item description
        public Sprite Icon                  => m_itemIcon;              // Get the m_item icon
        public bool IsStackable             => m_isStackable;           // Check if the m_item is stackable
        public ItemCategory Category        => m_category;              // Get the m_item category
        public int Price                    => m_itemPrice;             // Get the m_item price

        /*-------------------------------------------------------------------------
        | --- FindByID: Retrieve an InventoryItem by its ID from the registry --- |
        -------------------------------------------------------------------------*/
        public static InventoryItem FindByID(string itemID)
        {
            if (m_itemRegistry == null)
            {
                m_itemRegistry = new Dictionary<string, InventoryItem>();
                InventoryItem[] items = Resources.LoadAll<InventoryItem>("");

                foreach (InventoryItem item in items)
                {
                    if (!m_itemRegistry.ContainsKey(item.m_itemID))
                    {
                        m_itemRegistry[item.m_itemID] = item;
                    }
                }
            }

            if (string.IsNullOrEmpty(itemID) || !m_itemRegistry.ContainsKey(itemID))
                return null;

            return m_itemRegistry[itemID];
        }

        /*-----------------------------------------------------------------------------
        | --- SpawnPickup: Create a new Pickup instance at the specified position --- |
        -----------------------------------------------------------------------------*/
        public Pickup SpawnPickup(Vector3 position, int quantity)
        {
            Pickup pickup = Instantiate(m_pickup);
            pickup.transform.position = position;
            pickup.Setup(this, quantity);
            return pickup;
        }

        public virtual void OnPickup(Inventory inventory, int quantity)
        {
            inventory.TryAddToAvailableSlot(this, quantity);
        }

        public virtual void OnUse(Inventory inventory, int slotIndex) { }

        /*----------------------------------------------------------------------------------
        | --- OnBeforeSerialize: Receive a callback before Unity serializes the object --- |
        ----------------------------------------------------------------------------------*/
        public void OnBeforeSerialize()
        {
            // Generate a new unique ID if it's empty/blank
            if (string.IsNullOrWhiteSpace(m_itemID))
            {
                m_itemID = System.Guid.NewGuid().ToString();
            }
        }

        /*------------------------------------------------------------------------------------
        | --- OnAfterDeserialize: Receive a callback after Unity deserializes the object --- |
        ------------------------------------------------------------------------------------*/
        public void OnAfterDeserialize()
        {
            // Leaving blank due to required interface implementation; no logic is needed here
        }
    }
}
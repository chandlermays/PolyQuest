using System.Collections.Generic;
using UnityEngine;

namespace PolyQuest
{
    [CreateAssetMenu(fileName = "New Item", menuName = "PolyQuest/Item", order = 0)]
    public class InventoryItem : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private string m_itemID;
        [SerializeField] private string m_itemName;
        [SerializeField][TextArea] private string m_itemDescription;
        [SerializeField] private Sprite m_itemIcon;
        [SerializeField] private bool m_isStackable = false;
        [SerializeField] private ItemCategory m_category = ItemCategory.kNone;

        static Dictionary<string, InventoryItem> m_itemRegistry;

        public string GetID() => m_itemID;                          // Get the item ID
        public string GetName() => m_itemName;                      // Get the item name
        public string GetDescription() => m_itemDescription;        // Get the item description
        public Sprite GetIcon() => m_itemIcon;                      // Get the item icon
        public bool IsStackable() => m_isStackable;                 // Check if the item is stackable
        public ItemCategory GetCategory() => m_category;            // Get the item category

        /*------------------------------------------------------------------------
        | --- GetByID: Retrieve an InventoryItem by its ID from the registry --- |
        ------------------------------------------------------------------------*/
        public static InventoryItem GetByID(string itemID)
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
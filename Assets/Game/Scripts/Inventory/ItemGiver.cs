using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Inventories
{
    public class ItemGiver : MonoBehaviour, ISaveable
    {
        [SerializeField] private InventoryItem m_item;
        [SerializeField] private int m_quantity = 1;
        [SerializeField] private bool m_giveOnlyOnce = true;    // could be used if the player removed the item and needs to get it again?

        private const string kPlayerTag = "Player";

        private Inventory m_playerInventory;
        private ItemDropper m_playerItemDropper;
        private bool m_hasGivenItem = false;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_item, nameof(m_item));

            GameObject player = GameObject.FindWithTag(kPlayerTag);
            Utilities.CheckForNull(player, nameof(player));

            m_playerInventory = player.GetComponent<Inventory>();
            Utilities.CheckForNull(m_playerInventory, nameof(m_playerInventory));

            m_playerItemDropper = player.GetComponent<ItemDropper>();
            Utilities.CheckForNull(m_playerItemDropper, nameof(m_playerItemDropper));
        }

        /*-----------------------------------------------------------
        | --- GiveItem: Gives the configured item to the player --- |
        -----------------------------------------------------------*/
        public void GiveItem()
        {
            if (m_item == null)
                return;

            if (m_giveOnlyOnce && m_hasGivenItem)
                return;

            int quantityToGive = Mathf.Max(1, m_quantity);

            if (m_item.IsStackable)
            {
                GiveStackableItem(quantityToGive);
            }
            else
            {
                GiveNonStackableItems(quantityToGive);
            }

            m_hasGivenItem = true;
            SaveManager.Instance.Save();
        }

        /*-------------------------------------------------------------------
        | --- CaptureState: Capture whether this NPC has given the item --- |
        -------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            return JToken.FromObject(m_hasGivenItem);
        }

        /*-------------------------------------------------------------------
        | --- RestoreState: Restore whether this NPC has given the item --- |
        -------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state != null)
            {
                m_hasGivenItem = state.ToObject<bool>();
            }
        }

        /*-------------------------------------------------------------
        | --- GiveStackableItem: Gives a stackable item to player --- |
        -------------------------------------------------------------*/
        private void GiveStackableItem(int quantity)
        {
            bool success = m_playerInventory.TryAddToAvailableSlot(m_item, quantity);
            if (!success)
            {
                m_playerItemDropper.DropItem(m_item, quantity);
            }
        }

        /*-----------------------------------------------------------------
        | --- GiveNonStackableItems: Gives non-stackable items safely --- |
        -----------------------------------------------------------------*/
        private void GiveNonStackableItems(int quantity)
        {
            for (int i = 0; i < quantity; ++i)
            {
                bool success = m_playerInventory.TryAddToAvailableSlot(m_item, 1);
                if (!success)
                {
                    m_playerItemDropper.DropItem(m_item);
                }
            }
        }
    }
}
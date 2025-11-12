using UnityEngine;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.UI.Dragging;
using PolyQuest.Abilities;

namespace PolyQuest.Inventories
{
    public class ActionSlotUI : MonoBehaviour, IItemHolder, IDragContainer<InventoryItem>
    {
        [SerializeField] private InventoryItemIcon m_itemIcon;
        [SerializeField] private int m_index = 0;
        [SerializeField] private Actions m_playerActions;
        [SerializeField] private Cooldowns m_playerCooldowns;
        [SerializeField] private Image m_cooldownOverlay;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_itemIcon, nameof(m_itemIcon));
            Utilities.CheckForNull(m_playerActions, nameof(m_playerActions));
            Utilities.CheckForNull(m_playerCooldowns, nameof(m_playerCooldowns));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_playerActions.OnActionTabUpdated += UpdateIcon;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_playerActions.OnActionTabUpdated -= UpdateIcon;
        }

        /*----------------------------------------- 
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            m_cooldownOverlay.fillAmount = m_playerCooldowns.GetRemainingPercentage(GetItem());
        }

        /*------------------------------------------------
        | --- AddItems: Add items to the action slot --- |
        ------------------------------------------------*/
        public void AddItems(InventoryItem item, int quantity)
        {
            m_playerActions.AddActionItem(item, m_index, quantity);
        }

        /*--------------------------------------------------------
        | --- GetItem: Retrieve the item in this action slot --- |
        --------------------------------------------------------*/
        public InventoryItem GetItem()
        {
            return m_playerActions.GetActionItem(m_index);
        }

        /*-------------------------------------------------------------------------
        | --- GetQuantity: Retrieve the quantity of items in this action slot --- |
        -------------------------------------------------------------------------*/
        public int GetQuantity()
        {
            return m_playerActions.GetQuantity(m_index);
        }

        /*----------------------------------------------------------------------------------------------------
        | --- GetMaxItemsCapacity: Retrieve the maximum capacity for a specific item in this action slot --- |
        ----------------------------------------------------------------------------------------------------*/
        public int GetMaxItemsCapacity(InventoryItem item)
        {
            return m_playerActions.MaxAcceptable(item, m_index);
        }

        /*---------------------------------------------------------------------------------
        | --- RemoveItems: Remove a specified quantity of items from this action slot --- |
        ---------------------------------------------------------------------------------*/
        public void RemoveItems(int quantity)
        {
            m_playerActions.RemoveItems(m_index, quantity);
        }

        /*-------------------------------------------------------------------------
        | --- UpdateIcon: Update the item icon and quantity display in the UI --- |
        -------------------------------------------------------------------------*/
        private void UpdateIcon()
        {
            m_itemIcon.SetItem(GetItem(), GetQuantity());
        }
    }
}
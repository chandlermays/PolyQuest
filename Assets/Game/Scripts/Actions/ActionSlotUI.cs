using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.Abilities;
using PolyQuest.Input;
using PolyQuest.UI.Dragging;

namespace PolyQuest.Inventories
{
    public class ActionSlotUI : MonoBehaviour, IItemHolder, IDragContainer<InventoryItem>, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private InventoryItemIcon m_itemIcon;
        [SerializeField] private int m_index = 0;
        [SerializeField] private GameObject m_player;
        [SerializeField] private Image m_cooldownOverlay;

        private Actions m_playerActions;
        private Cooldowns m_playerCooldowns;
        private Inventory m_playerInventory;

        private bool m_isCursorOver = false;
        private InputAction m_doubleClickAction;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_itemIcon, nameof(m_itemIcon));

            m_playerActions = m_player.GetComponent<Actions>();
            Utilities.CheckForNull(m_playerActions, nameof(m_playerActions));

            m_playerCooldowns = m_player.GetComponent<Cooldowns>();
            Utilities.CheckForNull(m_playerCooldowns, nameof(m_playerCooldowns));

            m_playerInventory = m_player.GetComponent<Inventory>();
            Utilities.CheckForNull(m_playerInventory, nameof(m_playerInventory));
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

            if (m_doubleClickAction != null)
            {
                m_doubleClickAction.performed -= OnDoubleClick;
            }
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_doubleClickAction = InputManager.Instance.InputActions.UI.DoubleClick;
            m_doubleClickAction.performed += OnDoubleClick;
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            m_cooldownOverlay.fillAmount = m_playerCooldowns.GetRemainingPercentage(GetItem());
        }

        /*-------------------------------------------------------------
        | --- OnDoubleClick: Handle the double-click input action --- |
        -------------------------------------------------------------*/
        private void OnDoubleClick(InputAction.CallbackContext context)
        {
            if (m_isCursorOver)
            {
                HandleDoubleClick();
            }
        }

        /*--------------------------------------------------------------------
        | --- OnPointerEnter: Called when the pointer enters this object --- |
        --------------------------------------------------------------------*/
        public void OnPointerEnter(PointerEventData eventData)
        {
            m_isCursorOver = true;
            Debug.Log("Cursor entered ActionSlotUI.");
        }

        /*-------------------------------------------------------------------
        | --- OnPointerEnter: Called when the pointer exits this object --- |
        -------------------------------------------------------------------*/
        public void OnPointerExit(PointerEventData eventData)
        {
            m_isCursorOver = false;
            Debug.Log("Cursor exited ActionSlotUI.");
        }

        /*------------------------------------------------------------------------------
        | --- HandleDoubleClick: Handle double-click actions on the inventory slot --- |
        ------------------------------------------------------------------------------*/
        private void HandleDoubleClick()
        {
            Debug.Log("Executed the HandleDoubleClick method.");

            if (GetItem() == null)
                return;

            m_playerActions.Use(m_playerInventory.gameObject, m_index);
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
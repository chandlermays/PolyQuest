using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
//---------------------------------
using PolyQuest.Input;
using PolyQuest.UI.Dragging;

namespace PolyQuest.Inventories
{
    public class EquipmentSlotUI : MonoBehaviour, IItemHolder, IDragContainer<InventoryItem>, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Equipment m_playerEquipment;
        [SerializeField] InventoryItemIcon m_itemIcon;
        [SerializeField] EquipmentSlot m_equipmentSlot = EquipmentSlot.kNone;
        [SerializeField] Inventory m_playerInventory;

        private bool m_isCursorOver = false;
        private InputAction m_doubleClickAction;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_playerEquipment, nameof(m_playerEquipment));
            Utilities.CheckForNull(m_itemIcon, nameof(m_itemIcon));
            Utilities.CheckForNull(m_playerInventory, nameof(m_playerInventory));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_playerEquipment.OnEquipmentChanged += RedrawUI;

            if (InputManager.Instance != null)
            {
                m_doubleClickAction = InputManager.Instance.InputActions.UI.DoubleClick;
                m_doubleClickAction.performed += OnDoubleClick;
            }
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            if (m_doubleClickAction != null)
            {
                m_doubleClickAction.performed -= OnDoubleClick;
            }
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            m_playerEquipment.OnEquipmentChanged -= RedrawUI;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            RedrawUI();
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

        /*-----------------------------------------------------
        | --- AddItems: Add an item to the equipment slot --- |
        -----------------------------------------------------*/
        public void AddItems(InventoryItem item, int quantity)
        {
            m_playerEquipment.AddItem(m_equipmentSlot, (EquipableItem)item);
        }

        /*------------------------------------------------------------
        | --- Item: Get the item currently in the equipment slot --- |
        ------------------------------------------------------------*/
        public InventoryItem GetItem()
        {
            return m_playerEquipment.GetItemInSlot(m_equipmentSlot);
        }

        /*---------------------------------------------------------------------------------------
        | --- GetMaxItemsCapacity: Get the max items that can be added to an equipment slot --- |
        ---------------------------------------------------------------------------------------*/
        public int GetMaxItemsCapacity(InventoryItem item)
        {
            EquipableItem equipableItem = item as EquipableItem;

            if (equipableItem == null)
                return 0;

            if (!equipableItem.CanEquip(m_equipmentSlot, m_playerEquipment))
                return 0;

            if (GetItem() != null)
                return 0;

            return 1;
        }

        /*----------------------------------------------------------------------
        | --- Quantity: Get the quantity of the item in the equipment slot --- |
        ----------------------------------------------------------------------*/
        public int GetQuantity()
        {
            if (GetItem() != null)
                return 1;

            else return 0;
        }

        /*--------------------------------------------------------------
        | --- RemoveItems: Remove the item from the equipment slot --- |
        --------------------------------------------------------------*/
        public void RemoveItems(int quantity)
        {
            m_playerEquipment.RemoveItem(m_equipmentSlot);
        }

        /*-------------------------------------------------------------
        | --- RedrawUI: Update the UI to reflect the current item --- |
        -------------------------------------------------------------*/
        private void RedrawUI()
        {
            m_itemIcon.SetItem(m_playerEquipment.GetItemInSlot(m_equipmentSlot));
        }

        /*--------------------------------------------------------------------
        | --- OnPointerEnter: Called when the pointer enters this object --- |
        --------------------------------------------------------------------*/
        public void OnPointerEnter(PointerEventData eventData)
        {
            m_isCursorOver = true;
        }

        /*-------------------------------------------------------------------
        | --- OnPointerEnter: Called when the pointer exits this object --- |
        -------------------------------------------------------------------*/
        public void OnPointerExit(PointerEventData eventData)
        {
            m_isCursorOver = false;
        }

        /*----------------------------------------------------------------------
        | --- HandleDoubleClick: Equip or unequip the item on double-click --- |
        ----------------------------------------------------------------------*/
        private void HandleDoubleClick()
        {
            EquipableItem equippedItem = GetItem() as EquipableItem;
            if (equippedItem == null)
                return;

            if (m_playerInventory.TryAddToAvailableSlot(equippedItem, 1))
            {
                m_playerEquipment.RemoveItem(m_equipmentSlot);
            }
        }
    }
}
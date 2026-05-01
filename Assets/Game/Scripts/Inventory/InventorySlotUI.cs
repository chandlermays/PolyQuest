/*---------------------------
File: InventorySlotUI.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
//---------------------------------
using PolyQuest.Input;
using PolyQuest.UI.Dragging;

namespace PolyQuest.Inventories
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Represents a single inventory slot in the UI, displaying item and quantity.           *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Displays the item icon and quantity for its assigned inventory slot.                 *
     *      - Handles adding and removing items from its slot.                                     *
     *      - Supports drag-and-drop operations for inventory items.                               *
     *      - Updates its display when the underlying inventory data changes.                      *
     * ------------------------------------------------------------------------------------------- */
    public class InventorySlotUI : MonoBehaviour, IItemHolder, IDragContainer<InventoryItem>, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private InventoryItemIcon m_itemIcon;

        private int m_index;
        private Inventory m_playerInventory;
        private bool m_isCursorOver = false;
        private InputAction m_doubleClickAction;

        public InventoryItem GetItem() => m_playerInventory.GetItemAtSlot(m_index);
        public int GetQuantity() => m_playerInventory.GetQuantityAtSlot(m_index);

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
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

        /*---------------------------------------------------------------------------
        | --- Setup: Initialize the InventorySlotUI with an Inventory and index --- |
        ---------------------------------------------------------------------------*/
        public void Setup(Inventory inventory, int index)
        {
            m_playerInventory = inventory;
            m_index = index;
            m_itemIcon.SetItem(m_playerInventory.GetItemAtSlot(m_index), m_playerInventory.GetQuantityAtSlot(index));
        }

        /*-------------------------------------------------------------------------------------
        | --- GetMaxQuantity: Determine the maximum quantity of an item that can be added --- |
        -------------------------------------------------------------------------------------*/
        public int GetMaxQuantity(InventoryItem item)
        {
            if (m_playerInventory.HasSpaceFor(item))         
            {
                return int.MaxValue;
            }
            return 0;
        }

        /*-----------------------------------------------------------------------------
        | --- AddItems: Add a specified quantity of an item to the inventory slot --- |
        -----------------------------------------------------------------------------*/
        public void AddItems(InventoryItem item, int quantity)
        {
            m_playerInventory.SuppressItemAddedNotif = true;
            m_playerInventory.TryAddItemToSlot(m_index, item, quantity);
            m_playerInventory.SuppressItemAddedNotif = false;
        }

        /*-------------------------------------------------------------------------------------
        | --- RemoveItems: Remove a specified quantity of an item from the inventory slot --- |
        -------------------------------------------------------------------------------------*/
        public void RemoveItems(int quantity)
        {
            m_playerInventory.RemoveItemsFromSlot(m_index, quantity);
        }

        /*--------------------------------------------------------------------------------------
        | --- GetMaxItemsCapacity: Determine the maximum number of items that can be added --- |
        --------------------------------------------------------------------------------------*/
        public int GetMaxItemsCapacity(InventoryItem item)
        {
            if (m_playerInventory.HasSpaceFor(item))
            {
                return int.MaxValue;
            }
            return 0;
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

        /*------------------------------------------------------------------------------
        | --- HandleDoubleClick: Handle double-click actions on the inventory slot --- |
        ------------------------------------------------------------------------------*/
        private void HandleDoubleClick()
        {
            InventoryItem item = GetItem();
            item?.OnUse(m_playerInventory, m_index);
        }
    }
}
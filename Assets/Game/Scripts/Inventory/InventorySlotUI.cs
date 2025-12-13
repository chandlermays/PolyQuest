using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
//---------------------------------
using PolyQuest.UI.Dragging;
using PolyQuest.UI.Core;
using PolyQuest.Input;
using UnityEngine.SceneManagement;

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
    public class InventorySlotUI : MonoBehaviour, IItemHolder, IDragContainer<InventoryItem>, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private InventoryItemIcon m_itemIcon;

        private int m_index;
        private Inventory m_playerInventory;
        private Equipment m_playerEquipment;
        private bool m_isCursorOver = false;
        private InputAction m_doubleClickAction;
        private InputAction m_splitModifierAction;
        private int m_splitQuantity = 0;
        private ItemSplitDialog m_cachedSplitDialog;

        public InventoryItem GetItem() => m_playerInventory.GetItemAtSlot(m_index);
        public int GetQuantity() => m_playerInventory.GetQuantityAtSlot(m_index);
        public int GetDragQuantityOverride() => m_splitQuantity;

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                m_doubleClickAction = InputManager.Instance.InputActions.UI.DoubleClick;
                m_doubleClickAction.performed += OnDoubleClick;
                
                m_splitModifierAction = InputManager.Instance.InputActions.UI.SplitModifier;
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

            if (m_playerEquipment == null)
            {
                m_playerEquipment = m_playerInventory.GetComponent<Equipment>();
                Utilities.CheckForNull(m_playerEquipment, nameof(m_playerEquipment));
            }

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
            m_playerInventory.TryAddItemToSlot(m_index, item, quantity);
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

        /*--------------------------------------------------------------------
        | --- OnPointerClick: Called when the pointer clicks on object --- |
        --------------------------------------------------------------------*/
        public void OnPointerClick(PointerEventData eventData)
        {
            // Only handle left-click (button 0)
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // Check if split modifier is pressed when clicking
            if (m_splitModifierAction != null && m_splitModifierAction.IsPressed())
            {
                InventoryItem item = GetItem();
                int quantity = GetQuantity();

                // Only allow splitting stackable items with quantity > 1
                if (item != null && item.IsStackable && quantity > 1)
                {
                    ShowSplitDialog(quantity);
                }
            }
            else
            {
                // Reset split state for normal clicks/drags
                ResetSplitState();
            }
        }

        /*--------------------------------------------------------------------
        | --- ShowSplitDialog: Display dialog to get split quantity --- |
        --------------------------------------------------------------------*/
        private void ShowSplitDialog(int maxQuantity)
        {
            // Cache the dialog reference on first use
            if (m_cachedSplitDialog == null)
            {
                m_cachedSplitDialog = FindObjectOfType<ItemSplitDialog>();
                
                if (m_cachedSplitDialog == null)
                {
                    Debug.LogWarning("ItemSplitDialog not found in scene. Stack splitting will not work. Please add an ItemSplitDialog component to your UI.");
                    return;
                }
            }

            m_cachedSplitDialog.Show(
                maxQuantity - 1, // Max to split (leave at least 1 in source)
                OnSplitConfirmed,
                OnSplitCancelled
            );
        }

        /*--------------------------------------------------------------------
        | --- OnSplitConfirmed: Handle split quantity confirmation --- |
        --------------------------------------------------------------------*/
        private void OnSplitConfirmed(int quantity)
        {
            m_splitQuantity = quantity;
            // The split quantity is now set and will be used in the next drag operation
            // Note: Split state is reset at the start of next pointer interaction
        }

        /*--------------------------------------------------------------------
        | --- OnSplitCancelled: Handle split cancellation --- |
        --------------------------------------------------------------------*/
        private void OnSplitCancelled()
        {
            // Reset split state immediately when cancelled
            m_splitQuantity = 0;
        }

        /*--------------------------------------------------------------------
        | --- ResetSplitState: Reset split state to default --- |
        --------------------------------------------------------------------*/
        private void ResetSplitState()
        {
            m_splitQuantity = 0;
        }

        /*------------------------------------------------------------------------------
        | --- HandleDoubleClick: Handle double-click actions on the inventory slot --- |
        ------------------------------------------------------------------------------*/
        private void HandleDoubleClick()
        {
            InventoryItem item = GetItem();
            if (item == null)
                return;

            if (item.Category == ItemCategory.kConsumables)
            {
                if (item is ActionItem actionItem)
                {
                    bool used = actionItem.Use(m_playerInventory.gameObject);
                    if (used)
                    {
                        m_playerInventory.RemoveItemsFromSlot(m_index, 1);
                    }
                }
            }
            else if (item.Category == ItemCategory.kArmor || item.Category == ItemCategory.kWeapon)
            {
                if (item is EquipableItem equipableItem)
                {
                    EquipItem(equipableItem);
                }
            }
        }

        private void EquipItem(EquipableItem equipableItem)
        {
            EquipmentSlot targetSlot = equipableItem.TargetEquipmentSlot;
            if (targetSlot == EquipmentSlot.kNone)
                return;

            if (!equipableItem.CanEquip(targetSlot, m_playerEquipment))
                return;

            // Check if there's an item currently equipped in the target slot
            EquipableItem currentlyEquipped = m_playerEquipment.GetItemInSlot(targetSlot);
            if (currentlyEquipped != null)
            {
                // Remove the currently equipped item and add it back to the same inventory slot
                m_playerInventory.RemoveItemsFromSlot(m_index, 1);
                m_playerEquipment.AddItem(targetSlot, equipableItem);
                m_playerInventory.TryAddItemToSlot(m_index, currentlyEquipped, 1);
            }
            else
            {
                // Equip the new item directly
                m_playerInventory.RemoveItemsFromSlot(m_index, 1);
                m_playerEquipment.AddItem(targetSlot, equipableItem);
            }
        }
    }
}
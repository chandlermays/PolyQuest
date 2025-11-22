using UnityEngine;
//---------------------------------
using PolyQuest.UI.Dragging;

namespace PolyQuest.Inventories
{
    public class EquipmentSlotUI : MonoBehaviour, IItemHolder, IDragContainer<InventoryItem>
    {
        [SerializeField] Equipment m_playerEquipment;
        [SerializeField] InventoryItemIcon m_itemIcon;
        [SerializeField] EquipmentSlot m_equipmentSlot = EquipmentSlot.kNone;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_playerEquipment, nameof(m_playerEquipment));
            Utilities.CheckForNull(m_itemIcon, nameof(m_itemIcon));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_playerEquipment.OnEquipmentChanged += RedrawUI;
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
    }
}
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Inventories
{
    /* --------------------------------------------------------------------------------------------
     * Role: Manages the visual representation of the player's inventory in the UI.                *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Instantiates and organizes InventorySlotUI elements for each inventory slot.         *
     *      - Listens for inventory changes and updates the UI accordingly.                        *
     *      - Ensures the UI accurately reflects the current state of the inventory.               *
     * ------------------------------------------------------------------------------------------- */
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private InventorySlotUI m_slotPrefab;
        [SerializeField] private Inventory m_inventory;

        private readonly List<InventorySlotUI> m_slotUIs = new();

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_slotPrefab, nameof(m_slotPrefab));
            Utilities.CheckForNull(m_inventory, nameof(m_inventory));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_inventory.OnInventoryChanged += RedrawUI;
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            m_inventory.OnInventoryChanged -= RedrawUI;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            RedrawUI();
        }

        /*--------------------------------------------------------------------------------
        | --- RedrawUI: Redraw the inventory UI based on the current inventory state --- |
        --------------------------------------------------------------------------------*/
        private void RedrawUI()
        {
            int inventorySize = m_inventory.Size;

            // Create or reuse slot UIs as needed
            for (int i = 0; i < inventorySize; ++i)
            {
                InventorySlotUI slotUI;
                if (i < m_slotUIs.Count)
                {
                    slotUI = m_slotUIs[i];
                    slotUI.gameObject.SetActive(true);
                }
                else
                {
                    slotUI = Instantiate(m_slotPrefab, transform);
                    m_slotUIs.Add(slotUI);
                }
                slotUI.Setup(m_inventory, i);
            }

            // Hide any extra slot UIs
            for (int i = inventorySize; i < m_slotUIs.Count; ++i)
            {
                m_slotUIs[i].gameObject.SetActive(false);
            }
        }
    }
}
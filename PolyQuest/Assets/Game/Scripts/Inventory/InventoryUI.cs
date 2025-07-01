using System.Collections.Generic;
using UnityEngine;

namespace PolyQuest
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private InventorySlotUI m_inventoryItemPrefab;
        [SerializeField] private Inventory m_inventory;

        private readonly List<InventorySlotUI> m_slotUIs = new();

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_inventory.OnInventoryChanged += RedrawUI;
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
            int slotCount = m_inventory.GetSize();

            // Create new slots if needed
            while (m_slotUIs.Count < slotCount)
            {
                InventorySlotUI itemUI = Instantiate(m_inventoryItemPrefab, transform);
                m_slotUIs.Add(itemUI);
            }

            // Hide extra slots if inventory shrinks
            for (int i = 0; i < m_slotUIs.Count; ++i)
            {
                if (i < slotCount)
                {
                    m_slotUIs[i].gameObject.SetActive(true);
                    m_slotUIs[i].Setup(m_inventory, i);
                }
                else
                {
                    m_slotUIs[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
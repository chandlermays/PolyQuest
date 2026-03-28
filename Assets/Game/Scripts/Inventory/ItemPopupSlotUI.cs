using TMPro;
using UnityEngine;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.UI
{
    public class ItemPopupSlotUI : MonoBehaviour
    {
        [SerializeField] private Image m_itemIcon;
        [SerializeField] private TextMeshProUGUI m_itemName;
        [SerializeField] private TextMeshProUGUI m_itemQuantity;

        /*--------------------------------------------------------------------------
        | --- Setup: Updates the item popup with the provided item information --- |
        --------------------------------------------------------------------------*/
        public void Setup(InventoryItem item, int quantity)
        {
            m_itemIcon.sprite = item.Icon;
            m_itemName.text = item.Name;
            m_itemQuantity.text = $"x{quantity}";
        }
    }
}
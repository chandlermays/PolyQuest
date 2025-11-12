using PolyQuest.Inventories;
using UnityEngine;
using UnityEngine.UI;
//---------------------------------

namespace PolyQuest.Shops
{
    public class ShopFilterUI : MonoBehaviour
    {
        [SerializeField] private ItemCategory m_itemCategory;
        private Button m_button;
        private Shop m_currentShop;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_button = GetComponent<Button>();
        }

        /*---------------------------------------------------------------
        | --- SetShop: Sets the current shop for this filter button --- |
        ---------------------------------------------------------------*/
        public void SetShop(Shop currentShop)
        {
            m_currentShop = currentShop;
        }

        /*--------------------------------------------------------------
        | --- SelectFilter: Applies the filter to the current shop --- |
        --------------------------------------------------------------*/
        public void SelectFilter()
        {
            m_currentShop.SortByFilter(m_itemCategory);
        }

        /*------------------------------------------------------------
        | --- RefreshUI: Updates the button's interactable state --- |
        ------------------------------------------------------------*/
        public void RefreshUI()
        {
            m_button.interactable = m_currentShop.CurrentFilter != m_itemCategory;
        }
    }
}
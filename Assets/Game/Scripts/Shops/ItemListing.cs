using TMPro;
using UnityEngine;
using UnityEngine.UI;
//---------------------------------

namespace PolyQuest.Shops
{
    public class ItemListing : MonoBehaviour
    {
        [SerializeField] private Image m_itemIcon;
        [SerializeField] private TextMeshProUGUI m_itemName;
        [SerializeField] private TextMeshProUGUI m_availableStock;
        [SerializeField] private TextMeshProUGUI m_itemPrice;
        [SerializeField] private TextMeshProUGUI m_quantityToBuy;

        private Shop m_currentShop;
        private ShopItem m_shopItem;

        /*------------------------------------------------------------------------
        | --- Setup: Configures the m_item listing with the provided shop m_item --- |
        ------------------------------------------------------------------------*/
        public void Setup(Shop shop, ShopItem shopItem)
        {
            this.m_currentShop = shop;
            this.m_shopItem = shopItem;

            m_itemIcon.sprite = shopItem.Icon;
            m_itemName.text = shopItem.Name;
            m_availableStock.text = shopItem.AvailableStock.ToString();
            m_itemPrice.text = shopItem.Price.ToString();
            m_quantityToBuy.text = shopItem.QuantityToBuy.ToString();
        }

        /*----------------------------------------------------------------
        | --- Add: Adds one unit of the m_item to the current purchase --- |
        ----------------------------------------------------------------*/
        public void Add()
        {
            m_currentShop.AddToPurchase(m_shopItem.Item, 1);
        }

        /*------------------------------------------------------------------------
        | --- Remove: Removes one unit of the m_item from the current purchase --- |
        ------------------------------------------------------------------------*/
        public void Remove()
        {
            m_currentShop.AddToPurchase(m_shopItem.Item, -1);
        }
    }
}
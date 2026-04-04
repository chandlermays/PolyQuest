using TMPro;
using UnityEngine;
using UnityEngine.UI;
//---------------------------------

namespace PolyQuest.Shops
{
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] private ShopInteractor m_shopInteractor;
        [SerializeField] private TextMeshProUGUI m_shopNameText;
        [SerializeField] private Transform m_itemListingContainer;
        [SerializeField] private ItemListing m_itemListingPrefab;
        [SerializeField] private TextMeshProUGUI m_purchaseTotalText;
        [SerializeField] private Button m_purchaseButton;
        [SerializeField] private Button m_switchModeButton;

        private Shop m_currentShop;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_shopInteractor, nameof(m_shopInteractor));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_shopInteractor.OnActiveShopChanged += ShopChanged;
            ShopChanged();
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            m_shopInteractor.OnActiveShopChanged -= ShopChanged;
            if (m_currentShop != null)
            {
                m_currentShop.OnShopUpdated -= RefreshUI;
            }
        }

        /*----------------------------------------------------------
        | --- ShopChanged: Called when the active shop changes --- |
        ----------------------------------------------------------*/
        private void ShopChanged()
        {
            // Unsubscribe from previous shop events
            if (m_currentShop != null)
            {
                m_currentShop.OnShopUpdated -= RefreshUI;
            }

            m_currentShop = m_shopInteractor.ActiveShop;
            gameObject.SetActive(m_currentShop != null);

            foreach (ShopFilterUI filterButton in GetComponentsInChildren<ShopFilterUI>())
            {
                filterButton.SetShop(m_currentShop);
            }

            if (m_currentShop == null)
                return;

            m_shopNameText.text = m_currentShop.ShopName;

            // Subscribe to new shop events
            m_currentShop.OnShopUpdated += RefreshUI;

            RefreshUI();
        }

        /*---------------------------------------------------
        | --- RefreshUI: Refreshes the shop UI elements --- |
        ---------------------------------------------------*/
        private void RefreshUI()
        {
            foreach (Transform child in m_itemListingContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (ShopItem shopItem in m_currentShop.GetFilteredItems())
            {
                ItemListing itemListing = Instantiate<ItemListing>(m_itemListingPrefab, m_itemListingContainer);
                itemListing.Setup(m_currentShop, shopItem);
            }

            m_purchaseTotalText.text = $"Total: {m_currentShop.PurchaseTotal()} Gold";
            m_purchaseTotalText.color = !m_currentShop.InBuyMode || m_currentShop.HasSufficientFunds() ? Color.white : Color.red;
            m_purchaseButton.interactable = m_currentShop.CanAffordPurchase();

            // probably should change these names so they're not confusing. "confirm" for buying?
            m_switchModeButton.GetComponentInChildren<TextMeshProUGUI>().text = m_currentShop.InBuyMode ? "SELL" : "BUY";
            m_purchaseButton.GetComponentInChildren<TextMeshProUGUI>().text = m_currentShop.InBuyMode ? "BUY" : "SELL";

            foreach (ShopFilterUI filterButton in GetComponentsInChildren<ShopFilterUI>())
            {
                filterButton.RefreshUI();
            }
        }

        /*-----------------------------------------------------
        | --- CloseShop: Closes the currently active shop --- |
        -----------------------------------------------------*/
        public void CloseShop()
        {
            m_shopInteractor.SetActiveShop(null);
        }

        /*----------------------------------------------------------
        | --- CompletePurchase: Completes the current purchase --- |
        ----------------------------------------------------------*/
        public void CompletePurchase()
        {
            m_currentShop.CompletePurchase();
        }

        /*-------------------------------------------------------------
        | --- SwitchShopMode: Switches between buy and sell modes --- |
        -------------------------------------------------------------*/
        public void SwitchShopMode()
        {
            m_currentShop.SelectMode(!m_currentShop.InBuyMode);
        }
    }
}
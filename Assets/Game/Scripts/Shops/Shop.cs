using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Inventories;
using PolyQuest.UI.Core;
using PolyQuest.Player;
using PolyQuest.Saving;
using PolyQuest.Input;

namespace PolyQuest.Shops
{
    public class Shop : MonoBehaviour, IRaycastable, ISaveable
    {
        [SerializeField] private string m_shopName;
        [Range(0.0f, 100.0f)]
        [SerializeField] private float m_sellPricePercentage = 50.0f;
        [SerializeField] private ShopItemEntry[] m_shopInventory;

        [System.Serializable]
        private class ShopItemEntry
        {
            [SerializeField] private InventoryItem m_item;
            [SerializeField] private int m_initialStock;

            public InventoryItem Item => m_item;
            public int InitialStock => m_initialStock;
        }

        private ShopInteractor m_shopInteractor;
        private Dictionary<InventoryItem, int> m_purchase = new();
        private Dictionary<InventoryItem, int> m_currentStock = new();
        private bool m_isBuyingMode = true;
        private ItemCategory m_currentFilter = ItemCategory.kNone;

        public event Action OnShopUpdated;

        public string ShopName => m_shopName;
        public bool IsBuyMode => m_isBuyingMode;
        public ItemCategory CurrentFilter => m_currentFilter;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            foreach (ShopItemEntry entry in m_shopInventory)
            {
                m_currentStock[entry.Item] = entry.InitialStock;
            }
        }

        /*------------------------------------------------------------------
        | --- SetShopInteractor: Sets the ShopInteractor for this shop --- |
        ------------------------------------------------------------------*/
        public void SetShopInteractor(ShopInteractor shopper)
        {
            this.m_shopInteractor = shopper;
        }

        /*-------------------------------------------------------------
        | --- SelectMode: Sets the shop mode to buying or selling --- |
        -------------------------------------------------------------*/
        public void SelectMode(bool isBuying)
        {
            m_isBuyingMode = isBuying;
            OnShopUpdated?.Invoke();
        }

        /*---------------------------------------------------------------------
        | --- GetFilteredItems: Returns items based on the current filter --- |
        ---------------------------------------------------------------------*/
        public IEnumerable<ShopItem> GetFilteredItems()
        {
            foreach (ShopItem shopItem in GetAllItems())
            {
                if (m_currentFilter == ItemCategory.kNone || shopItem.Item.Category == m_currentFilter)
                {
                    yield return shopItem;
                }
            }
        }

        /*--------------------------------------------------------------------
        | --- SortByFilter: Sets the current m_item filter for the shop UI --- |
        --------------------------------------------------------------------*/
        public void SortByFilter(ItemCategory category)
        {
            m_currentFilter = category;
            OnShopUpdated?.Invoke();
        }

        /*----------------------------------------------------------------------------------
        | --- CanAffordPurchase: Checks if the shopper can afford the current purchase --- |
        ----------------------------------------------------------------------------------*/
        public bool CanAffordPurchase()
        {
            if (!HasItemsInPurchase())
                return false;

            if (!HasSufficientFunds())
                return false;

            if (!HasInventorySpace())
                return false;

            return true;
        }

        /*-------------------------------------------------------------------------------
        | --- HasItemsInPurchase: Checks if there are items in the current purchase --- |
        -------------------------------------------------------------------------------*/
        public bool HasItemsInPurchase()
        {
            return m_purchase.Count > 0;
        }

        /*--------------------------------------------------------------------------------------------
        | --- HasSufficientFunds: Checks if the shopper has enough gold for the current purchase --- |
        --------------------------------------------------------------------------------------------*/
        public bool HasSufficientFunds()
        {
            Wallet wallet = m_shopInteractor.GetComponent<Wallet>();

            return wallet.CurrentSilver >= PurchaseTotal();
        }

        /*-----------------------------------------------------------------------------------------------
        | --- HasInventorySpace: Checks if there is enough inventory space for the current purchase --- |
        -----------------------------------------------------------------------------------------------*/
        public bool HasInventorySpace()
        {
            if (!m_shopInteractor.TryGetComponent<Inventory>(out var shopperInventory))
                return false;

            List<InventoryItem> itemsToPurchase = new();
            foreach (ShopItem shopItem in GetAllItems())
            {
                InventoryItem item = shopItem.Item;
                int quantity = shopItem.QuantityToBuy;
                for (int i = 0; i < quantity; ++i)
                {
                    itemsToPurchase.Add(item);
                }
            }

            return shopperInventory.HasSpaceFor(itemsToPurchase);
        }

        /*----------------------------------------------------------------------------------------------------
        | --- CompletePurchase: Completes the current purchase and adds items to the shopper's inventory --- |
        ----------------------------------------------------------------------------------------------------*/
        public void CompletePurchase()
        {
            if (!m_shopInteractor.TryGetComponent<Inventory>(out var shopperInventory))
                return;

            if (!m_shopInteractor.TryGetComponent<Wallet>(out var shopperWallet))
                return;

            foreach (ShopItem shopItem in GetAllItems())
            {
                InventoryItem item = shopItem.Item;
                int quantity = shopItem.QuantityToBuy;
                int price = shopItem.Price;
                for (int i = 0; i < quantity; ++i)
                {
                    if (shopperWallet.CurrentSilver < price)
                        return;

                    bool success = shopperInventory.TryAddToAvailableSlot(item, 1);
                    if (success)
                    {
                        AddToPurchase(item, -1);
                        --m_currentStock[item];
                        shopperWallet.UpdateSiver(-price);
                    }
                }
            }

            OnShopUpdated?.Invoke();
        }

        /*--------------------------------------------------------------------------
        | --- AddToPurchase: Adds an item and quantity to the current purchase --- |
        --------------------------------------------------------------------------*/
        public void AddToPurchase(InventoryItem item, int quantity)
        {
            if (!m_purchase.ContainsKey(item))
            {
                m_purchase[item] = 0;
            }

            if (m_purchase[item] + quantity > m_currentStock[item])
            {
                m_purchase[item] = m_currentStock[item];
            }
            else
            {
                m_purchase[item] += quantity;
            }

            if (m_purchase[item] <= 0)
            {
                m_purchase.Remove(item);
            }

            OnShopUpdated?.Invoke();
        }

        /*--------------------------------------------------------------------------
        | --- PurchaseTotal: Calculates the total cost of the current purchase --- |
        --------------------------------------------------------------------------*/
        public int PurchaseTotal()
        {
            int total = 0;
            
            foreach (ShopItem item in GetAllItems())
            {
                total += item.Price * item.QuantityToBuy;
            }

            return total;
        }

        /*----------------------------------------------------------------------
        | --- GetCursorType: Returns the cursor type for shop interactions --- |
        ----------------------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kShop;
        }

        /*-------------------------------------------------------------------
        | --- HandleRaycast: Handles raycast interactions with the shop --- |
        -------------------------------------------------------------------*/
        public bool HandleRaycast(PlayerController playerController)
        {
            if (InputManager.Instance.InputActions.Gameplay.Interact.WasPressedThisFrame())
            {
                playerController.GetComponent<ShopInteractor>().SetActiveShop(this);
            }
            return true;
        }

        /*--------------------------------------------------------------
        | --- GetAllItems: Returns all items available in the shop --- |
        --------------------------------------------------------------*/
        private IEnumerable<ShopItem> GetAllItems()
        {
            foreach (ShopItemEntry entry in m_shopInventory)
            {
                int price = GetPrice(entry);
                m_purchase.TryGetValue(entry.Item, out int quantityInPurchase);
                yield return new ShopItem(entry.Item, m_currentStock[entry.Item], entry.Item.Price, quantityInPurchase);
            }
        }

        /*------------------------------------------------------------------------
        | --- GetPrice: Calculates the price based on buying or selling mode --- |
        ------------------------------------------------------------------------*/
        private int GetPrice(ShopItemEntry entry)
        {
            if (m_isBuyingMode)
            {
                return entry.Item.Price;
            }
            else
            {
                // Selling price is 50% of the m_item's price
                return Mathf.CeilToInt(entry.Item.Price * (m_sellPricePercentage / 100.0f));
            }
        }

        /*--------------------------------------------------------------------
        | --- CaptureState: Captures the current state of the shop stock --- |
        --------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            JObject state = new();
            IDictionary<string, JToken> stateDict = state;

            foreach (var pair in m_currentStock)
            {
                stateDict[pair.Key.ID] = JToken.FromObject(pair.Value);
            }
            return state;
        }

        /*----------------------------------------------------------------
        | --- RestoreState: Restores the shop stock from saved state --- |
        ----------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state is JObject stateObject)
            {
                IDictionary<string, JToken> stateDict = stateObject;
                m_currentStock.Clear();

                foreach (var pair in stateDict)
                {
                    InventoryItem item = InventoryItem.FindByID(pair.Key);
                    if (item)
                    {
                        m_currentStock[item] = pair.Value.ToObject<int>();
                    }
                }
            }
        }
    }
}
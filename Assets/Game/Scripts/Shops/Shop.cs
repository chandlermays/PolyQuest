/*---------------------------
File: Shop.cs
Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Inventories;
using PolyQuest.Player;
using PolyQuest.Saving;
using PolyQuest.UI.Core;
using PolyQuest.UI;

namespace PolyQuest.Shops
{
    public class Shop : MonoBehaviour, IRaycastable, ISaveable
    {
        [SerializeField] private ShopConfig m_shopConfig;

        private Inventory m_shopperInventory;
        private Wallet m_shopperWallet;
        private Outline m_outline;
        private WorldLabel m_worldLabel;

        private Dictionary<InventoryItem, int> m_purchase = new();
        private Dictionary<InventoryItem, int> m_currentStock = new();
        private ItemCategory m_currentFilter = ItemCategory.kNone;
        private bool m_isBuyingMode = true;

        public event Action OnShopUpdated;

        public string ShopName => m_shopConfig.name;
        public bool InBuyMode => m_isBuyingMode;
        public ItemCategory CurrentFilter => m_currentFilter;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_shopConfig, nameof(m_shopConfig));

            m_outline = GetComponent<Outline>();
            Utilities.CheckForNull(m_outline, nameof(m_outline));

            m_worldLabel = GetComponent<WorldLabel>();
            Utilities.CheckForNull(m_worldLabel, nameof(m_worldLabel));

            m_worldLabel.SetLabel(m_shopConfig.name);

            foreach (ShopConfig.ShopItemEntry entry in m_shopConfig.ShopInventory)
            {
                m_currentStock[entry.Item] = entry.InitialStock;
            }
        }

        /*------------------------------------------------------------------
        | --- SetShopInteractor: Sets the ShopInteractor for this shop --- |
        ------------------------------------------------------------------*/
        public void SetShopInteractor(ShopInteractor shopper)
        {
            if (shopper != null)
            {
                m_shopperInventory = shopper.GetComponent<Inventory>();
                m_shopperWallet = shopper.GetComponent<Wallet>();
            }
            else
            {
                m_shopperInventory = null;
                m_shopperWallet = null;
            }
        }

        /*----------------------------------------------------------------------
        | --- GetCursorType: Returns the cursor type for shop interactions --- |
        ----------------------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kTargeting;
        }

        /*-------------------------------------------------------------------
        | --- HandleRaycast: Handles raycast interactions with the shop --- |
        -------------------------------------------------------------------*/
        public bool HandleRaycast(PlayerController playerController)
        {
            if (InputManager.Instance.InputActions.Gameplay.Interact.WasPressedThisFrame())
            {
                playerController.GetComponent<ShopInteractor>().SetTargetShop(this);
            }
            return true;
        }

        /*----------------------------------------------------------------------------------------
        | --- ToggleHighlight: Enables or disables the outline highlight for the shop vendor --- |
        ----------------------------------------------------------------------------------------*/
        public void ToggleHighlight(bool highlight)
        {
            m_outline.enabled = highlight;
        }

        /*-------------------------------------------------------------------------------------
        | --- ToggleLabel: Controls the visibility of the world label for the shop vendor --- |
        -------------------------------------------------------------------------------------*/
        public void ToggleLabel(bool visible)
        {
            m_worldLabel.Toggle(visible);
        }

        /*-------------------------------------------------------------
        | --- SelectMode: Sets the shop mode to buying or selling --- |
        -------------------------------------------------------------*/
        public void SelectMode(bool isBuying)
        {
            m_isBuyingMode = isBuying;
            m_purchase.Clear();
            OnShopUpdated?.Invoke();
        }

        /*--------------------------------------------------------------------
        | --- SortByFilter: Sets the current item filter for the shop UI --- |
        --------------------------------------------------------------------*/
        public void SortByFilter(ItemCategory category)
        {
            m_currentFilter = category;
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

        /*----------------------------------------------------------------------------------
        | --- CanAffordPurchase: Checks if the shopper can afford the current purchase --- |
        ----------------------------------------------------------------------------------*/
        public bool CanAffordPurchase()
        {
            if (!HasItemsInPurchase())
                return false;

            if (m_isBuyingMode)
            {
                if (!HasSufficientFunds())
                    return false;

                if (!HasInventorySpace())
                    return false;
            }

            return true;
        }

        /*-------------------------------------------------------------------------------
        | --- HasItemsInPurchase: Checks if there are items in the current purchase --- |
        -------------------------------------------------------------------------------*/
        public bool HasItemsInPurchase()
        {
            return m_purchase.Count > 0;
        }

        /*----------------------------------------------------------------------------------------------
        | --- HasSufficientFunds: Checks if the shopper has enough silver for the current purchase --- |
        ----------------------------------------------------------------------------------------------*/
        public bool HasSufficientFunds()
        {
            return m_shopperWallet.CurrentSilver >= PurchaseTotal();
        }

        /*-----------------------------------------------------------------------------------------------
        | --- HasInventorySpace: Checks if there is enough inventory space for the current purchase --- |
        -----------------------------------------------------------------------------------------------*/
        public bool HasInventorySpace()
        {
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

            return m_shopperInventory.HasSpaceFor(itemsToPurchase);
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

            int maxAllowed = m_isBuyingMode ? m_currentStock[item] : GetQuantityFromInventory(item);

            m_purchase[item] = Mathf.Clamp(m_purchase[item] + quantity, 0, maxAllowed);

            if (m_purchase[item] <= 0)
            {
                m_purchase.Remove(item);
            }

            OnShopUpdated?.Invoke();
        }

        /*----------------------------------------------------------------------------------------------------
        | --- CompletePurchase: Completes the current purchase and adds items to the shopper's inventory --- |
        ----------------------------------------------------------------------------------------------------*/
        public void CompletePurchase()
        {
            if (m_isBuyingMode)
            {
                CompleteBuy();
            }
            else
            {
                CompleteSell();
            }

            OnShopUpdated?.Invoke();
            SaveManager.Instance.Save();
        }

        /*---------------------------------------------------------------------------------
        | --- CompleteBuy: Handles the logic for completing a purchase in buying mode --- |
        ---------------------------------------------------------------------------------*/
        private void CompleteBuy()
        {
            foreach (ShopItem shopItem in GetAllItems())
            {
                InventoryItem item = shopItem.Item;
                int quantity = shopItem.QuantityToBuy;
                int price = shopItem.Price;

                for (int i = 0; i < quantity; ++i)
                {
                    if (m_shopperWallet.CurrentSilver < price)
                        continue;

                    bool success = m_shopperInventory.TryAddToAvailableSlot(item, 1);
                    if (success)
                    {
                        AddToPurchase(item, -1);
                        --m_currentStock[item];
                        m_shopperWallet.UpdateSilver(-price);
                    }
                }
            }
        }

        /*-----------------------------------------------------------------------------------
        | --- CompleteSell: Handles the logic for completing a purchase in selling mode --- |
        -----------------------------------------------------------------------------------*/
        private void CompleteSell()
        {
            foreach (ShopItem shopItem in GetAllItems())
            {
                InventoryItem item = shopItem.Item;
                int quantity = shopItem.QuantityToBuy;
                int price = shopItem.Price;

                for (int i = 0; i < quantity; ++i)
                {
                    // Find and remove from the first slot that holds this item
                    for (int slot = 0; slot < m_shopperInventory.Size; ++slot)
                    {
                        if (m_shopperInventory.GetItemAtSlot(slot) == item)
                        {
                            m_shopperInventory.RemoveItemsFromSlot(slot, 1);
                            AddToPurchase(item, -1);
                            ++m_currentStock[item];
                            m_shopperWallet.UpdateSilver(price);
                            break;
                        }
                    }
                }
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
            // Re-seed from config first so new items are always present
            foreach (ShopConfig.ShopItemEntry entry in m_shopConfig.ShopInventory)
            {
                m_currentStock[entry.Item] = entry.InitialStock;
            }

            if (state is JObject stateObject)
            {
                IDictionary<string, JToken> stateDict = stateObject;
                foreach (var pair in stateDict)
                {
                    InventoryItem item = InventoryItem.FindByID(pair.Key);
                    if (item != null && m_currentStock.ContainsKey(item))
                    {
                        m_currentStock[item] = pair.Value.ToObject<int>();
                    }
                }
            }
        }

        /*--------------------------------------------------------------
        | --- GetAllItems: Returns all items available in the shop --- |
        --------------------------------------------------------------*/
        private IEnumerable<ShopItem> GetAllItems()
        {
            foreach (ShopConfig.ShopItemEntry entry in m_shopConfig.ShopInventory)
            {
                int price = GetPrice(entry);
                int stock = GetStock(entry);

                if (!m_isBuyingMode && stock <= 0)
                    continue;

                m_purchase.TryGetValue(entry.Item, out int quantityInPurchase);
                yield return new ShopItem(entry.Item, stock, price, quantityInPurchase);
            }
        }

        /*-------------------------------------------------------------------------------------------
        | --- GetStock: Returns the available stock for an item based on buying or selling mode --- |
        -------------------------------------------------------------------------------------------*/
        private int GetStock(ShopConfig.ShopItemEntry entry)
        {
            if (m_isBuyingMode)
            {
                return m_currentStock[entry.Item];
            }
            else
            {
                return GetQuantityFromInventory(entry.Item);
            }
        }

        /*---------------------------------------------------------------------------------------------------
        | --- GetQuantityFromInventory: Counts the total quantity of an item in the shopper's inventory --- |
        ---------------------------------------------------------------------------------------------------*/
        private int GetQuantityFromInventory(InventoryItem item)
        {
            if (m_shopperInventory == null)
                return 0;

            int quantity = 0;
            for (int i = 0; i < m_shopperInventory.Size; ++i)
            {
                if (m_shopperInventory.GetItemAtSlot(i) == item)
                {
                    quantity += m_shopperInventory.GetQuantityAtSlot(i);
                }
            }
            return quantity;
        }

        /*------------------------------------------------------------------------
        | --- GetPrice: Calculates the price based on buying or selling mode --- |
        ------------------------------------------------------------------------*/
        private int GetPrice(ShopConfig.ShopItemEntry entry)
        {
            if (m_isBuyingMode)
            {
                return entry.Item.Price;
            }
            else
            {
                return Mathf.CeilToInt(entry.Item.Price * (m_shopConfig.SellPricePercentage / 100.0f));
            }
        }
    }
}
/*---------------------------
| File: ShopTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Shops;
using PolyQuest.UI;

namespace PolyQuest.Tests
{
    public class ShopTests
    {
        private InventoryTestKit m_kit;

        private ActionItem m_sword, m_potion, m_ore, m_junk;
        private ShopConfig m_config;
        private Shop m_shop;
        private TextMeshProUGUI m_labelText;
        private GameObject m_labelRoot;

        private GameObject m_shopperObject;
        private Inventory m_inventory;
        private Wallet m_wallet;
        private ShopInteractor m_shopper;

        private int m_shopUpdated;

        /*------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for shop testing --- |
        ------------------------------------------------------------------------*/
        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            m_kit.IsolateSaves();

            // sword: non-stackable, 20.  potion: stackable, 10.  ore: stackable, 15 (sells for ceil(7.5) = 8).  junk: not sold by the shop
            m_sword = Priced("sword", false, 20, ItemCategory.kWeapon);
            m_potion = Priced("potion", true, 10, ItemCategory.kConsumables);
            m_ore = Priced("ore", true, 15, ItemCategory.kCraftingMaterial);
            m_junk = Priced("junk", true, 4, ItemCategory.kJunk);

            m_config = m_kit.Track(ScriptableObject.CreateInstance<ShopConfig>());
            m_config.name = "TestShop";
            SetSellPercentage(50f);
            SetStock((m_sword, 2), (m_potion, 5), (m_ore, 3));

            m_shopperObject = m_kit.NewObject("Shopper");
            m_inventory = m_kit.NewInventory(m_shopperObject, 6);
            m_wallet = m_shopperObject.AddComponent<Wallet>();
            m_shopper = m_shopperObject.AddComponent<ShopInteractor>();
            m_wallet.UpdateSilver(100);

            m_shop = BuildShop();
            m_shop.SetShopInteractor(m_shopper);

            m_shopUpdated = 0;
            m_shop.OnShopUpdated += () => ++m_shopUpdated;
        }

        /*-------------------------------------------------------------------
        | --- TearDown: Release test fixtures and game object resources --- |
        -------------------------------------------------------------------*/
        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }


        /*-----------------
        | --- Helpers --- |
        -----------------*/
        /*---------------------------------------------------------------------
        | --- Priced: Creates a registered item with a price and category --- |
        ---------------------------------------------------------------------*/
        private ActionItem Priced(string id, bool stackable, int price, ItemCategory category)
        {
            ActionItem item = m_kit.Item<ActionItem>(id, stackable);
            SerializedTestUtil.SetInt(item, "m_itemPrice", price);
            SerializedTestUtil.SetEnum(item, "m_category", category);
            return item;
        }

        /*--------------------------------------------------------------------
        | --- SetSellPercentage: Sets the config's sell price percentage --- |
        --------------------------------------------------------------------*/
        private void SetSellPercentage(float percentage) => SerializedTestUtil.SetFloat(m_config, "m_sellPricePercentage", percentage);

        /*---------------------------------------------------------------------------
        | --- SetStock: Sets the config's stocked items and their initial stock --- |
        ---------------------------------------------------------------------------*/
        private void SetStock(params (InventoryItem item, int stock)[] entries)
        {
            SerializedObject so = new SerializedObject(m_config);
            SerializedProperty array = so.FindProperty("m_shopInventory");
            array.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; ++i)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("m_item").objectReferenceValue = entries[i].item;
                element.FindPropertyRelative("m_initialStock").intValue = entries[i].stock;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /*-------------------------------------------------------------------------------------------------------------
        | --- BuildShop: Creates a shop from the test config, with an outline and world label, and runs its Awake --- |
        -------------------------------------------------------------------------------------------------------------*/
        private Shop BuildShop(string name = "Shop")
        {
            GameObject go = m_kit.NewObject(name);
            go.AddComponent<Outline>();

            m_labelRoot = new GameObject("LabelRoot");
            m_labelRoot.transform.SetParent(go.transform);
            m_labelText = m_labelRoot.AddComponent<TextMeshProUGUI>();

            WorldLabel label = go.AddComponent<WorldLabel>();
            SerializedTestUtil.SetObject(label, "m_labelRoot", m_labelRoot);
            SerializedTestUtil.SetObject(label, "m_labelText", m_labelText);

            Shop shop = go.AddComponent<Shop>();
            SerializedTestUtil.SetObject(shop, "m_shopConfig", m_config);
            InventoryTestKit.InvokeAwake(shop);
            return shop;
        }

        /*------------------------------------------------
        | --- Listed: The shop's listing for an item --- |
        ------------------------------------------------*/
        private ShopItem Listed(InventoryItem item) => m_shop.GetFilteredItems().Single(s => s.Item == item);

        /*-------------------------------------------------------------------------------
        | --- StockOf: The shop's current stock of an item, read from its save data --- |
        -------------------------------------------------------------------------------*/
        private int StockOf(InventoryItem item) => m_shop.CaptureState()[item.ID].ToObject<int>();

        /*----------------------------------------------------------------------
        | --- Owned: Totals an item's quantity across every inventory slot --- |
        ----------------------------------------------------------------------*/
        private int Owned(InventoryItem item)
        {
            int total = 0;
            for (int i = 0; i < m_inventory.Size; ++i)
                if (m_inventory.GetItemAtSlot(i) == item) total += m_inventory.GetQuantityAtSlot(i);
            return total;
        }


        /*-------------------------
        | --- Awake & Listing --- |
        -------------------------*/
        /*------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Awake_SeedsStockFromConfig_AndListsEveryEntry: Awake seeds stock from the config and lists every entry in config order, with base prices and nothing in the purchase --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Awake seeds stock from the config and lists every entry in config order, with base prices and nothing in the purchase.")]
        public void Awake_SeedsStockFromConfig_AndListsEveryEntry()
        {
            var listed = m_shop.GetFilteredItems().ToArray();

            Assert.That(listed.Select(s => s.Item), Is.EqualTo(new InventoryItem[] { m_sword, m_potion, m_ore }));
            Assert.That(listed.Select(s => s.AvailableStock), Is.EqualTo(new[] { 2, 5, 3 }));
            Assert.That(listed.Select(s => s.Price), Is.EqualTo(new[] { 20, 10, 15 }));
            Assert.That(listed.All(s => s.QuantityToBuy == 0));
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Awake_SetsTheWorldLabelToTheConfigName_AndShopNameMatches: Awake sets the world label text to the config's name, and ShopName matches it --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Awake sets the world label text to the config's name, and ShopName matches it.")]
        public void Awake_SetsTheWorldLabelToTheConfigName_AndShopNameMatches()
        {
            Assert.That(m_labelText.text, Is.EqualTo("TestShop"));
            Assert.That(m_shop.ShopName, Is.EqualTo("TestShop"));
        }

        /*---------------------------------------------------------------------------------------------------------------------------
        | --- NewShop_StartsInBuyMode_WithNoFilter: A new shop starts in buy mode with no category filter and an empty purchase --- |
        ---------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A new shop starts in buy mode with no category filter and an empty purchase.")]
        public void NewShop_StartsInBuyMode_WithNoFilter()
        {
            Assert.That(m_shop.InBuyMode, Is.True);
            Assert.That(m_shop.CurrentFilter, Is.EqualTo(ItemCategory.kNone));
            Assert.That(m_shop.HasItemsInPurchase(), Is.False);
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Awake_ConfigEntryWithNullItem_IsLeftOut: A config entry with no item assigned is left out of the shop; the other entries are still stocked and listed --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A config entry with no item assigned is left out of the shop; the other entries are still stocked and listed.")]
        public void Awake_ConfigEntryWithNullItem_IsLeftOut()
        {
            SetStock((m_potion, 5), (null, 1), (m_ore, 3));

            Shop shop = BuildShop("Broken");

            Assert.That(shop.GetFilteredItems().Select(s => s.Item), Is.EqualTo(new InventoryItem[] { m_potion, m_ore }));
        }

        /*----------------------------------------------------------------------------------------------------------------------------
        | --- OnValidate_ConfigEntryWithNullItem_Warns: An entry with no item gets an editor warning naming the config and entry --- |
        ----------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A config entry with no item gets an editor warning naming the config and the empty entry.")]
        public void OnValidate_ConfigEntryWithNullItem_Warns()
        {
            SetStock((m_potion, 5), (null, 1), (m_ore, 3));
            LogAssert.Expect(LogType.Warning, new Regex("TestShop.*entry #1 has no item"));

            typeof(ShopConfig).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(m_config, null);
        }


        /*-----------------------
        | --- Filter & Mode --- |
        -----------------------*/
        /*---------------------------------------------------------------------------------------------------------------------------------------
        | --- SortByFilter_ShowsOnlyThatCategory_AndRaisesTheEvent: SortByFilter lists only items in that category and raises OnShopUpdated --- |
        ---------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("SortByFilter lists only items in that category and raises OnShopUpdated.")]
        public void SortByFilter_ShowsOnlyThatCategory_AndRaisesTheEvent()
        {
            m_shop.SortByFilter(ItemCategory.kConsumables);

            Assert.That(m_shop.GetFilteredItems().Select(s => s.Item), Is.EqualTo(new InventoryItem[] { m_potion }));
            Assert.That(m_shopUpdated, Is.EqualTo(1));
        }

        /*--------------------------------------------------------------------------------------
        | --- SortByFilter_None_ShowsEverything: Filtering by kNone lists every item again --- |
        --------------------------------------------------------------------------------------*/
        [Test]
        [Description("Filtering by kNone lists every item again.")]
        public void SortByFilter_None_ShowsEverything()
        {
            m_shop.SortByFilter(ItemCategory.kWeapon);
            m_shop.SortByFilter(ItemCategory.kNone);

            Assert.That(m_shop.GetFilteredItems().Count(), Is.EqualTo(3));
        }

        /*---------------------------------------------------------------------------------------------------------------------
        | --- SortByFilter_CategoryWithNoItems_ShowsNothing: Filtering by a category the shop doesn't stock lists nothing --- |
        ---------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Filtering by a category the shop doesn't stock lists nothing.")]
        public void SortByFilter_CategoryWithNoItems_ShowsNothing()
        {
            m_shop.SortByFilter(ItemCategory.kQuestItem);

            Assert.That(m_shop.GetFilteredItems(), Is.Empty);
        }

        /*--------------------------------------------------------------------------------------------------------------------------
        | --- SelectMode_ClearsThePurchase_RaisesTheEvent: Switching mode clears the current purchase and raises OnShopUpdated --- |
        --------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Switching mode clears the current purchase and raises OnShopUpdated.")]
        public void SelectMode_ClearsThePurchase_RaisesTheEvent()
        {
            m_shop.AddToPurchase(m_potion, 2);
            m_shopUpdated = 0;

            m_shop.SelectMode(false);

            Assert.That(m_shop.InBuyMode, Is.False);
            Assert.That(m_shop.HasItemsInPurchase(), Is.False);
            Assert.That(m_shopUpdated, Is.EqualTo(1));
        }

        /*-------------------------------------------------------------------------------------------
        | --- SelectMode_KeepsTheCurrentFilter: Switching mode keeps the active category filter --- |
        -------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Switching mode keeps the active category filter.")]
        public void SelectMode_KeepsTheCurrentFilter()
        {
            m_shop.SortByFilter(ItemCategory.kWeapon);

            m_shop.SelectMode(false);

            Assert.That(m_shop.CurrentFilter, Is.EqualTo(ItemCategory.kWeapon));
        }


        /*-----------------------
        | --- AddToPurchase --- |
        -----------------------*/
        /*---------------------------------------------------------------------------------------------------------------------------------------------------
        | --- AddToPurchase_Buying_AccumulatesAndRaisesTheEvent: Repeated AddToPurchase calls accumulate the quantity and raise OnShopUpdated each time --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Repeated AddToPurchase calls accumulate the quantity and raise OnShopUpdated each time.")]
        public void AddToPurchase_Buying_AccumulatesAndRaisesTheEvent()
        {
            m_shop.AddToPurchase(m_potion, 2);
            m_shop.AddToPurchase(m_potion, 1);

            Assert.That(Listed(m_potion).QuantityToBuy, Is.EqualTo(3));
            Assert.That(m_shop.HasItemsInPurchase(), Is.True);
            Assert.That(m_shopUpdated, Is.EqualTo(2));
        }

        /*---------------------------------------------------------------------------------------------------------------
        | --- AddToPurchase_Buying_ClampsToStock: When buying, the purchase quantity is clamped to the shop's stock --- |
        ---------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("When buying, the purchase quantity is clamped to the shop's stock.")]
        public void AddToPurchase_Buying_ClampsToStock()
        {
            m_shop.AddToPurchase(m_potion, 99);

            Assert.That(Listed(m_potion).QuantityToBuy, Is.EqualTo(5));
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------------------
        | --- AddToPurchase_Negative_ReducesAndRemovesAtZero: A negative quantity reduces the purchase; reaching zero removes the item from the purchase --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A negative quantity reduces the purchase; reaching zero removes the item from the purchase.")]
        public void AddToPurchase_Negative_ReducesAndRemovesAtZero()
        {
            m_shop.AddToPurchase(m_potion, 2);
            m_shop.AddToPurchase(m_potion, -1);
            Assert.That(Listed(m_potion).QuantityToBuy, Is.EqualTo(1));

            m_shop.AddToPurchase(m_potion, -5);

            Assert.That(Listed(m_potion).QuantityToBuy, Is.EqualTo(0));
            Assert.That(m_shop.HasItemsInPurchase(), Is.False);
        }

        /*----------------------------------------------------------------------------------------------------------
        | --- AddToPurchase_OutOfStockItem_IsNotKept: Adding an item with zero stock leaves the purchase empty --- |
        ----------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Adding an item with zero stock leaves the purchase empty.")]
        public void AddToPurchase_OutOfStockItem_IsNotKept()
        {
            SetStock((m_potion, 0));
            m_shop = BuildShop("Empty");
            m_shop.SetShopInteractor(m_shopper);

            m_shop.AddToPurchase(m_potion, 1);

            Assert.That(m_shop.HasItemsInPurchase(), Is.False);
        }


        /*--------------------------------
        | --- Totals & Affordability --- |
        --------------------------------*/
        /*----------------------------------------------------------------------------------------------------------------
        | --- PurchaseTotal_SumsPriceTimesQuantity: PurchaseTotal is the sum of price * quantity across the purchase --- |
        ----------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("PurchaseTotal is the sum of price * quantity across the purchase.")]
        public void PurchaseTotal_SumsPriceTimesQuantity()
        {
            m_shop.AddToPurchase(m_sword, 2);       // 40
            m_shop.AddToPurchase(m_potion, 3);      // 30

            Assert.That(m_shop.PurchaseTotal(), Is.EqualTo(70));
        }

        /*-------------------------------------------------------------------------------------
        | --- CanAffordPurchase_EmptyPurchase_False: An empty purchase can't be completed --- |
        -------------------------------------------------------------------------------------*/
        [Test]
        [Description("An empty purchase can't be completed.")]
        public void CanAffordPurchase_EmptyPurchase_False()
        {
            Assert.That(m_shop.CanAffordPurchase(), Is.False);
        }

        /*-------------------------------------------------------------------------------------------------------------------------------
        | --- CanAffordPurchase_EnoughFundsAndSpace_True: A purchase within the wallet balance and inventory space can be completed --- |
        -------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A purchase within the wallet balance and inventory space can be completed.")]
        public void CanAffordPurchase_EnoughFundsAndSpace_True()
        {
            m_shop.AddToPurchase(m_sword, 1);

            Assert.That(m_shop.CanAffordPurchase(), Is.True);
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CanAffordPurchase_TotalExceedsWallet_False: A total above the wallet balance means insufficient funds, so the purchase can't be completed --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A total above the wallet balance means insufficient funds, so the purchase can't be completed.")]
        public void CanAffordPurchase_TotalExceedsWallet_False()
        {
            m_shop.AddToPurchase(m_sword, 2);       // 40
            m_wallet.UpdateSilver(-70);             // 30 left

            Assert.That(m_shop.HasSufficientFunds(), Is.False);
            Assert.That(m_shop.CanAffordPurchase(), Is.False);
        }

        /*-------------------------------------------------------------------------------------------------------------------
        | --- CanAffordPurchase_ExactFunds_True: A wallet balance exactly equal to the total counts as sufficient funds --- |
        -------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A wallet balance exactly equal to the total counts as sufficient funds.")]
        public void CanAffordPurchase_ExactFunds_True()
        {
            m_shop.AddToPurchase(m_sword, 1);       // 20
            m_wallet.UpdateSilver(-80);

            Assert.That(m_shop.HasSufficientFunds(), Is.True);
        }

        /*--------------------------------------------------------------------------------------------------------------------
        | --- CanAffordPurchase_InventoryFull_False: A full inventory means no space, so the purchase can't be completed --- |
        --------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A full inventory means no space, so the purchase can't be completed.")]
        public void CanAffordPurchase_InventoryFull_False()
        {
            for (int i = 0; i < 6; ++i)
                m_inventory.TryAddToAvailableSlot(m_kit.Item<ActionItem>("filler" + i), 1);
            m_shop.AddToPurchase(m_sword, 1);

            Assert.That(m_shop.HasInventorySpace(), Is.False);
            Assert.That(m_shop.CanAffordPurchase(), Is.False);
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------
        | --- HasInventorySpace_StackablePurchase_NeedsOneSlot_NotOnePerUnit: Several units of one stackable item need only one free slot --- |
        -------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Several units of one stackable item need only one free slot.")]
        public void HasInventorySpace_StackablePurchase_NeedsOneSlot_NotOnePerUnit()
        {
            for (int i = 0; i < 5; ++i)
                m_inventory.TryAddToAvailableSlot(m_kit.Item<ActionItem>("filler" + i), 1);     // one slot left
            m_shop.AddToPurchase(m_potion, 5);

            Assert.That(m_shop.HasInventorySpace(), Is.True);
        }

        /*-------------------------------------------------------------------------------------------------------------------------------
        | --- HasInventorySpace_NonStackablePurchase_NeedsOneSlotPerUnit: Each unit of a non-stackable item needs its own free slot --- |
        -------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Each unit of a non-stackable item needs its own free slot.")]
        public void HasInventorySpace_NonStackablePurchase_NeedsOneSlotPerUnit()
        {
            for (int i = 0; i < 5; ++i)
                m_inventory.TryAddToAvailableSlot(m_kit.Item<ActionItem>("filler" + i), 1);
            m_shop.AddToPurchase(m_sword, 2);

            Assert.That(m_shop.HasInventorySpace(), Is.False);
        }


        /*-----------------
        | --- Selling --- |
        -----------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------------------
        | --- SellMode_ListsOnlyItemsTheShopperOwns: Sell mode lists only items the shopper owns, with the owned count shown as available stock --- |
        -------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Sell mode lists only items the shopper owns, with the owned count shown as available stock.")]
        public void SellMode_ListsOnlyItemsTheShopperOwns()
        {
            m_inventory.TryAddToAvailableSlot(m_ore, 4);
            m_shop.SelectMode(false);

            var listed = m_shop.GetFilteredItems().ToArray();

            Assert.That(listed.Length, Is.EqualTo(1));
            Assert.That(listed[0].Item, Is.SameAs(m_ore));
            Assert.That(listed[0].AvailableStock, Is.EqualTo(4), "stock shown is the shopper's owned count");
        }

        /*-----------------------------------------------------------------------------------------------------------------------------
        | --- SellMode_PriceIsPercentageOfBasePrice_RoundedUp: Sell price is the base price times the sell percentage, rounded up --- |
        -----------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Sell price is the base price times the sell percentage, rounded up.")]
        public void SellMode_PriceIsPercentageOfBasePrice_RoundedUp()
        {
            m_inventory.TryAddToAvailableSlot(m_ore, 1);        // 15 * 50% = 7.5
            m_shop.SelectMode(false);

            Assert.That(Listed(m_ore).Price, Is.EqualTo(8));
        }

        /*-------------------------------------------------------------------------------------------------
        | --- SellMode_ZeroPercent_SellsForNothing: A 0% sell percentage makes items sell for nothing --- |
        -------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A 0% sell percentage makes items sell for nothing.")]
        public void SellMode_ZeroPercent_SellsForNothing()
        {
            SetSellPercentage(0f);
            m_inventory.TryAddToAvailableSlot(m_ore, 1);
            m_shop.SelectMode(false);

            Assert.That(Listed(m_ore).Price, Is.EqualTo(0));
        }

        /*------------------------------------------------------------------------------------------------------------------
        | --- SellMode_AddToPurchase_ClampsToOwned: When selling, the quantity is clamped to how many the shopper owns --- |
        ------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("When selling, the quantity is clamped to how many the shopper owns.")]
        public void SellMode_AddToPurchase_ClampsToOwned()
        {
            m_inventory.TryAddToAvailableSlot(m_ore, 3);
            m_shop.SelectMode(false);

            m_shop.AddToPurchase(m_ore, 10);

            Assert.That(Listed(m_ore).QuantityToBuy, Is.EqualTo(3));
        }

        /*------------------------------------------------------------------------------------------------------------------------------------------
        | --- SellMode_CanAffordPurchase_IgnoresWalletAndSpace: When selling, CanAffordPurchase ignores the wallet balance and inventory space --- |
        ------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("When selling, CanAffordPurchase ignores the wallet balance and inventory space.")]
        public void SellMode_CanAffordPurchase_IgnoresWalletAndSpace()
        {
            m_wallet.UpdateSilver(-100);
            m_inventory.TryAddToAvailableSlot(m_ore, 1);
            m_shop.SelectMode(false);
            m_shop.AddToPurchase(m_ore, 1);

            Assert.That(m_shop.CanAffordPurchase(), Is.True);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- SellMode_Complete_RemovesItems_PaysSilver_RestocksShop: Completing a sale removes the items, pays the sell price, adds them to the shop's stock, and clears the purchase --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing a sale removes the items, pays the sell price, adds them to the shop's stock, and clears the purchase.")]
        public void SellMode_Complete_RemovesItems_PaysSilver_RestocksShop()
        {
            m_inventory.TryAddToAvailableSlot(m_ore, 5);
            m_shop.SelectMode(false);
            m_shop.AddToPurchase(m_ore, 2);

            m_shop.CompletePurchase();

            Assert.That(Owned(m_ore), Is.EqualTo(3));
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(100 + 2 * 8));
            Assert.That(StockOf(m_ore), Is.EqualTo(3 + 2));
            Assert.That(m_shop.HasItemsInPurchase(), Is.False);
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------
        | --- SellMode_Complete_SellingEverything_LeavesNoPurchase: Selling every unit the shopper owns empties both their stack and the purchase --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Selling every unit the shopper owns empties both their stack and the purchase.")]
        public void SellMode_Complete_SellingEverything_LeavesNoPurchase()
        {
            m_inventory.TryAddToAvailableSlot(m_ore, 3);
            m_shop.SelectMode(false);
            m_shop.AddToPurchase(m_ore, 3);

            m_shop.CompletePurchase();

            Assert.That(Owned(m_ore), Is.EqualTo(0));
            Assert.That(m_shop.HasItemsInPurchase(), Is.False);
        }


        /*----------------
        | --- Buying --- |
        ----------------*/
        /*---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Buy_Complete_AddsItems_ChargesSilver_ReducesStock_ClearsPurchase: Completing a buy adds the items, charges the total, reduces the shop's stock, and clears the purchase --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing a buy adds the items, charges the total, reduces the shop's stock, and clears the purchase.")]
        public void Buy_Complete_AddsItems_ChargesSilver_ReducesStock_ClearsPurchase()
        {
            m_shop.AddToPurchase(m_potion, 3);
            m_shop.AddToPurchase(m_sword, 1);

            m_shop.CompletePurchase();

            Assert.That(Owned(m_potion), Is.EqualTo(3));
            Assert.That(Owned(m_sword), Is.EqualTo(1));
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(100 - 30 - 20));
            Assert.That(StockOf(m_potion), Is.EqualTo(2));
            Assert.That(StockOf(m_sword), Is.EqualTo(1));
            Assert.That(m_shop.HasItemsInPurchase(), Is.False);
        }

        /*---------------------------------------------------------------------------------------------
        | --- Buy_Complete_RaisesShopUpdated: Completing a buy raises OnShopUpdated at least once --- |
        ---------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing a buy raises OnShopUpdated at least once.")]
        public void Buy_Complete_RaisesShopUpdated()
        {
            m_shop.AddToPurchase(m_potion, 1);
            m_shopUpdated = 0;

            m_shop.CompletePurchase();

            Assert.That(m_shopUpdated, Is.GreaterThanOrEqualTo(1));
        }

        /*---------------------------------------------------------------------------------------------------------------------------
        | --- Buy_Complete_AutoSaves: Completing a buy autosaves: it writes the autosave and leaves the Player's own save alone --- |
        ---------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing a buy autosaves: it writes the autosave and leaves the Player's own save alone.")]
        public void Buy_Complete_AutoSaves()
        {
            m_shop.AddToPurchase(m_potion, 1);

            m_shop.CompletePurchase();

            Assert.That(InventoryTestKit.TestAutoSaveFileExists(), Is.True);
            Assert.That(InventoryTestKit.TestSaveFileExists(), Is.False, "an automatic save wrote the Player's own save");
        }


        /*----------------
        | --- Saving --- |
        ----------------*/
        /*-----------------------------------------------------------------------------------------------------------------------
        | --- CaptureState_StoresStockKeyedByItemID: CaptureState stores one stock count per stocked item, keyed by item ID --- |
        -----------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("CaptureState stores one stock count per stocked item, keyed by item ID.")]
        public void CaptureState_StoresStockKeyedByItemID()
        {
            JObject state = (JObject)m_shop.CaptureState();

            Assert.That(state["sword"].ToObject<int>(), Is.EqualTo(2));
            Assert.That(state["potion"].ToObject<int>(), Is.EqualTo(5));
            Assert.That(state["ore"].ToObject<int>(), Is.EqualTo(3));
            Assert.That(state.Count, Is.EqualTo(3));
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_OverridesStock: RestoreState overrides stock for the saved items; items missing from the save are re-seeded from the config --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("RestoreState overrides stock for the saved items; items missing from the save are re-seeded from the config.")]
        public void RestoreState_OverridesStock()
        {
            m_shop.RestoreState(new JObject { ["potion"] = 1, ["sword"] = 0 });

            Assert.That(StockOf(m_potion), Is.EqualTo(1));
            Assert.That(StockOf(m_sword), Is.EqualTo(0));
            Assert.That(StockOf(m_ore), Is.EqualTo(3), "unmentioned items are re-seeded from the config");
        }

        /*----------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_UnknownOrUnstockedItems_AreIgnored: Saved IDs that are unknown, or that the shop doesn't stock, are ignored --- |
        ----------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Saved IDs that are unknown, or that the shop doesn't stock, are ignored.")]
        public void RestoreState_UnknownOrUnstockedItems_AreIgnored()
        {
            m_shop.RestoreState(new JObject { ["nope"] = 9, ["junk"] = 9 });

            Assert.That(m_shop.CaptureState().Count(), Is.EqualTo(3));
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_UnreadableStockEntry_IsSkipped_TheRestLoad: A stock that isn't a whole number of 0 or more is skipped (config stock kept); the rest load --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A saved stock that isn't a whole number of 0 or more is skipped, leaving that item at its config stock; the other entries load.")]
        public void RestoreState_UnreadableStockEntry_IsSkipped_TheRestLoad()
        {
            m_shop.RestoreState(new JObject { ["potion"] = "lots", ["sword"] = -2, ["ore"] = 1 });

            Assert.That(StockOf(m_potion), Is.EqualTo(5), "config stock");
            Assert.That(StockOf(m_sword), Is.EqualTo(2), "config stock");
            Assert.That(StockOf(m_ore), Is.EqualTo(1));
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_NonObject_IsIgnored: A null or non-object state is ignored, as if the save never held this shop: the current stock is kept --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A null or non-object state is ignored, as if the save never held this shop: the current stock is kept.")]
        public void RestoreState_NonObject_IsIgnored()
        {
            m_shop.AddToPurchase(m_potion, 1);
            m_shop.CompletePurchase();
            Assert.That(StockOf(m_potion), Is.EqualTo(4));

            m_shop.RestoreState(JValue.CreateNull());
            m_shop.RestoreState(new JValue(5));

            Assert.That(StockOf(m_potion), Is.EqualTo(4));
        }

        /*------------------------------------------------------------------------------------------------------------------------------------
        | --- CaptureThenRestore_RoundTripsStock: Stock captured from one shop restores correctly into another shop with the same config --- |
        ------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Stock captured from one shop restores correctly into another shop with the same config.")]
        public void CaptureThenRestore_RoundTripsStock()
        {
            m_shop.AddToPurchase(m_potion, 2);
            m_shop.CompletePurchase();
            JToken saved = m_shop.CaptureState();

            Shop other = BuildShop("Other");
            other.RestoreState(saved);

            Assert.That(other.CaptureState()["potion"].ToObject<int>(), Is.EqualTo(3));
        }


        /*----------------------------
        | --- Scene Presentation --- |
        ----------------------------*/
        /*-------------------------------------------------------------------------------------------------------------------------
        | --- ToggleHighlight_EnablesAndDisablesTheOutline: ToggleHighlight enables and disables the shop's Outline component --- |
        -------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Category("ClickModel")]
        [Description("ToggleHighlight enables and disables the shop's Outline component.")]
        public void ToggleHighlight_EnablesAndDisablesTheOutline()
        {
            Outline outline = m_shop.GetComponent<Outline>();

            m_shop.ToggleHighlight(true);
            Assert.That(outline.enabled, Is.True);

            m_shop.ToggleHighlight(false);
            Assert.That(outline.enabled, Is.False);
        }

        /*--------------------------------------------------------------------------------------------------------------------
        | --- ToggleLabel_ShowsAndHidesTheLabelRoot: ToggleLabel activates and deactivates the world label's root object --- |
        --------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Category("ClickModel")]
        [Description("ToggleLabel activates and deactivates the world label's root object.")]
        public void ToggleLabel_ShowsAndHidesTheLabelRoot()
        {
            m_shop.ToggleLabel(false);
            Assert.That(m_labelRoot.activeSelf, Is.False);

            m_shop.ToggleLabel(true);
            Assert.That(m_labelRoot.activeSelf, Is.True);
        }

        /*-------------------------------------------------------------------------------
        | --- GetCursorType_IsTargeting: Hovering a shop shows the targeting cursor --- |
        -------------------------------------------------------------------------------*/
        [Test]
        [Category("ClickModel")]
        [Description("Hovering a shop shows the targeting cursor.")]
        public void GetCursorType_IsTargeting()
        {
            Assert.That(m_shop.GetCursorType(), Is.EqualTo(PolyQuest.UI.Core.CursorSettings.CursorType.kTargeting));
        }
    }
}
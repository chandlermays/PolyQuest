/*---------------------------
| File: ShopTests.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
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

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        private ActionItem Priced(string id, bool stackable, int price, ItemCategory category)
        {
            ActionItem item = m_kit.Item<ActionItem>(id, stackable);
            SerializedTestUtil.SetInt(item, "m_itemPrice", price);
            SerializedTestUtil.SetEnum(item, "m_category", category);
            return item;
        }

        private void SetSellPercentage(float percentage) => SerializedTestUtil.SetFloat(m_config, "m_sellPricePercentage", percentage);

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

        private ShopItem Listed(InventoryItem item) => m_shop.GetFilteredItems().Single(s => s.Item == item);

        private int StockOf(InventoryItem item) => m_shop.CaptureState()[item.ID].ToObject<int>();

        private int Owned(InventoryItem item)
        {
            int total = 0;
            for (int i = 0; i < m_inventory.Size; ++i)
                if (m_inventory.GetItemAtSlot(i) == item) total += m_inventory.GetQuantityAtSlot(i);
            return total;
        }

        /* --- Awake / listing --- */

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

        [Test]
        [Description("Awake sets the world label text to the config's name, and ShopName matches it.")]
        public void Awake_SetsTheWorldLabelToTheConfigName_AndShopNameMatches()
        {
            Assert.That(m_labelText.text, Is.EqualTo("TestShop"));
            Assert.That(m_shop.ShopName, Is.EqualTo("TestShop"));
        }

        [Test]
        [Description("A new shop starts in buy mode with no category filter and an empty purchase.")]
        public void NewShop_StartsInBuyMode_WithNoFilter()
        {
            Assert.That(m_shop.InBuyMode, Is.True);
            Assert.That(m_shop.CurrentFilter, Is.EqualTo(ItemCategory.kNone));
            Assert.That(m_shop.HasItemsInPurchase(), Is.False);
        }

        [Test]
        [Description("A config entry with a null item makes Awake throw.")]
        public void Awake_ConfigEntryWithNullItem_Throws()
        {
            SetStock((null, 1));

            Assert.That(() => BuildShop("Broken"), Throws.Exception);
        }

        [Test]
        [Description("Duplicate config entries for one item are listed twice and double-count the purchase total.")]
        public void DuplicateConfigEntries_DoubleCountThePurchaseTotal()
        {
            SetStock((m_potion, 5), (m_potion, 8));
            m_shop = BuildShop("Dup");
            m_shop.SetShopInteractor(m_shopper);

            m_shop.AddToPurchase(m_potion, 1);

            Assert.That(m_shop.GetFilteredItems().Count(), Is.EqualTo(2));
            Assert.That(m_shop.PurchaseTotal(), Is.EqualTo(20), "1 potion at 10 silver, counted twice");
        }

        /* --- Filter / mode --- */

        [Test]
        [Description("SortByFilter lists only items in that category and raises OnShopUpdated.")]
        public void SortByFilter_ShowsOnlyThatCategory_AndRaisesTheEvent()
        {
            m_shop.SortByFilter(ItemCategory.kConsumables);

            Assert.That(m_shop.GetFilteredItems().Select(s => s.Item), Is.EqualTo(new InventoryItem[] { m_potion }));
            Assert.That(m_shopUpdated, Is.EqualTo(1));
        }

        [Test]
        [Description("Filtering by kNone lists every item again.")]
        public void SortByFilter_None_ShowsEverything()
        {
            m_shop.SortByFilter(ItemCategory.kWeapon);
            m_shop.SortByFilter(ItemCategory.kNone);

            Assert.That(m_shop.GetFilteredItems().Count(), Is.EqualTo(3));
        }

        [Test]
        [Description("Filtering by a category the shop doesn't stock lists nothing.")]
        public void SortByFilter_CategoryWithNoItems_ShowsNothing()
        {
            m_shop.SortByFilter(ItemCategory.kQuestItem);

            Assert.That(m_shop.GetFilteredItems(), Is.Empty);
        }

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

        [Test]
        [Description("Switching mode keeps the active category filter.")]
        public void SelectMode_KeepsTheCurrentFilter()
        {
            m_shop.SortByFilter(ItemCategory.kWeapon);

            m_shop.SelectMode(false);

            Assert.That(m_shop.CurrentFilter, Is.EqualTo(ItemCategory.kWeapon));
        }

        /* --- AddToPurchase --- */

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

        [Test]
        [Description("When buying, the purchase quantity is clamped to the shop's stock.")]
        public void AddToPurchase_Buying_ClampsToStock()
        {
            m_shop.AddToPurchase(m_potion, 99);

            Assert.That(Listed(m_potion).QuantityToBuy, Is.EqualTo(5));
        }

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

        [Test]
        [Description("Buying an item the shop doesn't stock throws KeyNotFoundException and leaves a zero-quantity phantom entry in the purchase.")]
        public void AddToPurchase_ItemNotInShop_Throws_AndLeavesAPhantomEntry()
        {
            Assert.That(() => m_shop.AddToPurchase(m_junk, 1), Throws.TypeOf<KeyNotFoundException>());

            Assert.That(m_shop.HasItemsInPurchase(), Is.True, "phantom zero-quantity entry");
            Assert.That(m_shop.PurchaseTotal(), Is.EqualTo(0));
        }

        [Test]
        [Description("The phantom entry makes CanAffordPurchase true for a purchase worth nothing.")]
        public void PhantomEntry_EnablesAnEmptyPurchase()
        {
            Assert.That(() => m_shop.AddToPurchase(m_junk, 1), Throws.TypeOf<KeyNotFoundException>());

            Assert.That(m_shop.CanAffordPurchase(), Is.True);
        }

        /* --- Totals / affordability --- */

        [Test]
        [Description("PurchaseTotal is the sum of price * quantity across the purchase.")]
        public void PurchaseTotal_SumsPriceTimesQuantity()
        {
            m_shop.AddToPurchase(m_sword, 2);       // 40
            m_shop.AddToPurchase(m_potion, 3);      // 30

            Assert.That(m_shop.PurchaseTotal(), Is.EqualTo(70));
        }

        [Test]
        [Description("An empty purchase can't be completed.")]
        public void CanAffordPurchase_EmptyPurchase_False()
        {
            Assert.That(m_shop.CanAffordPurchase(), Is.False);
        }

        [Test]
        [Description("A purchase within the wallet balance and inventory space can be completed.")]
        public void CanAffordPurchase_EnoughFundsAndSpace_True()
        {
            m_shop.AddToPurchase(m_sword, 1);

            Assert.That(m_shop.CanAffordPurchase(), Is.True);
        }

        [Test]
        [Description("A total above the wallet balance means insufficient funds, so the purchase can't be completed.")]
        public void CanAffordPurchase_TotalExceedsWallet_False()
        {
            m_shop.AddToPurchase(m_sword, 2);       // 40
            m_wallet.UpdateSilver(-70);             // 30 left

            Assert.That(m_shop.HasSufficientFunds(), Is.False);
            Assert.That(m_shop.CanAffordPurchase(), Is.False);
        }

        [Test]
        [Description("A wallet balance exactly equal to the total counts as sufficient funds.")]
        public void CanAffordPurchase_ExactFunds_True()
        {
            m_shop.AddToPurchase(m_sword, 1);       // 20
            m_wallet.UpdateSilver(-80);

            Assert.That(m_shop.HasSufficientFunds(), Is.True);
        }

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

        [Test]
        [Description("Several units of one stackable item need only one free slot.")]
        public void HasInventorySpace_StackablePurchase_NeedsOneSlot_NotOnePerUnit()
        {
            for (int i = 0; i < 5; ++i)
                m_inventory.TryAddToAvailableSlot(m_kit.Item<ActionItem>("filler" + i), 1);     // one slot left
            m_shop.AddToPurchase(m_potion, 5);

            Assert.That(m_shop.HasInventorySpace(), Is.True);
        }

        [Test]
        [Description("Each unit of a non-stackable item needs its own free slot.")]
        public void HasInventorySpace_NonStackablePurchase_NeedsOneSlotPerUnit()
        {
            for (int i = 0; i < 5; ++i)
                m_inventory.TryAddToAvailableSlot(m_kit.Item<ActionItem>("filler" + i), 1);
            m_shop.AddToPurchase(m_sword, 2);

            Assert.That(m_shop.HasInventorySpace(), Is.False);
        }

        [Test]
        [Description("Without a ShopInteractor, HasSufficientFunds and HasInventorySpace throw NullReferenceException.")]
        public void HasSufficientFunds_WithoutAShopper_Throws()
        {
            Shop orphan = BuildShop("Orphan");

            Assert.That(() => orphan.HasSufficientFunds(), Throws.TypeOf<NullReferenceException>());
            Assert.That(() => orphan.HasInventorySpace(), Throws.TypeOf<NullReferenceException>());
        }

        [Test]
        [Description("Setting the ShopInteractor to null clears the shopper, so sell mode lists nothing.")]
        public void SetShopInteractor_Null_ClearsTheShopper_SellModeListsNothing()
        {
            m_inventory.TryAddToAvailableSlot(m_ore, 2);
            m_shop.SelectMode(false);
            Assert.That(m_shop.GetFilteredItems().Count(), Is.EqualTo(1));

            m_shop.SetShopInteractor(null);

            Assert.That(m_shop.GetFilteredItems(), Is.Empty);
        }

        /* --- Selling mode --- */

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

        [Test]
        [Description("Sell price is the base price times the sell percentage, rounded up.")]
        public void SellMode_PriceIsPercentageOfBasePrice_RoundedUp()
        {
            m_inventory.TryAddToAvailableSlot(m_ore, 1);        // 15 * 50% = 7.5
            m_shop.SelectMode(false);

            Assert.That(Listed(m_ore).Price, Is.EqualTo(8));
        }

        [Test]
        [Description("A 0% sell percentage makes items sell for nothing.")]
        public void SellMode_ZeroPercent_SellsForNothing()
        {
            SetSellPercentage(0f);
            m_inventory.TryAddToAvailableSlot(m_ore, 1);
            m_shop.SelectMode(false);

            Assert.That(Listed(m_ore).Price, Is.EqualTo(0));
        }

        [Test]
        [Description("When selling, the quantity is clamped to how many the shopper owns.")]
        public void SellMode_AddToPurchase_ClampsToOwned()
        {
            m_inventory.TryAddToAvailableSlot(m_ore, 3);
            m_shop.SelectMode(false);

            m_shop.AddToPurchase(m_ore, 10);

            Assert.That(Listed(m_ore).QuantityToBuy, Is.EqualTo(3));
        }

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

        [Test]
        [Description("An owned item the shop doesn't stock can be added to a sale but is never sold, and its phantom entry survives completion.")]
        public void SellMode_ItemNotInShop_IsAcceptedButNeverSold()
        {
            m_inventory.TryAddToAvailableSlot(m_junk, 2);
            m_shop.SelectMode(false);

            m_shop.AddToPurchase(m_junk, 2);

            Assert.That(m_shop.HasItemsInPurchase(), Is.True);
            Assert.That(m_shop.PurchaseTotal(), Is.EqualTo(0));
            Assert.That(m_shop.CanAffordPurchase(), Is.True);

            m_shop.CompletePurchase();

            Assert.That(Owned(m_junk), Is.EqualTo(2), "nothing sold");
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(100));
            Assert.That(m_shop.HasItemsInPurchase(), Is.True, "phantom purchase survives the sale");
        }

        /* --- Buying: CompletePurchase --- */

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

        [Test]
        [Description("Completing a buy raises OnShopUpdated at least once.")]
        public void Buy_Complete_RaisesShopUpdated()
        {
            m_shop.AddToPurchase(m_potion, 1);
            m_shopUpdated = 0;

            m_shop.CompletePurchase();

            Assert.That(m_shopUpdated, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        [Description("Completing a buy autosaves through the SaveManager, writing the (isolated) save file.")]
        public void Buy_Complete_WritesTheSaveFile()
        {
            m_shop.AddToPurchase(m_potion, 1);

            m_shop.CompletePurchase();

            Assert.That(InventoryTestKit.TestSaveFileExists(), Is.True);
        }

        [Test]
        [Description("With too little silver for the whole purchase, completing buys as many units as the wallet covers and leaves the rest in the purchase.")]
        public void Buy_Complete_WithPartialFunds_BuysWhatItCanAfford()
        {
            m_wallet.UpdateSilver(-75);             // 25 left
            m_shop.AddToPurchase(m_potion, 3);      // 10 each

            m_shop.CompletePurchase();

            Assert.That(Owned(m_potion), Is.EqualTo(2));
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(5));
            Assert.That(Listed(m_potion).QuantityToBuy, Is.EqualTo(1));
            Assert.That(StockOf(m_potion), Is.EqualTo(3));
        }

        [Test]
        [Description("With a full inventory, completing a buy charges nothing, keeps stock, and keeps the purchase.")]
        public void Buy_Complete_InventoryFull_ChargesNothing_KeepsPurchase()
        {
            for (int i = 0; i < 6; ++i)
                m_inventory.TryAddToAvailableSlot(m_kit.Item<ActionItem>("filler" + i), 1);
            m_shop.AddToPurchase(m_sword, 1);

            m_shop.CompletePurchase();

            Assert.That(Owned(m_sword), Is.EqualTo(0));
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(100));
            Assert.That(StockOf(m_sword), Is.EqualTo(2));
            Assert.That(Listed(m_sword).QuantityToBuy, Is.EqualTo(1));
        }

        [Test]
        [Description("Without a SaveManager, CompletePurchase applies the purchase and skips the autosave without throwing.")]
        public void Buy_Complete_WithoutASaveManager_DoesNotThrow()
        {
            InventoryTestKit.ClearSaveManager();
            m_shop.AddToPurchase(m_potion, 1);

            Assert.DoesNotThrow(() => m_shop.CompletePurchase(), "CompletePurchase threw without a SaveManager");

            Assert.That(Owned(m_potion), Is.EqualTo(1));
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(90));
        }

        /* --- Save / restore --- */

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

        [Test]
        [Description("RestoreState overrides stock for the saved items; items missing from the save are re-seeded from the config.")]
        public void RestoreState_OverridesStock()
        {
            m_shop.RestoreState(new JObject { ["potion"] = 1, ["sword"] = 0 });

            Assert.That(StockOf(m_potion), Is.EqualTo(1));
            Assert.That(StockOf(m_sword), Is.EqualTo(0));
            Assert.That(StockOf(m_ore), Is.EqualTo(3), "unmentioned items are re-seeded from the config");
        }

        [Test]
        [Description("Saved IDs that are unknown, or that the shop doesn't stock, are ignored.")]
        public void RestoreState_UnknownOrUnstockedItems_AreIgnored()
        {
            m_shop.RestoreState(new JObject { ["nope"] = 9, ["junk"] = 9 });

            Assert.That(m_shop.CaptureState().Count(), Is.EqualTo(3));
        }

        [Test]
        [Description("A non-object state resets stock to the config's initial values.")]
        public void RestoreState_NonObjectState_JustReseedsFromConfig()
        {
            m_shop.AddToPurchase(m_potion, 1);
            m_shop.CompletePurchase();
            Assert.That(StockOf(m_potion), Is.EqualTo(4));

            m_shop.RestoreState(JValue.CreateNull());

            Assert.That(StockOf(m_potion), Is.EqualTo(5));
        }

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

        [Test]
        [Description("RestoreState doesn't re-clamp a pending purchase, so completing it can buy more than the stock and drive stock negative.")]
        public void RestoreState_LeavesPurchaseAlone_AndOverBuyMakesStockNegative()
        {
            m_shop.AddToPurchase(m_potion, 3);
            m_shop.RestoreState(new JObject { ["potion"] = 1 });
            Assert.That(Listed(m_potion).QuantityToBuy, Is.EqualTo(3), "purchase still 3 although stock is 1");
            Assert.That(m_shop.PurchaseTotal(), Is.EqualTo(30));

            m_shop.CompletePurchase();

            Assert.That(Owned(m_potion), Is.EqualTo(3), "bought 3 from a stock of 1");
            Assert.That(StockOf(m_potion), Is.EqualTo(-2));
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(70));
        }

        /* --- Scene presentation --- */

        [Test]
        [Description("ToggleHighlight enables and disables the shop's Outline component.")]
        public void ToggleHighlight_EnablesAndDisablesTheOutline()
        {
            Outline outline = m_shop.GetComponent<Outline>();

            m_shop.ToggleHighlight(true);
            Assert.That(outline.enabled, Is.True);

            m_shop.ToggleHighlight(false);
            Assert.That(outline.enabled, Is.False);
        }

        [Test]
        [Description("ToggleLabel activates and deactivates the world label's root object.")]
        public void ToggleLabel_ShowsAndHidesTheLabelRoot()
        {
            m_shop.ToggleLabel(false);
            Assert.That(m_labelRoot.activeSelf, Is.False);

            m_shop.ToggleLabel(true);
            Assert.That(m_labelRoot.activeSelf, Is.True);
        }

        [Test]
        [Description("Hovering a shop shows the targeting cursor.")]
        public void GetCursorType_IsTargeting()
        {
            Assert.That(m_shop.GetCursorType(), Is.EqualTo(PolyQuest.UI.Core.CursorSettings.CursorType.kTargeting));
        }
    }
}
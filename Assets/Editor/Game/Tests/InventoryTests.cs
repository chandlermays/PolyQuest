/*---------------------------
| File: InventoryTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Pickups;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    public class InventoryTests
    {
        private const int kSlots = 4;

        private InventoryTestKit m_kit;
        private GameObject m_gameObject;
        private Inventory m_inventory;
        private Wallet m_wallet;

        private EquipableItem m_sword, m_shield, m_helmet, m_boots, m_scroll;
        private ActionItem m_potion, m_arrow;

        private int m_changedCount;
        private readonly List<(InventoryItem item, int quantity)> m_added = new();

        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();

            m_sword = m_kit.Item<EquipableItem>("sword");
            m_shield = m_kit.Item<EquipableItem>("shield");
            m_helmet = m_kit.Item<EquipableItem>("helmet");
            m_boots = m_kit.Item<EquipableItem>("boots");
            m_scroll = m_kit.Item<EquipableItem>("scroll");
            m_potion = m_kit.Item<ActionItem>("potion", stackable: true);
            m_arrow = m_kit.Item<ActionItem>("arrow", stackable: true);

            m_gameObject = m_kit.NewObject("InventoryTestSubject");
            m_inventory = m_kit.NewInventory(m_gameObject, kSlots);
            m_wallet = m_gameObject.AddComponent<Wallet>();

            m_changedCount = 0;
            m_added.Clear();
            m_inventory.OnInventoryChanged += () => ++m_changedCount;
            m_inventory.OnItemAdded += (item, quantity) => m_added.Add((item, quantity));
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        private void FillWithDistinctItems()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            m_inventory.TryAddToAvailableSlot(m_shield);
            m_inventory.TryAddToAvailableSlot(m_helmet);
            m_inventory.TryAddToAvailableSlot(m_boots);
            ResetEventCounts();
        }

        private void ResetEventCounts()
        {
            m_changedCount = 0;
            m_added.Clear();
        }

        // The empty-slot bookkeeping is derived state; it should always agree with what the slots actually hold
        private void AssertDerivedStateIsConsistent()
        {
            int emptyBySlots = 0;
            for (int i = 0; i < m_inventory.Size; ++i)
            {
                if (m_inventory.GetItemAtSlot(i) == null)
                    ++emptyBySlots;
            }

            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(emptyBySlots));
        }

        private static JObject Entry(string id, int quantity)
        {
            return new JObject { ["item"] = id, ["quantity"] = quantity };
        }

        private Inventory NewOtherInventory()
        {
            return m_kit.NewInventory(m_kit.NewObject("OtherInventory"), kSlots);
        }

        /* ============================== Adding ============================== */

        [Test]
        [Description("A new inventory has the configured size, and every slot is empty with quantity 0.")]
        public void NewInventory_IsEmpty()
        {
            Assert.That(m_inventory.Size, Is.EqualTo(kSlots));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(0));
        }

        [Test]
        [Description("Non-stackable items fill the lowest-index empty slot first, with quantity 1.")]
        public void TryAdd_NonStackable_FillsLowestEmptySlotFirst()
        {
            Assert.That(m_inventory.TryAddToAvailableSlot(m_sword), Is.True);
            Assert.That(m_inventory.TryAddToAvailableSlot(m_shield), Is.True);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_shield));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(1));
            AssertDerivedStateIsConsistent();
        }

        [Test]
        [Description("Adding the same non-stackable item twice uses two separate slots.")]
        public void TryAdd_SameNonStackableTwice_UsesTwoSlots()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 2));
        }

        [Test]
        [Description("Adding a stackable item that's already held merges into the existing stack.")]
        public void TryAdd_Stackable_MergesIntoExistingStack()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 3);
            m_inventory.TryAddToAvailableSlot(m_potion, 2);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(5));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 1));
            AssertDerivedStateIsConsistent();
        }

        [Test]
        [Description("Adding a non-stackable item to a full inventory returns false and changes nothing, raising no events.")]
        public void TryAdd_FullInventory_NonStackable_ReturnsFalse_AndChangesNothing()
        {
            FillWithDistinctItems();

            Assert.That(m_inventory.TryAddToAvailableSlot(m_scroll), Is.False);

            Assert.That(m_inventory.ContainsItem(m_scroll), Is.False);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(0));
            Assert.That(m_changedCount, Is.EqualTo(0));
            Assert.That(m_added, Is.Empty);
        }

        [Test]
        [Description("In a full inventory, a stackable item that's already held still merges into its stack.")]
        public void TryAdd_FullInventory_StackableAlreadyHeld_StillMerges()
        {
            m_inventory.TryAddToAvailableSlot(m_potion);
            m_inventory.TryAddToAvailableSlot(m_shield);
            m_inventory.TryAddToAvailableSlot(m_helmet);
            m_inventory.TryAddToAvailableSlot(m_boots);

            Assert.That(m_inventory.TryAddToAvailableSlot(m_potion, 2), Is.True);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
        }

        [Test]
        [Description("Adding with quantity 0 is a no-op: no slot is occupied and no OnItemAdded is raised.")]
        public void TryAdd_ZeroQuantity_DoesNotOccupyASlot()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 0);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null, "a zero-quantity add occupied a slot");
            Assert.That(m_inventory.ContainsItem(m_potion), Is.False);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
            Assert.That(m_added, Is.Empty, "OnItemAdded reported an add of nothing");
        }

        [Test]
        [Description("Adding a null item throws NullReferenceException.")]
        public void TryAdd_NullItem_ThrowsNullReference()
        {
            Assert.Throws<System.NullReferenceException>(() => m_inventory.TryAddToAvailableSlot(null));
        }

        /* --- Events on Add --- */

        [Test]
        [Description("A successful add raises OnItemAdded (with the item and quantity) and OnInventoryChanged once each.")]
        public void TryAdd_RaisesItemAdded_AndInventoryChanged_OnceEach()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_added, Has.Count.EqualTo(1));
            Assert.That(m_added[0].item, Is.SameAs(m_sword));
            Assert.That(m_added[0].quantity, Is.EqualTo(1));
            Assert.That(m_changedCount, Is.EqualTo(1));
        }

        [Test]
        [Description("SuppressItemAddedNotif silences OnItemAdded but not OnInventoryChanged.")]
        public void SuppressItemAddedNotif_SilencesItemAdded_ButNotInventoryChanged()
        {
            m_inventory.SuppressItemAddedNotif = true;

            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_added, Is.Empty);
            Assert.That(m_changedCount, Is.EqualTo(1));
        }

        [Test]
        [Description("When OnBeforeAddItem consumes a stackable add, no slot is used and OnInventoryChanged doesn't fire, but OnItemAdded still does.")]
        public void OnBeforeAddItem_ConsumingAStackable_SkipsSlotsAndInventoryChanged()
        {
            m_inventory.OnBeforeAddItem = (item, quantity) => true;

            bool result = m_inventory.TryAddToAvailableSlot(m_potion, 3);

            Assert.That(result, Is.True);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
            Assert.That(m_added, Has.Count.EqualTo(1));
            Assert.That(m_added[0].quantity, Is.EqualTo(3));
            Assert.That(m_changedCount, Is.EqualTo(0));
        }

        [Test]
        [Description("OnBeforeAddItem is only consulted for stackable items; non-stackables go straight into a slot.")]
        public void OnBeforeAddItem_IsNeverAskedAboutNonStackables()
        {
            int calls = 0;
            m_inventory.OnBeforeAddItem = (item, quantity) => { ++calls; return true; };

            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(calls, Is.EqualTo(0));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
        }

        [Test]
        [Description("When OnBeforeAddItem returns false, the add proceeds normally into a slot.")]
        public void OnBeforeAddItem_ReturningFalse_FallsThroughToNormalAdd()
        {
            int calls = 0;
            m_inventory.OnBeforeAddItem = (item, quantity) => { ++calls; return false; };

            m_inventory.TryAddToAvailableSlot(m_potion, 3);

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
            Assert.That(m_changedCount, Is.EqualTo(1));
        }

        /* ============================ TryAddItemToSlot ============================ */

        [Test]
        [Description("TryAddItemToSlot places the item in the requested empty slot, and later adds still fill the lowest free slot.")]
        public void TryAddItemToSlot_EmptySlot_PlacesItemThere()
        {
            Assert.That(m_inventory.TryAddItemToSlot(2, m_sword, 1), Is.True);

            Assert.That(m_inventory.GetItemAtSlot(2), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 1));

            m_inventory.TryAddToAvailableSlot(m_shield);
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_shield));
            AssertDerivedStateIsConsistent();
        }

        [Test]
        [Description("If the requested slot holds a different item, the item goes to the first available slot instead.")]
        public void TryAddItemToSlot_OccupiedByDifferentItem_FallsBackToFirstAvailableSlot()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.TryAddItemToSlot(0, m_shield, 1), Is.True);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_shield));
        }

        [Test]
        [Description("If the requested slot holds a different item and the inventory is full, TryAddItemToSlot returns false.")]
        public void TryAddItemToSlot_OccupiedByDifferentItem_WhenFull_ReturnsFalse()
        {
            FillWithDistinctItems();

            Assert.That(m_inventory.TryAddItemToSlot(0, m_scroll, 1), Is.False);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
        }

        [Test]
        [Description("Adding a stackable item to a slot already holding it stacks in that slot.")]
        public void TryAddItemToSlot_SameStackable_StacksInThatSlot()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 2);

            Assert.That(m_inventory.TryAddItemToSlot(0, m_potion, 3), Is.True);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(5));
        }

        [Test]
        [Description("A non-stackable item can't share a slot with a copy of itself; like any occupied slot, it goes to the first available slot instead.")]
        public void TryAddItemToSlot_SameNonStackable_FallsBackToFirstAvailableSlot()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            m_inventory.TryAddItemToSlot(0, m_sword, 1);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(1), "two non-stackable swords were stacked in one slot");
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(1));
            AssertDerivedStateIsConsistent();
        }

        [Test]
        [Description("Placing a held stackable item in a different empty slot creates a second stack of it.")]
        public void TryAddItemToSlot_StackableIntoEmptySlot_CreatesSecondStack()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 2);

            m_inventory.TryAddItemToSlot(2, m_potion, 1);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(2));
            Assert.That(m_inventory.GetQuantityAtSlot(2), Is.EqualTo(1));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 2));
        }

        [Test]
        [Description("TryAddItemToSlot doesn't consult OnBeforeAddItem.")]
        public void TryAddItemToSlot_BypassesOnBeforeAddItem()
        {
            int calls = 0;
            m_inventory.OnBeforeAddItem = (item, quantity) => { ++calls; return true; };

            m_inventory.TryAddItemToSlot(1, m_potion, 2);

            Assert.That(calls, Is.EqualTo(0));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(2));
        }

        [TestCase(kSlots)]
        [TestCase(-1)]
        [Description("A slot index outside the inventory throws IndexOutOfRangeException.")]
        public void TryAddItemToSlot_InvalidSlot_Throws(int slot)
        {
            Assert.Throws<System.IndexOutOfRangeException>(() => m_inventory.TryAddItemToSlot(slot, m_sword, 1));
        }

        [Test]
        [Description("With two stacks of an item, new units merge into the stack created first, not the lowest-index one.")]
        public void StackMerge_GoesToFirstRegisteredStack_NotLowestIndex()
        {
            m_inventory.TryAddItemToSlot(3, m_potion, 1);
            m_inventory.TryAddItemToSlot(1, m_potion, 1);

            m_inventory.TryAddToAvailableSlot(m_potion, 1);

            Assert.That(m_inventory.GetQuantityAtSlot(3), Is.EqualTo(2));
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(1));
        }

        /* ============================== Removal ============================== */

        [Test]
        [Description("Removing part of a stack leaves the remainder and raises OnInventoryChanged once.")]
        public void RemoveItemsFromSlot_Partial_LeavesTheRemainder()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 5);
            ResetEventCounts();

            m_inventory.RemoveItemsFromSlot(0, 2);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
            Assert.That(m_inventory.ContainsItem(m_potion), Is.True);
            Assert.That(m_changedCount, Is.EqualTo(1));
        }

        [Test]
        [Description("Removing the exact stack size empties the slot and the item is no longer held.")]
        public void RemoveItemsFromSlot_ExactAmount_EmptiesTheSlot()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 3);

            m_inventory.RemoveItemsFromSlot(0, 3);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(0));
            Assert.That(m_inventory.ContainsItem(m_potion), Is.False);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
            AssertDerivedStateIsConsistent();
        }

        [Test]
        [Description("Removing more than the stack holds is clamped silently and empties the slot.")]
        public void RemoveItemsFromSlot_MoreThanHeld_SilentlyClampsToTheStack()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 2);

            Assert.DoesNotThrow(() => m_inventory.RemoveItemsFromSlot(0, 10));

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }

        [Test]
        [Description("Removing from an empty slot changes nothing and raises no event.")]
        public void RemoveItemsFromSlot_EmptySlot_DoesNothing_AndRaisesNoEvent()
        {
            m_inventory.RemoveItemsFromSlot(2, 1);

            Assert.That(m_changedCount, Is.EqualTo(0));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }

        [Test]
        [Description("Emptying one of two stacks keeps the item tracked, and new units merge into the remaining stack.")]
        public void RemoveItemsFromSlot_EmptyingOneOfTwoStacks_KeepsTheItemTracked()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 1);
            m_inventory.TryAddItemToSlot(2, m_potion, 4);

            m_inventory.RemoveItemsFromSlot(0, 1);

            Assert.That(m_inventory.ContainsItem(m_potion), Is.True);

            m_inventory.TryAddToAvailableSlot(m_potion, 1);
            Assert.That(m_inventory.GetQuantityAtSlot(2), Is.EqualTo(5));
            AssertDerivedStateIsConsistent();
        }

        [Test]
        [Description("A slot freed by removal is the next one filled.")]
        public void RemovedSlot_IsTheNextOneReused()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            m_inventory.TryAddToAvailableSlot(m_shield);

            m_inventory.RemoveItemsFromSlot(0, 1);
            m_inventory.TryAddToAvailableSlot(m_helmet);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_helmet));
        }

        [Test]
        [Description("Removing from a slot index outside the inventory throws IndexOutOfRangeException.")]
        public void RemoveItemsFromSlot_InvalidSlot_Throws()
        {
            Assert.Throws<System.IndexOutOfRangeException>(() => m_inventory.RemoveItemsFromSlot(kSlots, 1));
        }

        /* ============================== HasSpaceFor ============================== */

        [Test]
        [Description("An empty inventory has space for any single item.")]
        public void HasSpaceFor_Single_EmptyInventory_IsTrue()
        {
            Assert.That(m_inventory.HasSpaceFor(m_sword), Is.True);
        }

        [Test]
        [Description("A full inventory has no space for a non-stackable item, even one it already holds.")]
        public void HasSpaceFor_Single_FullInventory_NonStackable_IsFalse_EvenIfHeld()
        {
            FillWithDistinctItems();

            Assert.That(m_inventory.HasSpaceFor(m_scroll), Is.False);
            Assert.That(m_inventory.HasSpaceFor(m_sword), Is.False);
        }

        [Test]
        [Description("A full inventory has space for a stackable item only if it already holds a stack of it.")]
        public void HasSpaceFor_Single_FullInventory_Stackable_OnlyIfAlreadyHeld()
        {
            m_inventory.TryAddToAvailableSlot(m_potion);
            m_inventory.TryAddToAvailableSlot(m_shield);
            m_inventory.TryAddToAvailableSlot(m_helmet);
            m_inventory.TryAddToAvailableSlot(m_boots);

            Assert.That(m_inventory.HasSpaceFor(m_potion), Is.True);
            Assert.That(m_inventory.HasSpaceFor(m_arrow), Is.False);
        }

        [Test]
        [Description("For a set of items, each non-stackable entry (duplicates included) needs its own empty slot.")]
        public void HasSpaceFor_Many_CountsEachNonStackableSeparately()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            m_inventory.TryAddToAvailableSlot(m_shield);       // 2 empty slots remain

            Assert.That(m_inventory.HasSpaceFor(new InventoryItem[] { m_helmet, m_boots }), Is.True);
            Assert.That(m_inventory.HasSpaceFor(new InventoryItem[] { m_helmet, m_boots, m_scroll }), Is.False);
            Assert.That(m_inventory.HasSpaceFor(new InventoryItem[] { m_helmet, m_helmet }), Is.True);
            Assert.That(m_inventory.HasSpaceFor(new InventoryItem[] { m_helmet, m_helmet, m_helmet }), Is.False);
        }

        [Test]
        [Description("For a set of items, each distinct new stackable needs one slot, and stackables already held need none.")]
        public void HasSpaceFor_Many_CountsEachNewStackableOnce_AndHeldStackablesNotAtAll()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            m_inventory.TryAddToAvailableSlot(m_shield);
            m_inventory.TryAddToAvailableSlot(m_helmet);       // 1 empty slot remains

            Assert.That(m_inventory.HasSpaceFor(new InventoryItem[] { m_arrow, m_arrow, m_arrow }), Is.True);
            Assert.That(m_inventory.HasSpaceFor(new InventoryItem[] { m_arrow, m_potion }), Is.False);

            m_inventory.TryAddToAvailableSlot(m_potion);       // now full, potion is held
            Assert.That(m_inventory.HasSpaceFor(new InventoryItem[] { m_potion, m_potion }), Is.True);
        }

        [Test]
        [Description("HasSpaceFor with a null item throws NullReferenceException.")]
        public void HasSpaceFor_NullItem_ThrowsNullReference()
        {
            Assert.Throws<System.NullReferenceException>(() => m_inventory.HasSpaceFor((InventoryItem)null));
        }

        /* ============================== Evaluate ============================== */

        [Test]
        [Description("Evaluate returns null (unhandled) for null or empty parameters.")]
        public void Evaluate_NullOrEmptyParameters_ReturnsNull()
        {
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItem, null), Is.Null);
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItem, new string[0]), Is.Null);
        }

        [Test]
        [Description("Evaluate returns null for predicates the inventory doesn't handle.")]
        public void Evaluate_UnhandledPredicate_ReturnsNull()
        {
            Assert.That(m_inventory.Evaluate(ConditionType.kHasQuest, new[] { "anything" }), Is.Null);
        }

        [Test]
        [Description("kHasItem is true for a held item's ID and false for a registered item that isn't held.")]
        public void HasItem_ReflectsWhetherTheItemIsHeld()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.Evaluate(ConditionType.kHasItem, new[] { m_sword.ID }), Is.True);
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItem, new[] { m_shield.ID }), Is.False);
        }

        [Test]
        [Description("kHasItem with an unregistered item ID returns false instead of throwing.")]
        public void HasItem_UnknownItemId_IsFalse_NotAnException()
        {
            bool? result = null;

            Assert.DoesNotThrow(() => result = m_inventory.Evaluate(ConditionType.kHasItem, new[] { "missing" }),
                                "kHasItem threw on an unknown item ID");

            Assert.That(result, Is.False);
        }

        [Test]
        [Description("kHasItems sums the quantity across every stack of the item and compares it to the required amount.")]
        public void HasItems_SumsQuantityAcrossStacks()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 2);
            m_inventory.TryAddItemToSlot(2, m_potion, 3);

            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, "1" }), Is.True);
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, "5" }), Is.True);
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, "6" }), Is.False);
        }

        [Test]
        [Description("kHasItems is false for an item that isn't held.")]
        public void HasItems_ItemNotHeld_IsFalse()
        {
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, "1" }), Is.False);
        }

        [Test]
        [Description("kHasItems is always false for non-stackable items, even when held.")]
        public void HasItems_NonStackable_IsAlwaysFalse()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_sword.ID, "1" }), Is.False);
        }

        [Test]
        [Description("kHasItems with an unregistered item ID returns false.")]
        public void HasItems_UnknownItemId_IsFalse()
        {
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { "missing", "1" }), Is.False);
        }

        [TestCase("0")]
        [TestCase("-1")]
        [TestCase("abc")]
        [TestCase("")]
        [Description("kHasItems is false when the required quantity is zero, negative, or not a number.")]
        public void HasItems_InvalidRequiredQuantity_IsFalse(string quantity)
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 5);

            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, quantity }), Is.False);
        }

        [Test]
        [Description("kHasItems without a quantity parameter returns false instead of throwing.")]
        public void HasItems_MissingQuantityParameter_IsFalse_NotAnException()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 5);
            bool? result = null;

            Assert.DoesNotThrow(() => result = m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID }),
                                "kHasItems threw when the quantity parameter was missing");

            Assert.That(result, Is.False);
        }

        /* ============================== Save / Restore ============================== */

        [Test]
        [Description("Capturing an empty inventory produces an empty JSON object.")]
        public void CaptureState_EmptyInventory_IsAnEmptyObject()
        {
            JObject state = (JObject)m_inventory.CaptureState();

            Assert.That(state.Properties().Count(), Is.EqualTo(0));
        }

        [Test]
        [Description("CaptureState stores only occupied slots, keyed by slot index, each with item ID and quantity.")]
        public void CaptureState_OnlyOccupiedSlots_KeyedBySlotIndex()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            m_inventory.TryAddItemToSlot(2, m_potion, 5);

            JObject state = (JObject)m_inventory.CaptureState();

            Assert.That(state.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "0", "2" }));
            Assert.That(state["0"]["item"].ToObject<string>(), Is.EqualTo(m_sword.ID));
            Assert.That(state["2"]["item"].ToObject<string>(), Is.EqualTo(m_potion.ID));
            Assert.That(state["2"]["quantity"].ToObject<int>(), Is.EqualTo(5));
        }

        [Test]
        [Description("Capture and restore through JSON text preserves each item, its slot, its quantity, and the empty-slot count.")]
        public void RoundTrip_ThroughJsonText_PreservesSlotsAndQuantities()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            m_inventory.TryAddItemToSlot(3, m_potion, 7);
            Inventory other = NewOtherInventory();

            // Through text, the way SavingSystem writes and reads it
            other.RestoreState(JToken.Parse(m_inventory.CaptureState().ToString()));

            Assert.That(other.GetItemAtSlot(0), Is.SameAs(m_sword));
            Assert.That(other.GetItemAtSlot(3), Is.SameAs(m_potion));
            Assert.That(other.GetQuantityAtSlot(3), Is.EqualTo(7));
            Assert.That(other.ContainsItem(m_potion), Is.True);
            Assert.That(other.GetEmptySlotCount(), Is.EqualTo(kSlots - 2));
        }

        [Test]
        [Description("RestoreState raises OnInventoryChanged once and never raises OnItemAdded.")]
        public void RestoreState_RaisesInventoryChanged_ButNotItemAdded()
        {
            m_inventory.RestoreState(new JObject { ["1"] = Entry(m_sword.ID, 1) });

            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_sword));
            Assert.That(m_changedCount, Is.EqualTo(1));
            Assert.That(m_added, Is.Empty);
            AssertDerivedStateIsConsistent();
        }

        [Test]
        [Description("RestoreState replaces the current contents rather than merging with them.")]
        public void RestoreState_ReplacesExistingContents()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            m_inventory.RestoreState(new JObject { ["1"] = Entry(m_potion.ID, 2) });

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.ContainsItem(m_sword), Is.False);
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(2));
        }

        [Test]
        [Description("A null or non-object state is ignored: contents are kept and no event fires.")]
        public void RestoreState_NonObject_IsIgnored()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            ResetEventCounts();

            m_inventory.RestoreState(null);
            m_inventory.RestoreState(new JValue(5));

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
            Assert.That(m_changedCount, Is.EqualTo(0));
        }

        [Test]
        [Description("A saved slot index beyond the inventory's size is silently dropped.")]
        public void RestoreState_SlotBeyondInventorySize_IsSilentlyDropped()
        {
            m_inventory.RestoreState(new JObject { [kSlots.ToString()] = Entry(m_sword.ID, 1) });

            Assert.That(m_inventory.ContainsItem(m_sword), Is.False);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }

        [Test]
        [Description("An unknown item ID leaves the slot truly empty (quantity 0), so the next item placed there gets its own quantity.")]
        public void RestoreState_UnknownItemId_LeavesNoGhostQuantity()
        {
            m_inventory.RestoreState(new JObject { ["0"] = Entry("gone", 7) });

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(0), "the empty slot holds a ghost quantity");
            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);

            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(1), "the next item inherited the ghost quantity");
        }

        [Test]
        [Description("Restoring an unknown item ID logs a warning that names the missing ID.")]
        public void RestoreState_UnknownItemId_LogsAWarningNamingIt()
        {
            LogAssert.Expect(LogType.Warning, new Regex("gone"));

            m_inventory.RestoreState(new JObject { ["0"] = Entry("gone", 3) });
        }

        [Test]
        [Description("A malformed entry is skipped without throwing; valid entries still load and listeners are notified.")]
        public void RestoreState_MalformedEntry_DoesNotThrow_AndLoadsTheRest()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            ResetEventCounts();
            JObject state = new JObject
            {
                ["0"] = new JObject { ["quantity"] = 1 },          // malformed: no "item"
                ["1"] = Entry(m_potion.ID, 2)                      // valid
            };

            Assert.DoesNotThrow(() => m_inventory.RestoreState(state), "a malformed entry aborted the restore");

            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(2));
            Assert.That(m_changedCount, Is.GreaterThan(0), "listeners were never told the inventory changed");
        }

        [Test]
        [Description("After save and load, the stack that receives merges changes from first-created to lowest-index.")]
        public void StackMergeTarget_ChangesAfterSaveAndLoad()
        {
            m_inventory.TryAddItemToSlot(3, m_potion, 1);
            m_inventory.TryAddItemToSlot(1, m_potion, 1);
            Inventory reloaded = NewOtherInventory();
            reloaded.RestoreState(m_inventory.CaptureState());

            m_inventory.TryAddToAvailableSlot(m_potion, 1);
            reloaded.TryAddToAvailableSlot(m_potion, 1);

            Assert.That(m_inventory.GetQuantityAtSlot(3), Is.EqualTo(2));
            Assert.That(reloaded.GetQuantityAtSlot(1), Is.EqualTo(2));
        }

        /* ============================== Wallet / CurrencyItem ============================== */

        [Test]
        [Description("A new wallet holds 0 silver.")]
        public void Wallet_StartsAtZero()
        {
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(0));
        }

        [Test]
        [Description("UpdateSilver adds to the balance and raises OnWalletUpdated once.")]
        public void Wallet_UpdateSilver_Adds_AndNotifies()
        {
            int notifications = 0;
            m_wallet.OnWalletUpdated += () => ++notifications;

            m_wallet.UpdateSilver(25);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(25));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        [Description("UpdateSilver lets the balance go negative.")]
        public void Wallet_UpdateSilver_AllowsANegativeBalance()
        {
            m_wallet.UpdateSilver(-10);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(-10));
        }

        [Test]
        [Description("Capturing and restoring a wallet preserves the balance and raises OnWalletUpdated once.")]
        public void Wallet_RoundTrip_RestoresBalance_AndNotifies()
        {
            m_wallet.UpdateSilver(40);
            Wallet other = m_kit.NewObject("OtherWallet").AddComponent<Wallet>();
            int notifications = 0;
            other.OnWalletUpdated += () => ++notifications;

            other.RestoreState(m_wallet.CaptureState());

            Assert.That(other.CurrentSilver, Is.EqualTo(40));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        [Description("Picking up currency adds to the wallet, not to an inventory slot, and raises no OnItemAdded.")]
        public void CurrencyItem_OnPickup_GoesToTheWallet_NotTheSlots()
        {
            CurrencyItem coin = m_kit.Item<CurrencyItem>("coin", stackable: true);

            coin.OnPickup(m_inventory, 15);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(15));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
            Assert.That(m_added, Is.Empty);
        }

        [Test]
        [Description("Currency can be picked up even when the inventory is full.")]
        public void CurrencyItem_OnPickup_WorksOnAFullInventory()
        {
            CurrencyItem coin = m_kit.Item<CurrencyItem>("coin", stackable: true);
            FillWithDistinctItems();

            coin.OnPickup(m_inventory, 15);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(15));
        }

        /* ============================== InventoryItem ============================== */

        [Test]
        [Description("An item's default OnPickup adds it to the inventory.")]
        public void ItemOnPickup_Default_AddsToTheInventory()
        {
            m_sword.OnPickup(m_inventory, 1);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
        }

        [Test]
        [Description("Picking up an item with a full inventory silently does nothing: no error, no event.")]
        public void ItemOnPickup_FullInventory_SilentlyDoesNothing()
        {
            FillWithDistinctItems();

            Assert.DoesNotThrow(() => m_scroll.OnPickup(m_inventory, 1));

            Assert.That(m_inventory.ContainsItem(m_scroll), Is.False);
            Assert.That(m_changedCount, Is.EqualTo(0));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("missing")]
        [Description("FindByID returns null for a null, empty, or unregistered ID.")]
        public void FindByID_NullEmptyOrUnknown_ReturnsNull(string id)
        {
            Assert.That(InventoryItem.FindByID(id), Is.Null);
        }

        [Test]
        [Description("FindByID returns the same registered instance for a known ID.")]
        public void FindByID_KnownId_ReturnsTheSameInstance()
        {
            Assert.That(InventoryItem.FindByID(m_potion.ID), Is.SameAs(m_potion));
        }

        [Test]
        [Description("A blank item ID is replaced with a GUID on serialization, and that GUID stays the same on later serializations.")]
        public void ItemID_BlankIdIsReplacedWithAGuid_WhenSerialized_AndThenStaysStable()
        {
            EquipableItem item = m_kit.Track(ScriptableObject.CreateInstance<EquipableItem>());
            SerializedTestUtil.SetString(item, "m_itemID", "   ");

            item.OnBeforeSerialize();
            string first = item.ID;
            item.OnBeforeSerialize();

            Assert.That(System.Guid.TryParse(first, out _), Is.True);
            Assert.That(item.ID, Is.EqualTo(first));
        }

        /* ============================== ActionItem.OnUse ============================== */

        [Test]
        [Description("Using a non-consumable ActionItem leaves its stack unchanged.")]
        public void ActionItem_OnUse_NonConsumable_IsNotConsumed()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 3);
            Assert.That(m_potion.IsConsumable, Is.False, "fixture: the potion must be non-consumable");

            m_potion.OnUse(m_inventory, 0);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3), "a non-consumable item was consumed");
        }

        [Test]
        [Description("Using a consumable ActionItem removes one from its stack.")]
        public void ActionItem_OnUse_Consumable_ConsumesOne()
        {
            ActionItem elixir = m_kit.Item<ActionItem>("elixir", stackable: true);
            SerializedTestUtil.SetBool(elixir, "m_isConsumable", true);
            m_inventory.TryAddToAvailableSlot(elixir, 3);

            elixir.OnUse(m_inventory, 0);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(2));
        }

        [Test]
        [Description("Using the last unit of a consumable ActionItem empties the slot.")]
        public void ActionItem_OnUse_LastConsumable_EmptiesTheSlot()
        {
            ActionItem elixir = m_kit.Item<ActionItem>("elixir", stackable: true);
            SerializedTestUtil.SetBool(elixir, "m_isConsumable", true);
            m_inventory.TryAddToAvailableSlot(elixir, 1);

            elixir.OnUse(m_inventory, 0);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }
    }
}
/*---------------------------
| File: InventoryTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
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
        public void NewInventory_IsEmpty()
        {
            Assert.That(m_inventory.Size, Is.EqualTo(kSlots));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(0));
        }

        [Test]
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
        public void TryAdd_SameNonStackableTwice_UsesTwoSlots()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 2));
        }

        [Test]
        public void TryAdd_Stackable_MergesIntoExistingStack()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 3);
            m_inventory.TryAddToAvailableSlot(m_potion, 2);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(5));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 1));
            AssertDerivedStateIsConsistent();
        }

        [Test]
        [Description("[Characterizes quirk] Stacks have no maximum size, and the quantity is an unchecked int: it wraps negative on overflow.")]
        public void TryAdd_Stackable_HasNoCap_AndOverflowsToNegative()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, int.MaxValue);
            m_inventory.TryAddToAvailableSlot(m_potion, 1);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(int.MinValue));
        }

        [Test]
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
        [Description("[Characterizes quirk] A non-stackable item added with quantity > 1 occupies ONE slot holding that quantity.")]
        public void TryAdd_NonStackable_WithQuantityAboveOne_OccupiesSingleSlot()
        {
            m_inventory.TryAddToAvailableSlot(m_sword, 3);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 1));
        }

        [Test]
        [Description("[Characterizes quirk] Quantity is never validated. Adding 0 succeeds and leaves an occupied slot holding a quantity of 0.")]
        public void TryAdd_ZeroQuantity_CreatesAnOccupiedZeroQuantitySlot()
        {
            Assert.That(m_inventory.TryAddToAvailableSlot(m_potion, 0), Is.True);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(0));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 1));
            Assert.That(m_added, Has.Count.EqualTo(1));
            Assert.That(m_added[0].quantity, Is.EqualTo(0));
        }

        [Test]
        [Description("[Characterizes quirk] A negative quantity is accepted by Add and subtracts from the stack (and raises OnItemAdded with the negative number).")]
        public void TryAdd_NegativeQuantity_SubtractsFromStack()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 5);
            ResetEventCounts();

            Assert.That(m_inventory.TryAddToAvailableSlot(m_potion, -2), Is.True);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
            Assert.That(m_added, Has.Count.EqualTo(1));
            Assert.That(m_added[0].quantity, Is.EqualTo(-2));
        }

        [Test]
        [Description("[Characterizes quirk] A null item is not guarded; it throws a NullReferenceException.")]
        public void TryAdd_NullItem_ThrowsNullReference()
        {
            Assert.Throws<System.NullReferenceException>(() => m_inventory.TryAddToAvailableSlot(null));
        }

        /* --- Events on Add --- */

        [Test]
        public void TryAdd_RaisesItemAdded_AndInventoryChanged_OnceEach()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_added, Has.Count.EqualTo(1));
            Assert.That(m_added[0].item, Is.SameAs(m_sword));
            Assert.That(m_added[0].quantity, Is.EqualTo(1));
            Assert.That(m_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void SuppressItemAddedNotif_SilencesItemAdded_ButNotInventoryChanged()
        {
            m_inventory.SuppressItemAddedNotif = true;

            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_added, Is.Empty);
            Assert.That(m_changedCount, Is.EqualTo(1));
        }

        [Test]
        [Description("[Characterizes quirk] When OnBeforeAddItem consumes a stackable item, the slots are untouched and OnInventoryChanged is NOT raised.")]
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
        public void OnBeforeAddItem_IsNeverAskedAboutNonStackables()
        {
            int calls = 0;
            m_inventory.OnBeforeAddItem = (item, quantity) => { ++calls; return true; };

            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(calls, Is.EqualTo(0));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
        }

        [Test]
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
        public void TryAddItemToSlot_OccupiedByDifferentItem_FallsBackToFirstAvailableSlot()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.TryAddItemToSlot(0, m_shield, 1), Is.True);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_shield));
        }

        [Test]
        public void TryAddItemToSlot_OccupiedByDifferentItem_WhenFull_ReturnsFalse()
        {
            FillWithDistinctItems();

            Assert.That(m_inventory.TryAddItemToSlot(0, m_scroll, 1), Is.False);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
        }

        [Test]
        public void TryAddItemToSlot_SameStackable_StacksInThatSlot()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 2);

            Assert.That(m_inventory.TryAddItemToSlot(0, m_potion, 3), Is.True);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(5));
        }

        [Test]
        [Description("[Characterizes quirk] Placing a non-stackable item onto a slot that already holds the same item bumps that slot's quantity to 2.")]
        public void TryAddItemToSlot_SameNonStackable_BumpsQuantityInsteadOfRejecting()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.TryAddItemToSlot(0, m_sword, 1), Is.True);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(2));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 1));
        }

        [Test]
        [Description("[Characterizes quirk] Placing a stackable into an EMPTY slot while a stack of it exists elsewhere creates a second stack (drag-and-drop rearrangement).")]
        public void TryAddItemToSlot_StackableIntoEmptySlot_CreatesSecondStack()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 2);

            m_inventory.TryAddItemToSlot(2, m_potion, 1);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(2));
            Assert.That(m_inventory.GetQuantityAtSlot(2), Is.EqualTo(1));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots - 2));
        }

        [Test]
        [Description("[Characterizes quirk] TryAddItemToSlot never consults OnBeforeAddItem.")]
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
        [Description("[Characterizes quirk] Slot indices are not bounds-checked; they throw IndexOutOfRangeException.")]
        public void TryAddItemToSlot_InvalidSlot_Throws(int slot)
        {
            Assert.Throws<System.IndexOutOfRangeException>(() => m_inventory.TryAddItemToSlot(slot, m_sword, 1));
        }

        [Test]
        [Description("[Characterizes quirk] Merging goes to the stack REGISTERED first (insertion order), not the lowest slot index.")]
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
        [Description("[Characterizes quirk] Removing more than a stack holds silently removes the whole stack. There is no failure signal.")]
        public void RemoveItemsFromSlot_MoreThanHeld_SilentlyClampsToTheStack()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 2);

            Assert.DoesNotThrow(() => m_inventory.RemoveItemsFromSlot(0, 10));

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }

        [Test]
        public void RemoveItemsFromSlot_EmptySlot_DoesNothing_AndRaisesNoEvent()
        {
            m_inventory.RemoveItemsFromSlot(2, 1);

            Assert.That(m_changedCount, Is.EqualTo(0));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }

        [Test]
        [Description("[Characterizes quirk] A negative removal quantity ADDS to the stack, and raises OnInventoryChanged but not OnItemAdded.")]
        public void RemoveItemsFromSlot_NegativeQuantity_AddsToTheStack()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 2);
            ResetEventCounts();

            m_inventory.RemoveItemsFromSlot(0, -3);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(5));
            Assert.That(m_added, Is.Empty);
            Assert.That(m_changedCount, Is.EqualTo(1));
        }

        [Test]
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
        public void RemovedSlot_IsTheNextOneReused()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            m_inventory.TryAddToAvailableSlot(m_shield);

            m_inventory.RemoveItemsFromSlot(0, 1);
            m_inventory.TryAddToAvailableSlot(m_helmet);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_helmet));
        }

        [Test]
        [Description("[Characterizes quirk] RemoveItemsFromSlot does not bounds-check the slot index.")]
        public void RemoveItemsFromSlot_InvalidSlot_Throws()
        {
            Assert.Throws<System.IndexOutOfRangeException>(() => m_inventory.RemoveItemsFromSlot(kSlots, 1));
        }

        /* ============================== HasSpaceFor ============================== */

        [Test]
        public void HasSpaceFor_Single_EmptyInventory_IsTrue()
        {
            Assert.That(m_inventory.HasSpaceFor(m_sword), Is.True);
        }

        [Test]
        public void HasSpaceFor_Single_FullInventory_NonStackable_IsFalse_EvenIfHeld()
        {
            FillWithDistinctItems();

            Assert.That(m_inventory.HasSpaceFor(m_scroll), Is.False);
            Assert.That(m_inventory.HasSpaceFor(m_sword), Is.False);
        }

        [Test]
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
        [Description("[Characterizes quirk] HasSpaceFor(null) is not guarded; it throws a NullReferenceException.")]
        public void HasSpaceFor_NullItem_ThrowsNullReference()
        {
            Assert.Throws<System.NullReferenceException>(() => m_inventory.HasSpaceFor((InventoryItem)null));
        }

        /* ============================== Evaluate ============================== */

        [Test]
        public void Evaluate_NullOrEmptyParameters_ReturnsNull()
        {
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItem, null), Is.Null);
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItem, new string[0]), Is.Null);
        }

        [Test]
        public void Evaluate_UnhandledPredicate_ReturnsNull()
        {
            Assert.That(m_inventory.Evaluate(ConditionType.kHasQuest, new[] { "anything" }), Is.Null);
        }

        [Test]
        public void HasItem_ReflectsWhetherTheItemIsHeld()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.Evaluate(ConditionType.kHasItem, new[] { m_sword.ID }), Is.True);
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItem, new[] { m_shield.ID }), Is.False);
        }

        [Test]
        [Description("[Characterizes quirk] kHasItem with an ID that matches no item THROWS (ArgumentNullException) instead of returning false. A deleted/renamed item referenced by a dialogue or quest condition crashes the check.")]
        public void HasItem_UnknownItemId_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => m_inventory.Evaluate(ConditionType.kHasItem, new[] { "missing" }));
        }

        [Test]
        public void HasItems_SumsQuantityAcrossStacks()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 2);
            m_inventory.TryAddItemToSlot(2, m_potion, 3);

            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, "1" }), Is.True);
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, "5" }), Is.True);
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, "6" }), Is.False);
        }

        [Test]
        public void HasItems_ItemNotHeld_IsFalse()
        {
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, "1" }), Is.False);
        }

        [Test]
        [Description("[Characterizes quirk] kHasItems is stackable-only. A non-stackable item that IS held still returns false.")]
        public void HasItems_NonStackable_IsAlwaysFalse()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_sword.ID, "1" }), Is.False);
        }

        [Test]
        [Description("[Characterizes quirk] Unlike kHasItem, kHasItems returns false (no exception) for an unknown item ID.")]
        public void HasItems_UnknownItemId_IsFalse()
        {
            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { "missing", "1" }), Is.False);
        }

        [TestCase("0")]
        [TestCase("-1")]
        [TestCase("abc")]
        [TestCase("")]
        [Description("[Characterizes quirk] A required quantity <= 0 or non-numeric is false, even when the item is held (0 is trivially satisfiable but fails).")]
        public void HasItems_InvalidRequiredQuantity_IsFalse(string quantity)
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 5);

            Assert.That(m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID, quantity }), Is.False);
        }

        [Test]
        [Description("[Characterizes quirk] kHasItems with only the item ID (no quantity parameter) throws IndexOutOfRangeException.")]
        public void HasItems_MissingQuantityParameter_Throws()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 5);

            Assert.Throws<System.IndexOutOfRangeException>(() => m_inventory.Evaluate(ConditionType.kHasItems, new[] { m_potion.ID }));
        }

        /* ============================== Save / Restore ============================== */

        [Test]
        public void CaptureState_EmptyInventory_IsAnEmptyObject()
        {
            JObject state = (JObject)m_inventory.CaptureState();

            Assert.That(state.Properties().Count(), Is.EqualTo(0));
        }

        [Test]
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
        public void RestoreState_RaisesInventoryChanged_ButNotItemAdded()
        {
            m_inventory.RestoreState(new JObject { ["1"] = Entry(m_sword.ID, 1) });

            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_sword));
            Assert.That(m_changedCount, Is.EqualTo(1));
            Assert.That(m_added, Is.Empty);
            AssertDerivedStateIsConsistent();
        }

        [Test]
        public void RestoreState_ReplacesExistingContents()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            m_inventory.RestoreState(new JObject { ["1"] = Entry(m_potion.ID, 2) });

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.ContainsItem(m_sword), Is.False);
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(2));
        }

        [Test]
        [Description("[Characterizes quirk] A state that is not a JObject (null, a number...) is silently ignored: no change and no event.")]
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
        [Description("[Characterizes quirk] Saved slots at or beyond the current inventory size are silently dropped (shrinking the inventory loses items).")]
        public void RestoreState_SlotBeyondInventorySize_IsSilentlyDropped()
        {
            m_inventory.RestoreState(new JObject { [kSlots.ToString()] = Entry(m_sword.ID, 1) });

            Assert.That(m_inventory.ContainsItem(m_sword), Is.False);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }

        [Test]
        [Description("[Characterizes quirk] Restoring an item ID that no longer exists leaves the slot empty but KEEPS its saved quantity; the next item placed there inherits it.")]
        public void RestoreState_UnknownItemId_LeavesGhostQuantityInTheEmptySlot()
        {
            m_inventory.RestoreState(new JObject { ["0"] = Entry("gone", 7) });

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(7));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));

            m_inventory.TryAddToAvailableSlot(m_sword);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(8));
        }

        [Test]
        [Description("[Characterizes quirk] A malformed slot entry (no 'item') throws part-way through, AFTER the existing inventory was already wiped, and OnInventoryChanged is never raised.")]
        public void RestoreState_MalformedEntry_Throws_AfterWipingTheInventory()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);
            ResetEventCounts();
            JObject malformed = new JObject { ["0"] = new JObject { ["quantity"] = 1 } };

            Assert.That(() => m_inventory.RestoreState(malformed), Throws.Exception);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.ContainsItem(m_sword), Is.False);
            Assert.That(m_changedCount, Is.EqualTo(0));
        }

        [Test]
        [Description("[Characterizes quirk] Which stack a merge targets depends on registration order. Save/load rebuilds that order by slot index, so the same sequence of calls behaves differently after a reload.")]
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
        public void Wallet_StartsAtZero()
        {
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(0));
        }

        [Test]
        public void Wallet_UpdateSilver_Adds_AndNotifies()
        {
            int notifications = 0;
            m_wallet.OnWalletUpdated += () => ++notifications;

            m_wallet.UpdateSilver(25);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(25));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        [Description("[Characterizes quirk] The wallet has no floor: spending more than you have goes negative.")]
        public void Wallet_UpdateSilver_AllowsANegativeBalance()
        {
            m_wallet.UpdateSilver(-10);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(-10));
        }

        [Test]
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
        public void CurrencyItem_OnPickup_GoesToTheWallet_NotTheSlots()
        {
            CurrencyItem coin = m_kit.Item<CurrencyItem>("coin", stackable: true);

            coin.OnPickup(m_inventory, 15);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(15));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
            Assert.That(m_added, Is.Empty);
        }

        [Test]
        public void CurrencyItem_OnPickup_WorksOnAFullInventory()
        {
            CurrencyItem coin = m_kit.Item<CurrencyItem>("coin", stackable: true);
            FillWithDistinctItems();

            coin.OnPickup(m_inventory, 15);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(15));
        }

        /* ============================== InventoryItem ============================== */

        [Test]
        public void ItemOnPickup_Default_AddsToTheInventory()
        {
            m_sword.OnPickup(m_inventory, 1);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
        }

        [Test]
        [Description("[Characterizes quirk] OnPickup returns void: on a full inventory the item is dropped on the floor of the method with no signal. Callers must check HasSpaceFor first.")]
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
        public void FindByID_NullEmptyOrUnknown_ReturnsNull(string id)
        {
            Assert.That(InventoryItem.FindByID(id), Is.Null);
        }

        [Test]
        public void FindByID_KnownId_ReturnsTheSameInstance()
        {
            Assert.That(InventoryItem.FindByID(m_potion.ID), Is.SameAs(m_potion));
        }

        [Test]
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
        [Description("[Characterizes quirk] ActionItem.OnUse (inventory use) consumes one item even when IsConsumable is false. Only the action-bar path (Actions.cs) checks IsConsumable.")]
        public void ActionItem_OnUse_ConsumesOne_EvenWhenNotConsumable()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 3);
            Assert.That(m_potion.IsConsumable, Is.False);

            m_potion.OnUse(m_inventory, 0);

            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(2));
        }

        [Test]
        public void ActionItem_OnUse_LastItem_EmptiesTheSlot()
        {
            m_inventory.TryAddToAvailableSlot(m_potion, 1);

            m_potion.OnUse(m_inventory, 0);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }
    }
}
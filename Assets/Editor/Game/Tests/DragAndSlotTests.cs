/*---------------------------
| File: DragAndSlotTests.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.UI.Dragging;

namespace PolyQuest.Tests
{
    public class DragAndSlotTests
    {
        private const BindingFlags kAny = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private InventoryTestKit m_kit;
        private GameObject m_playerObject;
        private Inventory m_inventory;
        private Equipment m_equipment;
        private InventorySlotUI[] m_slots;
        private EquipmentSlotUI m_weaponSlot, m_helmetSlot;
        private InventoryDragItem m_drag;

        private EquipableItem m_sword, m_axe, m_helm;
        private ActionItem m_potion, m_herb, m_stone;

        private const int kSlotCount = 4;

        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();

            m_sword = m_kit.Equipable<EquipableItem>("sword", EquipmentSlot.kWeapon);
            m_axe = m_kit.Equipable<EquipableItem>("axe", EquipmentSlot.kWeapon);
            m_helm = m_kit.Equipable<EquipableItem>("helm", EquipmentSlot.kHelmet);
            m_potion = m_kit.Item<ActionItem>("potion", stackable: true);
            m_herb = m_kit.Item<ActionItem>("herb", stackable: true);
            m_stone = m_kit.Item<ActionItem>("stone", stackable: false);

            m_playerObject = m_kit.NewObject("Player");
            m_playerObject.AddComponent<PolyQuest.Attributes.BaseStats>();
            m_equipment = m_playerObject.AddComponent<Equipment>();
            InventoryTestKit.InvokeAwake(m_equipment);
            m_inventory = m_kit.NewInventory(m_playerObject, kSlotCount);

            m_slots = new InventorySlotUI[kSlotCount];
            for (int i = 0; i < kSlotCount; ++i)
            {
                m_slots[i] = m_kit.NewObject("Slot" + i).AddComponent<InventorySlotUI>();
                SerializedTestUtil.SetObject(m_slots[i], "m_itemIcon", NewIcon());
                m_slots[i].Setup(m_inventory, i);
            }

            m_weaponSlot = NewEquipmentSlot(EquipmentSlot.kWeapon);
            m_helmetSlot = NewEquipmentSlot(EquipmentSlot.kHelmet);

            m_drag = m_kit.NewObject("Drag").AddComponent<InventoryDragItem>();
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        private InventoryItemIcon NewIcon()
        {
            GameObject go = m_kit.NewObject("Icon");
            go.AddComponent<Image>();
            InventoryItemIcon icon = go.AddComponent<InventoryItemIcon>();
            InventoryTestKit.InvokeAwake(icon);
            return icon;
        }

        private EquipmentSlotUI NewEquipmentSlot(EquipmentSlot slot)
        {
            EquipmentSlotUI ui = m_kit.NewObject("Equip" + slot).AddComponent<EquipmentSlotUI>();
            SerializedTestUtil.SetObject(ui, "m_playerEquipment", m_equipment);
            SerializedTestUtil.SetObject(ui, "m_itemIcon", NewIcon());
            SerializedTestUtil.SetEnum(ui, "m_equipmentSlot", slot);
            SerializedTestUtil.SetObject(ui, "m_playerInventory", m_inventory);
            InventoryTestKit.InvokeAwake(ui);
            return ui;
        }

        private void Put(int slot, InventoryItem item, int quantity = 1)
        {
            Assert.That(m_inventory.TryAddItemToSlot(slot, item, quantity), Is.True);
        }

        private void FillRemainingSlots()
        {
            for (int i = 0; i < kSlotCount; ++i)
            {
                if (m_inventory.GetItemAtSlot(i) == null)
                    Put(i, m_kit.Item<ActionItem>("filler" + i));
            }
        }

        private int CountOf(InventoryItem item)
        {
            int total = 0;
            for (int i = 0; i < kSlotCount; ++i)
                if (m_inventory.GetItemAtSlot(i) == item) total += m_inventory.GetQuantityAtSlot(i);
            return total;
        }

        // Runs the private drop algorithm exactly as OnEndDrag would after resolving the target container
        private void Drop(IDragSource<InventoryItem> source, IDragDestination<InventoryItem> destination)
        {
            typeof(DragItem<InventoryItem>).GetField("m_dragSource", kAny).SetValue(m_drag, source);
            typeof(DragItem<InventoryItem>).GetMethod("DropItemIntoContainer", kAny).Invoke(m_drag, new object[] { destination });
        }

        /* --- Fakes --- */

        private sealed class FakeContainer : IDragContainer<InventoryItem>
        {
            public InventoryItem Item;
            public int Quantity;
            public Func<InventoryItem, int> Capacity = _ => int.MaxValue;
            public readonly List<(InventoryItem item, int quantity)> Added = new();
            public readonly List<int> Removed = new();

            public InventoryItem GetItem() => Item;
            public int GetQuantity() => Quantity;
            public int GetMaxItemsCapacity(InventoryItem item) => Capacity(item);
            public void AddItems(InventoryItem item, int quantity) => Added.Add((item, quantity));
            public void RemoveItems(int quantity) => Removed.Add(quantity);
        }

        private sealed class FakeDestination : IDragDestination<InventoryItem>
        {
            public int Capacity = int.MaxValue;
            public readonly List<(InventoryItem item, int quantity)> Added = new();

            public int GetMaxItemsCapacity(InventoryItem item) => Capacity;
            public void AddItems(InventoryItem item, int quantity) => Added.Add((item, quantity));
        }

        // ================================================================================
        //  DragItem: drop algorithm (fakes)
        // ================================================================================

        [Test]
        public void Drop_OntoItself_DoesNothing()
        {
            Put(0, m_potion, 3);

            Drop(m_slots[0], m_slots[0]);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
        }

        [Test]
        public void Transfer_ToPlainDestination_MovesEverythingWhenCapacityAllows()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 5 };
            FakeDestination destination = new FakeDestination();

            Drop(source, destination);

            Assert.That(source.Removed, Is.EqualTo(new[] { 5 }));
            Assert.That(destination.Added, Is.EqualTo(new[] { (m_potion as InventoryItem, 5) }));
        }

        [Test]
        public void Transfer_IsLimitedByDestinationCapacity()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 5 };
            FakeDestination destination = new FakeDestination { Capacity = 2 };

            Drop(source, destination);

            Assert.That(source.Removed, Is.EqualTo(new[] { 2 }));
            Assert.That(destination.Added.Single().quantity, Is.EqualTo(2));
        }

        [Test]
        public void Transfer_ToZeroCapacityDestination_ChangesNothing()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 5 };
            FakeDestination destination = new FakeDestination { Capacity = 0 };

            Drop(source, destination);

            Assert.That(source.Removed, Is.Empty);
            Assert.That(destination.Added, Is.Empty);
        }

        [Test]
        public void Transfer_ToContainerHoldingTheSameItem_MergesInsteadOfSwapping()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 2 };
            FakeContainer destination = new FakeContainer { Item = m_potion, Quantity = 3 };

            Drop(source, destination);

            Assert.That(source.Removed, Is.EqualTo(new[] { 2 }));
            Assert.That(destination.Removed, Is.Empty);
            Assert.That(destination.Added.Single(), Is.EqualTo((m_potion as InventoryItem, 2)));
        }

        [Test]
        public void Transfer_ToAnEmptyContainer_IsATransfer_NotASwap()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 2 };
            FakeContainer destination = new FakeContainer { Item = null, Quantity = 0 };

            Drop(source, destination);

            Assert.That(source.Removed, Is.EqualTo(new[] { 2 }));
            Assert.That(destination.Removed, Is.Empty);
        }

        [Test]
        public void Swap_BothSidesAcceptTheOther_ExchangesStacks()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 3 };
            FakeContainer destination = new FakeContainer { Item = m_herb, Quantity = 2 };

            Drop(source, destination);

            Assert.That(source.Removed, Is.EqualTo(new[] { 3 }));
            Assert.That(destination.Removed, Is.EqualTo(new[] { 2 }));
            Assert.That(source.Added.Single(), Is.EqualTo((m_herb as InventoryItem, 2)));
            Assert.That(destination.Added.Single(), Is.EqualTo((m_potion as InventoryItem, 3)));
        }

        [Test]
        [Description("[Characterizes quirk] When the destination can only take part of the dragged stack, the overflow is handed back to the SOURCE, and the swapped-in item also lands in the source.")]
        public void Swap_PartialDestinationCapacity_ReturnsOverflowToTheSource()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 5 };
            FakeContainer destination = new FakeContainer
            {
                Item = m_herb,
                Quantity = 1,
                Capacity = item => item == m_potion ? 2 : 0
            };

            Drop(source, destination);

            Assert.That(source.Added, Is.EqualTo(new[] { (m_potion as InventoryItem, 3), (m_herb as InventoryItem, 1) }));
            Assert.That(destination.Added, Is.EqualTo(new[] { (m_potion as InventoryItem, 2) }));
        }

        [Test]
        public void Swap_ThatCannotBePlaced_IsRevertedWithoutLoss()
        {
            FakeContainer source = new FakeContainer
            {
                Item = m_potion,
                Quantity = 5,
                Capacity = item => item == m_potion ? 3 : int.MaxValue
            };
            FakeContainer destination = new FakeContainer
            {
                Item = m_herb,
                Quantity = 1,
                Capacity = _ => 0
            };

            Drop(source, destination);

            Assert.That(source.Removed, Is.EqualTo(new[] { 5 }));
            Assert.That(destination.Removed, Is.EqualTo(new[] { 1 }));
            Assert.That(destination.Added, Is.EqualTo(new[] { (m_herb as InventoryItem, 1) }));
            Assert.That(source.Added, Is.EqualTo(new[] { (m_potion as InventoryItem, 5) }));
        }

        // ================================================================================
        //  DragItem: real inventory <-> inventory
        // ================================================================================

        [Test]
        public void Inventory_MoveToEmptySlot_MovesTheWholeStack()
        {
            Put(0, m_potion, 5);

            Drop(m_slots[0], m_slots[2]);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetItemAtSlot(2), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(2), Is.EqualTo(5));
        }

        [Test]
        public void Inventory_MoveNonStackableToEmptySlot()
        {
            Put(1, m_stone);

            Drop(m_slots[1], m_slots[3]);

            Assert.That(m_inventory.GetItemAtSlot(1), Is.Null);
            Assert.That(m_inventory.GetItemAtSlot(3), Is.SameAs(m_stone));
        }

        [Test]
        public void Inventory_DropStackOnSameItem_Merges()
        {
            Put(0, m_potion, 3);
            Put(1, m_potion, 2);        // direct slot placement allows two stacks of the same item

            Drop(m_slots[0], m_slots[1]);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(5));
        }

        [Test]
        public void Inventory_DropOnDifferentItem_SwapsSlotsAndKeepsQuantities()
        {
            Put(0, m_potion, 3);
            Put(1, m_herb, 2);

            Drop(m_slots[0], m_slots[1]);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_herb));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(2));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(3));
        }

        [Test]
        public void Inventory_SwapInAFullInventory_Works()
        {
            Put(0, m_potion, 3);
            Put(1, m_stone);
            FillRemainingSlots();

            Drop(m_slots[0], m_slots[1]);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_stone));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(3));
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(0));
        }

        [Test]
        public void Inventory_Drag_DoesNotRaiseItemAddedNotifications()
        {
            Put(0, m_potion, 3);
            int added = 0;
            m_inventory.OnItemAdded += (item, quantity) => ++added;

            Drop(m_slots[0], m_slots[2]);

            Assert.That(added, Is.EqualTo(0));
            Assert.That(m_inventory.SuppressItemAddedNotif, Is.False, "the suppress flag is restored");
        }

        // ================================================================================
        //  DragItem: inventory <-> equipment
        // ================================================================================

        [Test]
        public void Equip_ByDragging_MovesItemFromInventoryToTheEquipmentSlot()
        {
            Put(0, m_sword);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(CountOf(m_sword), Is.EqualTo(0));
        }

        [Test]
        public void Equip_WrongSlotType_IsRejected()
        {
            Put(0, m_helm);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_helm));
        }

        [Test]
        public void Equip_NonEquipableItem_IsRejected()
        {
            Put(0, m_potion, 2);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(2));
        }

        [Test]
        public void Unequip_ByDraggingToAnEmptySlot_PutsItemInThatSlot()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            Drop(m_weaponSlot, m_slots[2]);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_inventory.GetItemAtSlot(2), Is.SameAs(m_sword));
        }

        [Test]
        public void Swap_EquippedItemWithInventoryItemOfTheSameSlotType_ExchangesThem()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            Put(1, m_axe);

            Drop(m_weaponSlot, m_slots[1]);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_axe));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_sword));
        }

        [Test]
        public void Swap_InventoryItemOverEquippedItem_ExchangesThem()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);
            Put(0, m_sword);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_axe));
        }

        [Test]
        [Description("[Characterizes quirk] Dropping an item that can't go in the slot onto an OCCUPIED equipment slot still unequips the current item into the inventory (the dropped item stays put).")]
        public void Swap_UnequipableItemOverEquippedItem_UnequipsTheEquippedOne()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);
            Put(0, m_potion, 3);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null, "the axe was unequipped");
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
            Assert.That(CountOf(m_axe), Is.EqualTo(1), "and moved into a free inventory slot");
        }

        [Test]
        [Description("[Characterizes quirk] Same as above but with a wrong-slot-type equipable (helmet onto the weapon slot).")]
        public void Swap_WrongSlotEquipableOverEquippedItem_UnequipsTheEquippedOne()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);
            Put(0, m_helm);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_helm));
            Assert.That(CountOf(m_axe), Is.EqualTo(1));
        }

        [Test]
        public void Swap_UnequipableItemOverEquippedItem_InAFullInventory_IsReverted()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);
            Put(0, m_potion, 3);
            FillRemainingSlots();

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_axe));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
        }

        [Test]
        [Description("[Characterizes quirk] Dragging the EQUIPPED item onto an inventory stack it can't swap with unequips it into a different free slot; the stack stays where it was.")]
        public void Swap_EquippedItemOverUnrelatedStack_MovesEquippedItemToAFreeSlot()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            Put(0, m_potion, 3);

            Drop(m_weaponSlot, m_slots[0]);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_potion));
            Assert.That(CountOf(m_sword), Is.EqualTo(1));
        }

        [Test]
        public void Swap_EquippedItemOverUnrelatedStack_InAFullInventory_IsReverted()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            Put(0, m_potion, 3);
            FillRemainingSlots();

            Drop(m_weaponSlot, m_slots[0]);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
            Assert.That(CountOf(m_sword), Is.EqualTo(0));
        }

        // ================================================================================
        //  InventorySlotUI
        // ================================================================================

        [Test]
        public void SlotUI_ReportsTheItemAndQuantityOfItsInventorySlot()
        {
            Put(2, m_potion, 4);

            Assert.That(m_slots[2].GetItem(), Is.SameAs(m_potion));
            Assert.That(m_slots[2].GetQuantity(), Is.EqualTo(4));
            Assert.That(m_slots[0].GetItem(), Is.Null);
        }

        [Test]
        public void SlotUI_Setup_ShowsTheIconOnlyWhenOccupied()
        {
            Put(1, m_stone);
            InventorySlotUI occupied = m_slots[1];
            InventorySlotUI empty = m_slots[0];

            occupied.Setup(m_inventory, 1);
            empty.Setup(m_inventory, 0);

            Assert.That(IconImage(occupied).enabled, Is.True);
            Assert.That(IconImage(empty).enabled, Is.False);
        }

        private static Image IconImage(InventorySlotUI slot)
        {
            InventoryItemIcon icon = (InventoryItemIcon)typeof(InventorySlotUI).GetField("m_itemIcon", kAny).GetValue(slot);
            return icon.GetComponent<Image>();
        }

        [Test]
        public void SlotUI_Capacity_IsUnlimitedWhileAnySlotIsFree()
        {
            Assert.That(m_slots[0].GetMaxItemsCapacity(m_stone), Is.EqualTo(int.MaxValue));
            Assert.That(m_slots[0].GetMaxItemsCapacity(m_stone), Is.EqualTo(m_slots[0].GetMaxQuantity(m_stone)));
        }

        [Test]
        [Description("[Characterizes quirk] Capacity is inventory-wide (HasSpaceFor), not per slot: a full inventory reports 0 even for the slot you'd be replacing, and a stackable already owned reports unlimited.")]
        public void SlotUI_Capacity_InAFullInventory_DependsOnStackingNotOnTheSlot()
        {
            Put(0, m_potion, 2);
            FillRemainingSlots();

            Assert.That(m_slots[1].GetMaxItemsCapacity(m_potion), Is.EqualTo(int.MaxValue), "stackable already owned");
            Assert.That(m_slots[1].GetMaxItemsCapacity(m_herb), Is.EqualTo(0), "stackable not owned");
            Assert.That(m_slots[1].GetMaxItemsCapacity(m_stone), Is.EqualTo(0), "non-stackable");
        }

        [Test]
        public void SlotUI_AddItems_PlacesInItsOwnSlot_WithoutNotification()
        {
            int added = 0;
            m_inventory.OnItemAdded += (item, quantity) => ++added;

            m_slots[2].AddItems(m_potion, 3);

            Assert.That(m_inventory.GetItemAtSlot(2), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(2), Is.EqualTo(3));
            Assert.That(added, Is.EqualTo(0));
            Assert.That(m_inventory.SuppressItemAddedNotif, Is.False);
        }

        [Test]
        public void SlotUI_RemoveItems_RemovesFromItsOwnSlot()
        {
            Put(1, m_potion, 5);

            m_slots[1].RemoveItems(2);

            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(3));
        }

        [Test]
        public void SlotUI_DoubleClick_UsesTheItem_OnlyWhileTheCursorIsOver()
        {
            Put(0, m_sword);

            DoubleClick(m_slots[0]);
            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null, "cursor not over the slot");

            m_slots[0].OnPointerEnter(null);
            DoubleClick(m_slots[0]);
            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
        }

        [Test]
        public void SlotUI_DoubleClick_AfterThePointerLeaves_DoesNothing()
        {
            Put(0, m_sword);
            m_slots[0].OnPointerEnter(null);
            m_slots[0].OnPointerExit(null);

            DoubleClick(m_slots[0]);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
        }

        [Test]
        public void SlotUI_DoubleClick_OnAnEmptySlot_IsHarmless()
        {
            m_slots[0].OnPointerEnter(null);

            Assert.DoesNotThrow(() => DoubleClick(m_slots[0]));
        }

        private static void DoubleClick(Component slot)
        {
            slot.GetType().GetMethod("OnDoubleClick", kAny).Invoke(slot, new object[] { default(InputAction.CallbackContext) });
        }

        // ================================================================================
        //  EquipmentSlotUI
        // ================================================================================

        [Test]
        public void EquipSlotUI_EmptySlot_ReportsNoItem_QuantityZero()
        {
            Assert.That(m_weaponSlot.GetItem(), Is.Null);
            Assert.That(m_weaponSlot.GetQuantity(), Is.EqualTo(0));
        }

        [Test]
        public void EquipSlotUI_OccupiedSlot_ReportsItem_QuantityOne()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            Assert.That(m_weaponSlot.GetItem(), Is.SameAs(m_sword));
            Assert.That(m_weaponSlot.GetQuantity(), Is.EqualTo(1));
        }

        [Test]
        public void EquipSlotUI_Capacity_IsOneOnlyForAMatchingItemIntoAnEmptySlot()
        {
            Assert.That(m_weaponSlot.GetMaxItemsCapacity(m_sword), Is.EqualTo(1));
            Assert.That(m_weaponSlot.GetMaxItemsCapacity(m_helm), Is.EqualTo(0), "wrong slot type");
            Assert.That(m_weaponSlot.GetMaxItemsCapacity(m_potion), Is.EqualTo(0), "not equipable");
            Assert.That(m_weaponSlot.GetMaxItemsCapacity(null), Is.EqualTo(0));

            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);
            Assert.That(m_weaponSlot.GetMaxItemsCapacity(m_sword), Is.EqualTo(0), "already occupied");
        }

        [Test]
        public void EquipSlotUI_AddItems_EquipsIntoItsConfiguredSlot()
        {
            m_helmetSlot.AddItems(m_helm, 1);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kHelmet), Is.SameAs(m_helm));
        }

        [Test]
        [Description("[Characterizes quirk] AddItems casts blindly to EquipableItem; callers must check capacity first or it throws InvalidCastException.")]
        public void EquipSlotUI_AddItems_WithANonEquipableItem_ThrowsInvalidCast()
        {
            Assert.That(() => m_weaponSlot.AddItems(m_potion, 1), Throws.TypeOf<InvalidCastException>());
        }

        [Test]
        public void EquipSlotUI_RemoveItems_UnequipsRegardlessOfQuantity()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            m_weaponSlot.RemoveItems(0);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
        }

        [Test]
        public void EquipSlotUI_RedrawUI_ShowsTheIconOnlyWhenEquipped()
        {
            Image image = ((InventoryItemIcon)typeof(EquipmentSlotUI).GetField("m_itemIcon", kAny).GetValue(m_weaponSlot)).GetComponent<Image>();

            Redraw(m_weaponSlot);
            Assert.That(image.enabled, Is.False);

            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            Redraw(m_weaponSlot);
            Assert.That(image.enabled, Is.True);
        }

        private static void Redraw(EquipmentSlotUI slot) => typeof(EquipmentSlotUI).GetMethod("RedrawUI", kAny).Invoke(slot, null);

        [Test]
        public void EquipSlotUI_DoubleClick_MovesTheEquippedItemToTheInventory()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            m_weaponSlot.OnPointerEnter(null);

            DoubleClick(m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(CountOf(m_sword), Is.EqualTo(1));
            Assert.That(m_inventory.SuppressItemAddedNotif, Is.False);
        }

        [Test]
        public void EquipSlotUI_DoubleClick_WithAFullInventory_KeepsItEquipped()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            FillRemainingSlots();
            m_weaponSlot.OnPointerEnter(null);

            DoubleClick(m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_inventory.SuppressItemAddedNotif, Is.False);
        }

        [Test]
        public void EquipSlotUI_DoubleClick_WithoutTheCursorOver_DoesNothing()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            DoubleClick(m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
        }

        [Test]
        public void EquipSlotUI_DoubleClick_OnAnEmptySlot_IsHarmless()
        {
            m_weaponSlot.OnPointerEnter(null);

            Assert.DoesNotThrow(() => DoubleClick(m_weaponSlot));
        }

        // ================================================================================
        //  InventoryDropTarget
        // ================================================================================

        [Test]
        public void DropTarget_AcceptsAnyQuantity()
        {
            InventoryDropTarget target = m_kit.NewObject("DropTarget").AddComponent<InventoryDropTarget>();

            Assert.That(target.GetMaxItemsCapacity(m_potion), Is.EqualTo(int.MaxValue));
            Assert.That(target.GetMaxItemsCapacity(null), Is.EqualTo(int.MaxValue));
        }

        [Test]
        [Description("Dropping to the world (DropTarget.AddItems -> ItemDropper.DropItem) spawns pickups, so it is covered in the PlayMode DropperTests.")]
        public void DropTarget_AddItems_WithoutAWiredDropper_Throws()
        {
            InventoryDropTarget target = m_kit.NewObject("DropTarget").AddComponent<InventoryDropTarget>();

            Assert.That(() => target.AddItems(m_potion, 1), Throws.TypeOf<NullReferenceException>());
        }

        // ================================================================================
        //  DragItem: pointer handlers
        // ================================================================================

        private (InventoryDragItem drag, Canvas canvas, CanvasGroup group, GameObject home) BuildDraggable()
        {
            GameObject canvasObject = m_kit.NewObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();

            GameObject home = new GameObject("Home");
            home.transform.SetParent(canvasObject.transform, false);

            GameObject dragObject = new GameObject("Draggable");
            dragObject.transform.SetParent(home.transform, false);
            CanvasGroup group = dragObject.AddComponent<CanvasGroup>();
            InventoryDragItem drag = dragObject.AddComponent<InventoryDragItem>();
            InventoryTestKit.InvokeAwake(drag);
            return (drag, canvas, group, home);
        }

        [Test]
        public void BeginDrag_BlocksRaycasts_AndMovesTheItemUnderTheCanvas()
        {
            var (drag, canvas, group, _) = BuildDraggable();

            ((IBeginDragHandler)drag).OnBeginDrag(new PointerEventData(null));

            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(drag.transform.parent, Is.SameAs(canvas.transform));
        }

        [Test]
        public void Drag_FollowsThePointerPosition()
        {
            var (drag, _, _, _) = BuildDraggable();

            ((IDragHandler)drag).OnDrag(new PointerEventData(null) { position = new Vector2(12f, 34f) });

            Assert.That(drag.transform.position, Is.EqualTo(new Vector3(12f, 34f, 0f)));
        }

        [Test]
        public void Awake_FindsTheDragSourceOnAParent()
        {
            GameObject canvasObject = m_kit.NewObject("Canvas");
            canvasObject.AddComponent<Canvas>();
            GameObject slotObject = new GameObject("SlotParent");
            slotObject.transform.SetParent(canvasObject.transform, false);
            InventorySlotUI parentSlot = slotObject.AddComponent<InventorySlotUI>();
            GameObject dragObject = new GameObject("Draggable");
            dragObject.transform.SetParent(slotObject.transform, false);
            dragObject.AddComponent<CanvasGroup>();
            InventoryDragItem drag = dragObject.AddComponent<InventoryDragItem>();

            InventoryTestKit.InvokeAwake(drag);

            object source = typeof(DragItem<InventoryItem>).GetField("m_dragSource", kAny).GetValue(drag);
            Assert.That(source, Is.SameAs(parentSlot));
        }
    }
}
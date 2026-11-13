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
        [Description("Dropping a slot onto itself leaves its item and quantity unchanged.")]
        public void Drop_OntoItself_DoesNothing()
        {
            Put(0, m_potion, 3);

            Drop(m_slots[0], m_slots[0]);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
        }

        [Test]
        [Description("Dropping onto a non-container destination with enough capacity moves the whole stack.")]
        public void Transfer_ToPlainDestination_MovesEverythingWhenCapacityAllows()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 5 };
            FakeDestination destination = new FakeDestination();

            Drop(source, destination);

            Assert.That(source.Removed, Is.EqualTo(new[] { 5 }));
            Assert.That(destination.Added, Is.EqualTo(new[] { (m_potion as InventoryItem, 5) }));
        }

        [Test]
        [Description("A transfer moves only as many units as the destination's capacity allows.")]
        public void Transfer_IsLimitedByDestinationCapacity()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 5 };
            FakeDestination destination = new FakeDestination { Capacity = 2 };

            Drop(source, destination);

            Assert.That(source.Removed, Is.EqualTo(new[] { 2 }));
            Assert.That(destination.Added.Single().quantity, Is.EqualTo(2));
        }

        [Test]
        [Description("A destination with zero capacity receives nothing, and nothing is removed from the source.")]
        public void Transfer_ToZeroCapacityDestination_ChangesNothing()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 5 };
            FakeDestination destination = new FakeDestination { Capacity = 0 };

            Drop(source, destination);

            Assert.That(source.Removed, Is.Empty);
            Assert.That(destination.Added, Is.Empty);
        }

        [Test]
        [Description("Dropping onto a container holding the same item merges the stacks instead of swapping.")]
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
        [Description("Dropping onto an empty container is a one-way transfer; nothing is taken from the destination.")]
        public void Transfer_ToAnEmptyContainer_IsATransfer_NotASwap()
        {
            FakeContainer source = new FakeContainer { Item = m_potion, Quantity = 2 };
            FakeContainer destination = new FakeContainer { Item = null, Quantity = 0 };

            Drop(source, destination);

            Assert.That(source.Removed, Is.EqualTo(new[] { 2 }));
            Assert.That(destination.Removed, Is.Empty);
        }

        [Test]
        [Description("When both containers accept the other's item, the two stacks are exchanged in full.")]
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
        [Description("In a swap, units the destination can't hold are returned to the source along with the destination's item.")]
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
        [Description("A swap that can't be placed on either side is reverted: each container gets its original stack back.")]
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
        [Description("Dragging a stack onto an empty inventory slot moves the whole stack there.")]
        public void Inventory_MoveToEmptySlot_MovesTheWholeStack()
        {
            Put(0, m_potion, 5);

            Drop(m_slots[0], m_slots[2]);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetItemAtSlot(2), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(2), Is.EqualTo(5));
        }

        [Test]
        [Description("Dragging a stack onto another stack of the same item merges them into the target slot.")]
        public void Inventory_DropStackOnSameItem_Merges()
        {
            Put(0, m_potion, 3);
            Put(1, m_potion, 2);        // direct slot placement allows two stacks of the same item

            Drop(m_slots[0], m_slots[1]);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(5));
        }

        [Test]
        [Description("Dragging onto a slot with a different item swaps the two slots, keeping each quantity.")]
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
        [Description("Swapping two slots works even when the inventory has no free slots.")]
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
        [Description("Moving items by dragging doesn't raise OnItemAdded, and the suppress flag is restored afterwards.")]
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
        [Description("Dragging an equipable item onto its matching equipment slot equips it and removes it from the inventory.")]
        public void Equip_ByDragging_MovesItemFromInventoryToTheEquipmentSlot()
        {
            Put(0, m_sword);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(CountOf(m_sword), Is.EqualTo(0));
        }

        [Test]
        [Description("Dragging an equipable item onto the wrong slot type is rejected; it stays in the inventory.")]
        public void Equip_WrongSlotType_IsRejected()
        {
            Put(0, m_helm);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_helm));
        }

        [Test]
        [Description("Dragging a non-equipable item onto an equipment slot is rejected; the stack stays intact.")]
        public void Equip_NonEquipableItem_IsRejected()
        {
            Put(0, m_potion, 2);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(2));
        }

        [Test]
        [Description("Dragging an equipped item onto an empty inventory slot unequips it into that slot.")]
        public void Unequip_ByDraggingToAnEmptySlot_PutsItemInThatSlot()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            Drop(m_weaponSlot, m_slots[2]);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_inventory.GetItemAtSlot(2), Is.SameAs(m_sword));
        }

        [Test]
        [Description("Dragging an equipped item onto an inventory item of the same slot type exchanges them.")]
        public void Swap_EquippedItemWithInventoryItemOfTheSameSlotType_ExchangesThem()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            Put(1, m_axe);

            Drop(m_weaponSlot, m_slots[1]);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_axe));
            Assert.That(m_inventory.GetItemAtSlot(1), Is.SameAs(m_sword));
        }

        [Test]
        [Description("Dragging an inventory item onto an occupied equipment slot of its type exchanges them.")]
        public void Swap_InventoryItemOverEquippedItem_ExchangesThem()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);
            Put(0, m_sword);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_axe));
        }

        [Test]
        [Description("Dropping a non-equipable item on an occupied equipment slot is rejected: the equipped item stays equipped and the stack stays where it was.")]
        public void Drop_UnequipableItemOnAnOccupiedEquipmentSlot_IsRejected()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);
            Put(0, m_potion, 3);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_axe));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_potion));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(3));
            Assert.That(CountOf(m_axe), Is.EqualTo(0));
        }

        [Test]
        [Description("Dropping an equipable item for a different slot type on an occupied equipment slot is rejected: the equipped item stays equipped and the dropped item stays where it was.")]
        public void Drop_WrongSlotEquipableOnAnOccupiedEquipmentSlot_IsRejected()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);
            Put(0, m_helm);

            Drop(m_slots[0], m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_axe));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_helm));
            Assert.That(CountOf(m_axe), Is.EqualTo(0));
        }

        [Test]
        [Description("Dragging an equipped item onto an unrelated stack unequips it into a free slot and leaves the stack in place.")]
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
        [Description("With a full inventory, dragging an equipped item onto an unrelated stack is reverted; it stays equipped.")]
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
        [Description("An inventory slot UI reports the item and quantity held in its inventory slot.")]
        public void SlotUI_ReportsTheItemAndQuantityOfItsInventorySlot()
        {
            Put(2, m_potion, 4);

            Assert.That(m_slots[2].GetItem(), Is.SameAs(m_potion));
            Assert.That(m_slots[2].GetQuantity(), Is.EqualTo(4));
            Assert.That(m_slots[0].GetItem(), Is.Null);
        }

        [Test]
        [Description("Setup enables the slot's icon image only when the slot holds an item.")]
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
        [Description("While any inventory slot is free, a slot's capacity is unlimited, and GetMaxQuantity agrees.")]
        public void SlotUI_Capacity_IsUnlimitedWhileAnySlotIsFree()
        {
            Assert.That(m_slots[0].GetMaxItemsCapacity(m_stone), Is.EqualTo(int.MaxValue));
            Assert.That(m_slots[0].GetMaxItemsCapacity(m_stone), Is.EqualTo(m_slots[0].GetMaxQuantity(m_stone)));
        }

        [Test]
        [Description("A slot UI's AddItems puts the items in its own slot without raising OnItemAdded.")]
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
        [Description("A slot UI's RemoveItems removes the given quantity from its own slot.")]
        public void SlotUI_RemoveItems_RemovesFromItsOwnSlot()
        {
            Put(1, m_potion, 5);

            m_slots[1].RemoveItems(2);

            Assert.That(m_inventory.GetQuantityAtSlot(1), Is.EqualTo(3));
        }

        [Test]
        [Description("Double-clicking an inventory slot uses its item only while the pointer is over that slot.")]
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
        [Description("After the pointer exits a slot, double-clicking does nothing.")]
        public void SlotUI_DoubleClick_AfterThePointerLeaves_DoesNothing()
        {
            Put(0, m_sword);
            m_slots[0].OnPointerEnter(null);
            m_slots[0].OnPointerExit(null);

            DoubleClick(m_slots[0]);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
        }

        [Test]
        [Description("Double-clicking an empty inventory slot doesn't throw.")]
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
        [Description("An empty equipment slot UI reports no item and quantity 0.")]
        public void EquipSlotUI_EmptySlot_ReportsNoItem_QuantityZero()
        {
            Assert.That(m_weaponSlot.GetItem(), Is.Null);
            Assert.That(m_weaponSlot.GetQuantity(), Is.EqualTo(0));
        }

        [Test]
        [Description("An occupied equipment slot UI reports the equipped item and quantity 1.")]
        public void EquipSlotUI_OccupiedSlot_ReportsItem_QuantityOne()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            Assert.That(m_weaponSlot.GetItem(), Is.SameAs(m_sword));
            Assert.That(m_weaponSlot.GetQuantity(), Is.EqualTo(1));
        }

        [Test]
        [Description("An equipment slot's capacity is 1 only for a matching equipable item into an empty slot; otherwise 0.")]
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
        [Description("An equipment slot UI's AddItems equips the item into the slot it is configured for.")]
        public void EquipSlotUI_AddItems_EquipsIntoItsConfiguredSlot()
        {
            m_helmetSlot.AddItems(m_helm, 1);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kHelmet), Is.SameAs(m_helm));
        }

        [Test]
        [Description("An equipment slot UI's RemoveItems unequips the item in its slot.")]
        public void EquipSlotUI_RemoveItems_UnequipsTheItem()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            m_weaponSlot.RemoveItems(1);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
        }

        [Test]
        [Description("RedrawUI enables the equipment slot's icon image only while an item is equipped.")]
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
        [Description("Double-clicking an equipment slot unequips its item into the inventory and restores the suppress flag.")]
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
        [Description("Double-clicking an equipment slot with a full inventory keeps the item equipped and restores the suppress flag.")]
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
        [Description("Double-clicking an equipment slot without the pointer over it does nothing.")]
        public void EquipSlotUI_DoubleClick_WithoutTheCursorOver_DoesNothing()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            DoubleClick(m_weaponSlot);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
        }

        [Test]
        [Description("Double-clicking an empty equipment slot doesn't throw.")]
        public void EquipSlotUI_DoubleClick_OnAnEmptySlot_IsHarmless()
        {
            m_weaponSlot.OnPointerEnter(null);

            Assert.DoesNotThrow(() => DoubleClick(m_weaponSlot));
        }

        // ================================================================================
        //  InventoryDropTarget
        // ================================================================================

        [Test]
        [Description("The world drop target accepts an unlimited quantity of any item, including null.")]
        public void DropTarget_AcceptsAnyQuantity()
        {
            InventoryDropTarget target = m_kit.NewObject("DropTarget").AddComponent<InventoryDropTarget>();

            Assert.That(target.GetMaxItemsCapacity(m_potion), Is.EqualTo(int.MaxValue));
            Assert.That(target.GetMaxItemsCapacity(null), Is.EqualTo(int.MaxValue));
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
        [Description("OnBeginDrag stops the item blocking raycasts and reparents it under the root canvas.")]
        public void BeginDrag_BlocksRaycasts_AndMovesTheItemUnderTheCanvas()
        {
            var (drag, canvas, group, _) = BuildDraggable();

            ((IBeginDragHandler)drag).OnBeginDrag(new PointerEventData(null));

            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(drag.transform.parent, Is.SameAs(canvas.transform));
        }

        [Test]
        [Description("OnDrag moves the dragged item to the pointer position.")]
        public void Drag_FollowsThePointerPosition()
        {
            var (drag, _, _, _) = BuildDraggable();

            ((IDragHandler)drag).OnDrag(new PointerEventData(null) { position = new Vector2(12f, 34f) });

            Assert.That(drag.transform.position, Is.EqualTo(new Vector3(12f, 34f, 0f)));
        }

        [Test]
        [Description("Awake finds the drag source on a parent GameObject.")]
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
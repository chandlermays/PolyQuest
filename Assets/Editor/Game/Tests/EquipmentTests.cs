/*---------------------------
| File: EquipmentTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Inventories;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    public class EquipmentTests
    {
        private const int kSlots = 4;

        private InventoryTestKit m_kit;
        private GameObject m_gameObject;
        private BaseStats m_stats;
        private Equipment m_equipment;
        private Inventory m_inventory;

        private EquipableItem m_sword, m_axe, m_helm;
        private Armor m_plate;
        private ActionItem m_potion;

        private int m_equipmentChanged;
        private int m_statsModified;
        private int m_itemAdded;

        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();

            m_sword = m_kit.Equipable<EquipableItem>("sword", EquipmentSlot.kWeapon);
            m_axe = m_kit.Equipable<EquipableItem>("axe", EquipmentSlot.kWeapon);
            m_helm = m_kit.Equipable<EquipableItem>("helm", EquipmentSlot.kHelmet);
            m_plate = m_kit.Equipable<Armor>("plate", EquipmentSlot.kChest);
            SerializedTestUtil.SetFloat(m_plate, "m_baseDefense", 3f);
            m_potion = m_kit.Item<ActionItem>("potion", stackable: true);

            m_gameObject = m_kit.NewObject("EquipmentTestSubject");
            m_stats = m_gameObject.AddComponent<BaseStats>();
            m_equipment = m_gameObject.AddComponent<Equipment>();
            InventoryTestKit.InvokeAwake(m_equipment);          // Equipment.Awake caches BaseStats
            m_inventory = m_kit.NewInventory(m_gameObject, kSlots);

            m_equipmentChanged = 0;
            m_statsModified = 0;
            m_itemAdded = 0;
            m_equipment.OnEquipmentChanged += () => ++m_equipmentChanged;
            m_stats.OnStatModified += () => ++m_statsModified;
            m_inventory.OnItemAdded += (item, quantity) => ++m_itemAdded;
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        private Equipment NewOtherEquipment()
        {
            GameObject other = m_kit.NewObject("OtherEquipment");
            other.AddComponent<BaseStats>();
            Equipment equipment = other.AddComponent<Equipment>();
            InventoryTestKit.InvokeAwake(equipment);
            return equipment;
        }

        private void ResetCounts()
        {
            m_equipmentChanged = 0;
            m_statsModified = 0;
            m_itemAdded = 0;
        }

        /* ============================== Equipment ============================== */

        [Test]
        public void GetItemInSlot_EmptySlot_ReturnsNull()
        {
            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
        }

        [Test]
        public void AddItem_StoresTheItem_AndNotifiesEquipmentAndStats()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_equipmentChanged, Is.EqualTo(1));
            Assert.That(m_statsModified, Is.EqualTo(1));
        }

        [Test]
        [Description("[Characterizes quirk] AddItem does not check the item's target slot. A sword can be stored in the helmet slot through the API; only EquipableItem.OnUse validates.")]
        public void AddItem_DoesNotValidateTheSlot()
        {
            m_equipment.AddItem(EquipmentSlot.kHelmet, m_sword);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kHelmet), Is.SameAs(m_sword));
        }

        [Test]
        [Description("[Characterizes quirk] AddItem over an occupied slot replaces the old item and hands it back to nobody. Returning it to the inventory is EquipableItem.OnUse's job.")]
        public void AddItem_OverAnOccupiedSlot_ReplacesAndDiscardsThePreviousItem()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_axe));
            Assert.That(m_equipment.OccupiedSlots.Count(), Is.EqualTo(1));
        }

        [Test]
        public void RemoveItem_OccupiedSlot_ClearsIt_AndNotifies()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            ResetCounts();

            m_equipment.RemoveItem(EquipmentSlot.kWeapon);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_equipmentChanged, Is.EqualTo(1));
            Assert.That(m_statsModified, Is.EqualTo(1));
        }

        [Test]
        public void RemoveItem_EmptySlot_DoesNothing_AndRaisesNoEvents()
        {
            m_equipment.RemoveItem(EquipmentSlot.kWeapon);

            Assert.That(m_equipmentChanged, Is.EqualTo(0));
            Assert.That(m_statsModified, Is.EqualTo(0));
        }

        /* --- Stat modifiers --- */

        [Test]
        public void EquippedArmor_AddsItsDefense_ThroughBaseStats_AndRemovingItReverts()
        {
            PlayerProgression progression = m_kit.Track(ScriptableObject.CreateInstance<PlayerProgression>());
            SerializedTestUtil.SetIntArray(progression, "m_defenseAmounts", 5);
            SerializedTestUtil.SetObject(m_stats, "m_baseProgression", progression);
            SerializedTestUtil.SetBool(m_stats, "m_useStatModifiers", true);

            Assert.That(m_stats.GetDefense(), Is.EqualTo(5f));

            m_equipment.AddItem(EquipmentSlot.kChest, m_plate);
            Assert.That(m_stats.GetDefense(), Is.EqualTo(8f).Within(1e-4f));

            m_equipment.RemoveItem(EquipmentSlot.kChest);
            Assert.That(m_stats.GetDefense(), Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void Modifiers_AreEmpty_WhenNothingIsEquipped()
        {
            Assert.That(m_equipment.GetAdditiveModifiers(Stat.kDefense), Is.Empty);
            Assert.That(m_equipment.GetPercentageModifiers(Stat.kDefense), Is.Empty);
        }

        /* --- Evaluate --- */

        [Test]
        public void Evaluate_HasItemEquipped_ReflectsWhatIsEquipped()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, new[] { m_sword.ID }), Is.True);
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, new[] { m_axe.ID }), Is.False);
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, new[] { "missing" }), Is.False);
        }

        [Test]
        public void Evaluate_NullOrEmptyParameters_OrUnhandledPredicate_ReturnsNull()
        {
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, null), Is.Null);
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, new string[0]), Is.Null);
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItem, new[] { m_sword.ID }), Is.Null);
        }

        /* --- Save / Restore --- */

        [Test]
        public void CaptureState_KeysAreSlotNames_ValuesAreItemIds()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            m_equipment.AddItem(EquipmentSlot.kHelmet, m_helm);

            JObject state = (JObject)m_equipment.CaptureState();

            Assert.That(state.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "kWeapon", "kHelmet" }));
            Assert.That(state["kWeapon"].ToObject<string>(), Is.EqualTo(m_sword.ID));
            Assert.That(state["kHelmet"].ToObject<string>(), Is.EqualTo(m_helm.ID));
        }

        [Test]
        public void RoundTrip_RestoresItems_AndNotifies()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            m_equipment.AddItem(EquipmentSlot.kHelmet, m_helm);
            Equipment other = NewOtherEquipment();
            int changed = 0;
            other.OnEquipmentChanged += () => ++changed;

            other.RestoreState(JToken.Parse(m_equipment.CaptureState().ToString()));

            Assert.That(other.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(other.GetItemInSlot(EquipmentSlot.kHelmet), Is.SameAs(m_helm));
            Assert.That(changed, Is.EqualTo(1));
        }

        [Test]
        public void RestoreState_ReplacesWhatWasEquipped()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            m_equipment.RestoreState(new JObject { ["kHelmet"] = m_helm.ID });

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kHelmet), Is.SameAs(m_helm));
        }

        [Test]
        [Description("[Characterizes quirk] A saved item ID that no longer exists is silently dropped, so the equipped item is lost. Events still fire.")]
        public void RestoreState_UnknownItemId_IsDropped_ButEventsStillFire()
        {
            m_equipment.RestoreState(new JObject { ["kWeapon"] = "gone" });

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_equipment.OccupiedSlots, Is.Empty);
            Assert.That(m_equipmentChanged, Is.EqualTo(1));
            Assert.That(m_statsModified, Is.EqualTo(1));
        }

        [Test]
        [Description("[Characterizes quirk] A saved ID that resolves to a non-equipable item (a potion) is dropped, and so is an unrecognised slot name.")]
        public void RestoreState_NonEquipableItem_OrUnknownSlot_IsDropped()
        {
            m_equipment.RestoreState(new JObject { ["kWeapon"] = m_potion.ID, ["kTail"] = m_sword.ID });

            Assert.That(m_equipment.OccupiedSlots, Is.Empty);
        }

        [Test]
        [Description("[Characterizes quirk] Restore does not check that an item belongs in the slot it was saved under; a sword restores into the helmet slot.")]
        public void RestoreState_DoesNotValidateItemAgainstSlot()
        {
            m_equipment.RestoreState(new JObject { ["kHelmet"] = m_sword.ID });

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kHelmet), Is.SameAs(m_sword));
        }

        [Test]
        [Description("[Characterizes quirk] A non-object state does NOT clear the equipment (the Clear is inside the type check), yet both events still fire.")]
        public void RestoreState_NonObject_KeepsEquipment_ButStillRaisesEvents()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            ResetCounts();

            m_equipment.RestoreState(new JValue(5));

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_equipmentChanged, Is.EqualTo(1));
            Assert.That(m_statsModified, Is.EqualTo(1));
        }

        /* ============================== EquipableItem.OnUse ============================== */

        [Test]
        public void OnUse_EquipsTheItem_AndRemovesItFromTheInventory()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            m_sword.OnUse(m_inventory, 0);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }

        [Test]
        public void OnUse_WhenTheSlotIsOccupied_SwapsInPlace_WithoutRaisingItemAdded()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            m_inventory.TryAddToAvailableSlot(m_axe);
            ResetCounts();

            m_axe.OnUse(m_inventory, 0);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_axe));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetQuantityAtSlot(0), Is.EqualTo(1));
            Assert.That(m_itemAdded, Is.EqualTo(0));
            Assert.That(m_inventory.SuppressItemAddedNotif, Is.False);
        }

        [Test]
        public void OnUse_WithNoTargetSlot_DoesNothing()
        {
            EquipableItem trinket = m_kit.Item<EquipableItem>("trinket");
            m_inventory.TryAddToAvailableSlot(trinket);

            trinket.OnUse(m_inventory, 0);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(trinket));
            Assert.That(m_equipment.OccupiedSlots, Is.Empty);
        }

        [Test]
        [Description("[Characterizes quirk] OnUse on an inventory with no Equipment component throws instead of declining. The exception type can differ between the Editor and a build, so only 'throws' is asserted.")]
        public void OnUse_WithoutAnEquipmentComponent_Throws()
        {
            Inventory bare = m_kit.NewInventory(m_kit.NewObject("BareInventory"), kSlots);
            bare.TryAddToAvailableSlot(m_sword);

            Assert.That(() => m_sword.OnUse(bare, 0), Throws.Exception);
        }

        [Test]
        public void CanEquip_RequiresTheItemsOwnTargetSlot()
        {
            Assert.That(m_sword.CanEquip(EquipmentSlot.kWeapon, m_equipment), Is.True);
            Assert.That(m_sword.CanEquip(EquipmentSlot.kHelmet, m_equipment), Is.False);
        }
    }
}
/*---------------------------
| File: EquipmentTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
        [Description("An empty equipment slot returns null.")]
        public void GetItemInSlot_EmptySlot_ReturnsNull()
        {
            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
        }

        [Test]
        [Description("AddItem stores the item in the slot and raises OnEquipmentChanged and OnStatModified once each.")]
        public void AddItem_StoresTheItem_AndNotifiesEquipmentAndStats()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_equipmentChanged, Is.EqualTo(1));
            Assert.That(m_statsModified, Is.EqualTo(1));
        }

        [Test]
        [Description("AddItem on an occupied slot replaces the item in that slot; the slot still counts once.")]
        public void AddItem_OverAnOccupiedSlot_ReplacesTheItem()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            m_equipment.AddItem(EquipmentSlot.kWeapon, m_axe);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_axe));
            Assert.That(m_equipment.OccupiedSlots.Count(), Is.EqualTo(1));
        }

        [Test]
        [Description("RemoveItem clears an occupied slot and raises OnEquipmentChanged and OnStatModified once each.")]
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
        [Description("RemoveItem on an empty slot changes nothing and raises no events.")]
        public void RemoveItem_EmptySlot_DoesNothing_AndRaisesNoEvents()
        {
            m_equipment.RemoveItem(EquipmentSlot.kWeapon);

            Assert.That(m_equipmentChanged, Is.EqualTo(0));
            Assert.That(m_statsModified, Is.EqualTo(0));
        }

        /* --- Stat modifiers --- */

        [Test]
        [Description("Equipped armor adds its defense through BaseStats' modifiers; unequipping it restores the base value.")]
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
        [Description("With nothing equipped, Equipment provides no additive or percentage modifiers.")]
        public void Modifiers_AreEmpty_WhenNothingIsEquipped()
        {
            Assert.That(m_equipment.GetAdditiveModifiers(Stat.kDefense), Is.Empty);
            Assert.That(m_equipment.GetPercentageModifiers(Stat.kDefense), Is.Empty);
        }

        /* --- Evaluate --- */

        [Test]
        [Description("kHasItemEquipped is true only for the ID of an item that is currently equipped.")]
        public void Evaluate_HasItemEquipped_ReflectsWhatIsEquipped()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, new[] { m_sword.ID }), Is.True);
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, new[] { m_axe.ID }), Is.False);
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, new[] { "missing" }), Is.False);
        }

        [Test]
        [Description("Evaluate returns null (unhandled) for null or empty parameters and for predicates other than kHasItemEquipped.")]
        public void Evaluate_NullOrEmptyParameters_OrUnhandledPredicate_ReturnsNull()
        {
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, null), Is.Null);
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItemEquipped, new string[0]), Is.Null);
            Assert.That(m_equipment.Evaluate(ConditionType.kHasItem, new[] { m_sword.ID }), Is.Null);
        }

        /* --- Save / Restore --- */

        [Test]
        [Description("CaptureState writes one entry per occupied slot, keyed by slot enum name, valued by item ID.")]
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
        [Description("Capturing and restoring through JSON text restores every equipped item and raises OnEquipmentChanged once.")]
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
        [Description("RestoreState replaces the current equipment; slots absent from the save are cleared.")]
        public void RestoreState_ReplacesWhatWasEquipped()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            m_equipment.RestoreState(new JObject { ["kHelmet"] = m_helm.ID });

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kHelmet), Is.SameAs(m_helm));
        }

        [Test]
        [Description("An unknown item ID in the save is dropped, but OnEquipmentChanged and OnStatModified still fire.")]
        public void RestoreState_UnknownItemId_IsDropped_ButEventsStillFire()
        {
            m_equipment.RestoreState(new JObject { ["kWeapon"] = "gone" });

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.Null);
            Assert.That(m_equipment.OccupiedSlots, Is.Empty);
            Assert.That(m_equipmentChanged, Is.EqualTo(1));
            Assert.That(m_statsModified, Is.EqualTo(1));
        }

        [Test]
        [Description("Restoring an unknown item ID logs a warning that names the missing ID.")]
        public void RestoreState_UnknownItemId_LogsAWarningNamingIt()
        {
            LogAssert.Expect(LogType.Warning, new Regex("gone"));

            m_equipment.RestoreState(new JObject { ["kWeapon"] = "gone" });
        }

        [Test]
        [Description("Saved entries whose item isn't equipable, or whose key isn't a valid slot name, are dropped.")]
        public void RestoreState_NonEquipableItem_OrUnknownSlot_IsDropped()
        {
            m_equipment.RestoreState(new JObject { ["kWeapon"] = m_potion.ID, ["kTail"] = m_sword.ID });

            Assert.That(m_equipment.OccupiedSlots, Is.Empty);
        }

        [Test]
        [Description("A null or non-object state is ignored, as if the save never held Equipment: what is equipped is kept and no event fires.")]
        public void RestoreState_NonObject_IsIgnored()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            ResetCounts();

            m_equipment.RestoreState(null);
            m_equipment.RestoreState(new JValue(5));

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_equipmentChanged, Is.EqualTo(0));
            Assert.That(m_statsModified, Is.EqualTo(0));
        }

        /* ============================== EquipableItem.OnUse ============================== */

        [Test]
        [Description("Using an equipable item equips it into its target slot and removes it from the inventory.")]
        public void OnUse_EquipsTheItem_AndRemovesItFromTheInventory()
        {
            m_inventory.TryAddToAvailableSlot(m_sword);

            m_sword.OnUse(m_inventory, 0);

            Assert.That(m_equipment.GetItemInSlot(EquipmentSlot.kWeapon), Is.SameAs(m_sword));
            Assert.That(m_inventory.GetItemAtSlot(0), Is.Null);
            Assert.That(m_inventory.GetEmptySlotCount(), Is.EqualTo(kSlots));
        }

        [Test]
        [Description("Using an item whose slot is occupied swaps the two in place, without raising OnItemAdded, and restores the suppress flag.")]
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
        [Description("Using an equipable item with no target slot does nothing; it stays in the inventory.")]
        public void OnUse_WithNoTargetSlot_DoesNothing()
        {
            EquipableItem trinket = m_kit.Item<EquipableItem>("trinket");
            m_inventory.TryAddToAvailableSlot(trinket);

            trinket.OnUse(m_inventory, 0);

            Assert.That(m_inventory.GetItemAtSlot(0), Is.SameAs(trinket));
            Assert.That(m_equipment.OccupiedSlots, Is.Empty);
        }

        [Test]
        [Description("CanEquip is true only for the item's own target slot.")]
        public void CanEquip_RequiresTheItemsOwnTargetSlot()
        {
            Assert.That(m_sword.CanEquip(EquipmentSlot.kWeapon, m_equipment), Is.True);
            Assert.That(m_sword.CanEquip(EquipmentSlot.kHelmet, m_equipment), Is.False);
        }
    }
}
/*---------------------------
| File: EnemyLootTableTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Inventories;

namespace PolyQuest.Tests
{
    public class EnemyLootTableTests
    {
        private InventoryTestKit m_kit;
        private EnemyLootTable m_table;
        private Equipment m_equipment;

        private InventoryItem m_gold, m_gem, m_trinket;
        private EquipableItem m_sword;

        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            m_gold = m_kit.Item<ActionItem>("gold-ish", stackable: true);
            m_gem = m_kit.Item<ActionItem>("gem", stackable: true);
            m_trinket = m_kit.Item<ActionItem>("trinket", stackable: false);
            m_sword = m_kit.Equipable<EquipableItem>("sword", EquipmentSlot.kWeapon);

            m_table = m_kit.Track(ScriptableObject.CreateInstance<EnemyLootTable>());

            GameObject go = m_kit.NewObject("LootSubject");
            go.AddComponent<BaseStats>();
            m_equipment = go.AddComponent<Equipment>();
            InventoryTestKit.InvokeAwake(m_equipment);
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        private struct Entry
        {
            public InventoryItem Item;
            public float[] Chance;
            public int[] Min;
            public int[] Max;
        }

        private static Entry E(InventoryItem item, float[] chance = null, int[] min = null, int[] max = null)
            => new Entry { Item = item, Chance = chance ?? new float[0], Min = min ?? new int[0], Max = max ?? new int[0] };

        private void Configure(string arrayField, params Entry[] entries)
        {
            SerializedObject so = new SerializedObject(m_table);
            SerializedProperty array = so.FindProperty(arrayField);
            Assert.That(array, Is.Not.Null, arrayField);
            array.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; ++i)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("m_item").objectReferenceValue = entries[i].Item;
                SetFloats(element.FindPropertyRelative("m_relativeChance"), entries[i].Chance);
                SetInts(element.FindPropertyRelative("m_minQuantity"), entries[i].Min);
                SetInts(element.FindPropertyRelative("m_maxQuantity"), entries[i].Max);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void ConfigureRolls(float[] chance, int[] min, int[] max)
        {
            SerializedObject so = new SerializedObject(m_table);
            SetFloats(so.FindProperty("m_dropChancePercentage"), chance);
            SetInts(so.FindProperty("m_minDrops"), min);
            SetInts(so.FindProperty("m_maxDrops"), max);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloats(SerializedProperty p, float[] v)
        {
            p.arraySize = v.Length;
            for (int i = 0; i < v.Length; ++i) p.GetArrayElementAtIndex(i).floatValue = v[i];
        }

        private static void SetInts(SerializedProperty p, int[] v)
        {
            p.arraySize = v.Length;
            for (int i = 0; i < v.Length; ++i) p.GetArrayElementAtIndex(i).intValue = v[i];
        }

        private EnemyLootTable.Dropped[] Roll(int level) => m_table.GetRandomDrops(level, m_equipment).ToArray();

        /* --- Weapon --- */

        [Test]
        [Description("The enemy's equipped weapon always drops (quantity 1), even when the loot table is empty.")]
        public void EquippedWeapon_AlwaysDrops_EvenWithAnEmptyTable()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Item, Is.SameAs(m_sword));
            Assert.That(drops[0].Quantity, Is.EqualTo(1));
        }

        [Test]
        [Description("With no weapon equipped and an empty table, nothing drops.")]
        public void NoWeapon_EmptyTable_DropsNothing()
        {
            Assert.That(Roll(1), Is.Empty);
        }

        /* --- Guaranteed drops --- */

        [Test]
        [Description("A stackable guaranteed drop takes its quantity from the min/max range for the level.")]
        public void GuaranteedDrop_StackableItem_UsesQuantityRangeForTheLevel()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 3 }, max: new[] { 3 }));

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Item, Is.SameAs(m_gold));
            Assert.That(drops[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        [Description("A non-stackable guaranteed drop always has quantity 1, whatever its configured range.")]
        public void GuaranteedDrop_NonStackableItem_AlwaysQuantityOne()
        {
            Configure("m_guaranteedDrops", E(m_trinket, min: new[] { 5 }, max: new[] { 9 }));

            Assert.That(Roll(1)[0].Quantity, Is.EqualTo(1));
        }

        [Test]
        [Description("Guaranteed entries with a null item are skipped; the rest still drop.")]
        public void GuaranteedDrop_NullItem_IsSkipped()
        {
            Configure("m_guaranteedDrops", E(null), E(m_gold, min: new[] { 1 }, max: new[] { 1 }));

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Item, Is.SameAs(m_gold));
        }

        [Test]
        [Description("Guaranteed drops are not subject to the drop-chance roll; they drop at 0% chance.")]
        public void GuaranteedDrop_IgnoresDropChance()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 2 }, max: new[] { 2 }));
            ConfigureRolls(new[] { 0f }, new[] { 5 }, new[] { 5 });

            Assert.That(Roll(1).Length, Is.EqualTo(1));
        }

        [Test]
        [Description("The equipped weapon, the guaranteed drops and the random drops all drop together from one roll.")]
        public void Weapon_Guaranteed_AndRandomDrops_AllDropTogether()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 1 }, max: new[] { 1 }));
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }, min: new[] { 1 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 100f }, new[] { 1 }, new[] { 1 });

            var items = Roll(1).Select(d => d.Item).ToArray();

            Assert.That(items, Is.EquivalentTo(new InventoryItem[] { m_sword, m_gold, m_gem }));
        }

        [Test]
        [Description("A stackable guaranteed drop whose quantity rolls 0 drops nothing; no drop ever has quantity 0.")]
        public void GuaranteedDrop_StackableRollingZero_DropsNothing()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 0 }, max: new[] { 0 }));

            Assert.That(Roll(1), Is.Empty);
        }

        [Test]
        [Description("A random drop of a stackable whose quantity rolls 0 is left out; the other drops of the roll still drop.")]
        public void RandomDrop_StackableRollingZero_IsLeftOut()
        {
            Configure("m_guaranteedDrops", E(m_gem, min: new[] { 2 }, max: new[] { 2 }));
            Configure("m_possibleDrops", E(m_gold, chance: new[] { 1f }, min: new[] { 0 }, max: new[] { 0 }));
            ConfigureRolls(new[] { 100f }, new[] { 3 }, new[] { 3 });

            var drops = Roll(1);

            Assert.That(drops.Select(d => d.Item), Is.EqualTo(new InventoryItem[] { m_gem }));
        }

        /* --- Random drops --- */

        [Test]
        [Description("At 0% drop chance, no random drops are rolled.")]
        public void DropChanceZero_NoRandomDrops()
        {
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }, min: new[] { 1 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 0f }, new[] { 3 }, new[] { 3 });

            Assert.That(Roll(1), Is.Empty);
        }

        [Test]
        [Description("At 100% drop chance, the drop count comes from the min/max drops range and each drop uses the entry's quantity range.")]
        public void DropChance100_YieldsMinMaxManyDrops()
        {
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }, min: new[] { 2 }, max: new[] { 2 }));
            ConfigureRolls(new[] { 100f }, new[] { 3 }, new[] { 3 });

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(3));
            Assert.That(drops.All(d => d.Item == m_gem && d.Quantity == 2));
        }

        [Test]
        [Description("An entry with a relative chance of 0 is never selected.")]
        public void SelectRandomItem_ZeroWeightEntryIsNeverChosen()
        {
            Configure("m_possibleDrops",
                E(m_gem, chance: new[] { 0f }, min: new[] { 1 }, max: new[] { 1 }),
                E(m_gold, chance: new[] { 5f }, min: new[] { 1 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 100f }, new[] { 50 }, new[] { 50 });

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(50));
            Assert.That(drops.All(d => d.Item == m_gold));
        }

        [Test]
        [Description("A table that rolls random drops but has no possible drops to pick from drops nothing.")]
        public void NoPossibleDrops_DropsNothing()
        {
            ConfigureRolls(new[] { 100f }, new[] { 2 }, new[] { 2 });

            Assert.That(Roll(1), Is.Empty);
        }

        [Test]
        [Description("A possible-drop entry with no item assigned drops nothing when it is selected.")]
        public void PossibleDrop_WithNullItem_DropsNothing()
        {
            Configure("m_possibleDrops", E(null, chance: new[] { 1f }));
            ConfigureRolls(new[] { 100f }, new[] { 1 }, new[] { 1 });

            Assert.That(Roll(1), Is.Empty);
        }

        /* --- GetByLevel behaviour (observed through the public API) --- */

        [Test]
        [Description("Per-level arrays are read at index level - 1.")]
        public void Level_SelectsArrayIndexLevelMinusOne()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 1, 2, 3 }, max: new[] { 1, 2, 3 }));

            Assert.That(Roll(1)[0].Quantity, Is.EqualTo(1));
            Assert.That(Roll(2)[0].Quantity, Is.EqualTo(2));
            Assert.That(Roll(3)[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        [Description("An enemy level past the end of a per-level array uses the array's last entry.")]
        public void Level_BeyondArrayLength_UsesLastEntry()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 1, 7 }, max: new[] { 1, 7 }));

            Assert.That(Roll(3)[0].Quantity, Is.EqualTo(7));
        }

        [Test]
        [Description("Empty per-level arrays read as their default value, so an empty drop-chance array means 0% chance.")]
        public void EmptyOrNullArrays_ReadAsDefault()
        {
            ConfigureRolls(new float[0], new int[0], new int[0]);
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }));

            Assert.That(Roll(1), Is.Empty, "0% chance by default");
        }
    }
}
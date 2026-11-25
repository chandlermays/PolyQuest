/*---------------------------
| File: EnemyLootTableTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
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

        /*------------------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for enemy loot table testing --- |
        ------------------------------------------------------------------------------------*/
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
        /*--------------------------------------------------------------------------------------------
        | --- Entry: One loot-table entry: its item and its per-level chance and quantity arrays --- |
        --------------------------------------------------------------------------------------------*/
        private struct Entry
        {
            public InventoryItem Item;
            public float[] Chance;
            public int[] Min;
            public int[] Max;
        }

        /*-----------------------------------------------------------------
        | --- E: Builds an entry, treating any omitted array as empty --- |
        -----------------------------------------------------------------*/
        private static Entry E(InventoryItem item, float[] chance = null, int[] min = null, int[] max = null)
            => new Entry { Item = item, Chance = chance ?? new float[0], Min = min ?? new int[0], Max = max ?? new int[0] };

        /*-----------------------------------------------------------------------------
        | --- Configure: Writes the given entries into the named loot-table array --- |
        -----------------------------------------------------------------------------*/
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

        /*--------------------------------------------------------------------------------
        | --- ConfigureRolls: Sets the per-level drop chance and min/max drop counts --- |
        --------------------------------------------------------------------------------*/
        private void ConfigureRolls(float[] chance, int[] min, int[] max)
        {
            SerializedObject so = new SerializedObject(m_table);
            SetFloats(so.FindProperty("m_dropChancePercentage"), chance);
            SetInts(so.FindProperty("m_minDrops"), min);
            SetInts(so.FindProperty("m_maxDrops"), max);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /*--------------------------------------------------------------------------
        | --- SetFloats: Writes a float array into a serialized array property --- |
        --------------------------------------------------------------------------*/
        private static void SetFloats(SerializedProperty p, float[] v)
        {
            p.arraySize = v.Length;
            for (int i = 0; i < v.Length; ++i) p.GetArrayElementAtIndex(i).floatValue = v[i];
        }

        /*-----------------------------------------------------------------------
        | --- SetInts: Writes an int array into a serialized array property --- |
        -----------------------------------------------------------------------*/
        private static void SetInts(SerializedProperty p, int[] v)
        {
            p.arraySize = v.Length;
            for (int i = 0; i < v.Length; ++i) p.GetArrayElementAtIndex(i).intValue = v[i];
        }

        /*----------------------------------------------------------
        | --- Roll: Rolls the table's drops at the given level --- |
        ----------------------------------------------------------*/
        private EnemyLootTable.Dropped[] Roll(int level) => m_table.GetRandomDrops(level, m_equipment).ToArray();

        /*---------------------------------------------------------------------------
        | --- Validate: Runs the table's editor-only OnValidate, as Unity would --- |
        ---------------------------------------------------------------------------*/
        private void Validate()
        {
            MethodInfo onValidate = typeof(EnemyLootTable).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onValidate, Is.Not.Null, "EnemyLootTable.OnValidate was not found. Was it renamed?");
            onValidate.Invoke(m_table, null);
        }


        /*----------------
        | --- Weapon --- |
        ----------------*/
        /*---------------------------------------------------------------------------------------------------------------------------------------------------
        | --- EquippedWeapon_AlwaysDrops_EvenWithAnEmptyTable: The enemy's equipped weapon always drops (quantity 1), even when the loot table is empty --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------*/
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

        /*-----------------------------------------------------------------------------------------------------
        | --- NoWeapon_EmptyTable_DropsNothing: With no weapon equipped and an empty table, nothing drops --- |
        -----------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("With no weapon equipped and an empty table, nothing drops.")]
        public void NoWeapon_EmptyTable_DropsNothing()
        {
            Assert.That(Roll(1), Is.Empty);
        }


        /*--------------------------
        | --- Guaranteed Drops --- |
        --------------------------*/
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- GuaranteedDrop_StackableItem_UsesQuantityRangeForTheLevel: A stackable guaranteed drop takes its quantity from the min/max range for the level --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------*/
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

        /*-------------------------------------------------------------------------------------------------------------------------------------------------
        | --- GuaranteedDrop_NonStackableItem_AlwaysQuantityOne: A non-stackable guaranteed drop always has quantity 1, whatever its configured range --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A non-stackable guaranteed drop always has quantity 1, whatever its configured range.")]
        public void GuaranteedDrop_NonStackableItem_AlwaysQuantityOne()
        {
            Configure("m_guaranteedDrops", E(m_trinket, min: new[] { 5 }, max: new[] { 9 }));

            Assert.That(Roll(1)[0].Quantity, Is.EqualTo(1));
        }

        /*-----------------------------------------------------------------------------------------------------------------
        | --- GuaranteedDrop_NullItem_IsSkipped: Guaranteed entries with a null item are skipped; the rest still drop --- |
        -----------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Guaranteed entries with a null item are skipped; the rest still drop.")]
        public void GuaranteedDrop_NullItem_IsSkipped()
        {
            Configure("m_guaranteedDrops", E(null), E(m_gold, min: new[] { 1 }, max: new[] { 1 }));

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Item, Is.SameAs(m_gold));
        }

        /*----------------------------------------------------------------------------------------------------------------------------
        | --- GuaranteedDrop_IgnoresDropChance: Guaranteed drops are not subject to the drop-chance roll; they drop at 0% chance --- |
        ----------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Guaranteed drops are not subject to the drop-chance roll; they drop at 0% chance.")]
        public void GuaranteedDrop_IgnoresDropChance()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 2 }, max: new[] { 2 }));
            ConfigureRolls(new[] { 0f }, new[] { 5 }, new[] { 5 });

            Assert.That(Roll(1).Length, Is.EqualTo(1));
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Weapon_Guaranteed_AndRandomDrops_AllDropTogether: The equipped weapon, the guaranteed drops and the random drops all drop together from one roll --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------------*/
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

        /*---------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- GuaranteedDrop_StackableRollingZero_DropsNothing: A stackable guaranteed drop whose quantity rolls 0 drops nothing; no drop ever has quantity 0 --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A stackable guaranteed drop whose quantity rolls 0 drops nothing; no drop ever has quantity 0.")]
        public void GuaranteedDrop_StackableRollingZero_DropsNothing()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 0 }, max: new[] { 0 }));

            Assert.That(Roll(1), Is.Empty);
        }

        /*------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RandomDrop_StackableRollingZero_IsLeftOut: A random drop of a stackable whose quantity rolls 0 is left out; the other drops of the roll still drop --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------------*/
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


        /*----------------------
        | --- Random Drops --- |
        ----------------------*/
        /*-------------------------------------------------------------------------------------
        | --- DropChanceZero_NoRandomDrops: At 0% drop chance, no random drops are rolled --- |
        -------------------------------------------------------------------------------------*/
        [Test]
        [Description("At 0% drop chance, no random drops are rolled.")]
        public void DropChanceZero_NoRandomDrops()
        {
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }, min: new[] { 1 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 0f }, new[] { 3 }, new[] { 3 });

            Assert.That(Roll(1), Is.Empty);
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- DropChance100_YieldsMinMaxManyDrops: At 100% drop chance, the drop count comes from the min/max drops range and each drop uses the entry's quantity range --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
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

        /*---------------------------------------------------------------------------------------------------------------
        | --- SelectRandomItem_ZeroWeightEntryIsNeverChosen: An entry with a relative chance of 0 is never selected --- |
        ---------------------------------------------------------------------------------------------------------------*/
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

        /*----------------------------------------------------------------------------------------------------------------------------------------------------
        | --- EveryWeightZeroAtTheLevel_DropsNothing: When every entry has weight 0 at a level, that level's pool drops nothing; other levels still drop --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("When every entry has weight 0 at a level, that level's pool drops nothing (no fallback to an entry); levels with weight still drop.")]
        public void EveryWeightZeroAtTheLevel_DropsNothing()
        {
            Configure("m_possibleDrops",
                E(m_gem, chance: new[] { 0f, 1f }, min: new[] { 1, 1 }, max: new[] { 1, 1 }),
                E(m_gold, chance: new[] { 0f, 1f }, min: new[] { 1, 1 }, max: new[] { 1, 1 }));
            ConfigureRolls(new[] { 100f, 100f }, new[] { 5, 5 }, new[] { 5, 5 });

            Assert.That(Roll(1), Is.Empty);
            Assert.That(Roll(2).Length, Is.EqualTo(5));
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------
        | --- MaxBelowMin_IsClampedToMin: A max below the min (for a quantity or the drop count) is read as the min instead of an inverted range --- |
        --------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A max below the min, for a quantity or for the number of drops, is read as the min instead of passing an inverted range.")]
        public void MaxBelowMin_IsClampedToMin()
        {
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }, min: new[] { 3 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 100f }, new[] { 2 }, new[] { 1 });

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(2));
            Assert.That(drops.All(d => d.Item == m_gem && d.Quantity == 3));
        }

        /*----------------------------------------------------------------------------------------------------------------------------
        | --- NoPossibleDrops_DropsNothing: A table that rolls random drops but has no possible drops to pick from drops nothing --- |
        ----------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A table that rolls random drops but has no possible drops to pick from drops nothing.")]
        public void NoPossibleDrops_DropsNothing()
        {
            ConfigureRolls(new[] { 100f }, new[] { 2 }, new[] { 2 });

            Assert.That(Roll(1), Is.Empty);
        }

        /*-------------------------------------------------------------------------------------------------------------------------------
        | --- PossibleDrop_WithNullItem_DropsNothing: A possible-drop entry with no item assigned drops nothing when it is selected --- |
        -------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A possible-drop entry with no item assigned drops nothing when it is selected.")]
        public void PossibleDrop_WithNullItem_DropsNothing()
        {
            Configure("m_possibleDrops", E(null, chance: new[] { 1f }));
            ConfigureRolls(new[] { 100f }, new[] { 1 }, new[] { 1 });

            Assert.That(Roll(1), Is.Empty);
        }


        /*--------------------------
        | --- Per-Level Arrays --- |
        --------------------------*/
        /*--------------------------------------------------------------------------------------------
        | --- Level_SelectsArrayIndexLevelMinusOne: Per-level arrays are read at index level - 1 --- |
        --------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Per-level arrays are read at index level - 1.")]
        public void Level_SelectsArrayIndexLevelMinusOne()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 1, 2, 3 }, max: new[] { 1, 2, 3 }));

            Assert.That(Roll(1)[0].Quantity, Is.EqualTo(1));
            Assert.That(Roll(2)[0].Quantity, Is.EqualTo(2));
            Assert.That(Roll(3)[0].Quantity, Is.EqualTo(3));
        }

        /*-----------------------------------------------------------------------------------------------------------------------------
        | --- Level_BeyondArrayLength_UsesLastEntry: An enemy level past the end of a per-level array uses the array's last entry --- |
        -----------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("An enemy level past the end of a per-level array uses the array's last entry.")]
        public void Level_BeyondArrayLength_UsesLastEntry()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 1, 7 }, max: new[] { 1, 7 }));

            Assert.That(Roll(3)[0].Quantity, Is.EqualTo(7));
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------
        | --- EmptyOrNullArrays_ReadAsDefault: Empty per-level arrays read as their default value, so an empty drop-chance array means 0% chance --- |
        --------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Empty per-level arrays read as their default value, so an empty drop-chance array means 0% chance.")]
        public void EmptyOrNullArrays_ReadAsDefault()
        {
            ConfigureRolls(new float[0], new int[0], new int[0]);
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }));

            Assert.That(Roll(1), Is.Empty, "0% chance by default");
        }


        /*-------------------------------------
        | --- Authoring Warnings (Editor) --- |
        -------------------------------------*/
        /*----------------------------------------------------------------------------------------------------------------------------------
        | --- OnValidate_GuaranteedDropThatCanOnlyRollZero_Warns: A guaranteed stackable whose quantity is 0 at every level is flagged --- |
        ----------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A guaranteed stackable whose quantity is 0 at every level (like an unfilled entry) gets an editor warning naming it.")]
        public void OnValidate_GuaranteedDropThatCanOnlyRollZero_Warns()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 0, 0 }, max: new[] { 0, 0 }));
            LogAssert.Expect(LogType.Warning, new Regex("gold-ish.*can only roll a quantity of 0"));

            Validate();
        }

        /*---------------------------------------------------------------------------------------------------------
        | --- OnValidate_MaxBelowMin_Warns: A quantity range whose max is below its min at a level is flagged --- |
        ---------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A quantity range whose max is below its min at a level gets an editor warning naming the entry and the level.")]
        public void OnValidate_MaxBelowMin_Warns()
        {
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f, 1f }, min: new[] { 1, 4 }, max: new[] { 2, 3 }));
            LogAssert.Expect(LogType.Warning, new Regex("gem.*max quantity below its min at level 2"));

            Validate();
        }

        /*--------------------------------------------------------------------------------------------------------------------
        | --- OnValidate_MismatchedQuantityArrays_Warns: A stackable with different min and max array lengths is flagged --- |
        --------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A stackable entry with a different number of min and max quantities gets an editor warning naming it.")]
        public void OnValidate_MismatchedQuantityArrays_Warns()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 1, 1, 1 }, max: new[] { 2, 2 }));
            LogAssert.Expect(LogType.Warning, new Regex("gold-ish.*3 min quantities but 2 max quantities"));

            Validate();
        }

        /*-----------------------------------------------------------------------------------------------------------------------
        | --- OnValidate_PossibleDropsAllWeightZero_Warns: Possible drops that all have weight 0 at every level are flagged --- |
        -----------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A table whose possible drops all have weight 0 at every level gets an editor warning: none of them can ever drop.")]
        public void OnValidate_PossibleDropsAllWeightZero_Warns()
        {
            Configure("m_possibleDrops",
                E(m_gem, chance: new[] { 0f, 0f }, min: new[] { 1 }, max: new[] { 1 }),
                E(m_gold, chance: new[] { 0f }, min: new[] { 1 }, max: new[] { 1 }));
            LogAssert.Expect(LogType.Warning, new Regex("every one has a weight of 0 at every level"));

            Validate();
        }
    }
}
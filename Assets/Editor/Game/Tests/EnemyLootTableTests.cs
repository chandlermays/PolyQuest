/*---------------------------
| File: EnemyLootTableTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
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
        public void EquippedWeapon_AlwaysDrops_EvenWithAnEmptyTable()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Item, Is.SameAs(m_sword));
            Assert.That(drops[0].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void NoWeapon_EmptyTable_DropsNothing()
        {
            Assert.That(Roll(1), Is.Empty);
        }

        [Test]
        public void NullEquipment_Throws()
        {
            Assert.That(() => m_table.GetRandomDrops(1, null).ToArray(), Throws.Exception);
        }

        [Test]
        [Description("[Characterizes quirk] GetRandomDrops is a lazy iterator: a null Equipment is not detected until enumeration.")]
        public void GetRandomDrops_IsLazy_NothingRunsUntilEnumerated()
        {
            Assert.DoesNotThrow(() => m_table.GetRandomDrops(1, null));
        }

        /* --- Guaranteed drops --- */

        [Test]
        public void GuaranteedDrop_StackableItem_UsesQuantityRangeForTheLevel()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 3 }, max: new[] { 3 }));

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Item, Is.SameAs(m_gold));
            Assert.That(drops[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void GuaranteedDrop_NonStackableItem_AlwaysQuantityOne()
        {
            Configure("m_guaranteedDrops", E(m_trinket, min: new[] { 5 }, max: new[] { 9 }));

            Assert.That(Roll(1)[0].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void GuaranteedDrop_NullItem_IsSkipped()
        {
            Configure("m_guaranteedDrops", E(null), E(m_gold, min: new[] { 1 }, max: new[] { 1 }));

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Item, Is.SameAs(m_gold));
        }

        [Test]
        [Description("[Characterizes quirk] Guaranteed drops are yielded even when the enemy's drop chance is 0.")]
        public void GuaranteedDrop_IgnoresDropChance()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 2 }, max: new[] { 2 }));
            ConfigureRolls(new[] { 0f }, new[] { 5 }, new[] { 5 });

            Assert.That(Roll(1).Length, Is.EqualTo(1));
        }

        [Test]
        public void Guaranteed_ComesAfterWeapon_BeforeRandomDrops()
        {
            m_equipment.AddItem(EquipmentSlot.kWeapon, m_sword);
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 1 }, max: new[] { 1 }));
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }, min: new[] { 1 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 100f }, new[] { 1 }, new[] { 1 });

            var items = Roll(1).Select(d => d.Item).ToArray();

            Assert.That(items, Is.EqualTo(new InventoryItem[] { m_sword, m_gold, m_gem }));
        }

        [Test]
        [Description("[Characterizes quirk] A stackable guaranteed drop with no quantity data at this level yields Quantity 0 (Random.Range(0, 1)).")]
        public void GuaranteedDrop_StackableWithoutQuantityArrays_YieldsZeroQuantity()
        {
            Configure("m_guaranteedDrops", E(m_gold));

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Quantity, Is.EqualTo(0));
        }

        /* --- Random drops --- */

        [Test]
        public void DropChanceZero_NoRandomDrops()
        {
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }, min: new[] { 1 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 0f }, new[] { 3 }, new[] { 3 });

            Assert.That(Roll(1), Is.Empty);
        }

        [Test]
        public void DropChance100_YieldsMinMaxManyDrops()
        {
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }, min: new[] { 2 }, max: new[] { 2 }));
            ConfigureRolls(new[] { 100f }, new[] { 3 }, new[] { 3 });

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(3));
            Assert.That(drops.All(d => d.Item == m_gem && d.Quantity == 2));
        }

        [Test]
        public void SelectRandomItem_ZeroWeightEntryIsNeverChosen()
        {
            Configure("m_possibleDrops",
                E(m_gem, chance: new[] { 0f }, min: new[] { 1 }, max: new[] { 1 }),
                E(m_gold, chance: new[] { 5f }, min: new[] { 1 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 100f }, new[] { 50 }, new[] { 50 });

            // A roll of exactly 0 would pick the zero-weight entry first (roll <= 0). Pin that edge separately.
            var drops = Roll(1);

            Assert.That(drops.Count(d => d.Item == m_gold), Is.GreaterThanOrEqualTo(48),
                "Only a roll of exactly 0.0 can select the zero-weight entry.");
        }

        [Test]
        [Description("[Characterizes quirk] With no weights configured, the first possible drop is always chosen.")]
        public void SelectRandomItem_NoWeights_FallsBackToFirstEntry()
        {
            Configure("m_possibleDrops",
                E(m_gem, min: new[] { 1 }, max: new[] { 1 }),
                E(m_gold, min: new[] { 1 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 100f }, new[] { 5 }, new[] { 5 });

            Assert.That(Roll(1).All(d => d.Item == m_gem));
        }

        [Test]
        [Description("[Characterizes quirk] With no possible drops configured, each roll yields default(Dropped) (null item, quantity 0) and logs a warning, instead of yielding nothing.")]
        public void NoPossibleDrops_YieldsEmptyDroppedStructs_AndWarns()
        {
            ConfigureRolls(new[] { 100f }, new[] { 2 }, new[] { 2 });
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no drop selected"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no drop selected"));

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(2));
            Assert.That(drops[0].Item == null);
            Assert.That(drops[0].Quantity, Is.EqualTo(0));
        }

        [Test]
        [Description("[Characterizes quirk] A possible-drop entry whose Item is null is selectable and produces default(Dropped), which downstream droppers must handle.")]
        public void PossibleDrop_WithNullItem_YieldsDefaultDropped()
        {
            Configure("m_possibleDrops", E(null, chance: new[] { 1f }));
            ConfigureRolls(new[] { 100f }, new[] { 1 }, new[] { 1 });
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no drop selected"));

            var drops = Roll(1);

            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Item == null);
        }

        /* --- GetByLevel behaviour (observed through the public API) --- */

        [Test]
        public void Level_SelectsArrayIndexLevelMinusOne()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 1, 2, 3 }, max: new[] { 1, 2, 3 }));

            Assert.That(Roll(1)[0].Quantity, Is.EqualTo(1));
            Assert.That(Roll(2)[0].Quantity, Is.EqualTo(2));
            Assert.That(Roll(3)[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void Level_BeyondArrayLength_UsesLastEntry()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 1, 7 }, max: new[] { 1, 7 }));

            Assert.That(Roll(99)[0].Quantity, Is.EqualTo(7));
        }

        [Test]
        [Description("[Characterizes quirk] Level 0 (or negative) reads default(T): quantity 0 and a 0% drop chance, rather than clamping to level 1.")]
        public void Level_Zero_ReadsDefaults_NotFirstEntry()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 4 }, max: new[] { 4 }));
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }, min: new[] { 1 }, max: new[] { 1 }));
            ConfigureRolls(new[] { 100f }, new[] { 1 }, new[] { 1 });

            var drops = Roll(0);

            Assert.That(drops.Length, Is.EqualTo(1), "Only the guaranteed drop; the random roll sees a 0% chance.");
            Assert.That(drops[0].Quantity, Is.EqualTo(0));
        }

        [Test]
        public void Level_Negative_BehavesLikeZero()
        {
            Configure("m_guaranteedDrops", E(m_gold, min: new[] { 4 }, max: new[] { 4 }));

            Assert.That(Roll(-5)[0].Quantity, Is.EqualTo(0));
        }

        [Test]
        public void EmptyOrNullArrays_ReadAsDefault()
        {
            ConfigureRolls(new float[0], new int[0], new int[0]);
            Configure("m_possibleDrops", E(m_gem, chance: new[] { 1f }));

            Assert.That(Roll(1), Is.Empty, "0% chance by default");
        }
    }
}
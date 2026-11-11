/*---------------------------
| File: DropperTests.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Inventories;
using PolyQuest.Pickups;
using PolyQuest.Saving;
using PolyQuest.UI;

namespace PolyQuest.Tests
{
    public class DropperTests
    {
        private PlayModeTestKit m_kit;
        private ActionItem m_potion, m_sword;
        private ItemDropper m_dropper;

        private NavMeshDataInstance m_navMesh;

        [SetUp]
        public void SetUp()
        {
            m_kit = new PlayModeTestKit();
            m_kit.BuildPlayer(inventorySize: 4);
            m_kit.BuildPickupTemplate();

            m_potion = m_kit.Item("potion", stackable: true, itemName: "Potion");
            m_sword = m_kit.Item("sword", stackable: false, itemName: "Sword");

            GameObject dropperObject = m_kit.Build("Dropper", go => go.AddComponent<ItemDropper>(), new Vector3(5f, 0f, 5f));
            m_dropper = dropperObject.GetComponent<ItemDropper>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_navMesh.valid)
                NavMesh.RemoveNavMeshData(m_navMesh);

            m_kit.Dispose();
        }

        /* --- Helpers --- */

        private static JObject Entry(string id, int quantity, Vector3 position, int sceneBuildIndex)
        {
            return new JObject
            {
                ["ItemID"] = id,
                ["Quantity"] = quantity,
                ["WorldPosition"] = position.ToToken(),
                ["SceneBuildIndex"] = sceneBuildIndex
            };
        }

        private static int CurrentScene => SceneManager.GetActiveScene().buildIndex;

        private void BuildFlatNavMesh()
        {
            var sources = new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(50f, 1f, 50f),
                    transform = Matrix4x4.TRS(new Vector3(0f, -0.5f, 0f), Quaternion.identity, Vector3.one),
                    area = 0
                }
            };
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(
                NavMesh.GetSettingsByID(0), sources, new Bounds(Vector3.zero, new Vector3(60f, 10f, 60f)), Vector3.zero, Quaternion.identity);
            m_navMesh = NavMesh.AddNavMeshData(data);
        }

        // ================================================================================
        //  ItemDropper: dropping
        // ================================================================================

        [Test]
        public void DropItem_SpawnsAPickupAtTheDropperPosition()
        {
            m_dropper.DropItem(m_potion, 3);

            Pickup pickup = m_kit.Spawned().Single();
            Assert.That(pickup.Item, Is.SameAs(m_potion));
            Assert.That(pickup.Quantity, Is.EqualTo(3));
            Assert.That(pickup.transform.position, Is.EqualTo(new Vector3(5f, 0f, 5f)));
        }

        [Test]
        public void DropItem_WithoutAQuantity_DropsOne()
        {
            m_dropper.DropItem(m_potion);

            Assert.That(m_kit.Spawned().Single().Quantity, Is.EqualTo(1));
        }

        [Test]
        public void DropItem_NonStackable_AlwaysDropsOne()
        {
            m_dropper.DropItem(m_sword, 7);

            Assert.That(m_kit.Spawned().Single().Quantity, Is.EqualTo(1));
        }

        [Test]
        public void CreatePickupAtLocation_UsesTheGivenLocation()
        {
            m_dropper.CreatePickupAtLocation(m_potion, new Vector3(1f, 2f, 3f), 2);

            Assert.That(m_kit.Spawned().Single().transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
        }

        [Test]
        public void DropItem_Null_Throws()
        {
            Assert.That(() => m_dropper.DropItem(null, 1), Throws.TypeOf<NullReferenceException>());
        }

        // ================================================================================
        //  ItemDropper: save / restore
        // ================================================================================

        [Test]
        public void CaptureState_DescribesEachPickup()
        {
            m_dropper.CreatePickupAtLocation(m_potion, new Vector3(1f, 2f, 3f), 4);

            JArray state = (JArray)m_dropper.CaptureState();

            Assert.That(state.Count, Is.EqualTo(1));
            Assert.That(state[0]["ItemID"].ToObject<string>(), Is.EqualTo("potion"));
            Assert.That(state[0]["Quantity"].ToObject<int>(), Is.EqualTo(4));
            Assert.That(state[0]["WorldPosition"].ToVector3(), Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(state[0]["SceneBuildIndex"].ToObject<int>(), Is.EqualTo(CurrentScene));
        }

        [Test]
        public void CaptureState_WithNoDrops_IsAnEmptyArray()
        {
            Assert.That(((JArray)m_dropper.CaptureState()).Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator CaptureState_IgnoresPickupsThatWereDestroyed()
        {
            m_dropper.DropItem(m_potion, 1);
            m_dropper.DropItem(m_sword, 1);
            UnityEngine.Object.Destroy(m_kit.Spawned().First().gameObject);
            yield return null;

            Assert.That(((JArray)m_dropper.CaptureState()).Count, Is.EqualTo(1));
        }

        [Test]
        public void RestoreState_RecreatesCurrentScenePickups()
        {
            JArray state = new JArray
            {
                Entry("potion", 3, new Vector3(1f, 0f, 1f), CurrentScene),
                Entry("sword", 1, new Vector3(2f, 0f, 2f), CurrentScene)
            };

            m_dropper.RestoreState(state);

            Pickup[] spawned = m_kit.Spawned().OrderBy(p => p.transform.position.x).ToArray();
            Assert.That(spawned.Length, Is.EqualTo(2));
            Assert.That(spawned[0].Item, Is.SameAs(m_potion));
            Assert.That(spawned[0].Quantity, Is.EqualTo(3));
            Assert.That(spawned[0].transform.position, Is.EqualTo(new Vector3(1f, 0f, 1f)));
            Assert.That(spawned[1].Item, Is.SameAs(m_sword));
        }

        [Test]
        public void CaptureThenRestore_RoundTrips()
        {
            m_dropper.CreatePickupAtLocation(m_potion, new Vector3(1f, 2f, 3f), 4);
            JToken saved = m_dropper.CaptureState();

            m_dropper.RestoreState(saved);

            JArray recaptured = (JArray)m_dropper.CaptureState();
            Assert.That(recaptured.Any(e => e["ItemID"].ToObject<string>() == "potion" && e["Quantity"].ToObject<int>() == 4));
        }

        [UnityTest]
        public IEnumerator RestoreState_ReplacesTheExistingDrops()
        {
            m_dropper.DropItem(m_potion, 1);
            m_dropper.DropItem(m_potion, 1);

            m_dropper.RestoreState(new JArray { Entry("sword", 1, Vector3.zero, CurrentScene) });
            yield return null;

            Pickup[] spawned = m_kit.Spawned();
            Assert.That(spawned.Length, Is.EqualTo(1));
            Assert.That(spawned[0].Item, Is.SameAs(m_sword));
        }

        [UnityTest]
        [Description("[Characterizes quirk] ClearExistingDrops only calls Destroy (deferred) and keeps the old references, so a CaptureState in the SAME frame as RestoreState reports the old drops plus the new ones. Next frame it is correct.")]
        public IEnumerator RestoreState_CaptureInTheSameFrame_ReportsOldAndNewDrops()
        {
            m_dropper.DropItem(m_potion, 1);

            m_dropper.RestoreState(new JArray { Entry("sword", 1, Vector3.zero, CurrentScene) });
            int sameFrame = ((JArray)m_dropper.CaptureState()).Count;

            yield return null;
            int nextFrame = ((JArray)m_dropper.CaptureState()).Count;

            Assert.That(sameFrame, Is.EqualTo(2));
            Assert.That(nextFrame, Is.EqualTo(1));
        }

        [Test]
        public void RestoreState_OtherSceneEntries_AreNotSpawned_ButAreKeptForTheNextCapture()
        {
            int otherScene = CurrentScene + 100;
            m_dropper.RestoreState(new JArray { Entry("potion", 2, new Vector3(9f, 0f, 9f), otherScene) });

            Assert.That(m_kit.Spawned(), Is.Empty);

            JArray captured = (JArray)m_dropper.CaptureState();
            Assert.That(captured.Count, Is.EqualTo(1));
            Assert.That(captured[0]["SceneBuildIndex"].ToObject<int>(), Is.EqualTo(otherScene));
            Assert.That(captured[0]["Quantity"].ToObject<int>(), Is.EqualTo(2));
            Assert.That(captured[0]["WorldPosition"].ToVector3(), Is.EqualTo(new Vector3(9f, 0f, 9f)));
        }

        [Test]
        public void RestoreState_ForgetsOtherSceneEntriesFromAnEarlierRestore()
        {
            m_dropper.RestoreState(new JArray { Entry("potion", 2, Vector3.zero, CurrentScene + 100) });

            m_dropper.RestoreState(new JArray());

            Assert.That(((JArray)m_dropper.CaptureState()).Count, Is.EqualTo(0));
        }

        [Test]
        public void RestoreState_NonArrayState_ChangesNothing()
        {
            m_dropper.DropItem(m_potion, 1);

            m_dropper.RestoreState(JValue.CreateNull());
            m_dropper.RestoreState(new JObject());

            Assert.That(((JArray)m_dropper.CaptureState()).Count, Is.EqualTo(1));
            Assert.That(m_kit.Spawned().Length, Is.EqualTo(1));
        }

        [UnityTest]
        [Description("[Characterizes quirk] An unknown item ID (e.g. an item removed from the project) makes FindByID return null and RestoreState throws; the existing drops were ALREADY cleared by then, and entries after the bad one are never restored.")]
        public IEnumerator RestoreState_UnknownItemID_Throws_AfterClearingTheExistingDrops()
        {
            m_dropper.DropItem(m_potion, 1);
            JArray state = new JArray
            {
                Entry("does-not-exist", 1, Vector3.zero, CurrentScene),
                Entry("sword", 1, Vector3.zero, CurrentScene)
            };

            Assert.That(() => m_dropper.RestoreState(state), Throws.TypeOf<NullReferenceException>());
            yield return null;

            Assert.That(m_kit.Spawned(), Is.Empty, "old drop destroyed, and the valid entry after the bad one was never restored");
        }

        [Test]
        public void RestoreState_UnknownItemID_InAnotherScene_IsKeptWithoutError()
        {
            Assert.DoesNotThrow(() =>
                m_dropper.RestoreState(new JArray { Entry("does-not-exist", 1, Vector3.zero, CurrentScene + 100) }));

            Assert.That(((JArray)m_dropper.CaptureState()).Count, Is.EqualTo(1));
        }

        // ================================================================================
        //  Pickup
        // ================================================================================

        [Test]
        public void Pickup_Setup_SetsTheWorldLabelToTheItemName()
        {
            m_dropper.DropItem(m_potion, 1);

            Pickup pickup = m_kit.Spawned().Single();
            WorldLabel label = pickup.GetComponent<WorldLabel>();
            Assert.That(PlayModeTestKit.Get<TextMeshProUGUI>(label, "m_labelText").text, Is.EqualTo("Potion"));
        }

        [Test]
        public void Pickup_CanBePickedUp_WhenThePlayerHasSpace()
        {
            m_dropper.DropItem(m_sword, 1);

            Assert.That(m_kit.Spawned().Single().CanBePickedUp(), Is.True);
        }

        [Test]
        public void Pickup_CanBePickedUp_FalseWhenTheInventoryIsFullOfOtherItems()
        {
            for (int i = 0; i < 4; ++i)
                m_kit.PlayerInventory.TryAddToAvailableSlot(m_kit.Item("filler" + i), 1);
            m_dropper.DropItem(m_sword, 1);

            Assert.That(m_kit.Spawned().Single().CanBePickedUp(), Is.False);
        }

        [Test]
        public void Pickup_CanBePickedUp_TrueForAStackableAlreadyOwned_EvenWhenFull()
        {
            m_kit.PlayerInventory.TryAddToAvailableSlot(m_potion, 1);
            for (int i = 0; i < 3; ++i)
                m_kit.PlayerInventory.TryAddToAvailableSlot(m_kit.Item("filler" + i), 1);
            m_dropper.DropItem(m_potion, 2);

            Assert.That(m_kit.Spawned().Single().CanBePickedUp(), Is.True);
        }

        [UnityTest]
        public IEnumerator Pickup_PickupItem_AddsToTheInventory_AndDestroysThePickup()
        {
            m_dropper.DropItem(m_potion, 3);
            Pickup pickup = m_kit.Spawned().Single();

            pickup.PickupItem();
            yield return null;

            Assert.That(m_kit.PlayerInventory.ContainsItem(m_potion), Is.True);
            Assert.That(m_kit.Spawned(), Is.Empty);
            int total = 0;
            for (int i = 0; i < m_kit.PlayerInventory.Size; ++i)
                if (m_kit.PlayerInventory.GetItemAtSlot(i) == m_potion) total += m_kit.PlayerInventory.GetQuantityAtSlot(i);
            Assert.That(total, Is.EqualTo(3));
        }

        [UnityTest]
        [Description("[Characterizes quirk] PickupItem ignores the result of TryAddToAvailableSlot: with a full inventory the pickup is destroyed and the item is silently lost. Callers must check CanBePickedUp first.")]
        public IEnumerator Pickup_PickupItem_WithAFullInventory_DestroysThePickupAndLosesTheItem()
        {
            for (int i = 0; i < 4; ++i)
                m_kit.PlayerInventory.TryAddToAvailableSlot(m_kit.Item("filler" + i), 1);
            m_dropper.DropItem(m_sword, 1);
            Pickup pickup = m_kit.Spawned().Single();

            pickup.PickupItem();
            yield return null;

            Assert.That(m_kit.PlayerInventory.ContainsItem(m_sword), Is.False);
            Assert.That(m_kit.Spawned(), Is.Empty);
        }

        // ================================================================================
        //  RandomDropper
        // ================================================================================

        private RandomDropper BuildEnemy(EnemyLootTable table, float distanceThreshold = 0.5f, int level = 1)
        {
            GameObject enemy = m_kit.Build("Enemy", go =>
            {
                BaseStats stats = go.AddComponent<BaseStats>();
                PlayModeTestKit.Set(stats, "m_level", level);
                go.AddComponent<Equipment>();
                RandomDropper dropper = go.AddComponent<RandomDropper>();
                PlayModeTestKit.Set(dropper, "m_enemyLootTable", table);
                PlayModeTestKit.Set(dropper, "m_distanceThreshold", distanceThreshold);
            });
            return enemy.GetComponent<RandomDropper>();
        }

        private EnemyLootTable BuildTable(Action<EnemyLootTable> configure)
        {
            EnemyLootTable table = m_kit.Track(ScriptableObject.CreateInstance<EnemyLootTable>());
            configure(table);
            return table;
        }

        private static Array Configs(params (InventoryItem item, float[] chance, int[] min, int[] max)[] entries)
        {
            Type configType = typeof(EnemyLootTable).GetNestedType("LootItemConfig", BindingFlags.NonPublic);
            Array array = Array.CreateInstance(configType, entries.Length);
            for (int i = 0; i < entries.Length; ++i)
            {
                object config = Activator.CreateInstance(configType, true);
                PlayModeTestKit.Set(config, "m_item", entries[i].item);
                PlayModeTestKit.Set(config, "m_relativeChance", entries[i].chance ?? new float[0]);
                PlayModeTestKit.Set(config, "m_minQuantity", entries[i].min ?? new int[0]);
                PlayModeTestKit.Set(config, "m_maxQuantity", entries[i].max ?? new int[0]);
                array.SetValue(config, i);
            }
            return array;
        }

        [Test]
        public void RandomDrop_SpawnsOnePickupPerDrop_WithTheRolledQuantity()
        {
            BuildFlatNavMesh();
            EnemyLootTable table = BuildTable(t =>
                PlayModeTestKit.Set(t, "m_guaranteedDrops", Configs((m_potion, null, new[] { 3 }, new[] { 3 }), (m_sword, null, null, null))));
            RandomDropper enemy = BuildEnemy(table);

            enemy.RandomDrop();

            Pickup[] spawned = m_kit.Spawned();
            Assert.That(spawned.Length, Is.EqualTo(2));
            Assert.That(spawned.Single(p => p.Item == m_potion).Quantity, Is.EqualTo(3));
            Assert.That(spawned.Single(p => p.Item == m_sword).Quantity, Is.EqualTo(1));
        }

        [Test]
        public void RandomDrop_UsesTheEnemysBaseStatsLevel()
        {
            BuildFlatNavMesh();
            EnemyLootTable table = BuildTable(t =>
                PlayModeTestKit.Set(t, "m_guaranteedDrops", Configs((m_potion, null, new[] { 1, 5 }, new[] { 1, 5 }))));

            BuildEnemy(table, level: 2).RandomDrop();

            Assert.That(m_kit.Spawned().Single().Quantity, Is.EqualTo(5));
        }

        [Test]
        public void RandomDrop_WithAnEmptyTable_DropsNothing()
        {
            BuildFlatNavMesh();
            RandomDropper enemy = BuildEnemy(BuildTable(_ => { }));

            enemy.RandomDrop();

            Assert.That(m_kit.Spawned(), Is.Empty);
        }

        [Test]
        public void RandomDrop_WithoutALootTable_Throws()
        {
            BuildFlatNavMesh();
            RandomDropper enemy = BuildEnemy(null);

            Assert.That(() => enemy.RandomDrop(), Throws.TypeOf<NullReferenceException>());
        }

        [Test]
        [Description("[Characterizes quirk] A drop roll with no possible drops configured yields default(Dropped); RandomDrop passes the null item straight to DropItem, so the loot table's warning is followed by a NullReferenceException.")]
        public void RandomDrop_WhenTheTableYieldsAnEmptyDrop_Throws()
        {
            BuildFlatNavMesh();
            EnemyLootTable table = BuildTable(t =>
            {
                PlayModeTestKit.Set(t, "m_dropChancePercentage", new[] { 100f });
                PlayModeTestKit.Set(t, "m_minDrops", new[] { 1 });
                PlayModeTestKit.Set(t, "m_maxDrops", new[] { 1 });
            });
            RandomDropper enemy = BuildEnemy(table);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no drop selected"));

            Assert.That(() => enemy.RandomDrop(), Throws.TypeOf<NullReferenceException>());
        }

        [Test]
        public void DropLocation_OnANavMesh_IsNearTheEnemy_AndOnTheMesh()
        {
            BuildFlatNavMesh();
            EnemyLootTable table = BuildTable(t =>
                PlayModeTestKit.Set(t, "m_guaranteedDrops", Configs((m_potion, null, new[] { 1 }, new[] { 1 }))));
            RandomDropper enemy = BuildEnemy(table, distanceThreshold: 0.5f);

            for (int i = 0; i < 10; ++i)
                enemy.RandomDrop();

            Pickup[] spawned = m_kit.Spawned();
            Assert.That(spawned.Length, Is.EqualTo(10));
            foreach (Pickup pickup in spawned)
            {
                Vector3 offset = pickup.transform.position - enemy.transform.position;
                Assert.That(new Vector2(offset.x, offset.z).magnitude, Is.LessThanOrEqualTo(0.5f + 1f + 0.01f));
                Assert.That(pickup.transform.position.y, Is.EqualTo(0f).Within(0.3f));
            }
        }

        [Test]
        [Description("[Characterizes quirk] With no NavMesh the dropper makes 20 failed attempts, logs a warning (editor only) and drops AT the enemy's feet.")]
        public void DropLocation_WithoutANavMesh_FallsBackToTheEnemyPosition_AndWarns()
        {
            NavMesh.RemoveAllNavMeshData();
            EnemyLootTable table = BuildTable(t =>
                PlayModeTestKit.Set(t, "m_guaranteedDrops", Configs((m_potion, null, new[] { 1 }, new[] { 1 }))));
            RandomDropper enemy = BuildEnemy(table);
            enemy.transform.position = new Vector3(3f, 0f, 4f);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Failed to find a valid drop location"));

            enemy.RandomDrop();

            Assert.That(m_kit.Spawned().Single().transform.position, Is.EqualTo(new Vector3(3f, 0f, 4f)));
        }
    }
}
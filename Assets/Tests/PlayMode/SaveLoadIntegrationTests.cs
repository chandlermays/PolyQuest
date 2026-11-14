/*---------------------------
| File: SaveLoadIntegrationTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    // The real Player prefab, the real SavingSystem and a real save file on disk (a throwaway slot, never the player's own saves).
    public class SaveLoadIntegrationTests
    {
        private PlayerRig m_rig;

        [SetUp]
        public void SetUp()
        {
            m_rig = new PlayerRig();
        }

        [TearDown]
        public void TearDown()
        {
            m_rig.Dispose();
        }

        /* --- Helpers --- */

        // Gives the Player something to lose: items, silver, XP/levels and some damage. Returns the damaged health.
        private float Seed()
        {
            m_rig.Inventory.TryAddToAvailableSlot(m_rig.StackableItem, 5);
            m_rig.Wallet.UpdateSilver(120);
            m_rig.LevelUpTo(3);
            m_rig.Damage(10f);
            return m_rig.Health.CurrentHealth;
        }

        /* ============================== Same Player ============================== */

        [UnityTest]
        [Description("Save, then change silver, inventory and health, then Load: silver, item quantity, level, experience and health return to their saved values on the same Player.")]
        public IEnumerator SaveThenLoad_RestoresWhatWasChangedAfterTheSave()
        {
            yield return null;
            float savedHealth = Seed();
            int savedSilver = m_rig.Wallet.CurrentSilver;
            int savedQuantity = m_rig.QuantityOf(m_rig.StackableItem);
            int savedLevel = m_rig.Stats.Level;
            float savedXp = m_rig.Experience.ExperiencePoints;
            SaveManager.Instance.Save();

            m_rig.Wallet.UpdateSilver(-50);
            m_rig.ClearInventory();
            m_rig.Health.ReplenishHealth(1000f);

            SaveManager.Instance.Load();

            Assert.That(m_rig.Wallet.CurrentSilver, Is.EqualTo(savedSilver));
            Assert.That(m_rig.QuantityOf(m_rig.StackableItem), Is.EqualTo(savedQuantity));
            Assert.That(m_rig.Stats.Level, Is.EqualTo(savedLevel));
            Assert.That(m_rig.Experience.ExperiencePoints, Is.EqualTo(savedXp));
            Assert.That(m_rig.Health.CurrentHealth, Is.EqualTo(savedHealth).Within(1e-3f));
        }

        [UnityTest]
        [Description("The save file holds an entry under the Player's UID with a key for every ISaveable component on the Player.")]
        public IEnumerator Save_CapturesEverySaveableComponentOnThePlayer()
        {
            yield return null;
            SaveManager.Instance.Save();

            JObject file = JObject.Parse(File.ReadAllText(PlayerRig.SavePath));
            string uid = m_rig.Player.GetComponent<SaveableEntity>().UID;

            Assert.That(file.ContainsKey(uid), Is.True, "the Player's UID is missing from the save file");
            JObject player = (JObject)file[uid];
            foreach (ISaveable saveable in m_rig.Player.GetComponents<ISaveable>())
                Assert.That(player.ContainsKey(saveable.GetType().ToString()), Is.True, saveable.GetType().Name);
        }

        /* ============================== A fresh Player (what a scene reload / Continue does) ============================== */

        [UnityTest]
        [Description("Loading into a newly spawned Player (as a scene reload or Continue does) restores silver, items, the equipped item, level and experience.")]
        public IEnumerator Load_IntoAFreshPlayer_RestoresInventoryEquipmentWalletAndProgression()
        {
            yield return null;
            Seed();
            m_rig.Equipment.AddItem(m_rig.Equipable.TargetEquipmentSlot, m_rig.Equipable);
            int silver = m_rig.Wallet.CurrentSilver;
            int quantity = m_rig.QuantityOf(m_rig.StackableItem);
            int level = m_rig.Stats.Level;
            float xp = m_rig.Experience.ExperiencePoints;
            SaveManager.Instance.Save();

            m_rig.SpawnPlayer();
            yield return null;
            Assert.That(m_rig.Stats.Level, Is.EqualTo(1), "fixture: a new Player should start at level 1");
            Assert.That(m_rig.Wallet.CurrentSilver, Is.EqualTo(0), "fixture: a new Player should start with an empty wallet");

            SaveManager.Instance.Load();

            Assert.That(m_rig.Wallet.CurrentSilver, Is.EqualTo(silver));
            Assert.That(m_rig.QuantityOf(m_rig.StackableItem), Is.EqualTo(quantity));
            Assert.That(m_rig.Equipment.GetItemInSlot(m_rig.Equipable.TargetEquipmentSlot), Is.SameAs(m_rig.Equipable));
            Assert.That(m_rig.Stats.Level, Is.EqualTo(level));
            Assert.That(m_rig.Experience.ExperiencePoints, Is.EqualTo(xp));
        }

        [UnityTest]
        [Description("Loading a damaged level-3 save into a fresh level-1 Player restores exactly the saved health, whatever order the Player's components restore in.")]
        public IEnumerator Load_IntoAFreshPlayer_DoesNotChangeTheSavedHealth()
        {
            yield return null;
            float savedHealth = Seed();
            float savedMax = m_rig.Stats.GetHealth();
            Assert.That(savedHealth, Is.LessThan(savedMax), "fixture: the Player must be damaged when saved");
            SaveManager.Instance.Save();

            m_rig.SpawnPlayer();
            yield return null;
            Assume.That(m_rig.Stats.GetHealth(), Is.LessThan(savedMax), "fixture: level 1 must have less max health than level 3");

            SaveManager.Instance.Load();

            Assert.That(m_rig.Health.CurrentHealth, Is.EqualTo(savedHealth).Within(1e-3f),
                        $"saved {savedHealth}/{savedMax}, loaded {m_rig.Health.CurrentHealth}");
        }

        /* ============================== Bad or missing files ============================== */

        [UnityTest]
        [Description("Load with no save file on disk does not throw and leaves the current game state unchanged.")]
        public IEnumerator Load_WithNoSaveFile_ChangesNothing()
        {
            yield return null;
            m_rig.Wallet.UpdateSilver(75);
            Assert.That(File.Exists(PlayerRig.SavePath), Is.False, "fixture: no save file should exist");

            Assert.DoesNotThrow(() => SaveManager.Instance.Load());

            Assert.That(m_rig.Wallet.CurrentSilver, Is.EqualTo(75));
        }

        [UnityTest]
        [Description("Loading a save file that is not valid JSON, with nothing to fall back to, does not throw and leaves the current game state as it was.")]
        public IEnumerator Load_CorruptSaveFile_DoesNotThrow_AndLeavesTheGameUntouched()
        {
            yield return null;
            m_rig.Wallet.UpdateSilver(75);
            File.WriteAllText(PlayerRig.SavePath, "{ this is not json");
            LogAssert.ignoreFailingMessages = true;         // for this test only: how the unreadable file is reported isn't pinned here

            Assert.DoesNotThrow(() => SaveManager.Instance.Load());

            Assert.That(m_rig.Wallet.CurrentSilver, Is.EqualTo(75));
        }

        [UnityTest]
        [Description("Saving over a save file that is not valid JSON does not throw and leaves a readable save holding the Player's current state.")]
        public IEnumerator Save_OverACorruptSaveFile_WritesAReadableSave()
        {
            yield return null;
            m_rig.Wallet.UpdateSilver(75);
            File.WriteAllText(PlayerRig.SavePath, "{ this is not json");
            LogAssert.ignoreFailingMessages = true;         // for this test only: how the unreadable file is reported isn't pinned here

            Assert.DoesNotThrow(() => SaveManager.Instance.Save());

            JObject file = null;
            Assert.DoesNotThrow(() => file = JObject.Parse(File.ReadAllText(PlayerRig.SavePath)), "the save file is still unreadable");
            Assert.That(file.ContainsKey(m_rig.Player.GetComponent<SaveableEntity>().UID), Is.True);
        }

        [UnityTest]
        [Description("A saved position that can't be read is ignored, as if the save never held it: the Player stays where it is instead of moving to the world origin.")]
        public IEnumerator RestoreState_UnreadablePosition_LeavesThePlayerWhereItIs()
        {
            yield return null;
            m_rig.Player.GetComponent<NavMeshAgent>().Warp(new Vector3(3f, 0f, 4f));
            Vector3 position = m_rig.Player.transform.position;
            Assert.That(new Vector2(position.x, position.z).magnitude, Is.GreaterThan(1f), "fixture: the Player must start away from the origin");

            m_rig.Player.GetComponent<MovementComponent>().RestoreState(new JValue(5));

            Assert.That(m_rig.Player.transform.position, Is.EqualTo(position).Using(Vector3EqualityComparer.Instance));
        }

        /* ============================== Slots and auto-saves ============================== */

        [UnityTest]
        [Description("AutoSave writes to its own _autosave slot and does not modify the main save file.")]
        public IEnumerator AutoSave_WritesItsOwnSlot_AndLeavesTheMainSaveAlone()
        {
            yield return null;
            m_rig.Wallet.UpdateSilver(10);
            SaveManager.Instance.Save();
            string mainBefore = File.ReadAllText(PlayerRig.SavePath);
            m_rig.Wallet.UpdateSilver(500);

            SaveManager.Instance.AutoSave();

            Assert.That(File.Exists(PlayerRig.AutoSavePath), Is.True);
            Assert.That(File.ReadAllText(PlayerRig.SavePath), Is.EqualTo(mainBefore));
            Assert.That(File.ReadAllText(PlayerRig.AutoSavePath), Is.Not.EqualTo(mainBefore));
        }

        [UnityTest]
        [Description("Gaining a level writes the save file for the current slot when none existed before.")]
        public IEnumerator LevelingUp_AutoSavesTheGame()
        {
            yield return null;
            Assert.That(File.Exists(PlayerRig.SavePath), Is.False, "fixture: no save file should exist yet");

            m_rig.LevelUpTo(2);

            Assert.That(File.Exists(PlayerRig.SavePath), Is.True);
        }

        [UnityTest]
        [Description("Save keeps the entries of entities that aren't loaded right now (another scene's), so their state survives a save made elsewhere.")]
        public IEnumerator Save_KeepsEntriesOfEntitiesInOtherScenes()
        {
            yield return null;
            File.WriteAllText(PlayerRig.SavePath, "{ \"other-scene-entity-uid\": { \"Some.Component\": 1 } }");

            SaveManager.Instance.Save();

            JObject file = JObject.Parse(File.ReadAllText(PlayerRig.SavePath));
            Assert.That(file.ContainsKey("other-scene-entity-uid"), Is.True);
        }
    }
}
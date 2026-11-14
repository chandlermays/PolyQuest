/*---------------------------
| File: RespawnTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Components;
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    // Player death in a real scene: the respawn reloads the last save through the real SaveManager and scene load.
    public class RespawnTests
    {
        private const string kScenePath = "Assets/Game/Scenes/Route 1/Route 1.unity";
        private const string kSaveSlot = "__polyquest_respawn_test__";
        private const string kCurrentSaveKey = "CurrentSaveName";
        private const float kTolerance = 1e-3f;

        private bool m_hadSaveKey;
        private string m_previousSaveName;

        [SetUp]
        public void SetUp()
        {
            m_hadSaveKey = PlayerPrefs.HasKey(kCurrentSaveKey);
            m_previousSaveName = PlayerPrefs.GetString(kCurrentSaveKey);
            PlayerPrefs.SetString(kCurrentSaveKey, kSaveSlot);
            DeleteSaveFiles();
            PlayerRig.DestroyPersistentObjects();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Unload the scene so its objects don't leak into the tests that run after this class
            Scene empty = SceneManager.CreateScene("Respawn_Empty_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            List<Scene> loaded = new();
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != empty)
                    loaded.Add(scene);
            }
            foreach (Scene scene in loaded)
                yield return SceneManager.UnloadSceneAsync(scene);

            PlayerRig.DestroyPersistentObjects();
            DeleteSaveFiles();
            if (m_hadSaveKey)
                PlayerPrefs.SetString(kCurrentSaveKey, m_previousSaveName);
            else
                PlayerPrefs.DeleteKey(kCurrentSaveKey);
        }

        /* --- Helpers --- */

        private static void DeleteSaveFiles()
        {
            foreach (string slot in new[] { kSaveSlot, kSaveSlot + "_autosave" })
            {
                string path = Path.Combine(Application.persistentDataPath, slot + ".sav");
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static GameObject Bandit()
        {
            GameObject bandit = new GameObject("Bandit");
            PlayModeTestKit.Set(bandit.AddComponent<FactionComponent>(), "m_faction", Faction.kEnemy);
            return bandit;
        }

        /* ============================== Tests ============================== */

        [UnityTest]
        [Description("When the Player dies, the last save is reloaded, and the Player comes back with full health and mana.")]
        public IEnumerator PlayerDeath_ReloadsTheLastSave_WithFullHealthAndMana()
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(kScenePath);
            Assert.That(buildIndex, Is.GreaterThanOrEqualTo(0), $"fixture: '{kScenePath}' is not in the build settings");
            yield return SceneManager.LoadSceneAsync(buildIndex);
            yield return null;

            GameObject player = GameObject.FindWithTag("Player");
            Assert.That(player, Is.Not.Null, "fixture: the scene has no Player");
            Assert.That(SaveManager.Instance, Is.Not.Null, "fixture: the scene did not spawn a SaveManager");
            HealthComponent health = player.GetComponent<HealthComponent>();
            ManaComponent mana = player.GetComponent<ManaComponent>();
            Experience experience = player.GetComponent<Experience>();
            GameObject bandit = Bandit();

            // Save while hurt and low on mana, so full health and mana after the respawn can't come from the save
            health.TakeDamage(bandit, health.CurrentHealth * 0.6f);
            Assert.That(mana.UseMana(mana.CurrentMana * 0.5f), Is.True, "fixture: the Player must be able to spend mana");
            SaveManager.Instance.Save();
            float savedExperience = experience.ExperiencePoints;

            // Progress after the save, which the respawn must undo
            int level = player.GetComponent<BaseStats>().Level;
            experience.GainExperience(1f);
            Assert.That(player.GetComponent<BaseStats>().Level, Is.EqualTo(level), "fixture: the extra XP must not level the Player up");

            health.TakeDamage(bandit, 100000f);
            Assert.That(health.IsDead, Is.True, "fixture: the Player must die");

            GameObject respawned = null;
            for (float waited = 0f; respawned == null; waited += Time.unscaledDeltaTime)
            {
                Assert.That(waited, Is.LessThan(30f), "the Player never respawned");
                yield return null;
                GameObject found = GameObject.FindWithTag("Player");
                if (found != null && found != player && player == null)
                    respawned = found;
            }
            yield return null;
            yield return null;

            BaseStats stats = respawned.GetComponent<BaseStats>();
            Assert.That(respawned.GetComponent<Experience>().ExperiencePoints, Is.EqualTo(savedExperience).Within(kTolerance),
                        "the last save was not reloaded");
            Assert.That(respawned.GetComponent<HealthComponent>().CurrentHealth, Is.EqualTo(stats.GetHealth()).Within(kTolerance));
            Assert.That(respawned.GetComponent<ManaComponent>().CurrentMana, Is.EqualTo(stats.GetMana()).Within(kTolerance));
        }
    }
}
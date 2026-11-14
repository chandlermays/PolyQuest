/*---------------------------
| File: PrefabIntegrityTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    public class PrefabIntegrityTests
    {
        private const string kGameFolder = "Assets/Game";

        private PlayerRig m_rig;
        private bool m_loadedAScene;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            m_rig?.Dispose();
            m_rig = null;

            if (m_loadedAScene)
            {
                // Unload the loaded scene so its objects don't leak into the tests that run after this class
                Scene empty = SceneManager.CreateScene("PrefabIntegrity_Empty_" + System.Guid.NewGuid().ToString("N"));
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
                m_loadedAScene = false;
            }

            PlayerRig.DestroyPersistentObjects();       // a scene load can leave singletons behind in DontDestroyOnLoad
        }

        /* --- Case sources --- */

        private static IEnumerable<string> GamePrefabs()
        {
#if UNITY_EDITOR
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { kGameFolder }))
                yield return AssetDatabase.GUIDToAssetPath(guid);
#else
            yield break;
#endif
        }

        private static IEnumerable<int> BuildScenes()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
                yield return i;
        }

        private static string PathOf(Component component)
        {
            return string.Join("/", component.GetComponentsInParent<Transform>(true).Reverse().Select(t => t.name));
        }

        /* ============================== Every prefab (static checks) ============================== */

        [TestCaseSource(nameof(GamePrefabs))]
        [Description("No GameObject in any prefab under Assets/Game has a component whose MonoBehaviour script was deleted or renamed.")]
        public void Prefab_HasNoMissingScripts(string path)
        {
#if UNITY_EDITOR
            GameObject root = PlayModeAssets.Load<GameObject>(path);
            List<string> offenders = new();

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                if (missing > 0)
                    offenders.Add($"{PathOf(child)} ({missing})");
            }

            Assert.That(offenders, Is.Empty, "Components whose script no longer exists:\n" + string.Join("\n", offenders));
#endif
        }

        [TestCaseSource(nameof(GamePrefabs))]
        [Description("No serialized object reference in any prefab under Assets/Game points to an asset that has since been deleted.")]
        public void Prefab_HasNoBrokenObjectReferences(string path)
        {
#if UNITY_EDITOR
            GameObject root = PlayModeAssets.Load<GameObject>(path);
            List<string> offenders = new();

            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                    continue;

                SerializedObject serialized = new SerializedObject(component);
                SerializedProperty property = serialized.GetIterator();
                while (property.NextVisible(true))
                {
                    // A reference to a deleted asset reads as null, but still carries a non-zero instance ID
                    if (property.propertyType == SerializedPropertyType.ObjectReference
                        && property.objectReferenceValue == null
                        && property.objectReferenceInstanceIDValue != 0)
                    {
                        offenders.Add($"{PathOf(component)} [{component.GetType().Name}].{property.propertyPath}");
                    }
                }
            }

            Assert.That(offenders, Is.Empty, "References to assets that no longer exist:\n" + string.Join("\n", offenders));
#endif
        }

        /* ============================== The Player prefab ============================== */

        [UnityTest]
        [Description("The real Player prefab spawns and runs three frames without any logged error or exception from Awake, OnEnable, Start or Update.")]
        public IEnumerator Player_SpawnsAndRunsAFewFrames_WithoutLoggingErrors()
        {
            // Any Debug.LogError / exception (including Utilities.CheckForNull) in Awake, OnEnable, Start or Update fails this test
            m_rig = new PlayerRig();

            yield return null;
            yield return null;
            yield return null;

            Assert.That(m_rig.Player.activeInHierarchy, Is.True);
        }

        [Test]
        [Description("The Player has a SaveableEntity with a non-empty UID, so its state is never saved under a blank key.")]
        public void Player_HasAStableSaveIdentifier()
        {
            m_rig = new PlayerRig();

            SaveableEntity entity = m_rig.Player.GetComponent<SaveableEntity>();

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.UID, Is.Not.Null.And.Not.Empty, "an empty UID means the Player's state is saved under a blank key");
        }

        [Test]
        [Description("No two ISaveable components on the Player share a type, since SaveableEntity keys each component's state by its type name.")]
        public void Player_EverySaveableComponent_IsOfADistinctType()
        {
            // SaveableEntity keys each component's state by its type name; two of the same type would overwrite each other
            m_rig = new PlayerRig();

            string[] typeNames = m_rig.Player.GetComponents<ISaveable>().Select(s => s.GetType().ToString()).ToArray();

            Assert.That(typeNames, Is.Unique);
            Assert.That(typeNames, Is.Not.Empty);
        }

        [UnityTest]
        [Description("After Start, a new Player's current health equals its max health from BaseStats, and it is alive.")]
        public IEnumerator Player_StartsAtFullHealth()
        {
            m_rig = new PlayerRig();

            yield return null;          // HealthComponent.Start initializes health

            Assert.That(m_rig.Health.CurrentHealth, Is.EqualTo(m_rig.Stats.GetHealth()).Within(1e-3f));
            Assert.That(m_rig.Health.IsDead, Is.False);
        }

        [UnityTest]
        [Description("A new Player starts at level 1 with 0 silver and 0 experience.")]
        public IEnumerator Player_StartsAtLevelOne_WithAnEmptyWallet()
        {
            m_rig = new PlayerRig();

            yield return null;

            Assert.That(m_rig.Stats.Level, Is.EqualTo(1));
            Assert.That(m_rig.Wallet.CurrentSilver, Is.EqualTo(0));
            Assert.That(m_rig.Experience.ExperiencePoints, Is.EqualTo(0f));
        }

        /* ============================== Every scene in Build Settings ============================== */

        [UnityTest]
        [Description("Each scene in Build Settings loads without logged errors, and every SaveableEntity in it (inactive included) has a non-blank UID that no other entity shares.")]
        public IEnumerator Scene_LoadsWithoutErrors_AndEverySaveableEntityHasAUniqueIdentifier([ValueSource(nameof(BuildScenes))] int buildIndex)
        {
            PlayerRig.DestroyPersistentObjects();

            m_loadedAScene = true;
            yield return SceneManager.LoadSceneAsync(buildIndex);
            yield return null;
            yield return null;

            SaveableEntity[] entities = Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            string[] blank = entities.Where(e => string.IsNullOrEmpty(e.UID)).Select(PathOfEntity).ToArray();
            string[] duplicated = entities.Where(e => !string.IsNullOrEmpty(e.UID))
                                          .GroupBy(e => e.UID)
                                          .Where(g => g.Count() > 1)
                                          .Select(g => $"{g.Key}: {string.Join(", ", g.Select(PathOfEntity))}")
                                          .ToArray();

            Assert.That(blank, Is.Empty, "SaveableEntities with a blank UID:\n" + string.Join("\n", blank));
            Assert.That(duplicated, Is.Empty, "SaveableEntities sharing a UID (they overwrite each other's save data):\n" + string.Join("\n", duplicated));
        }

        private static string PathOfEntity(SaveableEntity entity) => PathOf(entity);
    }
}
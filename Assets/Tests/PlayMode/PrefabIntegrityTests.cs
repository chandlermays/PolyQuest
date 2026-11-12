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

        [TearDown]
        public void TearDown()
        {
            m_rig?.Dispose();
            m_rig = null;
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
        public void Player_HasAStableSaveIdentifier()
        {
            m_rig = new PlayerRig();

            SaveableEntity entity = m_rig.Player.GetComponent<SaveableEntity>();

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.UID, Is.Not.Null.And.Not.Empty, "an empty UID means the Player's state is saved under a blank key");
        }

        [Test]
        public void Player_EverySaveableComponent_IsOfADistinctType()
        {
            // SaveableEntity keys each component's state by its type name; two of the same type would overwrite each other
            m_rig = new PlayerRig();

            string[] typeNames = m_rig.Player.GetComponents<ISaveable>().Select(s => s.GetType().ToString()).ToArray();

            Assert.That(typeNames, Is.Unique);
            Assert.That(typeNames, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator Player_StartsAtFullHealth()
        {
            m_rig = new PlayerRig();

            yield return null;          // HealthComponent.Start initializes health

            Assert.That(m_rig.Health.CurrentHealth, Is.EqualTo(m_rig.Stats.GetHealth()).Within(1e-3f));
            Assert.That(m_rig.Health.IsDead, Is.False);
        }

        [UnityTest]
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
        public IEnumerator Scene_LoadsWithoutErrors_AndEverySaveableEntityHasAUniqueIdentifier([ValueSource(nameof(BuildScenes))] int buildIndex)
        {
            PlayerRig.DestroyPersistentObjects();

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
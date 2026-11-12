/*---------------------------
| File: PlayerRig.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Components;
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Inventories;
using PolyQuest.SceneManagement;
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    internal sealed class PlayerRig : IDisposable
    {
        public const string kSaveSlot = "__polyquest_playmode_test__";

        private const string kPlayerPrefab = "Assets/Game/Characters/Player/Player.prefab";
        private const string kSaveSystemPrefab = "Assets/Game/Core/Save System.prefab";
        private const string kInputManagerPrefab = "Assets/Game/Core/Input Manager.prefab";
        private const string kCurrentSaveKey = "CurrentSaveName";
        private const string kAutoSaveSuffix = "_autosave";

        private static readonly FieldInfo kRegistryField =
            typeof(InventoryItem).GetField("m_itemRegistry", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly List<Object> m_created = new();
        private readonly bool m_hadSaveKey;
        private readonly string m_previousSaveName;

        public GameObject Player { get; private set; }
        public BaseStats Stats { get; private set; }
        public Experience Experience { get; private set; }
        public HealthComponent Health { get; private set; }
        public Inventory Inventory { get; private set; }
        public Equipment Equipment { get; private set; }
        public Wallet Wallet { get; private set; }

        // Real items from Resources, so the real item registry is exercised
        public ActionItem StackableItem { get; private set; }
        public EquipableItem Equipable { get; private set; }

        public static string SavePath => PathFor(kSaveSlot);
        public static string AutoSavePath => PathFor(kSaveSlot + kAutoSaveSuffix);

        public PlayerRig()
        {
            Assert.That(kRegistryField, Is.Not.Null, "InventoryItem.m_itemRegistry was not found. Was it renamed?");
            kRegistryField.SetValue(null, null);            // force the real registry (built from Resources) on first use
            DestroyPersistentObjects();

            m_hadSaveKey = PlayerPrefs.HasKey(kCurrentSaveKey);
            m_previousSaveName = PlayerPrefs.GetString(kCurrentSaveKey);
            PlayerPrefs.SetString(kCurrentSaveKey, kSaveSlot);
            DeleteSaveFiles();

            BuildGround();
            BuildCamera();
            BuildEventSystem();
            Spawn(kInputManagerPrefab);
            Spawn(kSaveSystemPrefab);
            Assert.That(SaveManager.Instance, Is.Not.Null, "The Save System prefab did not produce a SaveManager.");
            Assert.That(InputManager.Instance, Is.Not.Null, "The Input Manager prefab did not produce an InputManager.");

            SpawnPlayer();

            InventoryItem[] items = Resources.LoadAll<InventoryItem>("");
            StackableItem = items.OfType<ActionItem>().FirstOrDefault(i => i.IsStackable);
            Equipable = items.OfType<EquipableItem>().FirstOrDefault();
            Assert.That(StackableItem, Is.Not.Null, "No stackable ActionItem found under Resources.");
            Assert.That(Equipable, Is.Not.Null, "No EquipableItem found under Resources.");
        }

        /*--------------------------------------------------------------------------------------
        | --- SpawnPlayer: Instantiate a fresh Player (replaces the current one, like a scene reload) --- |
        --------------------------------------------------------------------------------------*/
        public void SpawnPlayer()
        {
            if (Player != null)
                Object.DestroyImmediate(Player);

            Player = Spawn(kPlayerPrefab);
            Stats = Require<BaseStats>();
            Experience = Require<Experience>();
            Health = Require<HealthComponent>();
            Inventory = Require<Inventory>();
            Equipment = Require<Equipment>();
            Wallet = Require<Wallet>();
        }

        private T Require<T>() where T : Component
        {
            T component = Player.GetComponent<T>();
            Assert.That(component, Is.Not.Null, $"The Player prefab has no {typeof(T).Name}.");
            return component;
        }

        /* --- Gameplay helpers --- */

        public void LevelUpTo(int level)
        {
            for (int guard = 0; Stats.Level < level; ++guard)
            {
                Assert.That(guard, Is.LessThan(50), "LevelUpTo is not converging.");
                Experience.GainExperience(Stats.GetExperienceToLevelUp() - Experience.ExperiencePoints);
            }
        }

        public void Damage(float amount)
        {
            GameObject attacker = new GameObject("Attacker");
            m_created.Add(attacker);
            Health.TakeDamage(attacker, amount);
        }

        public int QuantityOf(InventoryItem item)
        {
            int total = 0;
            for (int i = 0; i < Inventory.Size; ++i)
            {
                if (Inventory.GetItemAtSlot(i) == item)
                    total += Inventory.GetQuantityAtSlot(i);
            }
            return total;
        }

        public void ClearInventory()
        {
            for (int i = 0; i < Inventory.Size; ++i)
            {
                if (Inventory.GetItemAtSlot(i) != null)
                    Inventory.RemoveItemsFromSlot(i, Inventory.GetQuantityAtSlot(i));
            }
        }

        /* --- Environment --- */

        private GameObject Spawn(string prefabPath)
        {
            GameObject instance = Object.Instantiate(PlayModeAssets.Load<GameObject>(prefabPath));
            m_created.Add(instance);
            return instance;
        }

        private void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TestGround";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            m_created.Add(ground);

            NavMeshSurface surface = ground.AddComponent<NavMeshSurface>();
            surface.BuildNavMesh();
        }

        private void BuildCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.AddComponent<Camera>();
            m_created.Add(cameraObject);
        }

        private void BuildEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            m_created.Add(eventSystem);
        }

        /*---------------------------------------------------------------------------------------------
        | --- DestroyPersistentObjects: Remove singletons a previous scene load left in DontDestroyOnLoad --- |
        ---------------------------------------------------------------------------------------------*/
        public static void DestroyPersistentObjects()
        {
            GameObject probe = new GameObject("PersistentProbe");
            Object.DontDestroyOnLoad(probe);

            foreach (GameObject root in probe.scene.GetRootGameObjects())
            {
                if (root == probe)
                    continue;

                if (root.GetComponentInChildren<SaveManager>(true) != null
                    || root.GetComponentInChildren<InputManager>(true) != null
                    || root.GetComponentInChildren<InputGuard>(true) != null
                    || root.GetComponentInChildren<TransitionFade>(true) != null)
                {
                    Object.DestroyImmediate(root);
                }
            }

            Object.DestroyImmediate(probe);
            ClearStatic(typeof(SaveManager));
            ClearStatic(typeof(InputManager));
        }

        private static void ClearStatic(Type type)
        {
            PropertyInfo instance = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            instance.GetSetMethod(true).Invoke(null, new object[] { null });
        }

        private static string PathFor(string slot) => Path.Combine(Application.persistentDataPath, slot + ".sav");

        private static void DeleteSaveFiles()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(AutoSavePath)) File.Delete(AutoSavePath);
        }

        public void Dispose()
        {
            kRegistryField.SetValue(null, null);

            for (int i = m_created.Count - 1; i >= 0; --i)
            {
                if (m_created[i] != null)
                    Object.DestroyImmediate(m_created[i]);
            }
            m_created.Clear();

            ClearStatic(typeof(SaveManager));
            ClearStatic(typeof(InputManager));
            DeleteSaveFiles();

            if (m_hadSaveKey)
                PlayerPrefs.SetString(kCurrentSaveKey, m_previousSaveName);
            else
                PlayerPrefs.DeleteKey(kCurrentSaveKey);
        }
    }
}
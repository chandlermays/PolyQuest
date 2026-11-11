/*---------------------------
| File: InventoryTestKit.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    internal sealed class InventoryTestKit : System.IDisposable
    {
        private static readonly FieldInfo kRegistryField =
            typeof(InventoryItem).GetField("m_itemRegistry", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly List<Object> m_created = new();
        private readonly Dictionary<string, InventoryItem> m_registry = new();

        private const string kTestSaveName = "__polyquest_characterization_test__";
        private const string kCurrentSaveKey = "CurrentSaveName";
        private bool m_savesIsolated;
        private bool m_hadSaveKey;
        private string m_previousSaveName;

        public InventoryTestKit()
        {
            Assert.That(kRegistryField, Is.Not.Null, "InventoryItem.m_itemRegistry was not found. Was it renamed?");
            kRegistryField.SetValue(null, m_registry);
            ClearSaveManager();
        }

        /*-------------------------------------------------------------------------------------
        | --- IsolateSaves: Provide a SaveManager that only ever writes the throwaway save --- |
        -------------------------------------------------------------------------------------*/
        public SaveManager IsolateSaves()
        {
            m_hadSaveKey = PlayerPrefs.HasKey(kCurrentSaveKey);
            m_previousSaveName = PlayerPrefs.GetString(kCurrentSaveKey);
            m_savesIsolated = true;
            PlayerPrefs.SetString(kCurrentSaveKey, kTestSaveName);

            GameObject go = NewObject("TestSaveManager");
            go.AddComponent<SavingSystem>();
            SaveManager manager = go.AddComponent<SaveManager>();
            InvokeAwake(manager);
            Assert.That(SaveManager.Instance, Is.SameAs(manager));
            return manager;
        }

        public static bool TestSaveFileExists() => File.Exists(TestSavePath());

        private static string TestSavePath() => Path.Combine(Application.persistentDataPath, kTestSaveName + ".sav");

        public static void ClearSaveManager()
        {
            PropertyInfo instance = typeof(SaveManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            instance.GetSetMethod(true).Invoke(null, new object[] { null });
        }

        /*---------------------------------------------------------------------
        | --- Item: Create a registered item with a fixed ID and stackable --- |
        ---------------------------------------------------------------------*/
        public T Item<T>(string id, bool stackable = false) where T : InventoryItem
        {
            T item = ScriptableObject.CreateInstance<T>();
            SerializedTestUtil.SetString(item, "m_itemID", id);
            SerializedTestUtil.SetBool(item, "m_isStackable", stackable);

            // A freshly created asset can have null arrays/classes until Unity round-trips it; make them real
            if (item is EquipableItem)
            {
                SerializedTestUtil.SetArraySize(item, "m_additiveModifiers", 0);
                SerializedTestUtil.SetArraySize(item, "m_percentageModifiers", 0);
                SerializedTestUtil.SetArraySize(item, "m_equipCondition.m_AND", 0);
            }

            m_created.Add(item);
            m_registry[id] = item;
            return item;
        }

        /*-------------------------------------------------------------------------
        | --- Equipable: Create a registered EquipableItem for a target slot --- |
        -------------------------------------------------------------------------*/
        public T Equipable<T>(string id, EquipmentSlot slot) where T : EquipableItem
        {
            T item = Item<T>(id);
            SerializedTestUtil.SetEnum(item, "m_targetEquipmentSlot", slot);
            return item;
        }

        public GameObject NewObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            m_created.Add(gameObject);
            return gameObject;
        }

        public T Track<T>(T obj) where T : Object
        {
            m_created.Add(obj);
            return obj;
        }

        public Inventory NewInventory(GameObject gameObject, int size)
        {
            Inventory inventory = gameObject.AddComponent<Inventory>();
            SerializedTestUtil.SetInt(inventory, "m_inventorySize", size);
            InvokeAwake(inventory);
            return inventory;
        }

        public static void InvokeAwake(Component component)
        {
            // Awake may be declared on a base class (e.g. DragItem<T>); GetMethod does not return private base members
            MethodInfo awake = null;
            for (System.Type type = component.GetType(); type != null && awake == null; type = type.BaseType)
            {
                awake = type.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            }

            Assert.That(awake, Is.Not.Null, $"{component.GetType().Name}.Awake was not found. Was it renamed?");
            awake.Invoke(component, null);
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

            ClearSaveManager();
            if (m_savesIsolated)
            {
                if (File.Exists(TestSavePath()))
                    File.Delete(TestSavePath());

                if (m_hadSaveKey)
                    PlayerPrefs.SetString(kCurrentSaveKey, m_previousSaveName);
                else
                    PlayerPrefs.DeleteKey(kCurrentSaveKey);

                m_savesIsolated = false;
            }
        }
    }
}
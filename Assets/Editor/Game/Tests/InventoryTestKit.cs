/*---------------------------
| File: InventoryTestKit.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Quests;
using PolyQuest.Saving;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    internal sealed class InventoryTestKit : System.IDisposable
    {
        private static readonly FieldInfo kRegistryField =
            typeof(InventoryItem).GetField("m_itemRegistry", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo kFlagRegistryField =
            typeof(WorldFlag).GetField("m_flagRegistry", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly List<Object> m_created = new();
        private readonly Dictionary<string, InventoryItem> m_registry = new();
        private readonly Dictionary<string, WorldFlag> m_flagRegistry = new();

        private const string kTestSaveName = "__polyquest_characterization_test__";
        private const string kCurrentSaveKey = "CurrentSaveName";
        private bool m_savesIsolated;
        private bool m_hadSaveKey;
        private string m_previousSaveName;

        /*----------------------------------------------------------------------------------------------------------
        | --- InventoryTestKit: Points the item registry at this kit's own registry and clears any SaveManager --- |
        ----------------------------------------------------------------------------------------------------------*/
        public InventoryTestKit()
        {
            Assert.That(kRegistryField, Is.Not.Null, "InventoryItem.m_itemRegistry was not found. Was it renamed?");
            kRegistryField.SetValue(null, m_registry);
            Assert.That(kFlagRegistryField, Is.Not.Null, "WorldFlag.m_flagRegistry was not found. Was it renamed?");
            kFlagRegistryField.SetValue(null, m_flagRegistry);
            ClearSaveManager();
        }

        /*--------------------------------------------------------------------------------------
        | --- IsolateSaves: Provide a SaveManager that only ever writes the throwaway save --- |
        --------------------------------------------------------------------------------------*/
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

        /*-------------------------------------------------------------------------
        | --- TestSaveFileExists: Whether the throwaway test save file exists --- |
        -------------------------------------------------------------------------*/
        public static bool TestSaveFileExists() => File.Exists(TestSavePath());

        /*---------------------------------------------------------------------------------
        | --- TestAutoSaveFileExists: Whether the throwaway test autosave file exists --- |
        ---------------------------------------------------------------------------------*/
        public static bool TestAutoSaveFileExists() => File.Exists(Path.Combine(Application.persistentDataPath, kTestSaveName + "_autosave.sav"));

        /*------------------------------------------------------------
        | --- TestSavePath: Path of the throwaway test save file --- |
        ------------------------------------------------------------*/
        private static string TestSavePath() => Path.Combine(Application.persistentDataPath, kTestSaveName + ".sav");

        /*-----------------------------------------------------------------------------------------------------------------------------------------------
        | --- DeleteTestSaveFiles: Deletes every file of the test slot: the main save, the autosave, their backups and any moved-aside corrupt file --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------*/
        private static void DeleteTestSaveFiles()
        {
            foreach (string path in Directory.GetFiles(Application.persistentDataPath, kTestSaveName + "*"))
                File.Delete(path);
        }

        /*------------------------------------------------------------
        | --- ClearSaveManager: Clears the SaveManager singleton --- |
        ------------------------------------------------------------*/
        public static void ClearSaveManager()
        {
            PropertyInfo instance = typeof(SaveManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            instance.GetSetMethod(true).Invoke(null, new object[] { null });
        }

        /*----------------------------------------------------------------------
        | --- Item: Create a registered item with a fixed ID and stackable --- |
        ----------------------------------------------------------------------*/
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

        /*---------------------------------------------------------------
        | --- Flag: Create a registered WorldFlag with the given ID --- |
        ---------------------------------------------------------------*/
        public WorldFlag Flag(string id)
        {
            WorldFlag flag = ScriptableObject.CreateInstance<WorldFlag>();
            flag.name = id;
            SerializedTestUtil.SetString(flag, "m_flagID", id);

            m_created.Add(flag);
            m_flagRegistry[id] = flag;
            return flag;
        }

        /*--------------------------------------------------------------------------------
        | --- When: A conjunction holding one condition, as an authored one would be --- |
        --------------------------------------------------------------------------------*/
        public static Conjunction When(ConditionType type, params string[] parameters)
        {
            string parameterList = string.Join(",", parameters.Select(p => JsonConvert.ToString(p)));
            string condition = $"{{\"m_condition\":{(int)type},\"m_parameters\":[{parameterList}],\"m_negate\":false}}";
            return JsonUtility.FromJson<Conjunction>($"{{\"m_AND\":[{{\"m_OR\":[{condition}]}}]}}");
        }

        /*-------------------------------------------------------------------------------------------------------------------
        | --- ConfigureObjective: Sets an objective's Is Turn-In, Complete When condition and Also Completes objectives --- |
        -------------------------------------------------------------------------------------------------------------------*/
        public static void ConfigureObjective(QuestObjective objective, Conjunction completeWhen = null, bool isTurnIn = false, params QuestObjective[] alsoCompletes)
        {
            const BindingFlags kField = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(QuestObjective).GetField("m_completeWhen", kField).SetValue(objective, completeWhen ?? new Conjunction());
            typeof(QuestObjective).GetField("m_isTurnIn", kField).SetValue(objective, isTurnIn);
            typeof(QuestObjective).GetField("m_alsoCompletes", kField).SetValue(objective, alsoCompletes.ToList());
        }

        /*------------------------------------------------------------------------
        | --- Equipable: Create a registered EquipableItem for a target slot --- |
        ------------------------------------------------------------------------*/
        public T Equipable<T>(string id, EquipmentSlot slot) where T : EquipableItem
        {
            T item = Item<T>(id);
            SerializedTestUtil.SetEnum(item, "m_targetEquipmentSlot", slot);
            return item;
        }

        /*---------------------------------------------------------------------
        | --- NewObject: Creates a GameObject the kit destroys on Dispose --- |
        ---------------------------------------------------------------------*/
        public GameObject NewObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            m_created.Add(gameObject);
            return gameObject;
        }

        /*----------------------------------------------------------------------
        | --- Track: Registers an object for the kit to destroy on Dispose --- |
        ----------------------------------------------------------------------*/
        public T Track<T>(T obj) where T : Object
        {
            m_created.Add(obj);
            return obj;
        }

        /*------------------------------------------------------------------------------
        | --- NewInventory: Adds an Inventory of the given size and runs its Awake --- |
        ------------------------------------------------------------------------------*/
        public Inventory NewInventory(GameObject gameObject, int size)
        {
            Inventory inventory = gameObject.AddComponent<Inventory>();
            SerializedTestUtil.SetInt(inventory, "m_inventorySize", size);
            InvokeAwake(inventory);
            return inventory;
        }

        /*---------------------------------------------------------------------------------------
        | --- InvokeAwake: Runs a component's Awake, including one declared on a base class --- |
        ---------------------------------------------------------------------------------------*/
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

        /*-------------------------------------------------------------------------------------------------------------------------
        | --- Dispose: Destroys everything created, clears the item registry and SaveManager, and cleans up any isolated save --- |
        -------------------------------------------------------------------------------------------------------------------------*/
        public void Dispose()
        {
            kRegistryField.SetValue(null, null);
            kFlagRegistryField.SetValue(null, null);

            for (int i = m_created.Count - 1; i >= 0; --i)
            {
                if (m_created[i] != null)
                    Object.DestroyImmediate(m_created[i]);
            }
            m_created.Clear();

            ClearSaveManager();
            if (m_savesIsolated)
            {
                DeleteTestSaveFiles();

                if (m_hadSaveKey)
                    PlayerPrefs.SetString(kCurrentSaveKey, m_previousSaveName);
                else
                    PlayerPrefs.DeleteKey(kCurrentSaveKey);

                m_savesIsolated = false;
            }
        }
    }
}
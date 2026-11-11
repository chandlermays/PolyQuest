/*---------------------------
| File: PlayModeTestKit.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Inventories;
using PolyQuest.Pickups;
using PolyQuest.UI;

namespace PolyQuest.Tests
{
    internal sealed class PlayModeTestKit : IDisposable
    {
        private const BindingFlags kAny = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly FieldInfo kRegistryField =
            typeof(InventoryItem).GetField("m_itemRegistry", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly List<Object> m_created = new();
        private readonly Dictionary<string, InventoryItem> m_registry = new();

        public GameObject Player { get; private set; }
        public Inventory PlayerInventory { get; private set; }
        public Pickup PickupTemplate { get; private set; }

        public PlayModeTestKit()
        {
            Assert.That(kRegistryField, Is.Not.Null, "InventoryItem.m_itemRegistry was not found. Was it renamed?");
            kRegistryField.SetValue(null, m_registry);
        }

        /*---------------------------------------------------------------------
        | --- Set:
        ---------------------------------------------------------------------*/
        public static void Set(object target, string field, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo info = type.GetField(field, kAny | BindingFlags.DeclaredOnly);
                if (info != null)
                {
                    info.SetValue(target, value);
                    return;
                }
            }
            Assert.Fail($"'{field}' was not found on {target.GetType().Name}. Was it renamed?");
        }

        public static T Get<T>(object target, string field)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo info = type.GetField(field, kAny | BindingFlags.DeclaredOnly);
                if (info != null)
                    return (T)info.GetValue(target);
            }
            Assert.Fail($"'{field}' was not found on {target.GetType().Name}. Was it renamed?");
            return default;
        }

        public T Track<T>(T obj) where T : Object
        {
            m_created.Add(obj);
            return obj;
        }

        /*-----------------------------------------------------------------------------
        | --- Build:
        -----------------------------------------------------------------------------*/
        public GameObject Build(string name, Action<GameObject> configure, Vector3? position = null)
        {
            GameObject go = new GameObject(name);
            go.SetActive(false);
            if (position.HasValue)
                go.transform.position = position.Value;

            configure?.Invoke(go);
            go.SetActive(true);
            return Track(go);
        }

        /*-----------------------------------------------------------------------------------------
        | --- BuildPlayer:
        -----------------------------------------------------------------------------------------*/
        public void BuildPlayer(int inventorySize = 4)
        {
            Player = Build("Player", go =>
            {
                go.tag = "Player";
                PlayerInventory = go.AddComponent<Inventory>();
                Set(PlayerInventory, "m_inventorySize", inventorySize);
                go.AddComponent<ActionManager>();
            });
        }

        /*---------------------------------------------------------------------------------------------
        | --- BuildPickupTemplate:
        ---------------------------------------------------------------------------------------------*/
        public Pickup BuildPickupTemplate()
        {
            Assert.That(Player, Is.Not.Null, "Call BuildPlayer() before BuildPickupTemplate().");

            GameObject template = Build("PickupTemplate", go =>
            {
                go.AddComponent<Outline>();

                GameObject labelRoot = new GameObject("LabelRoot");
                labelRoot.transform.SetParent(go.transform, false);
                TextMeshProUGUI labelText = labelRoot.AddComponent<TextMeshProUGUI>();

                WorldLabel label = go.AddComponent<WorldLabel>();
                Set(label, "m_labelRoot", labelRoot);
                Set(label, "m_labelText", labelText);

                go.AddComponent<Pickup>();
            }, new Vector3(1000f, 1000f, 1000f));

            PickupTemplate = template.GetComponent<Pickup>();
            return PickupTemplate;
        }

        /*-------------------------------------------------------------------
        | --- Item:
        -------------------------------------------------------------------*/
        public ActionItem Item(string id, bool stackable = false, string itemName = null)
        {
            ActionItem item = Track(ScriptableObject.CreateInstance<ActionItem>());
            Set(item, "m_itemID", id);
            Set(item, "m_itemName", itemName ?? id);
            Set(item, "m_isStackable", stackable);
            if (PickupTemplate != null)
                Set(item, "m_pickup", PickupTemplate);

            m_registry[id] = item;
            return item;
        }

        public static Pickup[] AllPickups() => Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None);

        public Pickup[] Spawned()
        {
            List<Pickup> result = new();
            foreach (Pickup pickup in AllPickups())
            {
                if (pickup != PickupTemplate)
                    result.Add(pickup);
            }
            return result.ToArray();
        }

        public void Dispose()
        {
            kRegistryField.SetValue(null, null);

            foreach (Pickup pickup in AllPickups())
            {
                if (pickup != null)
                    Object.DestroyImmediate(pickup.gameObject);
            }

            for (int i = m_created.Count - 1; i >= 0; --i)
            {
                if (m_created[i] != null)
                    Object.DestroyImmediate(m_created[i]);
            }
            m_created.Clear();
        }
    }
}
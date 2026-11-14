/*---------------------------
| File: EntityTestKit.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Components;

namespace PolyQuest.Tests
{
    // Builds bare combat entities (Animator, NavMeshAgent, BaseStats on a test-made AIProgression) for EditMode tests,
    // and runs the Unity lifecycle methods EditMode doesn't call on its own.
    internal sealed class EntityTestKit : IDisposable
    {
        private const BindingFlags kAny = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private readonly List<Object> m_created = new();

        public T Track<T>(T obj) where T : Object
        {
            m_created.Add(obj);
            return obj;
        }

        // An entity at level 1 with the given base health and mana. Stat modifiers are on.
        public GameObject Entity(string name, int health = 100, int mana = 0)
        {
            AIProgression progression = Track(ScriptableObject.CreateInstance<AIProgression>());
            SerializedTestUtil.SetIntArray(progression, "m_maxHealthAmounts", health);
            SerializedTestUtil.SetIntArray(progression, "m_manaAmounts", mana);
            SerializedTestUtil.SetIntArray(progression, "m_manaRegenRates", 0);

            GameObject entity = Track(new GameObject(name));
            entity.AddComponent<Animator>();
            entity.AddComponent<NavMeshAgent>();
            BaseStats stats = entity.AddComponent<BaseStats>();
            SerializedTestUtil.SetObject(stats, "m_baseProgression", progression);
            SerializedTestUtil.SetBool(stats, "m_useStatModifiers", true);
            return entity;
        }

        // A plain object that only carries a faction, for use as an attacker.
        public GameObject Attacker(string name, Faction? faction)
        {
            GameObject attacker = Track(new GameObject(name));
            if (faction.HasValue)
                SetFaction(attacker, faction.Value);
            return attacker;
        }

        public static FactionComponent SetFaction(GameObject go, Faction faction)
        {
            if (!go.TryGetComponent(out FactionComponent component))
                component = go.AddComponent<FactionComponent>();
            SerializedTestUtil.SetEnum(component, "m_faction", faction);
            return component;
        }

        // Stands in for equipment: an additive modifier on one stat. Change its value, then call NotifyStatModified.
        public static FakeStatModifier Modifier(GameObject entity, Stat stat, float additive)
        {
            FakeStatModifier modifier = entity.AddComponent<FakeStatModifier>();
            modifier.TargetStat = stat;
            modifier.Additive = additive;
            return modifier;
        }

        public static HealthComponent AddHealth(GameObject entity) => Start(entity.AddComponent<HealthComponent>());
        public static ManaComponent AddMana(GameObject entity) => Start(entity.AddComponent<ManaComponent>());

        private static T Start<T>(T component) where T : Component
        {
            Run(component, "Awake");
            Run(component, "OnEnable");
            Run(component, "Start");
            return component;
        }

        private static void Run(Component component, string method)
        {
            for (Type type = component.GetType(); type != null; type = type.BaseType)
            {
                MethodInfo info = type.GetMethod(method, kAny | BindingFlags.DeclaredOnly);
                if (info != null)
                {
                    info.Invoke(component, null);
                    return;
                }
            }
            Assert.Fail($"{component.GetType().Name}.{method} was not found. Was it renamed?");
        }

        public void Dispose()
        {
            for (int i = m_created.Count - 1; i >= 0; --i)
            {
                if (m_created[i] != null)
                    Object.DestroyImmediate(m_created[i]);
            }
            m_created.Clear();
        }
    }
}
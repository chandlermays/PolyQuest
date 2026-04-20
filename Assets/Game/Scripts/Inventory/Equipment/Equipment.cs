/*---------------------------
File: Equipment.cs
Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Saving;
using PolyQuest.Tools;

namespace PolyQuest.Inventories
{
    public enum EquipmentSlot
    {
        kHelmet,
        kAmulet,
        kChest,
        kGloves,
        kLegs,
        kBoots,
        kWeapon,
        kShield,
        kNone
    }

    public class Equipment : MonoBehaviour, ISaveable, IConditionChecker
    {
        private Dictionary<EquipmentSlot, EquipableItem> m_equippedItems = new();
        private BaseStats m_baseStats;

        public IEnumerable<EquipmentSlot> OccupiedSlots => m_equippedItems.Keys;

        public event Action OnEquipmentChanged;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_baseStats = GetComponent<BaseStats>();
            Utilities.CheckForNull(m_baseStats, nameof(m_baseStats));
        }

        /*------------------------------------------------------------------------------
        | --- GetItemInSlot: Get the item currently equipped in the specified slot --- |
        ------------------------------------------------------------------------------*/
        public EquipableItem GetItemInSlot(EquipmentSlot slot)
        {
            m_equippedItems.TryGetValue(slot, out EquipableItem item);
            return item;
        }

        /*----------------------------------------------------------------
        | --- AddItem: Equip an item to the specified equipment slot --- |
        ----------------------------------------------------------------*/
        public void AddItem(EquipmentSlot slot, EquipableItem item)
        {
            m_equippedItems[slot] = item;
            OnEquipmentChanged?.Invoke();
            m_baseStats.NotifyStatModified();
        }

        /*------------------------------------------------------------------------
        | --- RemoveItem: Unequip the item from the specified equipment slot --- |
        ------------------------------------------------------------------------*/
        public void RemoveItem(EquipmentSlot slot)
        {
            if (m_equippedItems.ContainsKey(slot))
            {
                m_equippedItems.Remove(slot);
                OnEquipmentChanged?.Invoke();
                m_baseStats.NotifyStatModified();
            }
        }

        /*-------------------------------------------------------------------------------
        | --- CaptureState: Capture the current state of equipped items for saving --- |
        -------------------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            JObject state = new();
            IDictionary<string, JToken> stateDict = state;

            foreach (var pair in m_equippedItems)
            {
                stateDict[pair.Key.ToString()] = JToken.FromObject(pair.Value.ID);
            }
            return state;
        }

        /*----------------------------------------------------------------------------
        | --- RestoreState: Restore the state of equipped items from saved data --- |
        ----------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state is JObject stateObject)
            {
                m_equippedItems.Clear();

                IDictionary<string, JToken> stateDict = stateObject;

                foreach (var pair in stateObject)
                {
                    if (Enum.TryParse(pair.Key, true, out EquipmentSlot key))
                    {
                        if (InventoryItem.FindByID(pair.Value.ToObject<string>()) is EquipableItem item)
                        {
                            m_equippedItems[key] = item;
                        }
                    }
                }
            }
            OnEquipmentChanged?.Invoke();
            m_baseStats.NotifyStatModified();
        }

        /*----------------------------------------------------------------
        | --- Evaluate: Evaluate a condition based on equipped items --- |
        ----------------------------------------------------------------*/
        public bool? Evaluate(PredicateType predicate, string[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return null;

            switch (predicate)
            {
                case PredicateType.kHasItemEquipped:
                    foreach (var item in m_equippedItems.Values)
                    {
                        if (item.ID == parameters[0])
                            return true;
                    }
                    return false;
            }
            return null;
        }
    }
}
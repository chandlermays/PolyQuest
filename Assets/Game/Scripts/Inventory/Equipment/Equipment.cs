/*---------------------------
| File: Equipment.cs
| Author: Chandler Mays
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

    public class Equipment : MonoBehaviour, ISaveable, IConditionChecker, IStatModifier
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

        /*------------------------------------------------------------------------------
        | --- GetAdditiveModifiers: Get all additive modifiers from equipped items --- |
        ------------------------------------------------------------------------------*/
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            foreach (var slot in OccupiedSlots)
            {
                if (GetItemInSlot(slot) is not IStatModifier item)
                    continue;

                foreach (float modifier in item.GetAdditiveModifiers(stat))
                {
                    yield return modifier;
                }
            }
        }

        /*----------------------------------------------------------------------------------
        | --- GetPercentageModifiers: Get all percentage modifiers from equipped items --- |
        ----------------------------------------------------------------------------------*/
        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            foreach (var slot in OccupiedSlots)
            {
                if (GetItemInSlot(slot) is not IStatModifier item)
                    continue;

                foreach (float modifier in item.GetPercentageModifiers(stat))
                {
                    yield return modifier;
                }
            }
        }

        /*------------------------------------------------------------------------------
        | --- CaptureState: Capture the current state of equipped items for saving --- |
        ------------------------------------------------------------------------------*/
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

        /*----------------------------------------------------------------------------------------------
        | --- RestoreState: Restores the saved equipment; a slot that can't be restored is skipped --- |
        ----------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            // The whole save is the wrong shape: ignore it, keep what's equipped, and raise nothing
            if (state is not JObject stateObject)
                return;

            m_equippedItems.Clear();

            foreach (KeyValuePair<string, JToken> pair in stateObject)
            {
                // One bad slot is skipped; the other slots load
                if (!Enum.TryParse(pair.Key, true, out EquipmentSlot slot))
                {
                    WarnSkippedSlot(pair.Key, pair.Value, "it isn't an equipment slot");
                    continue;
                }

                string itemID = pair.Value != null && pair.Value.Type == JTokenType.String ? pair.Value.Value<string>() : null;
                if (InventoryItem.FindByID(itemID) is not EquipableItem item)
                {
                    WarnSkippedSlot(pair.Key, pair.Value, itemID == null ? "its item ID can't be read" : "no equipable item has that ID");
                    continue;
                }

                m_equippedItems[slot] = item;
            }

            // Once, after the whole restore: stats update, and an empty weapon slot falls back to Unarmed
            OnEquipmentChanged?.Invoke();
            m_baseStats.NotifyStatModified();
        }

        /*---------------------------------------------------------------------------------------------
        | --- WarnSkippedSlot: Editor warning naming a saved slot and item ID that weren't loaded --- |
        ---------------------------------------------------------------------------------------------*/
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void WarnSkippedSlot(string slot, JToken itemID, string reason)
        {
            Debug.LogWarning($"Equipment on '{name}': skipped saved slot '{slot}' with item ID '{itemID}' because {reason}. The other slots loaded.", this);
        }

        /*----------------------------------------------------------------
        | --- Evaluate: Evaluate a condition based on equipped items --- |
        ----------------------------------------------------------------*/
        public bool? Evaluate(ConditionType predicate, string[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return null;

            switch (predicate)
            {
                case ConditionType.kHasItemEquipped:
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
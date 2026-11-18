/*---------------------------
| File: Cooldowns.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Saving;

namespace PolyQuest.Abilities
{
    public class Cooldowns : MonoBehaviour, ISaveable
    {
        // NOTE: InventoryItem -> ActionItem vs. Ability

        private class Cooldown
        {
            public float Remaining;
            public float Duration;
        }

        // Keyed by the item's cooldown group if it has one, otherwise by its item ID
        private readonly Dictionary<string, Cooldown> m_cooldowns = new();

        private const string kGroupPrefix = "group:";

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            List<string> keys = new List<string>(m_cooldowns.Keys);

            foreach (string key in keys)
            {
                Cooldown cooldown = m_cooldowns[key];
                cooldown.Remaining -= Time.deltaTime;
                if (cooldown.Remaining <= 0f)
                {
                    m_cooldowns.Remove(key);
                }
            }
        }

        /*-----------------------------------------------------------------------------------
        | --- KeyOf: The cooldown an item uses: its group's if it has one, else its own --- |
        -----------------------------------------------------------------------------------*/
        private static string KeyOf(InventoryItem inventoryItem)
        {
            if (inventoryItem is Ability ability && !string.IsNullOrEmpty(ability.CooldownGroup))
                return kGroupPrefix + ability.CooldownGroup;

            return inventoryItem.ID;
        }

        /*------------------------------------------------------------------------------
        | --- StartCooldown: Initiates a cooldown for the specified inventory item --- |
        ------------------------------------------------------------------------------*/
        public void StartCooldown(InventoryItem inventoryitem, float duration)
        {
            if (inventoryitem == null)
                return;

            string key = KeyOf(inventoryitem);
            if (string.IsNullOrEmpty(key))
                return;

            if (duration <= 0f)
            {
                m_cooldowns.Remove(key);
                return;
            }

            m_cooldowns[key] = new Cooldown { Remaining = duration, Duration = duration };
        }

        /*------------------------------------------------------------------------------------------------------
        | --- GetRemainingCooldown: Retrieves the remaining cooldown time for the specified inventory item --- |
        ------------------------------------------------------------------------------------------------------*/
        public float GetRemainingCooldown(InventoryItem inventoryItem)
        {
            Cooldown cooldown = Find(inventoryItem);
            return cooldown != null ? cooldown.Remaining : 0f;
        }

        /*--------------------------------------------------------------------------------------------------------------
        | --- GetRemainingPercentage: Retrieves the remaining cooldown percentage for the specified inventory item --- |
        --------------------------------------------------------------------------------------------------------------*/
        public float GetRemainingPercentage(InventoryItem inventoryItem)
        {
            Cooldown cooldown = Find(inventoryItem);
            return cooldown != null ? cooldown.Remaining / cooldown.Duration : 0f;
        }

        private Cooldown Find(InventoryItem inventoryItem)
        {
            if (inventoryItem == null)
                return null;

            string key = KeyOf(inventoryItem);
            if (string.IsNullOrEmpty(key))
                return null;

            return m_cooldowns.TryGetValue(key, out Cooldown cooldown) ? cooldown : null;
        }

        /*-------------------------------------------------------------------------------
        | --- CaptureState: Saves each running cooldown's remaining time and length --- |
        -------------------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            JObject state = new();
            foreach (KeyValuePair<string, Cooldown> pair in m_cooldowns)
            {
                state[pair.Key] = new JObject
                {
                    ["remaining"] = pair.Value.Remaining,
                    ["duration"] = pair.Value.Duration,
                };
            }
            return state;
        }

        /*-------------------------------------------------------------------------------------------
        | --- RestoreState: Resumes the saved cooldowns; entries that can't be read are skipped --- |
        -------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state is not JObject stateObject)
                return;

            m_cooldowns.Clear();

            foreach (KeyValuePair<string, JToken> pair in stateObject)
            {
                if (pair.Value is not JObject entry)
                    continue;

                if (!TryReadNumber(entry["remaining"], out float remaining) || !TryReadNumber(entry["duration"], out float duration))
                    continue;

                if (remaining <= 0f || duration <= 0f)
                    continue;

                m_cooldowns[pair.Key] = new Cooldown { Remaining = Mathf.Min(remaining, duration), Duration = duration };
            }
        }

        private static bool TryReadNumber(JToken token, out float value)
        {
            value = 0f;
            if (token == null || (token.Type != JTokenType.Float && token.Type != JTokenType.Integer))
                return false;

            value = token.Value<float>();
            return true;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.Abilities
{
    public class Cooldowns : MonoBehaviour
    {
        // NOTE: Should I just use ActionItem instead of InventoryItem?
        // InventoryItem is the base class of ActionItem, so it works,
        // but ActionItems would be more explicitly clear as it handles cooldown timers
        // whereas generic InventoryItems do not, like weapons, armor, resources, etc.

        private Dictionary<InventoryItem, float> m_cooldownDurations = new();
        private Dictionary<InventoryItem, float> m_initialCooldownDurations = new();

        /*----------------------------------------- 
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            List<InventoryItem> keys = new List<InventoryItem>(m_cooldownDurations.Keys);

            foreach (InventoryItem inventoryItem in keys)
            {
                m_cooldownDurations[inventoryItem] -= Time.deltaTime;
                if (m_cooldownDurations[inventoryItem] <= 0f)
                {
                    m_cooldownDurations.Remove(inventoryItem);
                    m_initialCooldownDurations.Remove(inventoryItem);
                }
            }
        }

        /*------------------------------------------------------------------------------
        | --- StartCooldown: Initiates a cooldown for the specified inventory item --- |
        ------------------------------------------------------------------------------*/
        public void StartCooldown(InventoryItem inventoryitem, float duration)
        {
            if (inventoryitem == null)
                return;

            m_cooldownDurations[inventoryitem] = duration;
            m_initialCooldownDurations[inventoryitem] = duration;
        }

        /*------------------------------------------------------------------------------------------------------
        | --- GetRemainingCooldown: Retrieves the remaining cooldown time for the specified inventory item --- |
        ------------------------------------------------------------------------------------------------------*/
        public float GetRemainingCooldown(InventoryItem inventoryItem)
        {
            if (inventoryItem == null)
                return 0f;

            if (!m_cooldownDurations.ContainsKey(inventoryItem))
                return 0f;

            return m_cooldownDurations[inventoryItem];
        }

        /*--------------------------------------------------------------------------------------------------------------
        | --- GetRemainingPercentage: Retrieves the remaining cooldown percentage for the specified inventory item --- |
        --------------------------------------------------------------------------------------------------------------*/
        public float GetRemainingPercentage(InventoryItem inventoryItem)
        {
            if (inventoryItem == null)
                return 0f;

            if (!m_cooldownDurations.ContainsKey(inventoryItem))
                return 0f;

            return m_cooldownDurations[inventoryItem] / m_initialCooldownDurations[inventoryItem];
        }
    }
}
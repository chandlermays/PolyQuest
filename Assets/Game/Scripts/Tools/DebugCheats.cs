/*---------------------------
File: DebugCheats.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Components;
using PolyQuest.Input;
using PolyQuest.Inventories;

namespace PolyQuest.Tools
{
    public class DebugCheats : MonoBehaviour
    {
        [SerializeField] private GameObject m_player;

        [Header("Health Cheat Settings")]
        [Tooltip("The amount of health to add or remove when the cheat is activated.")]
        [SerializeField] private float m_healthAmount = 20f;
        private HealthComponent m_healthComponent;

        [Header("Experience Cheat Settings")]
        [Tooltip("The amount of experience to add when the cheat is activated.")]
        [SerializeField] private float m_experienceAmount = 20f;
        private Experience m_experience;

        [Header("Inventory Cheat Settings")]
        [Tooltip("The item we want to add when the cheat is activated.")]
        [SerializeField] private InventoryItem m_inventoryItem;
        [SerializeField] private int m_itemQuantity = 1;
        private Inventory m_inventory;

        private PolyQuestInputActions m_inputActions;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false;
            return;
#endif
            Utilities.CheckForNull(m_player, nameof(m_player));
            m_healthComponent = m_player.GetComponent<HealthComponent>();
            m_experience = m_player.GetComponent<Experience>();
            m_inventory = m_player.GetComponent<Inventory>();
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_inputActions = InputManager.Instance.InputActions;
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            // --- Health ---
            if (m_inputActions.Debug.DecreaseHealth.WasPerformedThisFrame())
            {
                m_healthComponent.TakeDamage(gameObject, m_healthAmount);
                Debug.Log($"Debug Cheat: Decreased health by {m_healthAmount}. Current = {m_healthComponent.CurrentHealth}");
            }

            if (m_inputActions.Debug.IncreaseHealth.WasPerformedThisFrame())
            {
                m_healthComponent.ReplenishHealth(m_healthAmount);
                Debug.Log($"Debug Cheat: Increased health by {m_healthAmount}. Current = {m_healthComponent.CurrentHealth}");
            }

            // --- Experience ---
            if (m_inputActions.Debug.IncreaseExperience.WasPerformedThisFrame())
            {
                m_experience.GainExperience(m_experienceAmount);
                Debug.Log($"Debug Cheat: Increased experience by {m_experienceAmount}. Current Total = {m_experience.ExperiencePoints}, Current Level XP = {m_experience.GetCurrentLevelXP}");
            }

            // --- Inventory ---
            if (m_inputActions.Debug.AddItem.WasPerformedThisFrame())
            {
                if (m_inventoryItem == null)
                {
                    Debug.LogWarning("Debug Cheat: No item assigned to add.");
                    return;
                }

                bool added = m_inventory.TryAddToAvailableSlot(m_inventoryItem, m_itemQuantity);
                if (added)
                {
                    Debug.Log($"Debug Cheat: Added {m_itemQuantity} {m_inventoryItem.name} to the inventory.");
                }
                else
                {
                    Debug.Log($"Debug Cheat: {m_inventoryItem.name} could not be added to the inventory.");
                }
            }
        }
    }
}
/*---------------------------
File: ManaComponent.cs
Author: Chandler Mays
----------------------------*/
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Saving;

namespace PolyQuest.Components
{
    public class ManaComponent : EntityComponent, ISaveable
    {
        [Header("Mana Settings")]
        [SerializeField] private float m_mana;
        [SerializeField] private float m_manaRegenRate;

        private float m_maxMana;
        public event Action OnManaChanged;

        public float CurrentMana => m_mana;
        public float ManaRegenRate => m_manaRegenRate;

        private BaseStats m_stats;
        private Experience m_experienceComponent;

        private bool m_hasBeenInitialized = false;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();

            m_stats = GetComponent<BaseStats>();
            Utilities.CheckForNull(m_stats, nameof(m_stats));

            TryGetComponent(out m_experienceComponent);
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            if (m_experienceComponent != null)
            {
                m_experienceComponent.OnLevelUp += FullyReplenishMana;
            }

            m_stats.OnStatModified += RecalculateMana;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            if (m_experienceComponent != null)
            {
                m_experienceComponent.OnLevelUp -= FullyReplenishMana;
            }

            m_stats.OnStatModified -= RecalculateMana;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            if (!m_hasBeenInitialized)
            {
                m_maxMana = m_stats.GetMana();
                m_manaRegenRate = m_stats.GetManaRegenRate();
                m_mana = m_maxMana;
                m_hasBeenInitialized = true;
                OnManaChanged?.Invoke();
            }
        }

        /*---------------------------------------
        | --- Update: Called once per frame --- |
        ---------------------------------------*/
        private void Update()
        {
            if (m_mana < m_maxMana)
            {
                m_mana += m_manaRegenRate * Time.deltaTime;
                if (m_mana > m_maxMana)
                {
                    m_mana = m_maxMana;
                }
                OnManaChanged?.Invoke();
            }
        }

        /*------------------------------------------------------------------------------
        | --- RecalculateMaxMana: Recalculate the Entity's Max Mana and Regen Rate --- |
        ------------------------------------------------------------------------------*/
        public void RecalculateMana()
        {
            float newMaxMana = m_stats.GetMana();
            float newRegenRate = m_stats.GetManaRegenRate();

            // First-time initialization safeguard
            if (m_maxMana <= 0f)
            {
                m_maxMana = newMaxMana;
                m_manaRegenRate = newRegenRate;
                m_mana = Mathf.Clamp(m_mana, 0f, newMaxMana);
                OnManaChanged?.Invoke();
                return;
            }

            if (m_mana > newMaxMana)
            {
                m_mana = newMaxMana; // Clamp down if necessary
            }

            m_maxMana = newMaxMana;
            m_manaRegenRate = newRegenRate;

            OnManaChanged?.Invoke();
        }

        /*--------------------------------------------------------------
        | --- UseMana: Attempt to Use the Specified Amount of Mana --- |
        --------------------------------------------------------------*/
        public bool UseMana(float amount)
        {
            if (amount > m_mana)
                return false;

            m_mana -= amount;
            OnManaChanged?.Invoke();
            return true;
        }

        /*--------------------------------------------------------------
        | --- ReplenishMana: Replenish the Entity's Mana by amount --- |
        --------------------------------------------------------------*/
        public void ReplenishMana(float amount)
        {
            m_mana = Mathf.Min(m_mana + amount, m_stats.GetMana());
            OnManaChanged?.Invoke();
        }

        /*---------------------------------------------------------------
        | --- FullyReplenishMana: Fully replenish the Entity's Mana --- |
        ---------------------------------------------------------------*/
        public void FullyReplenishMana()
        {
            m_maxMana = m_stats.GetMana();
            m_manaRegenRate = m_stats.GetManaRegenRate();
            m_mana = m_maxMana;
            OnManaChanged?.Invoke();
        }

        /*------------------------------------------------------------------------
        | --- CaptureState: Captures the current state of the Entity's Mana --- |
        ------------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            return JToken.FromObject(m_mana);
        }

        /*---------------------------------------------------------------
        | --- RestoreState: Restores the state from the saved data --- |
        ---------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            m_mana = state.ToObject<float>();
            m_hasBeenInitialized = true;
            OnManaChanged?.Invoke();
        }
    }
}
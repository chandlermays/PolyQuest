/*---------------------------
| File: ManaComponent.cs
| Author: Chandler Mays
----------------------------*/
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Saving;

namespace PolyQuest.Components
{
    public class ManaComponent : EntityComponent, ISaveable, IRestoreCompleted
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
        private float? m_restoredMana = null;       // The saved mana, put back exactly once the whole load is done

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

        /*------------------------------------------------------------------------------------------------------------
        | --- RecalculateMana: Updates the maximum and regen rate; mana keeps the same percentage of the maximum --- |
        ------------------------------------------------------------------------------------------------------------*/
        public void RecalculateMana()
        {
            float newMaxMana = m_stats.GetMana();

            // 20/100 becomes 10/50 and back to 20/100; before the first maximum is known there's nothing to scale from
            float scaled = m_maxMana > 0f ? m_mana / m_maxMana * newMaxMana : m_mana;
            m_mana = Mathf.Clamp(scaled, 0f, newMaxMana);

            m_maxMana = newMaxMana;
            m_manaRegenRate = m_stats.GetManaRegenRate();

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

        /*-----------------------------------------------------------------------
        | --- CaptureState: Captures the current state of the Entity's Mana --- |
        -----------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            return JToken.FromObject(m_mana);
        }

        /*------------------------------------------------------------------------------------------
        | --- RestoreState: Restores the state from the saved data; unreadable data is ignored --- |
        ------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state == null || (state.Type != JTokenType.Float && state.Type != JTokenType.Integer))
                return;

            m_maxMana = m_stats.GetMana();
            m_manaRegenRate = m_stats.GetManaRegenRate();
            m_mana = state.Value<float>();
            m_restoredMana = m_mana;             // Other components may still change the maximum during this load
            m_hasBeenInitialized = true;
            OnManaChanged?.Invoke();
        }

        /*-------------------------------------------------------------------------------------------------
        | --- OnRestoreCompleted: Puts the saved mana back exactly, now that the maximum is final too --- |
        -------------------------------------------------------------------------------------------------*/
        public void OnRestoreCompleted()
        {
            if (m_restoredMana == null)
                return;

            m_maxMana = m_stats.GetMana();
            m_manaRegenRate = m_stats.GetManaRegenRate();
            m_mana = Mathf.Clamp(m_restoredMana.Value, 0f, m_maxMana);
            m_restoredMana = null;
            OnManaChanged?.Invoke();
        }
    }
}
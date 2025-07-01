using UnityEngine;
using System;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest
{
    /*-------------------------------------------- 
    | --- The Health Component for an Entity --- |
    --------------------------------------------*/
    public class HealthComponent : EntityComponent, ISaveable
    {
        [Header("Health Settings")]
        private float m_health;

        private bool m_isDead = false;
        public event Action OnHealthChanged;        // Event for when the Health changes
        public event Action OnHit;                  // Event for when the Entity is hit

        public float CurrentHealth => m_health;     // Returns the Current Health
        public bool IsDead() => m_isDead;           // Returns if the Entity is Dead

        /* --- Components --- */
        private Stats m_stats;

        /* --- Animation Parameter --- */
        private const string kDeath = "Death";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();
            m_stats = GetComponent<Stats>();
            m_health = m_stats.GetHealth();
            OnHealthChanged?.Invoke();
        }

        /*---------------------------------------------------------------
        | --- TakeDamage: The Behavior of this Entity taking Damage --- |
        ---------------------------------------------------------------*/
        public void TakeDamage(GameObject instigator, float damage)
        {
            if (m_isDead) return;

            m_health -= damage;

            OnHealthChanged?.Invoke();
            OnHit?.Invoke();

            if (m_health <= 0)
            {
                OnDeath();
                GrantExperience(instigator);
            }
        }

        /*--------------------------------------------------------- 
        | --- OnDeath: The Behavior when this Entity has Died --- |
        ---------------------------------------------------------*/
        private void OnDeath()
        {
            if (m_isDead) return;

            m_isDead = true;
            OnHealthChanged?.Invoke();
            Animator.SetTrigger(kDeath);
        }

        /*------------------------------------------------------------
        | --- GrantExperience: Give Experience to the Instigator --- |
        ------------------------------------------------------------*/
        private void GrantExperience(GameObject instigator)
        {
            if (!instigator.TryGetComponent<Experience>(out var experience)) return;

            experience.GainExperience(GetComponent<Stats>().GetExperience());
        }

        /*--------------------------------------------------------------
        | --- ReplenishHealth: Fully replenish the Entity's Health --- |
        --------------------------------------------------------------*/
        public void ReplenishHealth()
        {
            m_health = m_stats.GetHealth();
            OnHealthChanged?.Invoke();
        }

        /*----------------------------------------------------------------------
        | --- CaptureState: Save the Current State of the Health Component --- |
        ----------------------------------------------------------------------*/
        public object CaptureState()
        {
            return m_health;
        }

        /*----------------------------------------------------------------------
        | --- RestoreState: Load the Current State of the Health Component --- |
        ----------------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            m_health = (float)state;
            OnHealthChanged?.Invoke();  // Update the Health Bar

            if (m_health <= 0)
            {
                OnDeath();
            }
        }
    }
}
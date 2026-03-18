using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Saving;

namespace PolyQuest.Components
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Manages health, damage, and death logic for an entity.                                *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Tracks current health and death state.                                               *
     *      - Handles taking damage, triggering death, and replenishing health.                    *
     *      - Raises events for health changes and hits.                                           *
     *      - Grants experience to the instigator on death.                                        *
     *      - Supports saving and restoring health state.                                          *
     * ------------------------------------------------------------------------------------------- */
    public class HealthComponent : EntityComponent, ISaveable
    {
        [Header("Health Settings")]
        [SerializeField] private float m_health;

        private bool m_isDead = false;
        public event Action OnHealthChanged;        // Event for when the Health changes
        public event Action<GameObject> OnHit;      // Event for when the Entity is hit
        public event Action OnDeath;                // Event for when the Entity dies

        public float CurrentHealth => m_health;     // Returns the Current Health
        public bool IsDead => m_isDead;             // Returns if the Entity is Dead

        [System.Serializable]
        private struct HealthSaveData
        {
            public float health;
            public bool isDead;
        }

        private bool m_hasBeenInitialized = false;
        private float m_lastKnownMaxHealth = 0f;

        /* --- Components --- */
        private BaseStats m_stats;
        private Experience m_experienceComponent;
        private CapsuleCollider m_capsuleCollider;

        /* --- Animation Parameter --- */
        private const string kDeath = "Death";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();

            m_stats = GetComponent<BaseStats>();
            Utilities.CheckForNull(m_stats, nameof(m_stats));

            TryGetComponent(out m_experienceComponent);
            TryGetComponent(out m_capsuleCollider);
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            if (m_experienceComponent != null)
            {
                m_experienceComponent.OnLevelUp += FullyReplenishHealth;
            }

            m_stats.OnStatModified += RecalculateMaxHealth;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            if (m_experienceComponent != null)
            {
                m_experienceComponent.OnLevelUp -= FullyReplenishHealth;
            }

            m_stats.OnStatModified -= RecalculateMaxHealth;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            // only set the health if it hasn't been restored from a save state
            if (!m_hasBeenInitialized)
            {
                m_health = m_stats.GetHealth();
                m_lastKnownMaxHealth = m_health;
                m_hasBeenInitialized = true;
                OnHealthChanged?.Invoke();

                if (Owner.name.Contains("Player"))
                {
                    Debug.Log($"Max health starts at: {m_lastKnownMaxHealth} with the Current health at: {m_health}");
                }
            }
        }

        /*------------------------------------------------------------------
        | --- RecalculateMaxHealth: Recalculate Health on Stat Changes --- |
        ------------------------------------------------------------------*/
        public void RecalculateMaxHealth()
        {
            float newMaxHealth = m_stats.GetHealth();

            // First-time initialization safeguard
            if (m_lastKnownMaxHealth <= 0f)
            {
                m_lastKnownMaxHealth = newMaxHealth;
                m_health = Mathf.Clamp(m_health, 0f, newMaxHealth);
                OnHealthChanged?.Invoke();
                return;
            }

            float delta = newMaxHealth - m_lastKnownMaxHealth;
            m_health = Mathf.Clamp(m_health + delta, 0f, newMaxHealth);

            m_lastKnownMaxHealth = newMaxHealth;
            OnHealthChanged?.Invoke();

            Debug.Log($"Max Health recalculated: {newMaxHealth}, Current Health: {m_health}");
        }

        /*---------------------------------------------------------------
        | --- TakeDamage: The Behavior of this Entity taking Damage --- |
        ---------------------------------------------------------------*/
        public void TakeDamage(GameObject instigator, float damage)
        {
            if (m_isDead)
                return;

            FactionComponent myFaction = GetComponent<FactionComponent>();
            FactionComponent attackerFaction = instigator.GetComponent<FactionComponent>();

            if (myFaction != null && attackerFaction != null)
            {
                if (!attackerFaction.IsHostileTo(myFaction))
                {
                    // Prevent friendly fire
                    return;
                }
            }

            m_health -= damage;

            OnHealthChanged?.Invoke();
            OnHit?.Invoke(instigator);

            if (m_health <= 0)
            {
                HandleDeath();
                GrantExperience(instigator);
            }
        }

        /*------------------------------------------------------------------
        | --- ReplenishHealth: Replenish the Entity's Health by amount --- |
        ------------------------------------------------------------------*/
        public void ReplenishHealth(float amount)
        {
            if (m_isDead)
                return;

            m_health = Mathf.Min(m_health + amount, m_stats.GetHealth());
            OnHealthChanged?.Invoke();
        }

        /*-------------------------------------------------------------------
        | --- FullyReplenishHealth: Fully replenish the Entity's Health --- |
        -------------------------------------------------------------------*/
        public void FullyReplenishHealth()
        {
            if (m_isDead)
                return;

            m_health = m_stats.GetHealth();
            OnHealthChanged?.Invoke();
        }

        /*-----------------------------------------------------
        | --- Heal: Heal the Entity by a specified amount --- |
        -----------------------------------------------------*/
        public void Heal(float amount)
        {
            m_health = Mathf.Min(m_health + amount, m_stats.GetHealth());
            OnHealthChanged?.Invoke();
        }

        /*------------------------------------------------------------- 
        | --- HandleDeath: The Behavior when this Entity has Died --- |
        -------------------------------------------------------------*/
        private void HandleDeath()
        {
            if (m_isDead)
                return;

            m_isDead = true;
            OnHealthChanged?.Invoke();

            ApplyDeathState();
            OnDeath?.Invoke();
        }

        /*---------------------------------------------------------- 
        | --- ApplyDeathState: Apply the Death Animation State --- |
        ----------------------------------------------------------*/
        private void ApplyDeathState()
        {
            Animator.SetTrigger(kDeath);

            if (m_capsuleCollider != null)
            {
                m_capsuleCollider.enabled = false;
            }
        }

        /*------------------------------------------------------------
        | --- GrantExperience: Give Experience to the Instigator --- |
        ------------------------------------------------------------*/
        private void GrantExperience(GameObject instigator)
        {
            if (!instigator.TryGetComponent<Experience>(out var experience)) return;

            experience.GainExperience(m_stats.GetExperienceReward());
        }

        /*-------------------------------------------------------------------------
        | --- CaptureState: Captures the current state of the Entity's Health --- |
        -------------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            HealthSaveData data = new()
            {
                health = m_health,
                isDead = m_isDead
            };
            return JToken.FromObject(data);
        }

        /*--------------------------------------------------------------------------
        | --- RestoreState: Restores the Entity's Health state from saved data --- |
        --------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            HealthSaveData data = state.ToObject<HealthSaveData>();
            m_health = data.health;
            m_isDead = data.isDead;
            m_hasBeenInitialized = true;
            m_lastKnownMaxHealth = m_stats.GetHealth();
            OnHealthChanged?.Invoke();

            if (m_isDead)
            {
                ApplyDeathState();
                Animator.Update(0f);
                Animator.Play(kDeath, 0, 1f);
            }
            else
            {
                // Restore the "alive" state
                Animator.Rebind();

                if (m_capsuleCollider != null)
                {
                    m_capsuleCollider.enabled = true;
                }
            }
        }
    }
}
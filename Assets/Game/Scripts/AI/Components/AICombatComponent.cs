using UnityEngine;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /// <summary>
    /// AI-specific wrapper around CombatComponent for attack behavior.
    /// Provides simplified interface for AI states to trigger attacks and check combat readiness.
    /// </summary>
    public class AICombatComponent : MonoBehaviour
    {
        [Header("Combat Settings")]
        [Tooltip("Range at which AI can attack")]
        [SerializeField] private float m_attackRange = 2f;
        
        [Tooltip("Cooldown between attacks")]
        [SerializeField] private float m_attackCooldown = 1.5f;

        private CombatComponent m_combatComponent;
        private float m_timeSinceLastAttack = Mathf.Infinity;
        private Transform m_transform;

        /// <summary>
        /// Gets the attack range.
        /// </summary>
        public float AttackRange => m_attackRange;

        /// <summary>
        /// Configures combat settings from AIData.
        /// </summary>
        /// <param name="data">AI configuration data</param>
        public void Configure(AIData data)
        {
            if (data == null)
                return;

            m_attackRange = data.AttackRange;
            m_attackCooldown = data.AttackCooldown;
        }

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_transform = transform;
            m_combatComponent = GetComponent<CombatComponent>();
            
            if (m_combatComponent == null)
            {
                Debug.LogWarning($"AICombatComponent on {gameObject.name} requires CombatComponent but none found.");
            }
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            m_timeSinceLastAttack += Time.deltaTime;
        }

        /// <summary>
        /// Checks if the AI can attack the given target.
        /// </summary>
        /// <param name="target">Target to check</param>
        /// <returns>True if target can be attacked</returns>
        public bool CanAttack(GameObject target)
        {
            if (target == null || m_combatComponent == null)
                return false;

            // Check if target is in range
            float distance = Vector3.Distance(m_transform.position, target.transform.position);
            if (distance > m_attackRange)
                return false;

            // Check if combat component can attack
            return m_combatComponent.CanAttack(target);
        }

        /// <summary>
        /// Attempts to attack the target if cooldown allows.
        /// </summary>
        /// <param name="target">Target to attack</param>
        /// <returns>True if attack was initiated</returns>
        public bool TryAttack(GameObject target)
        {
            if (target == null || m_combatComponent == null)
                return false;

            // Check cooldown
            if (m_timeSinceLastAttack < m_attackCooldown)
                return false;

            // Check if target is in range
            float distance = Vector3.Distance(m_transform.position, target.transform.position);
            if (distance > m_attackRange)
                return false;

            // Set target and trigger attack
            m_combatComponent.SetTarget(target);
            m_timeSinceLastAttack = 0f;
            return true;
        }

        /// <summary>
        /// Gets whether attack cooldown has elapsed.
        /// </summary>
        /// <returns>True if ready to attack</returns>
        public bool IsReadyToAttack()
        {
            return m_timeSinceLastAttack >= m_attackCooldown;
        }

        /// <summary>
        /// Cancels current combat action.
        /// </summary>
        public void Cancel()
        {
            if (m_combatComponent != null)
            {
                m_combatComponent.Cancel();
            }
        }

        /*------------------------------------------------------------------------------- 
        | --- OnDrawGizmosSelected: Visualize attack range --- |
        -------------------------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            if (m_transform == null)
                m_transform = transform;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(m_transform.position, m_attackRange);
        }
    }
}

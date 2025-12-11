using UnityEngine;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.AI.Components
{
    /// <summary>
    /// Minimal AI combat component that wraps the existing CombatComponent functionality.
    /// Provides CanAttack and TryAttack methods for AI states to use.
    /// This component queries the existing PolyQuest.Components.CombatComponent on the same GameObject.
    /// </summary>
    public class AICombatComponent : MonoBehaviour
    {
        [Header("Combat Settings")]
        [Tooltip("Maximum range for attacking targets")]
        [SerializeField] private float m_attackRange = 2f;

        [Tooltip("Minimum time between attacks")]
        [SerializeField] private float m_attackCooldown = 1.5f;

        private PolyQuest.Components.CombatComponent m_combatComponent;
        private float m_timeSinceLastAttack = Mathf.Infinity;

        /// <summary>
        /// Gets the attack range.
        /// </summary>
        public float AttackRange => m_attackRange;

        /// <summary>
        /// Gets the attack cooldown duration.
        /// </summary>
        public float AttackCooldown => m_attackCooldown;

        /// <summary>
        /// Gets the time elapsed since the last attack.
        /// </summary>
        public float TimeSinceLastAttack => m_timeSinceLastAttack;

        private void Awake()
        {
            // Get reference to the existing CombatComponent
            m_combatComponent = GetComponent<PolyQuest.Components.CombatComponent>();
            
            if (m_combatComponent == null)
            {
                Debug.LogWarning($"[AICombatComponent] No CombatComponent found on {gameObject.name}. " +
                                 "AI combat functionality will be limited. Please add a CombatComponent.");
            }
        }

        private void Update()
        {
            m_timeSinceLastAttack += Time.deltaTime;
        }

        /// <summary>
        /// Checks if the AI can attack the specified target.
        /// Considers cooldown and range.
        /// </summary>
        /// <param name="target">The target to check</param>
        /// <returns>True if the target can be attacked</returns>
        public bool CanAttack(GameObject target)
        {
            if (target == null)
                return false;

            // Check if cooldown has elapsed
            if (m_timeSinceLastAttack < m_attackCooldown)
                return false;

            // Check if target is in range
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > m_attackRange)
                return false;

            // If we have the full CombatComponent, use its CanAttack check
            if (m_combatComponent != null)
            {
                return m_combatComponent.CanAttack(target);
            }

            return true;
        }

        /// <summary>
        /// Attempts to attack the specified target.
        /// </summary>
        /// <param name="target">The target to attack</param>
        /// <returns>True if the attack was initiated successfully</returns>
        public bool TryAttack(GameObject target)
        {
            if (!CanAttack(target))
                return false;

            if (m_combatComponent != null)
            {
                // Use the existing CombatComponent to set target
                m_combatComponent.SetTarget(target);
                m_timeSinceLastAttack = 0f;
                return true;
            }
            else
            {
                // TODO: Implement fallback attack behavior if no CombatComponent exists
                Debug.LogWarning($"[AICombatComponent] Cannot attack - no CombatComponent on {gameObject.name}");
                return false;
            }
        }

        /// <summary>
        /// Sets the combat target directly if CombatComponent is available.
        /// </summary>
        /// <param name="target">The target to set</param>
        public void SetTarget(GameObject target)
        {
            if (m_combatComponent != null)
            {
                m_combatComponent.SetTarget(target);
            }
        }

        /// <summary>
        /// Cancels the current attack/target.
        /// </summary>
        public void CancelAttack()
        {
            if (m_combatComponent != null)
            {
                m_combatComponent.Cancel();
            }
        }

        /// <summary>
        /// Sets combat parameters from an AIData asset.
        /// </summary>
        /// <param name="aiData">The AI data to apply</param>
        public void ApplyAIData(AIData aiData)
        {
            if (aiData == null)
            {
                Debug.LogWarning($"[AICombatComponent] Null AIData provided to {gameObject.name}");
                return;
            }

            m_attackRange = aiData.AttackRange;
            m_attackCooldown = aiData.AttackCooldown;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_attackRange);
        }
    }
}

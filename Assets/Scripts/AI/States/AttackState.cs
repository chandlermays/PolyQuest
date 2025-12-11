using UnityEngine;

namespace PolyQuest.AI
{
    /// <summary>
    /// Attack state - AI pursues and attacks the current target.
    /// Handles missing CombatComponent gracefully by transitioning to other states.
    /// </summary>
    public class AttackState : IAIState
    {
        private readonly AIController m_controller;
        private bool m_hasAlertedAllies;

        /// <summary>
        /// Constructor for AttackState.
        /// </summary>
        /// <param name="controller">Reference to the AI controller</param>
        public AttackState(AIController controller)
        {
            m_controller = controller;
        }

        /// <summary>
        /// Called when entering the attack state.
        /// </summary>
        public void Enter()
        {
            m_hasAlertedAllies = false;

            // Validate we can actually attack
            if (m_controller.CombatComponent == null)
            {
                Debug.LogWarning($"[AttackState] CombatComponent not found on {m_controller.gameObject.name}. Cannot attack. Returning to patrol/idle.");
                
                if (m_controller.PatrolComponent != null && m_controller.PatrolComponent.HasWaypoints)
                {
                    m_controller.StateMachine.ChangeState("Patrol");
                }
                else
                {
                    m_controller.StateMachine.ChangeState("Idle");
                }
                return;
            }

            // Set target on combat component if we have one
            if (m_controller.CurrentTarget != null)
            {
                m_controller.CombatComponent.SetTarget(m_controller.CurrentTarget);
            }

            // Set chase speed if configured
            // TODO: MovementComponent doesn't expose SetSpeed() method - access NavMeshAgent directly
            if (m_controller.Data != null && m_controller.Data.ChaseSpeed > 0)
            {
                var navAgent = m_controller.MovementComponent?.NavMeshAgent;
                if (navAgent != null)
                {
                    navAgent.speed = m_controller.Data.ChaseSpeed;
                }
            }
        }

        /// <summary>
        /// Called when exiting the attack state.
        /// </summary>
        public void Exit()
        {
            // Cancel combat when leaving attack state
            if (m_controller.CombatComponent != null)
            {
                m_controller.CombatComponent.Cancel();
            }
        }

        /// <summary>
        /// Called every frame while in attack state.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        public void Tick(float deltaTime)
        {
            // Validate we still have required components and target
            if (m_controller.CombatComponent == null || m_controller.CurrentTarget == null)
            {
                m_controller.StateMachine.ChangeState("Suspicion");
                return;
            }

            // Check if target is still valid
            if (!m_controller.CombatComponent.CanAttack(m_controller.CurrentTarget))
            {
                // Target no longer valid - enter suspicion
                m_controller.StateMachine.ChangeState("Suspicion");
                return;
            }

            // Alert nearby allies (once per combat encounter)
            if (!m_hasAlertedAllies)
            {
                AlertNearbyAllies();
                m_hasAlertedAllies = true;
            }

            // The CombatComponent handles movement and attacking in its own Update loop
            // We just need to ensure the target is set
            // NOTE: CombatComponent.Update() will handle:
            // - Moving towards target if out of range
            // - Stopping and attacking when in range
            // - Attack cooldown management
            
            // Alternatively, if we want manual control, we can manage movement and call AttackBehavior():
            // float distanceToTarget = Vector3.Distance(m_controller.transform.position, m_controller.CurrentTarget.transform.position);
            // float attackRange = m_controller.Data != null ? m_controller.Data.AttackRange : 2f;
            // 
            // if (distanceToTarget > attackRange)
            // {
            //     m_controller.MovementComponent?.MoveTo(m_controller.CurrentTarget.transform.position);
            // }
            // else
            // {
            //     m_controller.MovementComponent?.Stop();
            //     m_controller.CombatComponent.AttackBehavior();
            // }
        }

        /// <summary>
        /// Handle events while in attack state.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="data">Optional event data</param>
        public void OnEvent(string eventName, object data)
        {
            switch (eventName)
            {
                case "TargetDetected":
                    // Update target if a new one is detected
                    if (data is GameObject newTarget)
                    {
                        m_controller.SetTarget(newTarget);
                        if (m_controller.CombatComponent != null)
                        {
                            m_controller.CombatComponent.SetTarget(newTarget);
                        }
                    }
                    break;

                case "TargetLost":
                    // Lost sight of target - enter suspicion
                    m_controller.StateMachine.ChangeState("Suspicion");
                    break;

                case "Aggravate":
                    // Already in attack mode, just ensure we're aggro'd
                    m_controller.Aggravate();
                    break;
            }
        }

        /// <summary>
        /// Alert nearby enemy AI to attack.
        /// Similar to EnemyController.AlertNearbyEnemies() behavior.
        /// </summary>
        private void AlertNearbyAllies()
        {
            if (m_controller.Data == null)
                return;

            float alertRange = m_controller.Data.AlertRange;
            
            // Use SphereCast to find nearby enemies
            RaycastHit[] hits = Physics.SphereCastAll(
                m_controller.transform.position, 
                alertRange, 
                Vector3.up, 
                0f
            );

            foreach (RaycastHit hit in hits)
            {
                // Look for other AIController instances
                AIController otherAI = hit.collider.GetComponent<AIController>();
                
                if (otherAI != null && otherAI != m_controller && otherAI.Type == AIController.AIType.Enemy)
                {
                    // Alert the other AI
                    otherAI.Aggravate();
                    if (m_controller.CurrentTarget != null)
                    {
                        otherAI.SetTarget(m_controller.CurrentTarget);
                    }
                }

                // Also look for legacy EnemyController instances for backwards compatibility
                // TODO: Remove this once all enemies are migrated to new AIController
                var legacyEnemy = hit.collider.GetComponent<EnemyController>();
                if (legacyEnemy != null && legacyEnemy != m_controller as EnemyController)
                {
                    legacyEnemy.Aggravate();
                }
            }
        }
    }
}

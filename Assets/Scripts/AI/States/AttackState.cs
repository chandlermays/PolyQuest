using UnityEngine;
//---------------------------------
using PolyQuest.AI.Components;
using PolyQuest.Components;

namespace PolyQuest.AI.States
{
    /// <summary>
    /// State where the AI attacks a target.
    /// Consults AICombatComponent for attack capability and moves toward the target if out of range.
    /// Transitions to Suspicion when target is lost or out of range.
    /// </summary>
    public class AttackState : IAIState
    {
        private GameObject m_target;
        private AICombatComponent m_combatComponent;
        private DetectionComponent m_detectionComponent;
        private MovementComponent m_movementComponent;
        private float m_chaseSpeed = 5f;
        private float m_alertRange = 5f;

        public void Enter(AIController controller)
        {
            m_combatComponent = controller.GetAICombatComponent();
            m_detectionComponent = controller.GetComponent<DetectionComponent>();
            m_movementComponent = controller.MovementComponent;

            // Get target from detection component
            if (m_detectionComponent != null && m_detectionComponent.HasTarget)
            {
                m_target = m_detectionComponent.CurrentTarget;
            }

            if (m_target == null)
            {
                Debug.LogWarning($"[AttackState] {controller.gameObject.name} entered attack state with no target");
                controller.StateMachine.ChangeState(new SuspicionState());
                return;
            }

            // Get chase speed and alert range from AIData
            if (controller.AIData != null)
            {
                m_chaseSpeed = controller.AIData.ChaseSpeed;
                m_alertRange = controller.AIData.AlertRange;
            }

            // Alert nearby allies
            if (controller.AIType == AIType.Enemy)
            {
                AlertNearbyAllies(controller);
            }

            Debug.Log($"[AttackState] {controller.gameObject.name} entered attack state targeting {m_target.name}");
        }

        public void Tick(AIController controller)
        {
            // Check if target is still valid
            if (!IsTargetValid())
            {
                // Target lost, transition to suspicion
                controller.StateMachine.ChangeState(new SuspicionState());
                return;
            }

            // Update detection component to verify target is still in range
            if (m_detectionComponent != null && !m_detectionComponent.IsTargetValid(m_target))
            {
                // Target no longer detected (out of range or LOS lost)
                controller.StateMachine.ChangeState(new SuspicionState());
                return;
            }

            float distanceToTarget = Vector3.Distance(controller.transform.position, m_target.transform.position);

            // Try to attack if in range
            if (m_combatComponent != null && m_combatComponent.CanAttack(m_target))
            {
                m_combatComponent.TryAttack(m_target);
                
                // Stop moving when attacking
                if (m_movementComponent != null)
                {
                    m_movementComponent.Stop();
                }
            }
            else
            {
                // Move toward target if out of attack range
                if (m_movementComponent != null)
                {
                    // Note: Chase speed should be configured on NavMeshAgent.
                    // MovementComponent doesn't expose SetSpeed currently.
                    m_movementComponent.MoveTo(m_target.transform.position);
                }
            }

            // Set combat target if combat component exists
            if (m_combatComponent != null)
            {
                m_combatComponent.SetTarget(m_target);
            }
        }

        public void Exit(AIController controller)
        {
            // Cancel attack when leaving state
            if (m_combatComponent != null)
            {
                m_combatComponent.CancelAttack();
            }

            if (m_movementComponent != null)
            {
                m_movementComponent.Stop();
            }
        }

        public void OnEvent(AIController controller, string eventName, object eventData = null)
        {
            switch (eventName)
            {
                case "TargetLost":
                    // Target lost, transition to suspicion
                    controller.StateMachine.ChangeState(new SuspicionState());
                    break;

                case "TargetDetected":
                    // New target detected, update current target
                    if (eventData is GameObject newTarget)
                    {
                        m_target = newTarget;
                    }
                    break;

                case "Damaged":
                    // Already in attack state, no transition needed
                    // Could reset aggro timer or alert allies again
                    break;
            }
        }

        private bool IsTargetValid()
        {
            return m_target != null && m_target.activeInHierarchy;
        }

        private void AlertNearbyAllies(AIController controller)
        {
            // Use SphereCast to find nearby allies
            RaycastHit[] hits = Physics.SphereCastAll(
                controller.transform.position,
                m_alertRange,
                Vector3.up,
                0f
            );

            foreach (RaycastHit hit in hits)
            {
                // Look for other AIControllers
                AIController allyController = hit.collider.GetComponent<AIController>();

                if (allyController != null && allyController != controller && allyController.AIType == AIType.Enemy)
                {
                    // Alert the ally by sending an event
                    allyController.StateMachine.TriggerEvent("TargetDetected", m_target);
                }
            }

            Debug.Log($"[AttackState] {controller.gameObject.name} alerted nearby allies within {m_alertRange}m");
        }
    }
}

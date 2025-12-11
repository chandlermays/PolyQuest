using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Attack state for AI. The AI pursues and attacks a target.
    /// Transitions to suspicion state when target is lost, or back to patrol if target dies.
    /// </summary>
    public class AttackState : IAIState
    {
        private GameObject m_currentTarget;
        private float m_timeSinceLastSawTarget;

        /// <summary>
        /// Called when entering attack state.
        /// </summary>
        public void Enter(AIController controller)
        {
            m_timeSinceLastSawTarget = 0f;

            // Set chase speed
            AIMovementComponent movement = controller.GetMovementComponent();
            if (movement != null)
            {
                movement.SetChaseSpeed();
            }

            // Get target from detection component
            DetectionComponent detection = controller.GetDetectionComponent();
            if (detection != null)
            {
                m_currentTarget = detection.CurrentTarget;
            }

            // Alert nearby enemies
            AlertNearbyEnemies(controller);
        }

        /// <summary>
        /// Called every frame while in attack state.
        /// </summary>
        public void Tick(AIController controller)
        {
            m_timeSinceLastSawTarget += Time.deltaTime;

            DetectionComponent detection = controller.GetDetectionComponent();
            AIMovementComponent movement = controller.GetMovementComponent();
            AICombatComponent combat = controller.GetCombatComponent();

            // Update target from detection
            if (detection != null)
            {
                if (detection.HasTarget)
                {
                    m_currentTarget = detection.CurrentTarget;
                    m_timeSinceLastSawTarget = 0f;
                }
                else
                {
                    // Check if we've lost target for too long
                    AIData data = controller.AIData;
                    float maxLostTime = data != null ? data.SuspicionTime : 5f;

                    if (m_timeSinceLastSawTarget > maxLostTime)
                    {
                        // Lost target, go to suspicion state
                        controller.ChangeState(new SuspicionState());
                        return;
                    }
                }
            }

            // Validate target still exists and is valid
            if (m_currentTarget == null || !IsTargetValid(m_currentTarget))
            {
                // Target is dead or invalid, return to patrol
                PatrolComponent patrol = controller.GetPatrolComponent();
                if (patrol != null && patrol.HasWaypoints)
                {
                    controller.ChangeState(new PatrolState());
                }
                else
                {
                    controller.ChangeState(new IdleState());
                }
                return;
            }

            // Check distance to target
            float distanceToTarget = Vector3.Distance(controller.transform.position, m_currentTarget.transform.position);
            
            // Get attack range
            float attackRange = 2f; // default
            if (combat != null)
            {
                attackRange = combat.AttackRange;
            }

            if (distanceToTarget <= attackRange)
            {
                // Within attack range, stop and attack
                if (movement != null)
                {
                    movement.Stop();
                    movement.LookAt(m_currentTarget.transform.position);
                }

                if (combat != null)
                {
                    combat.TryAttack(m_currentTarget);
                }
            }
            else
            {
                // Too far, chase the target
                if (movement != null)
                {
                    movement.MoveTo(m_currentTarget.transform.position);
                }
            }
        }

        /// <summary>
        /// Called when exiting attack state.
        /// </summary>
        public void Exit(AIController controller)
        {
            // Cancel combat
            AICombatComponent combat = controller.GetCombatComponent();
            if (combat != null)
            {
                combat.Cancel();
            }

            // Stop movement
            AIMovementComponent movement = controller.GetMovementComponent();
            if (movement != null)
            {
                movement.Stop();
            }

            m_currentTarget = null;
        }

        /// <summary>
        /// Handles events while in attack state.
        /// </summary>
        public void OnEvent(AIController controller, string eventName, object eventData = null)
        {
            switch (eventName)
            {
                case "OnTargetLost":
                    // Target lost, go to suspicion
                    controller.ChangeState(new SuspicionState());
                    break;

                case "OnTargetDied":
                    // Target died, return to patrol
                    PatrolComponent patrol = controller.GetPatrolComponent();
                    if (patrol != null && patrol.HasWaypoints)
                    {
                        controller.ChangeState(new PatrolState());
                    }
                    else
                    {
                        controller.ChangeState(new IdleState());
                    }
                    break;
            }
        }

        /*---------------------------------------------------------------------------- 
        | --- IsTargetValid: Checks if target is still valid --- |
        ----------------------------------------------------------------------------*/
        private bool IsTargetValid(GameObject target)
        {
            if (target == null)
                return false;

            // Check if target is alive
            PolyQuest.Components.HealthComponent health = target.GetComponent<PolyQuest.Components.HealthComponent>();
            if (health != null && health.IsDead)
                return false;

            return true;
        }

        /*---------------------------------------------------------------------------- 
        | --- AlertNearbyEnemies: Alerts nearby friendly AIs --- |
        ----------------------------------------------------------------------------*/
        private void AlertNearbyEnemies(AIController controller)
        {
            AIData data = controller.AIData;
            if (data == null)
                return;

            float alertRange = data.AlertRange;
            Collider[] colliders = Physics.OverlapSphere(controller.transform.position, alertRange);

            foreach (Collider collider in colliders)
            {
                AIController otherAI = collider.GetComponent<AIController>();
                
                if (otherAI != null && otherAI != controller && otherAI.AIType == AIType.Enemy)
                {
                    // Alert the other AI
                    otherAI.OnAggro();
                }
            }
        }
    }
}

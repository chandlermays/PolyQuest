using UnityEngine;
//---------------------------------
using PolyQuest.AI.Components;
using PolyQuest.Components;

namespace PolyQuest.AI.States
{
    /// <summary>
    /// State where the AI patrols between waypoints.
    /// Uses PatrolComponent to manage waypoints and MovementComponent for navigation.
    /// Transitions to other states when targets are detected or on external events.
    /// </summary>
    public class PatrolState : IAIState
    {
        private PatrolComponent m_patrolComponent;
        private MovementComponent m_movementComponent;
        private float m_timeSinceArrivedAtWaypoint = Mathf.Infinity;
        private bool m_hasReachedWaypoint = false;

        public void Enter(AIController controller)
        {
            m_patrolComponent = controller.GetComponent<PatrolComponent>();
            m_movementComponent = controller.MovementComponent;

            if (m_patrolComponent == null)
            {
                Debug.LogWarning($"[PatrolState] No PatrolComponent found on {controller.gameObject.name}. " +
                                 "Cannot patrol without waypoints. Consider adding PatrolComponent or using IdleState.");
                return;
            }

            if (m_movementComponent == null)
            {
                Debug.LogError($"[PatrolState] No MovementComponent found on {controller.gameObject.name}. " +
                               "Cannot patrol without movement capability.");
                return;
            }

            // Set patrol speed from PatrolComponent or AIData
            if (controller.AIData != null)
            {
                // TODO: Set NavMeshAgent speed directly or via MovementComponent API
                // m_movementComponent.SetSpeed(controller.AIData.PatrolSpeed);
            }

            m_timeSinceArrivedAtWaypoint = Mathf.Infinity;
            m_hasReachedWaypoint = false;

            Debug.Log($"[PatrolState] {controller.gameObject.name} entered patrol state");
        }

        public void Tick(AIController controller)
        {
            if (m_patrolComponent == null || m_movementComponent == null)
                return;

            // Get current waypoint
            Vector3 targetWaypoint = m_patrolComponent.GetCurrentWaypoint();

            // Check if we've reached the current waypoint
            if (m_patrolComponent.IsAtCurrentWaypoint(controller.transform.position))
            {
                if (!m_hasReachedWaypoint)
                {
                    m_hasReachedWaypoint = true;
                    m_timeSinceArrivedAtWaypoint = 0f;
                    m_movementComponent.Stop();
                }
            }

            // Wait at waypoint for dwell time
            if (m_hasReachedWaypoint)
            {
                m_timeSinceArrivedAtWaypoint += Time.deltaTime;

                if (m_timeSinceArrivedAtWaypoint >= m_patrolComponent.DwellTime)
                {
                    // Move to next waypoint
                    m_patrolComponent.AdvanceToNextWaypoint();
                    m_hasReachedWaypoint = false;
                    m_timeSinceArrivedAtWaypoint = Mathf.Infinity;
                }
            }
            else
            {
                // Continue moving to waypoint
                m_movementComponent.MoveTo(targetWaypoint);
            }
        }

        public void Exit(AIController controller)
        {
            if (m_movementComponent != null)
            {
                m_movementComponent.Stop();
            }
        }

        public void OnEvent(AIController controller, string eventName, object eventData = null)
        {
            switch (eventName)
            {
                case "TargetDetected":
                    // Enemy AI: transition to attack when target detected
                    if (controller.AIType == AIType.Enemy)
                    {
                        controller.StateMachine.ChangeState(new AttackState());
                    }
                    // NPC AI: might ignore or react differently
                    break;

                case "Damaged":
                    // If damaged while patrolling, become suspicious or aggressive
                    if (controller.AIType == AIType.Enemy)
                    {
                        controller.StateMachine.ChangeState(new SuspicionState());
                    }
                    break;
            }
        }
    }
}

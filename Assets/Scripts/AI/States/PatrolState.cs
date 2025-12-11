using UnityEngine;
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /// <summary>
    /// Patrol state - AI moves between waypoints defined in PatrolComponent.
    /// Gracefully handles missing components by transitioning to Idle.
    /// </summary>
    public class PatrolState : IAIState
    {
        private readonly AIController m_controller;
        private float m_dwellTimer;
        private bool m_isDwelling;

        /// <summary>
        /// Constructor for PatrolState.
        /// </summary>
        /// <param name="controller">Reference to the AI controller</param>
        public PatrolState(AIController controller)
        {
            m_controller = controller;
        }

        /// <summary>
        /// Called when entering the patrol state.
        /// </summary>
        public void Enter()
        {
            m_dwellTimer = 0f;
            m_isDwelling = false;

            // Check if patrol is possible
            if (m_controller.PatrolComponent == null || !m_controller.PatrolComponent.HasWaypoints)
            {
                Debug.LogWarning($"[PatrolState] PatrolComponent not found or no waypoints on {m_controller.gameObject.name}. Transitioning to Idle.");
                m_controller.StateMachine.ChangeState("Idle");
                return;
            }

            if (m_controller.MovementComponent == null)
            {
                Debug.LogError($"[PatrolState] MovementComponent required for patrol on {m_controller.gameObject.name}. Transitioning to Idle.");
                m_controller.StateMachine.ChangeState("Idle");
                return;
            }

            // Start moving to first waypoint
            MoveToCurrentWaypoint();
        }

        /// <summary>
        /// Called when exiting the patrol state.
        /// </summary>
        public void Exit()
        {
            // Stop movement when leaving patrol
            if (m_controller.MovementComponent != null)
            {
                m_controller.MovementComponent.Stop();
            }
        }

        /// <summary>
        /// Called every frame while in patrol state.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        public void Tick(float deltaTime)
        {
            // Verify components are still valid
            if (m_controller.PatrolComponent == null || m_controller.MovementComponent == null)
            {
                m_controller.StateMachine.ChangeState("Idle");
                return;
            }

            // For enemies, check for targets and interrupt patrol
            if (m_controller.Type == AIController.AIType.Enemy)
            {
                if (m_controller.CurrentTarget != null && 
                    m_controller.CombatComponent != null &&
                    m_controller.CombatComponent.CanAttack(m_controller.CurrentTarget))
                {
                    m_controller.StateMachine.ChangeState("Attack");
                    return;
                }
            }

            // Handle dwelling at waypoint
            if (m_isDwelling)
            {
                m_dwellTimer += deltaTime;
                
                float dwellTime = m_controller.Data != null ? m_controller.Data.WaypointDwellTime : 2f;
                
                if (m_dwellTimer >= dwellTime)
                {
                    m_isDwelling = false;
                    m_controller.PatrolComponent.AdvanceToNextWaypoint();
                    MoveToCurrentWaypoint();
                }
                return;
            }

            // Check if reached current waypoint
            if (m_controller.PatrolComponent.HasReachedCurrentWaypoint(m_controller.transform.position))
            {
                m_isDwelling = true;
                m_dwellTimer = 0f;
                m_controller.MovementComponent.Stop();
            }
        }

        /// <summary>
        /// Handle events while in patrol state.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="data">Optional event data</param>
        public void OnEvent(string eventName, object data)
        {
            switch (eventName)
            {
                case "TargetDetected":
                    if (m_controller.Type == AIController.AIType.Enemy && data is GameObject target)
                    {
                        m_controller.SetTarget(target);
                        m_controller.StateMachine.ChangeState("Attack");
                    }
                    break;

                case "Aggravate":
                    if (m_controller.Type == AIController.AIType.Enemy)
                    {
                        m_controller.Aggravate();
                    }
                    break;
            }
        }

        /// <summary>
        /// Move to the current waypoint in the patrol path.
        /// </summary>
        private void MoveToCurrentWaypoint()
        {
            if (m_controller.MovementComponent == null || m_controller.PatrolComponent == null)
                return;

            Vector3 targetWaypoint = m_controller.PatrolComponent.CurrentWaypoint;

            // TODO: MovementComponent API uses StartMoveAction(destination) which cancels combat.
            // For patrol, we may want to use MoveTo(destination) directly or set speed.
            // Current implementation: Use MoveTo for simpler patrol behavior.
            m_controller.MovementComponent.MoveTo(targetWaypoint);

            // Set patrol speed if configured
            // TODO: MovementComponent doesn't expose SetSpeed() - it uses NavMeshAgent.speed directly.
            // If patrol speed needs to be set, access NavMeshAgent directly or add MovementComponent.SetSpeed().
            if (m_controller.Data != null && m_controller.Data.PatrolSpeed > 0)
            {
                var navAgent = m_controller.MovementComponent.NavMeshAgent;
                if (navAgent != null)
                {
                    navAgent.speed = m_controller.Data.PatrolSpeed;
                }
            }
            else if (m_controller.PatrolComponent.PatrolSpeed > 0)
            {
                var navAgent = m_controller.MovementComponent.NavMeshAgent;
                if (navAgent != null)
                {
                    navAgent.speed = m_controller.PatrolComponent.PatrolSpeed;
                }
            }
        }
    }
}

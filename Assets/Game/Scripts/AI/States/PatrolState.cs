using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Patrol state for AI. The AI moves between waypoints defined in PatrolComponent.
    /// Can transition to attack or suspicion states when targets are detected.
    /// </summary>
    public class PatrolState : IAIState
    {
        private float m_timeSinceArrivedAtWaypoint = Mathf.Infinity;
        private bool m_hasReachedWaypoint;

        /// <summary>
        /// Called when entering patrol state.
        /// </summary>
        public void Enter(AIController controller)
        {
            m_timeSinceArrivedAtWaypoint = Mathf.Infinity;
            m_hasReachedWaypoint = false;

            // Set patrol speed
            AIMovementComponent movement = controller.GetMovementComponent();
            if (movement != null)
            {
                movement.SetPatrolSpeed();
            }

            // Start moving to first waypoint
            MoveToNextWaypoint(controller);
        }

        /// <summary>
        /// Called every frame while in patrol state.
        /// </summary>
        public void Tick(AIController controller)
        {
            m_timeSinceArrivedAtWaypoint += Time.deltaTime;

            // Check for targets if detection component is available
            DetectionComponent detection = controller.GetDetectionComponent();
            if (detection != null && detection.HasTarget && controller.AIType == AIType.Enemy)
            {
                // Target detected, transition to attack state
                controller.ChangeState(new AttackState());
                return;
            }

            PatrolComponent patrol = controller.GetPatrolComponent();
            AIMovementComponent movement = controller.GetMovementComponent();

            if (patrol == null || !patrol.HasWaypoints || movement == null)
            {
                // No patrol path configured, go to idle
                controller.ChangeState(new IdleState());
                return;
            }

            AIData data = controller.AIData;
            float waypointTolerance = data != null ? data.WaypointTolerance : 1f;

            // Check if we've reached the current waypoint
            if (!m_hasReachedWaypoint && movement.HasReachedDestination(waypointTolerance))
            {
                m_hasReachedWaypoint = true;
                m_timeSinceArrivedAtWaypoint = 0f;
            }

            // Wait at waypoint for dwell time
            if (m_hasReachedWaypoint)
            {
                float waypointDwellTime = data != null ? data.WaypointDwellTime : 3f;

                if (m_timeSinceArrivedAtWaypoint >= waypointDwellTime)
                {
                    // Move to next waypoint
                    patrol.AdvanceToNextWaypoint();
                    MoveToNextWaypoint(controller);
                    m_hasReachedWaypoint = false;
                    m_timeSinceArrivedAtWaypoint = Mathf.Infinity;
                }
            }
        }

        /// <summary>
        /// Called when exiting patrol state.
        /// </summary>
        public void Exit(AIController controller)
        {
            // Stop movement when leaving patrol
            AIMovementComponent movement = controller.GetMovementComponent();
            if (movement != null)
            {
                movement.Stop();
            }
        }

        /// <summary>
        /// Handles events while in patrol state.
        /// </summary>
        public void OnEvent(AIController controller, string eventName, object eventData = null)
        {
            switch (eventName)
            {
                case "OnTargetDetected":
                    if (controller.AIType == AIType.Enemy)
                    {
                        controller.ChangeState(new AttackState());
                    }
                    break;

                case "OnAggro":
                    if (controller.AIType == AIType.Enemy)
                    {
                        controller.ChangeState(new AttackState());
                    }
                    break;
            }
        }

        /*---------------------------------------------------------------------------- 
        | --- MoveToNextWaypoint: Initiates movement to the next patrol waypoint --- |
        ----------------------------------------------------------------------------*/
        private void MoveToNextWaypoint(AIController controller)
        {
            PatrolComponent patrol = controller.GetPatrolComponent();
            AIMovementComponent movement = controller.GetMovementComponent();

            if (patrol == null || movement == null)
                return;

            Vector3 waypoint = patrol.CurrentWaypoint;
            movement.MoveTo(waypoint);
        }
    }
}

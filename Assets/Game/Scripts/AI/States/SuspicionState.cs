using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Suspicion state for AI. The AI has lost sight of a target but remains alert.
    /// This is a timed state that counts down before returning to patrol/idle.
    /// If target is regained during suspicion, transitions back to attack.
    /// </summary>
    public class SuspicionState : IAIState
    {
        private float m_suspicionTimer;
        private Vector3 m_lastKnownPosition;

        /// <summary>
        /// Called when entering suspicion state.
        /// </summary>
        public void Enter(AIController controller)
        {
            // Get suspicion duration from AI data
            AIData data = controller.AIData;
            m_suspicionTimer = data != null ? data.SuspicionTime : 5f;

            // Stop movement
            AIMovementComponent movement = controller.GetMovementComponent();
            if (movement != null)
            {
                movement.Stop();
            }

            // Store last known target position if available
            DetectionComponent detection = controller.GetDetectionComponent();
            if (detection != null && detection.CurrentTarget != null)
            {
                m_lastKnownPosition = detection.CurrentTarget.transform.position;
            }
            else
            {
                m_lastKnownPosition = controller.transform.position;
            }
        }

        /// <summary>
        /// Called every frame while in suspicion state.
        /// </summary>
        public void Tick(AIController controller)
        {
            m_suspicionTimer -= Time.deltaTime;

            // Check if target has been reacquired
            DetectionComponent detection = controller.GetDetectionComponent();
            if (detection != null && detection.HasTarget)
            {
                // Target found again, return to attack
                controller.ChangeState(new AttackState());
                return;
            }

            // If suspicion timer has expired, return to patrol or idle
            if (m_suspicionTimer <= 0f)
            {
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

            // Look around at last known position
            AIMovementComponent movement = controller.GetMovementComponent();
            if (movement != null)
            {
                movement.LookAt(m_lastKnownPosition);
            }
        }

        /// <summary>
        /// Called when exiting suspicion state.
        /// </summary>
        public void Exit(AIController controller)
        {
            // Clear target from detection component
            DetectionComponent detection = controller.GetDetectionComponent();
            if (detection != null)
            {
                detection.ClearTarget();
            }
        }

        /// <summary>
        /// Handles events while in suspicion state.
        /// </summary>
        public void OnEvent(AIController controller, string eventName, object eventData = null)
        {
            switch (eventName)
            {
                case "OnTargetDetected":
                    // Target reacquired, go back to attack
                    controller.ChangeState(new AttackState());
                    break;

                case "OnAggro":
                    // Aggro event received, go to attack
                    controller.ChangeState(new AttackState());
                    break;

                case "CancelSuspicion":
                    // Force return to patrol/idle
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
    }
}

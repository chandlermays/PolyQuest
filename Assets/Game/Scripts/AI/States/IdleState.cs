using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Idle state for AI. The AI stops movement and waits.
    /// Can transition to patrol or detection states based on events.
    /// </summary>
    public class IdleState : IAIState
    {
        private float m_idleDuration;
        private float m_timeInIdle;

        /// <summary>
        /// Constructor for IdleState.
        /// </summary>
        /// <param name="idleDuration">How long to remain idle (0 for infinite)</param>
        public IdleState(float idleDuration = 0f)
        {
            m_idleDuration = idleDuration;
        }

        /// <summary>
        /// Called when entering idle state.
        /// </summary>
        public void Enter(AIController controller)
        {
            m_timeInIdle = 0f;
            
            // Stop all movement
            AIMovementComponent movement = controller.GetMovementComponent();
            if (movement != null)
            {
                movement.Stop();
            }
        }

        /// <summary>
        /// Called every frame while in idle state.
        /// </summary>
        public void Tick(AIController controller)
        {
            m_timeInIdle += Time.deltaTime;

            // If idle duration is set and expired, transition to patrol
            if (m_idleDuration > 0 && m_timeInIdle >= m_idleDuration)
            {
                // Check if we should patrol
                PatrolComponent patrol = controller.GetPatrolComponent();
                if (patrol != null && patrol.HasWaypoints)
                {
                    controller.ChangeState(new PatrolState());
                }
            }

            // Check for targets if detection component is available
            DetectionComponent detection = controller.GetDetectionComponent();
            if (detection != null && detection.HasTarget)
            {
                // Target detected, transition to attack state
                if (controller.AIType == AIType.Enemy)
                {
                    controller.ChangeState(new AttackState());
                }
            }
        }

        /// <summary>
        /// Called when exiting idle state.
        /// </summary>
        public void Exit(AIController controller)
        {
            // Nothing special to clean up
        }

        /// <summary>
        /// Handles events while in idle state.
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

                case "StartPatrol":
                    PatrolComponent patrol = controller.GetPatrolComponent();
                    if (patrol != null && patrol.HasWaypoints)
                    {
                        controller.ChangeState(new PatrolState());
                    }
                    break;
            }
        }
    }
}

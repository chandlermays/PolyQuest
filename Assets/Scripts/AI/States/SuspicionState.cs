using UnityEngine;
//---------------------------------

namespace PolyQuest.AI.States
{
    /// <summary>
    /// State where the AI is suspicious after losing sight of a target.
    /// This is a timed state that counts down; if target not regained, transitions back to Patrol/Idle.
    /// If target is re-detected, transitions to Attack state.
    /// </summary>
    public class SuspicionState : IAIState
    {
        private float m_suspicionDuration = 3f;
        private Vector3 m_lastKnownTargetPosition;

        public void Enter(AIController controller)
        {
            // Get suspicion duration from AIData if available
            if (controller.AIData != null)
            {
                m_suspicionDuration = controller.AIData.SuspicionDuration;
            }

            // Stop movement when entering suspicion
            var movement = controller.MovementComponent;
            if (movement != null)
            {
                movement.Stop();
            }

            Debug.Log($"[SuspicionState] {controller.gameObject.name} entered suspicion state for {m_suspicionDuration}s");
        }

        public void Tick(AIController controller)
        {
            // Check if suspicion time has elapsed using the state machine's timer
            if (controller.StateMachine.HasBeenInStateFor(m_suspicionDuration))
            {
                // Suspicion time expired, return to patrol or idle
                TransitionToDefaultState(controller);
            }

            // While suspicious, the AI could look around or investigate
            // TODO: Add investigation behavior (look around, move to last known position, etc.)
        }

        public void Exit(AIController controller)
        {
            // No specific cleanup needed
        }

        public void OnEvent(AIController controller, string eventName, object eventData = null)
        {
            switch (eventName)
            {
                case "TargetDetected":
                    // Target re-acquired, transition to attack
                    if (eventData is GameObject target)
                    {
                        m_lastKnownTargetPosition = target.transform.position;
                        controller.StateMachine.ChangeState(new AttackState());
                    }
                    break;

                case "TargetLost":
                    // Target lost again while suspicious - reset timer
                    // The state machine's timer will continue counting
                    break;

                case "Damaged":
                    // If damaged while suspicious, immediately transition to attack
                    controller.StateMachine.ChangeState(new AttackState());
                    break;
            }
        }

        private void TransitionToDefaultState(AIController controller)
        {
            // Check if patrol component exists
            var patrolComponent = controller.GetComponent<PolyQuest.AI.Components.PatrolComponent>();
            
            if (patrolComponent != null && patrolComponent.WaypointCount > 0)
            {
                // Return to patrol if waypoints exist
                controller.StateMachine.ChangeState(new PatrolState());
            }
            else
            {
                // Otherwise, return to idle
                controller.StateMachine.ChangeState(new IdleState());
            }

            Debug.Log($"[SuspicionState] {controller.gameObject.name} suspicion ended, returning to default state");
        }
    }
}

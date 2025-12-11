using UnityEngine;
//---------------------------------

namespace PolyQuest.AI.States
{
    /// <summary>
    /// State where the AI is idle and not performing any actions.
    /// Typically used as a default state or when there are no patrol waypoints.
    /// Can transition to other states based on events (e.g., target detected).
    /// </summary>
    public class IdleState : IAIState
    {
        private float m_idleTime;

        /// <summary>
        /// Gets the time spent in idle state.
        /// </summary>
        public float IdleTime => m_idleTime;

        public void Enter(AIController controller)
        {
            m_idleTime = 0f;
            
            // Stop any movement
            var movement = controller.MovementComponent;
            if (movement != null)
            {
                movement.Stop();
            }

            Debug.Log($"[IdleState] {controller.gameObject.name} entered idle state");
        }

        public void Tick(AIController controller)
        {
            m_idleTime += Time.deltaTime;

            // Idle state is passive - it waits for events to trigger transitions
            // The AIController will handle state transitions based on detection events
        }

        public void Exit(AIController controller)
        {
            // No cleanup needed for idle state
        }

        public void OnEvent(AIController controller, string eventName, object eventData = null)
        {
            switch (eventName)
            {
                case "TargetDetected":
                    // Transition to attack state when target detected
                    var combatComponent = controller.GetAICombatComponent();
                    if (combatComponent != null && eventData is GameObject target)
                    {
                        controller.StateMachine.ChangeState(new AttackState());
                    }
                    break;
            }
        }
    }
}

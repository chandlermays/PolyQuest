using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Generic state machine for managing AI state transitions and execution.
    /// Handles state enter/exit semantics, updates, and optional timed transitions.
    /// </summary>
    public class AIStateMachine
    {
        private IAIState m_currentState;
        private AIController m_controller;
        private float m_timeInState;

        /// <summary>
        /// Gets the currently active state.
        /// </summary>
        public IAIState CurrentState => m_currentState;

        /// <summary>
        /// Gets the time elapsed since entering the current state.
        /// </summary>
        public float TimeInState => m_timeInState;

        /// <summary>
        /// Initializes the state machine with a controller reference.
        /// </summary>
        /// <param name="controller">The AIController that owns this state machine</param>
        public AIStateMachine(AIController controller)
        {
            m_controller = controller;
        }

        /// <summary>
        /// Changes to a new state, properly calling Exit on the old state and Enter on the new state.
        /// </summary>
        /// <param name="newState">The state to transition to</param>
        public void ChangeState(IAIState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning($"[AIStateMachine] Attempted to change to null state on {m_controller.gameObject.name}");
                return;
            }

            // Exit the current state if one exists
            if (m_currentState != null)
            {
                m_currentState.Exit(m_controller);
            }

            // Transition to the new state
            m_currentState = newState;
            m_timeInState = 0f;

            // Enter the new state
            m_currentState.Enter(m_controller);

            Debug.Log($"[AIStateMachine] {m_controller.gameObject.name} changed to state: {m_currentState.GetType().Name}");
        }

        /// <summary>
        /// Updates the current state. Should be called from MonoBehaviour Update().
        /// </summary>
        public void Tick()
        {
            if (m_currentState != null)
            {
                m_currentState.Tick(m_controller);
                m_timeInState += Time.deltaTime;
            }
        }

        /// <summary>
        /// Forwards an event to the current state for handling.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="eventData">Optional data associated with the event</param>
        public void TriggerEvent(string eventName, object eventData = null)
        {
            if (m_currentState != null)
            {
                m_currentState.OnEvent(m_controller, eventName, eventData);
            }
        }

        /// <summary>
        /// Checks if enough time has elapsed in the current state for a timed transition.
        /// </summary>
        /// <param name="duration">The duration threshold in seconds</param>
        /// <returns>True if the time in state exceeds the duration</returns>
        public bool HasBeenInStateFor(float duration)
        {
            return m_timeInState >= duration;
        }
    }
}

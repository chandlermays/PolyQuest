using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Generic state machine for AI. Manages state transitions and ensures proper 
    /// Enter/Exit semantics. States are plain C# objects that implement IAIState.
    /// </summary>
    public class AIStateMachine
    {
        private IAIState m_currentState;
        private AIController m_controller;
        private float m_timeInCurrentState;

        /// <summary>
        /// Gets the current active state.
        /// </summary>
        public IAIState CurrentState => m_currentState;

        /// <summary>
        /// Gets the time elapsed in the current state.
        /// </summary>
        public float TimeInCurrentState => m_timeInCurrentState;

        /// <summary>
        /// Constructor for the state machine.
        /// </summary>
        /// <param name="controller">The AIController that owns this state machine</param>
        public AIStateMachine(AIController controller)
        {
            m_controller = controller;
        }

        /// <summary>
        /// Changes to a new state, calling Exit on the current state and Enter on the new state.
        /// </summary>
        /// <param name="newState">The state to transition to</param>
        public void ChangeState(IAIState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning($"Attempted to change to null state on {m_controller.gameObject.name}");
                return;
            }

            // Exit current state
            if (m_currentState != null)
            {
                m_currentState.Exit(m_controller);
            }

            // Enter new state
            m_currentState = newState;
            m_timeInCurrentState = 0f;
            m_currentState.Enter(m_controller);
        }

        /// <summary>
        /// Updates the current state. Should be called every frame from MonoBehaviour Update.
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        public void Tick(float deltaTime)
        {
            if (m_currentState != null)
            {
                m_timeInCurrentState += deltaTime;
                m_currentState.Tick(m_controller);
            }
        }

        /// <summary>
        /// Sends an event to the current state for handling.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Optional event data</param>
        public void SendEvent(string eventName, object eventData = null)
        {
            if (m_currentState != null)
            {
                m_currentState.OnEvent(m_controller, eventName, eventData);
            }
        }
    }
}

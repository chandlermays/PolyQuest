using System.Collections.Generic;

namespace PolyQuest.AI
{
    /// <summary>
    /// Manages AI state transitions and updates the current state.
    /// Supports optional timed transitions and event-driven state changes.
    /// </summary>
    public class AIStateMachine
    {
        private IAIState m_currentState;
        private Dictionary<string, IAIState> m_states = new Dictionary<string, IAIState>();
        private float m_timeInCurrentState;

        /// <summary>
        /// Gets the currently active state.
        /// </summary>
        public IAIState CurrentState => m_currentState;

        /// <summary>
        /// Gets the time elapsed in the current state.
        /// </summary>
        public float TimeInCurrentState => m_timeInCurrentState;

        /// <summary>
        /// Registers a state with the state machine.
        /// </summary>
        /// <param name="stateName">Unique identifier for the state</param>
        /// <param name="state">State instance to register</param>
        public void RegisterState(string stateName, IAIState state)
        {
            if (!m_states.ContainsKey(stateName))
            {
                m_states[stateName] = state;
            }
        }

        /// <summary>
        /// Changes the current state to a new state.
        /// Exits the current state and enters the new state.
        /// </summary>
        /// <param name="newState">The new state to transition to</param>
        public void ChangeState(IAIState newState)
        {
            if (m_currentState == newState)
                return;

            m_currentState?.Exit();
            m_currentState = newState;
            m_timeInCurrentState = 0f;
            m_currentState?.Enter();
        }

        /// <summary>
        /// Changes the current state by name.
        /// </summary>
        /// <param name="stateName">Name of the registered state</param>
        public void ChangeState(string stateName)
        {
            if (m_states.TryGetValue(stateName, out IAIState state))
            {
                ChangeState(state);
            }
        }

        /// <summary>
        /// Updates the current state. Should be called every frame from AIController.Update().
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        public void Tick(float deltaTime)
        {
            m_timeInCurrentState += deltaTime;
            m_currentState?.Tick(deltaTime);
        }

        /// <summary>
        /// Sends an event to the current state.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="data">Optional event data</param>
        public void SendEvent(string eventName, object data = null)
        {
            m_currentState?.OnEvent(eventName, data);
        }

        /// <summary>
        /// Gets a registered state by name.
        /// </summary>
        /// <param name="stateName">Name of the state</param>
        /// <returns>The state instance, or null if not found</returns>
        public IAIState GetState(string stateName)
        {
            m_states.TryGetValue(stateName, out IAIState state);
            return state;
        }
    }
}

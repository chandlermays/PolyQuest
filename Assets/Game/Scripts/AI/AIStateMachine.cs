using UnityEngine;

namespace PolyQuest.AI
{
    public class AIStateMachine : MonoBehaviour
    {
        private AIController m_owner;
        private IAIState m_currentState;

        private void Awake()
        {
            m_owner = GetComponent<AIController>();
            Utilities.CheckForNull(m_owner, nameof(m_owner));
        }

        public void SetState(IAIState newState)
        {
            if (m_currentState == newState || newState == null)
                return;

            m_currentState.Exit();

            m_currentState = newState;
            m_currentState.Initalize(m_owner);
            m_currentState.Enter();
        }

        private void Update()
        {
            m_currentState.Tick();
        }
    }
}
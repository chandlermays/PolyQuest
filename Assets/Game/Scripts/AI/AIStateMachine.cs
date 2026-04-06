using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Drives state transitions and delegates per-frame updates to the active AI state.       *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Holds a reference to the current IAIState.                                             *
     *      - Calls Exit/Initialize/Enter on transition and Tick each frame.                         *
     *      - Guards against null states and redundant transitions.                                  *
     * --------------------------------------------------------------------------------------------- */
    public class AIStateMachine : MonoBehaviour
    {
        private NEW_AIController m_owner;
        private IAIState m_currentState;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_owner = GetComponent<NEW_AIController>();
            Utilities.CheckForNull(m_owner, nameof(m_owner));
        }

        /*---------------------------------------------------------------------------
        | --- SetState: Transition to a new state, calling lifecycle hooks --- |
        ---------------------------------------------------------------------------*/
        public void SetState(IAIState newState)
        {
            if (newState == null)
                return;

            // Identical instance check — avoids re-entering the same object
            if (m_currentState == newState)
                return;

            m_currentState?.Exit();

            m_currentState = newState;
            m_currentState.Initalize(m_owner);
            m_currentState.Enter();
        }

        /*----------------------------------------- 
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            m_currentState?.Tick();
        }
    }
}
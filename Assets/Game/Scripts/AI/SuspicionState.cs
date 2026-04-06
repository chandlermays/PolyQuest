using UnityEngine;
//---------------------------------
using PolyQuest.Core;

namespace PolyQuest.AI
{
    /* ----------------------------------------------------------------------------------------------
     * Role: AI behaviour state representing brief awareness after losing sight of the target.      *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Cancels the current action so the agent stands still.                                  *
     *      - Transitions back to AttackState if the agent becomes aggravated again.                 *
     *      - Transitions to PatrolState once suspicion time has elapsed.                            *
     * --------------------------------------------------------------------------------------------- */
    public class SuspicionState : IAIState
    {
        private NEW_AIController m_owner;

        /*-------------------------------------------------------------
        | --- Initalize: Store reference to the owning controller --- |
        -------------------------------------------------------------*/
        public void Initalize(NEW_AIController owner)
        {
            m_owner = owner;
        }

        /*----------------------------------------------------------------------
        | --- Enter: Cancel movement so the agent freezes in place --- |
        ----------------------------------------------------------------------*/
        public void Enter()
        {
            m_owner.GetComponent<ActionManager>().CancelCurrentAction();
        }

        /*------------------------------------------------------------------
        | --- Tick: Wait out the suspicion timer, then return to patrol --- |
        ------------------------------------------------------------------*/
        public void Tick()
        {
            // Re-aggro takes priority
            if (m_owner.IsAggrevated() && m_owner.Combat.CanAttack(m_owner.Target))
            {
                m_owner.StateMachine.SetState(new AttackState());
                return;
            }

            if (m_owner.TimeSinceLastSawTarget >= m_owner.SuspicionTime)
            {
                m_owner.StateMachine.SetState(new PatrolState());
            }
        }

        /*-------------------------------------------------------
        | --- Exit: Nothing to clean up for this state --- |
        -------------------------------------------------------*/
        public void Exit() { }
    }
}
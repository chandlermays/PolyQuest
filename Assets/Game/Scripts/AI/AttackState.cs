/*---------------------------
File: AttackState.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /* -----------------------------------------------------------------------------------------------
     * Role: AI behaviour state for actively pursuing and attacking the target.                      *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Drives CombatComponent to attack the target each tick.                                 *
     *      - Keeps TimeSinceLastSawTarget reset while the target is visible.                        *
     *      - Alerts nearby allied agents via a shout radius (aggro propagation).                    *
     *      - Transitions to SuspicionState when the target is no longer aggro'd or attackable.      *
     * --------------------------------------------------------------------------------------------- */
    public class AttackState : IAIState
    {
        private AIController m_owner;

        /*-------------------------------------------------------------
        | --- Initalize: Store reference to the owning controller --- |
        -------------------------------------------------------------*/
        public void Initalize(AIController owner)
        {
            m_owner = owner;
        }

        /*-------------------------------------------------------------------
        | --- Enter: Nothing to set up; attack logic begins in Tick --- |
        -------------------------------------------------------------------*/
        public void Enter() { }

        /*------------------------------------------------------------------
        | --- Tick: Attack the target and evaluate state transitions --- |
        ------------------------------------------------------------------*/
        public void Tick()
        {
            if (!m_owner.IsAggrevated() || !m_owner.Combat.CanAttack(m_owner.Target))
            {
                m_owner.StateMachine.SetState(new SuspicionState());
                return;
            }

            m_owner.TimeSinceLastSawTarget = 0f;
            m_owner.Combat.SetTarget(m_owner.Target);

            AggrevateNearbyEnemies();
        }

        /*---------------------------------------------------------------------------
        | --- Exit: Clear the combat target when leaving the attack state --- |
        ---------------------------------------------------------------------------*/
        public void Exit()
        {
            m_owner.Combat.Cancel();
        }

        /*---------------------------------------------------------------------------
        | --- AggrevateNearbyEnemies: Shout to alert allied agents within range --- |
        ---------------------------------------------------------------------------*/
        private void AggrevateNearbyEnemies()
        {
            RaycastHit[] hits = Physics.SphereCastAll(m_owner.transform.position, m_owner.AlertRange, Vector3.up, 0f);

            foreach (RaycastHit hit in hits)
            {
                AIController ai = hit.collider.GetComponent<AIController>();

                if (ai != null && ai != m_owner)
                {
                    ai.Aggrevate(m_owner.Target);
                }
            }
        }
    }
}
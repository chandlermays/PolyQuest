using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /* ----------------------------------------------------------------------------------------------
     * Role: AI behaviour state for patrolling a NavigationPath or standing guard.                  *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Cycles through NavigationPath waypoints with dwell time at each stop.                  *
     *      - Falls back to the guard position when no patrol path is assigned.                      *
     *      - Transitions to AttackState when the agent becomes aggravated.                          *
     *      - Transitions to SuspicionState when the target was recently seen.                       *
     * --------------------------------------------------------------------------------------------- */
    public class PatrolState : IAIState
    {
        private AIController m_owner;
        private int m_currentWaypointIndex = 0;
        private float m_timeSinceArrivedAtWaypoint = Mathf.Infinity;

        /*-------------------------------------------------------------
        | --- Initalize: Store reference to the owning controller --- |
        -------------------------------------------------------------*/
        public void Initalize(AIController owner)
        {
            m_owner = owner;
        }

        /*-------------------------------------------------------
        | --- Enter: Called once when entering the state --- |
        -------------------------------------------------------*/
        public void Enter()
        {
            m_currentWaypointIndex = 0;
            m_timeSinceArrivedAtWaypoint = Mathf.Infinity;
        }

        /*-------------------------------------------------
        | --- Tick: Called each frame while active --- |
        -------------------------------------------------*/
        public void Tick()
        {
            // Evaluate transitions first
            if (m_owner.IsAggrevated() && m_owner.Combat.CanAttack(m_owner.Target))
            {
                m_owner.StateMachine.SetState(new AttackState());
                return;
            }

            if (m_owner.TimeSinceLastSawTarget < m_owner.SuspicionTime)
            {
                m_owner.StateMachine.SetState(new SuspicionState());
                return;
            }

            PatrolBehaviour();
            m_timeSinceArrivedAtWaypoint += Time.deltaTime;
        }

        /*--------------------------------------------------------
        | --- Exit: Called once when leaving the state --- |
        --------------------------------------------------------*/
        public void Exit() { }

        /*----------------------------------------------------------------------
        | --- PatrolBehaviour: Move along the path or return to guard pos --- |
        ----------------------------------------------------------------------*/
        private void PatrolBehaviour()
        {
            Vector3 nextPosition = m_owner.GuardPosition;

            if (m_owner.PatrolPath != null)
            {
                if (AtWaypoint())
                {
                    m_timeSinceArrivedAtWaypoint = 0f;
                    CycleWaypoint();
                }
                nextPosition = GetCurrentWaypoint();
            }

            if (m_timeSinceArrivedAtWaypoint > m_owner.WaypointDwellTime)
            {
                m_owner.Movement.StartMoveAction(nextPosition);
            }
        }

        /*-----------------------------------------------------------------------
        | --- AtWaypoint: Returns true when close enough to current waypoint --- |
        -----------------------------------------------------------------------*/
        private bool AtWaypoint()
        {
            return Vector3.Distance(m_owner.transform.position, GetCurrentWaypoint())
                   < m_owner.WaypointTolerance;
        }

        /*------------------------------------------------------------------
        | --- CycleWaypoint: Advance to the next index in the path --- |
        ------------------------------------------------------------------*/
        private void CycleWaypoint()
        {
            m_currentWaypointIndex = m_owner.PatrolPath.GetNextIndex(m_currentWaypointIndex);
        }

        /*-----------------------------------------------------------
        | --- GetCurrentWaypoint: Return the active waypoint pos --- |
        -----------------------------------------------------------*/
        private Vector3 GetCurrentWaypoint()
        {
            return m_owner.PatrolPath.GetWaypoint(m_currentWaypointIndex);
        }
    }
}
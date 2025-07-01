using UnityEngine;

namespace PolyQuest
{
    public abstract class NonPlayerController : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] protected NavigationPath m_navigationPath;
        [SerializeField] protected float m_waypointTolerance = 1f;
        [SerializeField] protected float m_waypointDwellTime = 3f;

        /* --- References --- */
        protected Transform m_transform;
        protected MovementComponent m_movementComponent;

        protected Vector3 m_startPosition;
        protected Quaternion m_startRotation;
        protected float m_timeSinceArrivedAtWaypoint = Mathf.Infinity;
        protected int m_currentWaypointIndex = 0;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected virtual void Awake()
        {
            m_transform = transform;

            m_movementComponent = GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_movementComponent, nameof(m_movementComponent));
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        protected virtual void Start()
        {
            m_startPosition = m_transform.position;
            m_startRotation = m_transform.rotation;
        }

        /*--------------------------------------------------------------------- 
        | --- PatrolState: The State of Patrolling an Associated Waypoint --- |
        ---------------------------------------------------------------------*/
        protected void PatrolState()
        {
            Vector3 nextPosition = m_startPosition;

            if (m_navigationPath != null)
            {
                if (AtWaypoint(GetCurrentWaypoint()))
                {
                    m_timeSinceArrivedAtWaypoint = 0;
                    CycleWaypoint();
                }
                nextPosition = GetCurrentWaypoint();
            }

            if (m_timeSinceArrivedAtWaypoint > m_waypointDwellTime)
            {
                m_movementComponent.StartMoveAction(nextPosition);

                // Check if the character has reached its starting destination
                if (AtWaypoint(m_startPosition))
                {
                    m_movementComponent.RotateTo(m_startRotation);
                }
            }
        }

        /*-------------------------------------------------------------------- 
        | --- AtWaypoint: Checks if the Agent is at the current Waypoint --- |
        --------------------------------------------------------------------*/
        protected bool AtWaypoint(Vector3 destination)
        {
            float distanceToWaypoint = Vector3.Distance(m_transform.position, destination);
            return distanceToWaypoint < m_waypointTolerance;
        }

        /*--------------------------------------------------------------- 
        | --- CycleWaypoint: Moves to the next Waypoint in the Path --- |
        ---------------------------------------------------------------*/
        protected void CycleWaypoint()
        {
            m_currentWaypointIndex = m_navigationPath.GetNextIndex(m_currentWaypointIndex);
        }

        /*------------------------------------------------------------------- 
        | --- GetCurrentWaypoint: Returns the current Waypoint destination --- |
        -------------------------------------------------------------------*/
        protected Vector3 GetCurrentWaypoint()
        {
            return m_navigationPath.GetWaypoint(m_currentWaypointIndex);
        }
    }
}
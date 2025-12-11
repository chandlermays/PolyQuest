using UnityEngine;
using System;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Component responsible for managing patrol behavior including waypoint navigation.
    /// Exposes events for when patrol points are reached and provides configurable patrol paths.
    /// </summary>
    public class PatrolComponent : MonoBehaviour
    {
        [Header("Patrol Settings")]
        [Tooltip("Optional navigation path with predefined waypoints")]
        [SerializeField] private NavigationPath m_navigationPath;
        
        [Tooltip("Custom waypoint positions (used if navigation path is not set)")]
        [SerializeField] private Vector3[] m_customWaypoints;
        
        [Tooltip("Whether to loop through waypoints or ping-pong")]
        [SerializeField] private bool m_isLooping = true;

        private int m_currentWaypointIndex = 0;
        private int m_direction = 1;
        private Vector3[] m_waypoints;
        private Vector3 m_startPosition;

        /// <summary>
        /// Event raised when a waypoint is reached.
        /// </summary>
        public event Action<Vector3> OnWaypointReached;

        /// <summary>
        /// Gets whether this component has any waypoints configured.
        /// </summary>
        public bool HasWaypoints => m_waypoints != null && m_waypoints.Length > 0;

        /// <summary>
        /// Gets the current waypoint position.
        /// </summary>
        public Vector3 CurrentWaypoint
        {
            get
            {
                if (!HasWaypoints)
                    return m_startPosition;
                
                return m_waypoints[m_currentWaypointIndex];
            }
        }

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_startPosition = transform.position;
            CacheWaypoints();
        }

        /// <summary>
        /// Advances to the next waypoint in the patrol path.
        /// </summary>
        public void AdvanceToNextWaypoint()
        {
            if (!HasWaypoints)
                return;

            OnWaypointReached?.Invoke(CurrentWaypoint);

            if (m_isLooping)
            {
                m_currentWaypointIndex = (m_currentWaypointIndex + 1) % m_waypoints.Length;
            }
            else
            {
                // Ping-pong behavior
                if (m_currentWaypointIndex + m_direction >= m_waypoints.Length || 
                    m_currentWaypointIndex + m_direction < 0)
                {
                    m_direction *= -1;
                }
                m_currentWaypointIndex += m_direction;
            }
        }

        /// <summary>
        /// Resets patrol to the first waypoint.
        /// </summary>
        public void ResetPatrol()
        {
            m_currentWaypointIndex = 0;
            m_direction = 1;
        }

        /// <summary>
        /// Gets waypoint at specific index.
        /// </summary>
        /// <param name="index">Index of waypoint</param>
        /// <returns>Waypoint position</returns>
        public Vector3 GetWaypoint(int index)
        {
            if (!HasWaypoints || index < 0 || index >= m_waypoints.Length)
                return m_startPosition;
            
            return m_waypoints[index];
        }

        /*--------------------------------------------------------------------------- 
        | --- CacheWaypoints: Store the Waypoints of the Path for Accessibility --- |
        ---------------------------------------------------------------------------*/
        private void CacheWaypoints()
        {
            // Priority 1: Use NavigationPath if available
            if (m_navigationPath != null)
            {
                int waypointCount = m_navigationPath.transform.childCount;
                if (waypointCount > 0)
                {
                    m_waypoints = new Vector3[waypointCount];
                    for (int i = 0; i < waypointCount; i++)
                    {
                        m_waypoints[i] = m_navigationPath.transform.GetChild(i).position;
                    }
                    return;
                }
            }

            // Priority 2: Use custom waypoints
            if (m_customWaypoints != null && m_customWaypoints.Length > 0)
            {
                m_waypoints = m_customWaypoints;
                return;
            }

            // No waypoints configured
            m_waypoints = new Vector3[0];
        }

        /*---------------------------------------------------------------------------------- 
        | --- OnDrawGizmosSelected: Visualize the Waypoints with Gizmos at each Point --- |
        ----------------------------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            if (!HasWaypoints)
                return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < m_waypoints.Length; i++)
            {
                Gizmos.DrawWireSphere(m_waypoints[i], 0.3f);
                
                if (m_isLooping || i < m_waypoints.Length - 1)
                {
                    int nextIndex = m_isLooping ? (i + 1) % m_waypoints.Length : Mathf.Min(i + 1, m_waypoints.Length - 1);
                    Gizmos.DrawLine(m_waypoints[i], m_waypoints[nextIndex]);
                }
            }
        }
    }
}

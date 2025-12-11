using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.AI.Components
{
    /// <summary>
    /// Component responsible for managing patrol behavior including waypoints and movement patterns.
    /// Exposes events for when patrol points are reached and provides configurable patrol settings.
    /// </summary>
    public class PatrolComponent : MonoBehaviour
    {
        [Header("Patrol Settings")]
        [Tooltip("List of waypoints for the patrol route")]
        [SerializeField] private List<Transform> m_waypoints = new List<Transform>();

        [Tooltip("Whether to loop back to the first waypoint or reverse direction")]
        [SerializeField] private bool m_loopWaypoints = true;

        [Tooltip("Use a NavigationPath asset instead of transform waypoints")]
        [SerializeField] private NavigationPath m_navigationPath;

        [Tooltip("Patrol movement speed")]
        [SerializeField] private float m_patrolSpeed = 2f;

        [Tooltip("Distance threshold to consider a waypoint reached")]
        [SerializeField] private float m_waypointTolerance = 1f;

        [Tooltip("Time to wait at each waypoint before moving to the next")]
        [SerializeField] private float m_dwellTime = 2f;

        private int m_currentWaypointIndex = 0;
        private bool m_isReversing = false;
        private Vector3 m_startPosition;

        /// <summary>
        /// Event triggered when a patrol waypoint is reached.
        /// Argument: index of the waypoint that was reached.
        /// </summary>
        public event Action<int> OnWaypointReached;

        /// <summary>
        /// Event triggered when the patrol cycle completes (returns to first waypoint).
        /// </summary>
        public event Action OnPatrolCycleCompleted;

        /// <summary>
        /// Gets the current waypoint index.
        /// </summary>
        public int CurrentWaypointIndex => m_currentWaypointIndex;

        /// <summary>
        /// Gets the total number of waypoints.
        /// </summary>
        public int WaypointCount
        {
            get
            {
                if (m_navigationPath != null)
                {
                    // NavigationPath uses child transforms
                    return m_navigationPath.transform.childCount;
                }
                return m_waypoints.Count;
            }
        }

        /// <summary>
        /// Gets the patrol speed.
        /// </summary>
        public float PatrolSpeed => m_patrolSpeed;

        /// <summary>
        /// Gets the waypoint tolerance distance.
        /// </summary>
        public float WaypointTolerance => m_waypointTolerance;

        /// <summary>
        /// Gets the dwell time at waypoints.
        /// </summary>
        public float DwellTime => m_dwellTime;

        private void Awake()
        {
            m_startPosition = transform.position;
        }

        /// <summary>
        /// Gets the current target waypoint position.
        /// </summary>
        /// <returns>World position of the current waypoint</returns>
        public Vector3 GetCurrentWaypoint()
        {
            if (WaypointCount == 0)
                return m_startPosition;

            if (m_navigationPath != null)
                return m_navigationPath.GetWaypoint(m_currentWaypointIndex);

            if (m_currentWaypointIndex >= 0 && m_currentWaypointIndex < m_waypoints.Count)
                return m_waypoints[m_currentWaypointIndex].position;

            return m_startPosition;
        }

        /// <summary>
        /// Advances to the next waypoint in the patrol route.
        /// Respects loop and reverse settings.
        /// </summary>
        public void AdvanceToNextWaypoint()
        {
            if (WaypointCount == 0)
                return;

            OnWaypointReached?.Invoke(m_currentWaypointIndex);

            if (m_navigationPath != null)
            {
                // Use NavigationPath's built-in cycling logic
                int previousIndex = m_currentWaypointIndex;
                m_currentWaypointIndex = m_navigationPath.GetNextIndex(m_currentWaypointIndex);
                
                if (m_currentWaypointIndex == 0 && previousIndex != 0)
                {
                    OnPatrolCycleCompleted?.Invoke();
                }
            }
            else
            {
                // Manual waypoint list logic
                if (m_loopWaypoints)
                {
                    m_currentWaypointIndex = (m_currentWaypointIndex + 1) % m_waypoints.Count;
                    if (m_currentWaypointIndex == 0)
                    {
                        OnPatrolCycleCompleted?.Invoke();
                    }
                }
                else
                {
                    // Ping-pong between waypoints
                    if (!m_isReversing)
                    {
                        m_currentWaypointIndex++;
                        if (m_currentWaypointIndex >= m_waypoints.Count)
                        {
                            m_currentWaypointIndex = m_waypoints.Count - 2;
                            m_isReversing = true;
                            OnPatrolCycleCompleted?.Invoke();
                        }
                    }
                    else
                    {
                        m_currentWaypointIndex--;
                        if (m_currentWaypointIndex < 0)
                        {
                            m_currentWaypointIndex = 1;
                            m_isReversing = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the given position is at the current waypoint.
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>True if within tolerance of the current waypoint</returns>
        public bool IsAtCurrentWaypoint(Vector3 position)
        {
            Vector3 waypoint = GetCurrentWaypoint();
            float distance = Vector3.Distance(position, waypoint);
            return distance <= m_waypointTolerance;
        }

        /// <summary>
        /// Resets the patrol to the first waypoint.
        /// </summary>
        public void ResetPatrol()
        {
            m_currentWaypointIndex = 0;
            m_isReversing = false;
        }

        /// <summary>
        /// Sets the patrol speed.
        /// </summary>
        /// <param name="speed">New patrol speed</param>
        public void SetPatrolSpeed(float speed)
        {
            m_patrolSpeed = Mathf.Max(0f, speed);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_navigationPath != null)
            {
                // Let NavigationPath draw its own gizmos
                return;
            }

            if (m_waypoints.Count == 0)
                return;

            // Draw waypoint spheres and path lines
            Gizmos.color = Color.cyan;
            for (int i = 0; i < m_waypoints.Count; i++)
            {
                if (m_waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(m_waypoints[i].position, 0.5f);
                    
                    if (i < m_waypoints.Count - 1 && m_waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(m_waypoints[i].position, m_waypoints[i + 1].position);
                    }
                    else if (m_loopWaypoints && i == m_waypoints.Count - 1 && m_waypoints[0] != null)
                    {
                        Gizmos.DrawLine(m_waypoints[i].position, m_waypoints[0].position);
                    }
                }
            }
        }
    }
}

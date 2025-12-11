using System;
using System.Collections.Generic;
using UnityEngine;

namespace PolyQuest.AI
{
    /// <summary>
    /// Modular component that manages waypoint-based patrol behavior.
    /// Can be queried by AI states or other scripts for patrol logic.
    /// </summary>
    public class PatrolComponent : MonoBehaviour
    {
        [Header("Waypoint Settings")]
        [Tooltip("List of waypoints to patrol. Can also be set via NavigationPath.")]
        [SerializeField] private List<Transform> m_waypointTransforms = new List<Transform>();

        [Tooltip("Optional: Use a NavigationPath object instead of individual transforms")]
        [SerializeField] private NavigationPath m_navigationPath;

        [Tooltip("Speed at which to patrol (overrides AIData if set > 0)")]
        [SerializeField] private float m_patrolSpeed = 0f;

        [Tooltip("Distance to waypoint considered 'arrived'")]
        [SerializeField] private float m_arrivalRadius = 1f;

        [Tooltip("Whether to loop the patrol path")]
        [SerializeField] private bool m_loopPath = true;

        // Runtime data
        private List<Vector3> m_waypoints = new List<Vector3>();
        private int m_currentWaypointIndex = 0;
        private int m_direction = 1; // For ping-pong patrol

        /// <summary>
        /// Event triggered when a patrol point is reached.
        /// </summary>
        public event Action<int> OnPatrolPointReached;

        /// <summary>
        /// Gets the list of waypoint positions.
        /// </summary>
        public List<Vector3> Waypoints => m_waypoints;

        /// <summary>
        /// Gets or sets the patrol speed.
        /// </summary>
        public float PatrolSpeed
        {
            get => m_patrolSpeed;
            set => m_patrolSpeed = value;
        }

        /// <summary>
        /// Gets or sets the arrival radius.
        /// </summary>
        public float ArrivalRadius
        {
            get => m_arrivalRadius;
            set => m_arrivalRadius = value;
        }

        /// <summary>
        /// Gets the current waypoint index.
        /// </summary>
        public int CurrentWaypointIndex => m_currentWaypointIndex;

        /// <summary>
        /// Gets the current target waypoint position.
        /// </summary>
        public Vector3 CurrentWaypoint
        {
            get
            {
                if (m_waypoints.Count == 0)
                    return transform.position;

                return m_waypoints[m_currentWaypointIndex];
            }
        }

        /// <summary>
        /// Gets whether there are any waypoints configured.
        /// </summary>
        public bool HasWaypoints => m_waypoints.Count > 0;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            InitializeWaypoints();
        }

        /*----------------------------------------------------------
        | --- InitializeWaypoints: Cache waypoint positions --- |
        ----------------------------------------------------------*/
        private void InitializeWaypoints()
        {
            m_waypoints.Clear();

            // TODO: If a NavigationPath already exists in the repo with similar API,
            // harmonize this implementation and prefer the existing component.
            // Current NavigationPath uses child transforms and has GetWaypoint(index) and GetNextIndex(index).

            // Use NavigationPath if assigned
            if (m_navigationPath != null)
            {
                // NavigationPath stores waypoints as child transforms
                // We'll cache them for consistency
                int childCount = m_navigationPath.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    m_waypoints.Add(m_navigationPath.transform.GetChild(i).position);
                }
            }
            // Otherwise use individual transforms
            else if (m_waypointTransforms.Count > 0)
            {
                foreach (Transform waypoint in m_waypointTransforms)
                {
                    if (waypoint != null)
                    {
                        m_waypoints.Add(waypoint.position);
                    }
                }
            }

            if (m_waypoints.Count == 0)
            {
                Debug.LogWarning($"[PatrolComponent] No waypoints configured on {gameObject.name}. Patrol behavior will not function.");
            }
        }

        /*-----------------------------------------------------------------
        | --- HasReachedCurrentWaypoint: Check if at current waypoint --- |
        -----------------------------------------------------------------*/
        public bool HasReachedCurrentWaypoint(Vector3 currentPosition)
        {
            if (m_waypoints.Count == 0)
                return true;

            float distance = Vector3.Distance(currentPosition, CurrentWaypoint);
            return distance <= m_arrivalRadius;
        }

        /*-----------------------------------------------------------------
        | --- AdvanceToNextWaypoint: Move to the next waypoint in path --- |
        -----------------------------------------------------------------*/
        public void AdvanceToNextWaypoint()
        {
            if (m_waypoints.Count == 0)
                return;

            int previousIndex = m_currentWaypointIndex;

            if (m_loopPath)
            {
                m_currentWaypointIndex = (m_currentWaypointIndex + 1) % m_waypoints.Count;
            }
            else
            {
                // Ping-pong behavior
                if (m_currentWaypointIndex + m_direction >= m_waypoints.Count || 
                    m_currentWaypointIndex + m_direction < 0)
                {
                    m_direction *= -1;
                }
                m_currentWaypointIndex += m_direction;
            }

            // Trigger event
            OnPatrolPointReached?.Invoke(previousIndex);
        }

        /*-----------------------------------------------------------------
        | --- ResetPatrol: Reset to first waypoint --- |
        -----------------------------------------------------------------*/
        public void ResetPatrol()
        {
            m_currentWaypointIndex = 0;
            m_direction = 1;
        }

        /*-----------------------------------------------------------------
        | --- OnDrawGizmosSelected: Visualize patrol path --- |
        -----------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            // Use cached waypoints at runtime, or read from sources in editor
            List<Vector3> gizmoWaypoints = new List<Vector3>();

            if (Application.isPlaying && m_waypoints.Count > 0)
            {
                gizmoWaypoints = m_waypoints;
            }
            else if (m_navigationPath != null)
            {
                int childCount = m_navigationPath.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    gizmoWaypoints.Add(m_navigationPath.transform.GetChild(i).position);
                }
            }
            else if (m_waypointTransforms.Count > 0)
            {
                foreach (Transform waypoint in m_waypointTransforms)
                {
                    if (waypoint != null)
                    {
                        gizmoWaypoints.Add(waypoint.position);
                    }
                }
            }

            // Draw waypoints and connections
            Gizmos.color = Color.cyan;
            for (int i = 0; i < gizmoWaypoints.Count; i++)
            {
                Gizmos.DrawWireSphere(gizmoWaypoints[i], m_arrivalRadius);

                if (i < gizmoWaypoints.Count - 1)
                {
                    Gizmos.DrawLine(gizmoWaypoints[i], gizmoWaypoints[i + 1]);
                }
                else if (m_loopPath && gizmoWaypoints.Count > 1)
                {
                    Gizmos.DrawLine(gizmoWaypoints[i], gizmoWaypoints[0]);
                }
            }
        }
    }
}

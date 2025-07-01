using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    /*----------------------------------------------------------------- 
    | --- Definition and Management of an Agent's Navigation Path --- |
    -----------------------------------------------------------------*/
    public class NavigationPath : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private float m_waypointGizmoRadius = 0.5f;
        [SerializeField] private bool m_isLooping = true;

        private Vector3[] m_waypoints;
        private int m_direction = 1;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            CacheWaypoints();
        }

        /*---------------------------------------------------------------------------------- 
        | --- OnDrawGizmos: Visualize the Waypoints with Gizmos drawn at each Waypoint --- |
        ----------------------------------------------------------------------------------*/
        private void OnDrawGizmos()
        {
            CacheWaypoints();

            for (int i = 0; i < m_waypoints.Length; ++i)
            {
                Gizmos.DrawSphere(m_waypoints[i], m_waypointGizmoRadius);

                if (m_isLooping || i < m_waypoints.Length - 1)
                {
                    int j = GetNextIndexForGizmos(i);
                    Gizmos.DrawLine(m_waypoints[i], m_waypoints[j]);
                }
            }
        }

        /*------------------------------------------------------------------------------ 
        | --- GetNextIndex: Returns the next Waypoint Index in the Navigation Path --- |
        ------------------------------------------------------------------------------*/
        public int GetNextIndex(int i)
        {
            if (m_isLooping)
            {
                int nextIndex = (i + 1) % m_waypoints.Length;
                return nextIndex;
            }
            else
            {
                if (i + m_direction >= m_waypoints.Length || i + m_direction < 0)
                {
                    m_direction *= -1;
                }
                int nextIndex = i + m_direction;
                return nextIndex;
            }
        }

        /*--------------------------------------------------------------------- 
        | --- GetWaypoint: Returns the Waypoint with the Associated Index --- |
        ---------------------------------------------------------------------*/
        public Vector3 GetWaypoint(int i)
        {
            return m_waypoints[i];
        }

        /*--------------------------------------------------------------------------- 
        | --- CacheWaypoints: Store the Waypoints of the Path for Accessibility --- |
        ---------------------------------------------------------------------------*/
        private void CacheWaypoints()
        {
            m_waypoints = new Vector3[transform.childCount];
            for (int i = 0; i < transform.childCount; ++i)
            {
                m_waypoints[i] = transform.GetChild(i).position;
            }
        }

        /*---------------------------------------------------------------------------------------------------- 
        | --- GetNextIndexForGizmos: Returns the next Waypoint Index in the Navigation Path (for Gizmos) --- |
        ----------------------------------------------------------------------------------------------------*/
        private int GetNextIndexForGizmos(int i)
        {
            if (m_isLooping)
            {
                return (i + 1) % m_waypoints.Length;
            }
            else
            {
                return i + 1 < m_waypoints.Length ? i + 1 : i;
            }
        }
    }
}
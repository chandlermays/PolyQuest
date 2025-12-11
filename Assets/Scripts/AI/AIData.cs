using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// ScriptableObject that stores tuning values for AI behavior.
    /// These values are shared across states and components and can be easily tweaked without code changes.
    /// Create instances via: Assets -> Create -> PolyQuest -> AI -> AI Data
    /// </summary>
    [CreateAssetMenu(fileName = "New AI Data", menuName = "PolyQuest/AI/AI Data", order = 0)]
    public class AIData : ScriptableObject
    {
        [Header("Detection Settings")]
        [Tooltip("Maximum distance at which the AI can detect targets")]
        [SerializeField] private float m_sightRange = 10f;

        [Tooltip("Field of view angle in degrees (180 = half circle, 360 = full circle)")]
        [SerializeField] private float m_fieldOfViewAngle = 90f;

        [Tooltip("Layer mask for targets that can be detected")]
        [SerializeField] private LayerMask m_targetLayers;

        [Tooltip("Layer mask for obstacles that block line of sight")]
        [SerializeField] private LayerMask m_obstacleLayers;

        [Header("Suspicion Settings")]
        [Tooltip("Time in seconds to remain suspicious after losing sight of target")]
        [SerializeField] private float m_suspicionDuration = 3f;

        [Header("Movement Settings")]
        [Tooltip("Movement speed while patrolling")]
        [SerializeField] private float m_patrolSpeed = 2f;

        [Tooltip("Movement speed while chasing a target")]
        [SerializeField] private float m_chaseSpeed = 5f;

        [Tooltip("Distance tolerance for reaching waypoints")]
        [SerializeField] private float m_waypointTolerance = 1f;

        [Tooltip("Time to wait at each waypoint before moving to the next")]
        [SerializeField] private float m_waypointDwellTime = 2f;

        [Header("Combat Settings")]
        [Tooltip("Maximum distance at which the AI can attack")]
        [SerializeField] private float m_attackRange = 2f;

        [Tooltip("Minimum time between attacks in seconds")]
        [SerializeField] private float m_attackCooldown = 1.5f;

        [Tooltip("Range at which nearby allies are alerted to combat")]
        [SerializeField] private float m_alertRange = 5f;

        [Tooltip("Time in seconds the AI remains aggressive after being hit")]
        [SerializeField] private float m_aggroCooldown = 5f;

        // Public getters for all settings
        public float SightRange => m_sightRange;
        public float FieldOfViewAngle => m_fieldOfViewAngle;
        public LayerMask TargetLayers => m_targetLayers;
        public LayerMask ObstacleLayers => m_obstacleLayers;
        public float SuspicionDuration => m_suspicionDuration;
        public float PatrolSpeed => m_patrolSpeed;
        public float ChaseSpeed => m_chaseSpeed;
        public float WaypointTolerance => m_waypointTolerance;
        public float WaypointDwellTime => m_waypointDwellTime;
        public float AttackRange => m_attackRange;
        public float AttackCooldown => m_attackCooldown;
        public float AlertRange => m_alertRange;
        public float AggroCooldown => m_aggroCooldown;
    }
}

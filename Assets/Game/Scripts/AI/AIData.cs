using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// ScriptableObject that stores AI configuration data for tuning behavior across states and components.
    /// This allows designers to create different AI profiles without changing code.
    /// </summary>
    [CreateAssetMenu(fileName = "New AI Data", menuName = "PolyQuest/AI/AI Data", order = 0)]
    public class AIData : ScriptableObject
    {
        [Header("Detection Settings")]
        [Tooltip("Maximum distance at which the AI can detect targets")]
        [SerializeField] private float m_sightRange = 10f;
        
        [Tooltip("Field of view angle in degrees (180 = half-sphere, 360 = full sphere)")]
        [SerializeField] private float m_fieldOfViewAngle = 90f;
        
        [Tooltip("Layer mask for what the AI can detect")]
        [SerializeField] private LayerMask m_detectionLayers = -1;
        
        [Tooltip("Range at which nearby allies are alerted")]
        [SerializeField] private float m_alertRange = 5f;

        [Header("Patrol Settings")]
        [Tooltip("Movement speed while patrolling")]
        [SerializeField] private float m_patrolSpeed = 2f;
        
        [Tooltip("Distance threshold to consider waypoint reached")]
        [SerializeField] private float m_waypointTolerance = 1f;
        
        [Tooltip("Time to wait at each waypoint before moving to next")]
        [SerializeField] private float m_waypointDwellTime = 3f;

        [Header("Combat Settings")]
        [Tooltip("Movement speed while chasing/attacking")]
        [SerializeField] private float m_chaseSpeed = 5f;
        
        [Tooltip("Maximum range at which AI can attack")]
        [SerializeField] private float m_attackRange = 2f;
        
        [Tooltip("Time between attacks")]
        [SerializeField] private float m_attackCooldown = 1.5f;

        [Header("Suspicion Settings")]
        [Tooltip("Time to remain suspicious after losing sight of target")]
        [SerializeField] private float m_suspicionTime = 5f;
        
        [Tooltip("Time aggro remains active after being hit")]
        [SerializeField] private float m_aggroCooldown = 5f;

        // Public accessors
        public float SightRange => m_sightRange;
        public float FieldOfViewAngle => m_fieldOfViewAngle;
        public LayerMask DetectionLayers => m_detectionLayers;
        public float AlertRange => m_alertRange;
        public float PatrolSpeed => m_patrolSpeed;
        public float WaypointTolerance => m_waypointTolerance;
        public float WaypointDwellTime => m_waypointDwellTime;
        public float ChaseSpeed => m_chaseSpeed;
        public float AttackRange => m_attackRange;
        public float AttackCooldown => m_attackCooldown;
        public float SuspicionTime => m_suspicionTime;
        public float AggroCooldown => m_aggroCooldown;
    }
}

using UnityEngine;

namespace PolyQuest.AI
{
    /// <summary>
    /// ScriptableObject containing tuning values for AI behavior.
    /// This allows designers to configure AI parameters without modifying code.
    /// </summary>
    [CreateAssetMenu(fileName = "New AI Data", menuName = "PolyQuest/AI/AI Data")]
    public class AIData : ScriptableObject
    {
        [Header("Detection Settings")]
        [Tooltip("How far the AI can see targets")]
        [SerializeField] private float m_sightRange = 10f;

        [Tooltip("Field of view angle in degrees")]
        [SerializeField] private float m_fovAngle = 90f;

        [Tooltip("How often to check for targets (in seconds)")]
        [SerializeField] private float m_checkInterval = 0.5f;

        [Header("Suspicion Settings")]
        [Tooltip("How long to remain suspicious after losing sight of target")]
        [SerializeField] private float m_suspicionTime = 3f;

        [Header("Patrol Settings")]
        [Tooltip("Movement speed during patrol")]
        [SerializeField] private float m_patrolSpeed = 2f;

        [Tooltip("How close to waypoint before considering it reached")]
        [SerializeField] private float m_patrolWaypointTolerance = 1f;

        [Tooltip("How long to wait at waypoint before moving to next")]
        [SerializeField] private float m_waypointDwellTime = 2f;

        [Header("Chase Settings")]
        [Tooltip("Movement speed when chasing target")]
        [SerializeField] private float m_chaseSpeed = 4f;

        [Header("Combat Settings")]
        [Tooltip("Maximum range to attack target")]
        [SerializeField] private float m_attackRange = 2f;

        [Tooltip("Minimum time between attacks")]
        [SerializeField] private float m_attackCooldown = 1.5f;

        [Tooltip("Range to alert nearby allies")]
        [SerializeField] private float m_alertRange = 5f;

        [Tooltip("How long aggro persists after being hit")]
        [SerializeField] private float m_aggroCooldown = 5f;

        // Public accessors
        public float SightRange => m_sightRange;
        public float FovAngle => m_fovAngle;
        public float CheckInterval => m_checkInterval;
        public float SuspicionTime => m_suspicionTime;
        public float PatrolSpeed => m_patrolSpeed;
        public float PatrolWaypointTolerance => m_patrolWaypointTolerance;
        public float WaypointDwellTime => m_waypointDwellTime;
        public float ChaseSpeed => m_chaseSpeed;
        public float AttackRange => m_attackRange;
        public float AttackCooldown => m_attackCooldown;
        public float AlertRange => m_alertRange;
        public float AggroCooldown => m_aggroCooldown;
    }
}

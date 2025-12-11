using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.AI.Components
{
    /// <summary>
    /// Component responsible for detecting targets (players/enemies) using configurable ranges, FOV, and layers.
    /// Provides robust target detection with line-of-sight checks and exposes events for target acquisition/loss.
    /// </summary>
    public class DetectionComponent : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Maximum distance at which targets can be detected")]
        [SerializeField] private float m_sightRange = 10f;

        [Tooltip("Field of view angle in degrees (180 = half circle, 360 = full circle)")]
        [SerializeField] private float m_fieldOfViewAngle = 90f;

        [Tooltip("Layers that can be detected as targets")]
        [SerializeField] private LayerMask m_targetLayers = -1;

        [Tooltip("Layers that block line of sight")]
        [SerializeField] private LayerMask m_obstacleLayers = -1;

        [Tooltip("Height offset for line-of-sight raycast origin")]
        [SerializeField] private float m_eyeHeight = 1.5f;

        [Tooltip("How often to check for targets (in seconds)")]
        [SerializeField] private float m_detectionInterval = 0.2f;

        [Tooltip("Require line of sight to detect target")]
        [SerializeField] private bool m_requireLineOfSight = true;

        private GameObject m_currentTarget;
        private float m_timeSinceLastDetection = 0f;
        private Transform m_transform;

        /// <summary>
        /// Event triggered when a new target is detected.
        /// Argument: The target GameObject that was detected.
        /// </summary>
        public event Action<GameObject> OnTargetDetected;

        /// <summary>
        /// Event triggered when the current target is lost.
        /// </summary>
        public event Action OnTargetLost;

        /// <summary>
        /// Gets the currently detected target, or null if no target is detected.
        /// </summary>
        public GameObject CurrentTarget => m_currentTarget;

        /// <summary>
        /// Gets whether a target is currently detected.
        /// </summary>
        public bool HasTarget => m_currentTarget != null;

        /// <summary>
        /// Gets the sight range.
        /// </summary>
        public float SightRange => m_sightRange;

        /// <summary>
        /// Gets the field of view angle.
        /// </summary>
        public float FieldOfViewAngle => m_fieldOfViewAngle;

        private void Awake()
        {
            m_transform = transform;
        }

        private void Update()
        {
            m_timeSinceLastDetection += Time.deltaTime;

            if (m_timeSinceLastDetection >= m_detectionInterval)
            {
                m_timeSinceLastDetection = 0f;
                UpdateTargetDetection();
            }
        }

        /// <summary>
        /// Manually triggers target detection check. Useful for immediate detection needs.
        /// </summary>
        public void ForceDetectionCheck()
        {
            UpdateTargetDetection();
        }

        /// <summary>
        /// Checks if a specific target is currently detected and valid.
        /// </summary>
        /// <param name="target">The target to check</param>
        /// <returns>True if the target is valid and detected</returns>
        public bool IsTargetValid(GameObject target)
        {
            if (target == null)
                return false;

            if (!target.activeInHierarchy)
                return false;

            float distance = Vector3.Distance(m_transform.position, target.transform.position);
            if (distance > m_sightRange)
                return false;

            if (!IsInFieldOfView(target.transform.position))
                return false;

            if (m_requireLineOfSight && !HasLineOfSight(target.transform.position))
                return false;

            return true;
        }

        /// <summary>
        /// Sets detection parameters from an AIData asset.
        /// </summary>
        /// <param name="aiData">The AI data to apply</param>
        public void ApplyAIData(AIData aiData)
        {
            if (aiData == null)
            {
                Debug.LogWarning($"[DetectionComponent] Null AIData provided to {gameObject.name}");
                return;
            }

            m_sightRange = aiData.SightRange;
            m_fieldOfViewAngle = aiData.FieldOfViewAngle;
            m_targetLayers = aiData.TargetLayers;
            m_obstacleLayers = aiData.ObstacleLayers;
        }

        /// <summary>
        /// Clears the current target and triggers OnTargetLost if a target was present.
        /// </summary>
        public void ClearTarget()
        {
            if (m_currentTarget != null)
            {
                m_currentTarget = null;
                OnTargetLost?.Invoke();
            }
        }

        private void UpdateTargetDetection()
        {
            GameObject newTarget = FindNearestTarget();

            // Target changed from valid to null
            if (m_currentTarget != null && newTarget == null)
            {
                m_currentTarget = null;
                OnTargetLost?.Invoke();
                return;
            }

            // New target detected
            if (m_currentTarget == null && newTarget != null)
            {
                m_currentTarget = newTarget;
                OnTargetDetected?.Invoke(m_currentTarget);
                return;
            }

            // Target switched to a different object
            if (m_currentTarget != null && newTarget != null && m_currentTarget != newTarget)
            {
                m_currentTarget = newTarget;
                OnTargetDetected?.Invoke(m_currentTarget);
            }
        }

        private GameObject FindNearestTarget()
        {
            // Use OverlapSphere to find potential targets
            Collider[] hits = Physics.OverlapSphere(m_transform.position, m_sightRange, m_targetLayers);

            GameObject closestTarget = null;
            float closestDistance = Mathf.Infinity;

            foreach (Collider hit in hits)
            {
                // Skip self
                if (hit.gameObject == gameObject)
                    continue;

                GameObject potentialTarget = hit.gameObject;

                // Check field of view
                if (!IsInFieldOfView(potentialTarget.transform.position))
                    continue;

                // Check line of sight if required
                if (m_requireLineOfSight && !HasLineOfSight(potentialTarget.transform.position))
                    continue;

                // Find the closest valid target
                float distance = Vector3.Distance(m_transform.position, potentialTarget.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = potentialTarget;
                }
            }

            return closestTarget;
        }

        private bool IsInFieldOfView(Vector3 targetPosition)
        {
            // If FOV is 360 degrees, always in view
            if (m_fieldOfViewAngle >= 360f)
                return true;

            Vector3 directionToTarget = (targetPosition - m_transform.position).normalized;
            float angleToTarget = Vector3.Angle(m_transform.forward, directionToTarget);
            
            return angleToTarget <= m_fieldOfViewAngle / 2f;
        }

        private bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 eyePosition = m_transform.position + Vector3.up * m_eyeHeight;
            Vector3 directionToTarget = targetPosition - eyePosition;
            float distanceToTarget = directionToTarget.magnitude;

            // Raycast to check for obstacles
            if (Physics.Raycast(eyePosition, directionToTarget.normalized, out RaycastHit hit, distanceToTarget, m_obstacleLayers))
            {
                // Hit an obstacle before reaching the target
                return false;
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (m_transform == null)
                m_transform = transform;

            // Draw sight range
            Gizmos.color = HasTarget ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(m_transform.position, m_sightRange);

            // Draw field of view
            if (m_fieldOfViewAngle < 360f)
            {
                Vector3 leftBoundary = Quaternion.Euler(0, -m_fieldOfViewAngle / 2f, 0) * m_transform.forward * m_sightRange;
                Vector3 rightBoundary = Quaternion.Euler(0, m_fieldOfViewAngle / 2f, 0) * m_transform.forward * m_sightRange;

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(m_transform.position, m_transform.position + leftBoundary);
                Gizmos.DrawLine(m_transform.position, m_transform.position + rightBoundary);
            }

            // Draw line to current target
            if (HasTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(m_transform.position + Vector3.up * m_eyeHeight, CurrentTarget.transform.position);
            }
        }
    }
}

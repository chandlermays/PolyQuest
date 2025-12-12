using System;
using UnityEngine;

namespace PolyQuest.AI
{
    /// <summary>
    /// Modular component that performs target detection using OverlapSphere and raycasts for line-of-sight.
    /// Exposes events for target detection and loss.
    /// </summary>
    public class DetectionComponent : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Maximum sight range for detection")]
        [SerializeField] private float m_sightRange = 10f;

        [Tooltip("Field of view angle in degrees")]
        [SerializeField] private float m_fovAngle = 90f;

        [Tooltip("How often to perform detection checks (in seconds)")]
        [SerializeField] private float m_checkInterval = 0.5f;

        [Tooltip("Layer mask for potential targets")]
        [SerializeField] private LayerMask m_targetLayers = -1;

        [Tooltip("Layer mask for obstacles that block line of sight")]
        [SerializeField] private LayerMask m_obstacleLayers = -1;

        [Tooltip("Tag to search for when detecting targets (default: Player)")]
        [SerializeField] private string m_targetTag = "Player";

        [Tooltip("Whether to require line of sight for detection")]
        [SerializeField] private bool m_requireLineOfSight = true;

        [Tooltip("Height offset for raycast origin (eye level, default 1.5m)")]
        [SerializeField] private float m_raycastHeightOffset = 1.5f;

        // Runtime state
        private GameObject m_currentTarget;
        private float m_timeSinceLastCheck;
        private bool m_hasTarget;

        /// <summary>
        /// Event triggered when a target is detected.
        /// </summary>
        public event Action<GameObject> OnTargetDetected;

        /// <summary>
        /// Event triggered when a previously detected target is lost.
        /// </summary>
        public event Action OnTargetLost;

        /// <summary>
        /// Gets the currently detected target (may be null).
        /// </summary>
        public GameObject CurrentTarget => m_currentTarget;

        /// <summary>
        /// Gets whether a target is currently detected.
        /// </summary>
        public bool HasTarget => m_hasTarget;

        /// <summary>
        /// Gets or sets the sight range.
        /// </summary>
        public float SightRange
        {
            get => m_sightRange;
            set => m_sightRange = value;
        }

        /// <summary>
        /// Gets or sets the FOV angle.
        /// </summary>
        public float FovAngle
        {
            get => m_fovAngle;
            set => m_fovAngle = value;
        }

        /// <summary>
        /// Gets or sets the check interval.
        /// </summary>
        public float CheckInterval
        {
            get => m_checkInterval;
            set => m_checkInterval = value;
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            m_timeSinceLastCheck += Time.deltaTime;

            if (m_timeSinceLastCheck >= m_checkInterval)
            {
                m_timeSinceLastCheck = 0f;
                PerformDetectionCheck();
            }
        }

        /*-----------------------------------------------------------------
        | --- PerformDetectionCheck: Check for targets in detection range --- |
        -----------------------------------------------------------------*/
        private void PerformDetectionCheck()
        {
            GameObject detectedTarget = FindTarget();

            // Target detected for the first time
            if (detectedTarget != null && !m_hasTarget)
            {
                m_currentTarget = detectedTarget;
                m_hasTarget = true;
                OnTargetDetected?.Invoke(m_currentTarget);
            }
            // Target lost
            else if (detectedTarget == null && m_hasTarget)
            {
                m_currentTarget = null;
                m_hasTarget = false;
                OnTargetLost?.Invoke();
            }
            // Target changed
            else if (detectedTarget != null && m_currentTarget != detectedTarget)
            {
                m_currentTarget = detectedTarget;
                OnTargetDetected?.Invoke(m_currentTarget);
            }
        }

        /*-----------------------------------------------------------------
        | --- FindTarget: Search for valid target in detection range --- |
        -----------------------------------------------------------------*/
        private GameObject FindTarget()
        {
            // Perform overlap sphere check
            Collider[] colliders = Physics.OverlapSphere(transform.position, m_sightRange, m_targetLayers);

            GameObject closestTarget = null;
            float closestDistance = Mathf.Infinity;

            foreach (Collider collider in colliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                    continue;

                // Check tag if specified
                if (!string.IsNullOrEmpty(m_targetTag) && !collider.CompareTag(m_targetTag))
                    continue;

                // Check if within FOV
                Vector3 directionToTarget = (collider.transform.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                if (angleToTarget > m_fovAngle / 2f)
                    continue;

                // Check line of sight if required
                if (m_requireLineOfSight && !HasLineOfSight(collider.gameObject))
                    continue;

                // Track closest valid target
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestTarget = collider.gameObject;
                    closestDistance = distance;
                }
            }

            return closestTarget;
        }

        /*-----------------------------------------------------------------
        | --- HasLineOfSight: Raycast to check for obstacles --- |
        -----------------------------------------------------------------*/
        private bool HasLineOfSight(GameObject target)
        {
            if (target == null)
                return false;

            // Use height offset for raycast origin (eye level)
            Vector3 rayOrigin = transform.position + Vector3.up * m_raycastHeightOffset;
            Vector3 targetPosition = target.transform.position + Vector3.up * m_raycastHeightOffset;
            
            Vector3 directionToTarget = targetPosition - rayOrigin;
            float distanceToTarget = directionToTarget.magnitude;

            // Perform raycast from eye level to target eye level
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, directionToTarget.normalized, out hit, distanceToTarget, m_obstacleLayers))
            {
                // Check if we hit the target or an obstacle
                return hit.collider.gameObject == target;
            }

            // No obstacles in the way
            return true;
        }

        /*-----------------------------------------------------------------
        | --- ForceDetectionCheck: Immediately perform a detection check --- |
        | Performs detection check immediately and resets the timer, so the
        | next scheduled check will occur after a full CheckInterval.
        -----------------------------------------------------------------*/
        public void ForceDetectionCheck()
        {
            PerformDetectionCheck();
            m_timeSinceLastCheck = 0f;
        }

        /*-----------------------------------------------------------------
        | --- ClearTarget: Clear the current target --- |
        -----------------------------------------------------------------*/
        public void ClearTarget()
        {
            if (m_hasTarget)
            {
                m_currentTarget = null;
                m_hasTarget = false;
                OnTargetLost?.Invoke();
            }
        }

        /*-----------------------------------------------------------------
        | --- OnDrawGizmosSelected: Visualize detection range and FOV --- |
        -----------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            // Draw sight range
            Gizmos.color = m_hasTarget ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_sightRange);

            // Draw FOV cone
            Vector3 fovLine1 = Quaternion.AngleAxis(m_fovAngle / 2f, transform.up) * transform.forward * m_sightRange;
            Vector3 fovLine2 = Quaternion.AngleAxis(-m_fovAngle / 2f, transform.up) * transform.forward * m_sightRange;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, fovLine1);
            Gizmos.DrawRay(transform.position, fovLine2);

            // Draw line to current target
            if (m_hasTarget && m_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, m_currentTarget.transform.position);
            }
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /// <summary>
    /// Component responsible for detecting targets (players/enemies) using physics-based sensing.
    /// Uses OverlapSphere with layer masks and raycasts for line-of-sight checks.
    /// Exposes events for target detection and loss with robust null handling.
    /// </summary>
    public class DetectionComponent : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Maximum detection range")]
        [SerializeField] private float m_sightRange = 10f;
        
        [Tooltip("Field of view angle in degrees")]
        [SerializeField] private float m_fieldOfViewAngle = 90f;
        
        [Tooltip("Layers that can be detected as targets")]
        [SerializeField] private LayerMask m_detectionLayers = -1;
        
        [Tooltip("How often to check for targets (in seconds)")]
        [SerializeField] private float m_detectionInterval = 0.2f;
        
        [Tooltip("Layers that block line of sight")]
        [SerializeField] private LayerMask m_obstacleLayers = -1;

        private GameObject m_currentTarget;
        private float m_timeSinceLastDetectionCheck;
        private Transform m_transform;
        private List<GameObject> m_detectedTargets = new List<GameObject>();

        /// <summary>
        /// Event raised when a valid target is detected.
        /// </summary>
        public event Action<GameObject> OnTargetDetected;

        /// <summary>
        /// Event raised when the current target is lost.
        /// </summary>
        public event Action OnTargetLost;

        /// <summary>
        /// Gets the currently tracked target.
        /// </summary>
        public GameObject CurrentTarget => m_currentTarget;

        /// <summary>
        /// Gets whether a valid target is currently detected.
        /// </summary>
        public bool HasTarget => m_currentTarget != null;

        /// <summary>
        /// Configures detection settings from AIData.
        /// </summary>
        /// <param name="data">AI configuration data</param>
        public void Configure(AIData data)
        {
            if (data == null)
                return;

            m_sightRange = data.SightRange;
            m_fieldOfViewAngle = data.FieldOfViewAngle;
            m_detectionLayers = data.DetectionLayers;
        }

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_transform = transform;
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            m_timeSinceLastDetectionCheck += Time.deltaTime;

            if (m_timeSinceLastDetectionCheck >= m_detectionInterval)
            {
                m_timeSinceLastDetectionCheck = 0f;
                UpdateDetection();
            }
        }

        /// <summary>
        /// Manually trigger target detection check.
        /// </summary>
        public void CheckForTargets()
        {
            UpdateDetection();
        }

        /// <summary>
        /// Clears the current target.
        /// </summary>
        public void ClearTarget()
        {
            if (m_currentTarget != null)
            {
                GameObject previousTarget = m_currentTarget;
                m_currentTarget = null;
                OnTargetLost?.Invoke();
            }
        }

        /// <summary>
        /// Manually sets a target (useful for aggro systems).
        /// </summary>
        /// <param name="target">Target to track</param>
        public void SetTarget(GameObject target)
        {
            if (target == null)
            {
                ClearTarget();
                return;
            }

            bool isNewTarget = m_currentTarget != target;
            m_currentTarget = target;

            if (isNewTarget)
            {
                OnTargetDetected?.Invoke(target);
            }
        }

        /// <summary>
        /// Checks if a specific target is within detection range and FOV.
        /// </summary>
        /// <param name="target">Target to check</param>
        /// <returns>True if target is detected</returns>
        public bool CanDetect(GameObject target)
        {
            if (target == null)
                return false;

            // Check distance
            float distance = Vector3.Distance(m_transform.position, target.transform.position);
            if (distance > m_sightRange)
                return false;

            // Check FOV
            if (!IsInFieldOfView(target.transform.position))
                return false;

            // Check line of sight
            if (!HasLineOfSight(target.transform.position))
                return false;

            return true;
        }

        /*---------------------------------------------------------------------------- 
        | --- UpdateDetection: Main detection logic called periodically --- |
        ----------------------------------------------------------------------------*/
        private void UpdateDetection()
        {
            // Check if current target is still valid
            if (m_currentTarget != null)
            {
                // Validate target still exists and is alive
                if (!IsValidTarget(m_currentTarget) || !CanDetect(m_currentTarget))
                {
                    ClearTarget();
                }
                else
                {
                    // Target still valid, no need to search for new ones
                    return;
                }
            }

            // Search for new targets
            GameObject newTarget = FindNearestTarget();
            if (newTarget != null)
            {
                SetTarget(newTarget);
            }
        }

        /*---------------------------------------------------------------------------- 
        | --- FindNearestTarget: Searches for the nearest valid target --- |
        ----------------------------------------------------------------------------*/
        private GameObject FindNearestTarget()
        {
            m_detectedTargets.Clear();
            Collider[] colliders = Physics.OverlapSphere(m_transform.position, m_sightRange, m_detectionLayers);

            GameObject nearest = null;
            float nearestDistance = Mathf.Infinity;

            foreach (Collider collider in colliders)
            {
                GameObject target = collider.gameObject;

                // Don't detect self
                if (target == gameObject)
                    continue;

                // Validate target
                if (!IsValidTarget(target))
                    continue;

                // Check FOV
                if (!IsInFieldOfView(target.transform.position))
                    continue;

                // Check line of sight
                if (!HasLineOfSight(target.transform.position))
                    continue;

                // Track distance
                float distance = Vector3.Distance(m_transform.position, target.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = target;
                    nearestDistance = distance;
                }

                m_detectedTargets.Add(target);
            }

            return nearest;
        }

        /*---------------------------------------------------------------------------- 
        | --- IsInFieldOfView: Checks if position is within FOV cone --- |
        ----------------------------------------------------------------------------*/
        private bool IsInFieldOfView(Vector3 targetPosition)
        {
            Vector3 directionToTarget = (targetPosition - m_transform.position).normalized;
            float angle = Vector3.Angle(m_transform.forward, directionToTarget);
            return angle < m_fieldOfViewAngle / 2f;
        }

        /*---------------------------------------------------------------------------- 
        | --- HasLineOfSight: Performs raycast to check for obstacles --- |
        ----------------------------------------------------------------------------*/
        private bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - m_transform.position;
            float distance = direction.magnitude;

            // Raycast from slightly above transform to avoid ground collision
            Vector3 origin = m_transform.position + Vector3.up * 0.5f;
            Vector3 targetPoint = targetPosition + Vector3.up * 0.5f;

            if (Physics.Raycast(origin, (targetPoint - origin).normalized, out RaycastHit hit, distance, m_obstacleLayers))
            {
                // Something is blocking line of sight
                return false;
            }

            return true;
        }

        /*---------------------------------------------------------------------------- 
        | --- IsValidTarget: Checks if target has required components and is alive --- |
        ----------------------------------------------------------------------------*/
        private bool IsValidTarget(GameObject target)
        {
            if (target == null)
                return false;

            // Check if target has health component and is alive
            HealthComponent health = target.GetComponent<HealthComponent>();
            if (health != null && health.IsDead)
                return false;

            return true;
        }

        /*------------------------------------------------------------------------------- 
        | --- OnDrawGizmosSelected: Visualize detection range and FOV --- |
        -------------------------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            if (m_transform == null)
                m_transform = transform;

            // Draw detection range
            Gizmos.color = HasTarget ? Color.red : Color.green;
            Gizmos.DrawWireSphere(m_transform.position, m_sightRange);

            // Draw FOV cone
            Gizmos.color = Color.yellow;
            Vector3 viewAngleA = DirectionFromAngle(-m_fieldOfViewAngle / 2f);
            Vector3 viewAngleB = DirectionFromAngle(m_fieldOfViewAngle / 2f);
            
            Gizmos.DrawLine(m_transform.position, m_transform.position + viewAngleA * m_sightRange);
            Gizmos.DrawLine(m_transform.position, m_transform.position + viewAngleB * m_sightRange);

            // Draw line to current target
            if (HasTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(m_transform.position, m_currentTarget.transform.position);
            }
        }

        /*---------------------------------------------------------------------------- 
        | --- DirectionFromAngle: Helper to calculate direction from angle --- |
        ----------------------------------------------------------------------------*/
        private Vector3 DirectionFromAngle(float angleInDegrees)
        {
            angleInDegrees += m_transform.eulerAngles.y;
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
    }
}

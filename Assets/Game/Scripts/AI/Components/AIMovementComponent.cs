using UnityEngine;
using UnityEngine.AI;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /// <summary>
    /// AI-specific wrapper around MovementComponent that abstracts movement commands.
    /// Provides safe navigation with fallbacks when NavMeshAgent is absent.
    /// Supports both NavMeshAgent and simple transform-based movement.
    /// </summary>
    public class AIMovementComponent : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Speed for patrol movement")]
        [SerializeField] private float m_patrolSpeed = 2f;
        
        [Tooltip("Speed for chase/combat movement")]
        [SerializeField] private float m_chaseSpeed = 5f;
        
        [Tooltip("Use NavMeshAgent if available, otherwise use simple movement")]
        [SerializeField] private bool m_useNavMesh = true;

        private MovementComponent m_movementComponent;
        private NavMeshAgent m_navMeshAgent;
        private Transform m_transform;
        private Vector3 m_targetPosition;
        private bool m_isMoving;
        private float m_currentSpeed;

        /// <summary>
        /// Gets whether the AI is currently moving.
        /// </summary>
        public bool IsMoving => m_isMoving;

        /// <summary>
        /// Configures movement settings from AIData.
        /// </summary>
        /// <param name="data">AI configuration data</param>
        public void Configure(AIData data)
        {
            if (data == null)
                return;

            m_patrolSpeed = data.PatrolSpeed;
            m_chaseSpeed = data.ChaseSpeed;
        }

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_transform = transform;
            m_movementComponent = GetComponent<MovementComponent>();
            m_navMeshAgent = GetComponent<NavMeshAgent>();
            m_currentSpeed = m_patrolSpeed;
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            // Handle simple movement fallback if not using NavMesh
            if (!m_useNavMesh || m_navMeshAgent == null || !m_navMeshAgent.enabled)
            {
                UpdateSimpleMovement();
            }
        }

        /// <summary>
        /// Moves the AI to the specified position.
        /// </summary>
        /// <param name="position">Destination position</param>
        public void MoveTo(Vector3 position)
        {
            m_targetPosition = position;
            m_isMoving = true;

            // Try using MovementComponent with NavMeshAgent first
            if (m_movementComponent != null && m_useNavMesh)
            {
                // Check if agent is on NavMesh
                if (m_navMeshAgent != null && m_navMeshAgent.isOnNavMesh)
                {
                    m_movementComponent.MoveTo(position);
                    
                    // Update agent speed
                    if (m_navMeshAgent.enabled)
                    {
                        m_navMeshAgent.speed = m_currentSpeed;
                    }
                }
                else
                {
                    // NavMeshAgent not available, use simple movement
                    m_useNavMesh = false;
                }
            }
        }

        /// <summary>
        /// Stops all movement.
        /// </summary>
        public void Stop()
        {
            m_isMoving = false;

            if (m_movementComponent != null)
            {
                m_movementComponent.Stop();
            }
            else if (m_navMeshAgent != null && m_navMeshAgent.enabled && m_navMeshAgent.isOnNavMesh)
            {
                m_navMeshAgent.isStopped = true;
            }
        }

        /// <summary>
        /// Sets movement speed to patrol speed.
        /// </summary>
        public void SetPatrolSpeed()
        {
            SetSpeed(m_patrolSpeed);
        }

        /// <summary>
        /// Sets movement speed to chase speed.
        /// </summary>
        public void SetChaseSpeed()
        {
            SetSpeed(m_chaseSpeed);
        }

        /// <summary>
        /// Sets custom movement speed.
        /// </summary>
        /// <param name="speed">Speed value</param>
        public void SetSpeed(float speed)
        {
            m_currentSpeed = speed;

            if (m_navMeshAgent != null && m_navMeshAgent.enabled && m_navMeshAgent.isOnNavMesh)
            {
                m_navMeshAgent.speed = speed;
            }
        }

        /// <summary>
        /// Checks if AI has reached the destination.
        /// </summary>
        /// <param name="tolerance">Distance tolerance</param>
        /// <returns>True if at destination</returns>
        public bool HasReachedDestination(float tolerance = 1f)
        {
            if (!m_isMoving)
                return true;

            float distance = Vector3.Distance(m_transform.position, m_targetPosition);
            return distance <= tolerance;
        }

        /// <summary>
        /// Rotates AI to face the specified rotation.
        /// </summary>
        /// <param name="rotation">Target rotation</param>
        public void RotateTo(Quaternion rotation)
        {
            if (m_movementComponent != null)
            {
                m_movementComponent.RotateTo(rotation);
            }
            else
            {
                m_transform.rotation = rotation;
            }
        }

        /// <summary>
        /// Rotates AI to face the specified position.
        /// </summary>
        /// <param name="position">Position to look at</param>
        public void LookAt(Vector3 position)
        {
            Vector3 direction = (position - m_transform.position).normalized;
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                m_transform.rotation = Quaternion.Slerp(m_transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        /*---------------------------------------------------------------------------- 
        | --- UpdateSimpleMovement: Fallback movement using Transform --- |
        ----------------------------------------------------------------------------*/
        private void UpdateSimpleMovement()
        {
            if (!m_isMoving)
                return;

            float distance = Vector3.Distance(m_transform.position, m_targetPosition);
            
            if (distance < 0.1f)
            {
                m_isMoving = false;
                return;
            }

            // Simple movement toward target
            Vector3 direction = (m_targetPosition - m_transform.position).normalized;
            m_transform.position += direction * m_currentSpeed * Time.deltaTime;

            // Rotate to face movement direction
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                m_transform.rotation = Quaternion.Slerp(m_transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }
}

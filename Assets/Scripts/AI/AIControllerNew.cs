using UnityEngine;
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /// <summary>
    /// Unified AI controller that replaces EnemyController and NPCController.
    /// Uses a state machine and component-based composition for flexible AI behavior.
    /// Configure behavior via AIType enum and AIData ScriptableObject.
    /// </summary>
    public class AIControllerNew : MonoBehaviour
    {
        /// <summary>
        /// Type of AI behavior this controller should exhibit.
        /// </summary>
        public enum AIType
        {
            NPC,    // Non-hostile, patrols only
            Enemy   // Hostile, detects and attacks targets
        }

        [Header("AI Configuration")]
        [Tooltip("Type of AI behavior (NPC or Enemy)")]
        [SerializeField] private AIType m_aiType = AIType.NPC;

        [Tooltip("AI configuration data asset")]
        [SerializeField] private AIData m_aiData;

        [Header("Optional Component References")]
        [Tooltip("Optional: Reference to player GameObject for detection")]
        [SerializeField] private GameObject m_playerTarget;

        // Core AI components
        private AIStateMachine m_stateMachine;

        // Component references (queried at runtime)
        private MovementComponent m_movementComponent;
        private CombatComponent m_combatComponent;
        private PatrolComponent m_patrolComponent;
        private DetectionComponent m_detectionComponent;

        // State instances
        private IdleState m_idleState;
        private PatrolState m_patrolState;
        private SuspicionState m_suspicionState;
        private AttackState m_attackState;

        // Runtime state
        private GameObject m_currentTarget;
        private bool m_isAggro;

        /// <summary>
        /// Gets the current AI type.
        /// </summary>
        public AIType Type => m_aiType;

        /// <summary>
        /// Gets the AI configuration data.
        /// </summary>
        public AIData Data => m_aiData;

        /// <summary>
        /// Gets the state machine instance.
        /// </summary>
        public AIStateMachine StateMachine => m_stateMachine;

        /// <summary>
        /// Gets the current target (if any).
        /// </summary>
        public GameObject CurrentTarget => m_currentTarget;

        /// <summary>
        /// Gets whether the AI is currently aggro'd.
        /// </summary>
        public bool IsAggro => m_isAggro;

        /// <summary>
        /// Gets the movement component.
        /// </summary>
        public MovementComponent MovementComponent => m_movementComponent;

        /// <summary>
        /// Gets the combat component (may be null).
        /// </summary>
        public CombatComponent CombatComponent => m_combatComponent;

        /// <summary>
        /// Gets the patrol component (may be null).
        /// </summary>
        public PatrolComponent PatrolComponent => m_patrolComponent;

        /// <summary>
        /// Gets the detection component (may be null).
        /// </summary>
        public DetectionComponent DetectionComponent => m_detectionComponent;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            // Query for required components
            m_movementComponent = GetComponent<MovementComponent>();
            if (m_movementComponent == null)
            {
                Debug.LogError($"[AIControllerNew] MovementComponent is required on {gameObject.name}. AI will not function properly.");
            }

            // Query for optional components
            m_combatComponent = GetComponent<CombatComponent>();
            m_patrolComponent = GetComponent<PatrolComponent>();
            m_detectionComponent = GetComponent<DetectionComponent>();

            // Log warnings for missing optional components based on AI type
            if (m_aiType == AIType.Enemy)
            {
                if (m_combatComponent == null)
                {
                    Debug.LogWarning($"[AIControllerNew] CombatComponent not found on Enemy AI '{gameObject.name}'. Attack behavior will be disabled.");
                }
                if (m_detectionComponent == null)
                {
                    Debug.LogWarning($"[AIControllerNew] DetectionComponent not found on Enemy AI '{gameObject.name}'. Detection will be disabled.");
                }
            }

            // Validate AI data
            if (m_aiData == null)
            {
                Debug.LogWarning($"[AIControllerNew] AIData not assigned on {gameObject.name}. Using default values may cause unexpected behavior.");
            }

            // Initialize state machine
            m_stateMachine = new AIStateMachine();

            // Create state instances
            // TODO: States need access to components and data - pass references in constructor
            m_idleState = new IdleState(this);
            m_patrolState = new PatrolState(this);
            m_suspicionState = new SuspicionState(this);
            m_attackState = new AttackState(this);

            // Register states with the state machine
            m_stateMachine.RegisterState("Idle", m_idleState);
            m_stateMachine.RegisterState("Patrol", m_patrolState);
            m_stateMachine.RegisterState("Suspicion", m_suspicionState);
            m_stateMachine.RegisterState("Attack", m_attackState);
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            // Subscribe to detection events if available
            if (m_detectionComponent != null)
            {
                m_detectionComponent.OnTargetDetected += HandleTargetDetected;
                m_detectionComponent.OnTargetLost += HandleTargetLost;
            }

            // Subscribe to patrol events if available
            if (m_patrolComponent != null)
            {
                m_patrolComponent.OnPatrolPointReached += HandlePatrolPointReached;
            }

            // Set default player target if not assigned
            if (m_playerTarget == null && m_aiType == AIType.Enemy)
            {
                m_playerTarget = GameObject.FindGameObjectWithTag("Player");
                if (m_playerTarget == null)
                {
                    Debug.LogWarning($"[AIControllerNew] No Player target found for Enemy AI '{gameObject.name}'.");
                }
            }

            // Start in appropriate initial state
            if (m_patrolComponent != null)
            {
                m_stateMachine.ChangeState(m_patrolState);
            }
            else
            {
                m_stateMachine.ChangeState(m_idleState);
            }
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            // Tick the state machine
            m_stateMachine.Tick(Time.deltaTime);
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            // Unsubscribe from events
            if (m_detectionComponent != null)
            {
                m_detectionComponent.OnTargetDetected -= HandleTargetDetected;
                m_detectionComponent.OnTargetLost -= HandleTargetLost;
            }

            if (m_patrolComponent != null)
            {
                m_patrolComponent.OnPatrolPointReached -= HandlePatrolPointReached;
            }
        }

        /*------------------------------------------------------------
        | --- SetTarget: Sets the current target for the AI --- |
        ------------------------------------------------------------*/
        public void SetTarget(GameObject target)
        {
            m_currentTarget = target;
        }

        /*--------------------------------------------------------------
        | --- Aggravate: Make the AI aggressive towards its target --- |
        --------------------------------------------------------------*/
        public void Aggravate()
        {
            m_isAggro = true;

            // Transition to attack state if we're an enemy
            if (m_aiType == AIType.Enemy && m_currentTarget != null)
            {
                m_stateMachine.ChangeState(m_attackState);
            }
        }

        /*-------------------------------------------------------------------
        | --- ClearAggro: Clear aggro state and return to normal behavior --- |
        -------------------------------------------------------------------*/
        public void ClearAggro()
        {
            m_isAggro = false;
        }

        /*------------------------------------------------------------------------
        | --- HandleTargetDetected: Called when detection component finds target --- |
        ------------------------------------------------------------------------*/
        private void HandleTargetDetected(GameObject target)
        {
            if (m_aiType != AIType.Enemy)
                return;

            SetTarget(target);

            // Transition to attack state
            if (m_combatComponent != null && m_combatComponent.CanAttack(target))
            {
                m_stateMachine.ChangeState(m_attackState);
            }
        }

        /*------------------------------------------------------------------------
        | --- HandleTargetLost: Called when detection component loses target --- |
        ------------------------------------------------------------------------*/
        private void HandleTargetLost()
        {
            // Transition to suspicion state
            if (m_stateMachine.CurrentState == m_attackState)
            {
                m_stateMachine.ChangeState(m_suspicionState);
            }
        }

        /*----------------------------------------------------------------------------
        | --- HandlePatrolPointReached: Called when patrol component reaches waypoint --- |
        ----------------------------------------------------------------------------*/
        private void HandlePatrolPointReached(int waypointIndex)
        {
            // Could be used for custom behavior at waypoints
            // For now, the PatrolState handles waypoint logic
        }

        /*----------------------------------------------------------------------------
        | --- OnDrawGizmosSelected: Visualize AI detection and alert ranges --- |
        ----------------------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            if (m_aiData == null)
                return;

            // Draw sight range for enemies
            if (m_aiType == AIType.Enemy)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, m_aiData.SightRange);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, m_aiData.AlertRange);
            }
        }
    }
}

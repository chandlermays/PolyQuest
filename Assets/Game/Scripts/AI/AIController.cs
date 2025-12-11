using UnityEngine;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /// <summary>
    /// AI types that determine behavior patterns.
    /// </summary>
    public enum AIType
    {
        NPC,    // Non-combatant AI that patrols
        Enemy   // Combatant AI that can detect and attack targets
    }

    /// <summary>
    /// Unified AI controller using state machine and component-based composition.
    /// Replaces the old EnemyController and NPCController with a single, robust system.
    /// Behaviors are determined by AIType and configured via AIData ScriptableObject.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class AIController : MonoBehaviour
    {
        [Header("AI Configuration")]
        [Tooltip("Type of AI behavior (NPC or Enemy)")]
        [SerializeField] private AIType m_aiType = AIType.NPC;
        
        [Tooltip("AI configuration data (ScriptableObject)")]
        [SerializeField] private AIData m_aiData;
        
        [Tooltip("Initial state on start (leave null to auto-determine)")]
        [SerializeField] private string m_initialState = "Idle";

        // Components (optional, queried via GetComponent)
        private PatrolComponent m_patrolComponent;
        private DetectionComponent m_detectionComponent;
        private AICombatComponent m_combatComponent;
        private AIMovementComponent m_movementComponent;
        private HealthComponent m_healthComponent;

        // State machine
        private AIStateMachine m_stateMachine;

        // Aggro tracking
        private float m_timeSinceAggro = Mathf.Infinity;

        /// <summary>
        /// Gets the AI type.
        /// </summary>
        public AIType AIType => m_aiType;

        /// <summary>
        /// Gets the AI configuration data.
        /// </summary>
        public AIData AIData => m_aiData;

        /// <summary>
        /// Gets the patrol component (null if not present).
        /// </summary>
        public PatrolComponent GetPatrolComponent() => m_patrolComponent;

        /// <summary>
        /// Gets the detection component (null if not present).
        /// </summary>
        public DetectionComponent GetDetectionComponent() => m_detectionComponent;

        /// <summary>
        /// Gets the combat component (null if not present).
        /// </summary>
        public AICombatComponent GetCombatComponent() => m_combatComponent;

        /// <summary>
        /// Gets the movement component (null if not present).
        /// </summary>
        public AIMovementComponent GetMovementComponent() => m_movementComponent;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            // Query for optional components
            m_patrolComponent = GetComponent<PatrolComponent>();
            m_detectionComponent = GetComponent<DetectionComponent>();
            m_combatComponent = GetComponent<AICombatComponent>();
            m_movementComponent = GetComponent<AIMovementComponent>();
            m_healthComponent = GetComponent<HealthComponent>();

            // Validate required components
            if (m_healthComponent == null)
            {
                Debug.LogError($"NewAIController on {gameObject.name} requires HealthComponent!");
            }

            // Configure components with AIData if available
            if (m_aiData != null)
            {
                ConfigureComponents();
            }

            // Initialize state machine
            m_stateMachine = new AIStateMachine(this);
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            // Subscribe to detection events
            if (m_detectionComponent != null)
            {
                m_detectionComponent.OnTargetDetected += HandleTargetDetected;
                m_detectionComponent.OnTargetLost += HandleTargetLost;
            }

            // Subscribe to health events
            if (m_healthComponent != null)
            {
                m_healthComponent.OnHit += HandleHit;
            }
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            // Unsubscribe from detection events
            if (m_detectionComponent != null)
            {
                m_detectionComponent.OnTargetDetected -= HandleTargetDetected;
                m_detectionComponent.OnTargetLost -= HandleTargetLost;
            }

            // Unsubscribe from health events
            if (m_healthComponent != null)
            {
                m_healthComponent.OnHit -= HandleHit;
            }
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            // Set initial state
            IAIState initialState = DetermineInitialState();
            m_stateMachine.ChangeState(initialState);
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            // Don't update if dead
            if (m_healthComponent != null && m_healthComponent.IsDead)
            {
                return;
            }

            // Update aggro timer
            m_timeSinceAggro += Time.deltaTime;

            // Tick state machine
            m_stateMachine.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Changes the current AI state.
        /// </summary>
        /// <param name="newState">State to transition to</param>
        public void ChangeState(IAIState newState)
        {
            m_stateMachine.ChangeState(newState);
        }

        /// <summary>
        /// Called when AI is aggravated (typically from being hit).
        /// </summary>
        public void OnAggro()
        {
            m_timeSinceAggro = 0f;

            // If we're an enemy and not already attacking, transition to attack
            if (m_aiType == AIType.Enemy)
            {
                // Set target if detection component is available and has one
                if (m_detectionComponent != null && m_detectionComponent.HasTarget)
                {
                    m_stateMachine.ChangeState(new AttackState());
                }
                else
                {
                    // No target yet, but we're aggro'd - send event to current state
                    m_stateMachine.SendEvent("OnAggro");
                }
            }
        }

        /// <summary>
        /// Checks if AI is currently aggro'd.
        /// </summary>
        public bool IsAggro()
        {
            if (m_aiData == null)
                return false;

            return m_timeSinceAggro < m_aiData.AggroCooldown;
        }

        /*---------------------------------------------------------------------------- 
        | --- ConfigureComponents: Apply AIData settings to components --- |
        ----------------------------------------------------------------------------*/
        private void ConfigureComponents()
        {
            if (m_detectionComponent != null)
            {
                m_detectionComponent.Configure(m_aiData);
            }

            if (m_combatComponent != null)
            {
                m_combatComponent.Configure(m_aiData);
            }

            if (m_movementComponent != null)
            {
                m_movementComponent.Configure(m_aiData);
            }
        }

        /*---------------------------------------------------------------------------- 
        | --- DetermineInitialState: Determines the starting state based on config --- |
        ----------------------------------------------------------------------------*/
        private IAIState DetermineInitialState()
        {
            // If initial state is explicitly set, use it
            switch (m_initialState.ToLower())
            {
                case "idle":
                    return new IdleState();
                case "patrol":
                    if (m_patrolComponent != null && m_patrolComponent.HasWaypoints)
                        return new PatrolState();
                    break;
                case "attack":
                    if (m_aiType == AIType.Enemy && m_detectionComponent != null)
                        return new AttackState();
                    break;
            }

            // Auto-determine based on AI type and available components
            if (m_patrolComponent != null && m_patrolComponent.HasWaypoints)
            {
                return new PatrolState();
            }

            return new IdleState();
        }

        /*---------------------------------------------------------------------------- 
        | --- Event Handlers --- |
        ----------------------------------------------------------------------------*/

        private void HandleTargetDetected(GameObject target)
        {
            // Only enemies react to target detection
            if (m_aiType == AIType.Enemy)
            {
                m_stateMachine.SendEvent("OnTargetDetected", target);
            }
        }

        private void HandleTargetLost()
        {
            if (m_aiType == AIType.Enemy)
            {
                m_stateMachine.SendEvent("OnTargetLost");
            }
        }

        private void HandleHit()
        {
            OnAggro();
        }

        /*---------------------------------------------------------------------------- 
        | --- OnDrawGizmosSelected: Debug visualization --- |
        ----------------------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            // Draw current state name
#if UNITY_EDITOR
            if (Application.isPlaying && m_stateMachine != null && m_stateMachine.CurrentState != null)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2f,
                    $"State: {m_stateMachine.CurrentState.GetType().Name}"
                );
            }
#endif
        }
    }
}

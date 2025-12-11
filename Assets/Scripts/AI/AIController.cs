using UnityEngine;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.AI.Components;
using PolyQuest.AI.States;

namespace PolyQuest.AI
{
    /// <summary>
    /// Type of AI behavior configuration.
    /// </summary>
    public enum AIType
    {
        /// <summary>Non-aggressive AI that patrols but doesn't attack</summary>
        NPC,
        /// <summary>Aggressive AI that detects and attacks targets</summary>
        Enemy
    }

    /// <summary>
    /// Central AI controller that manages state-based AI behavior for both NPCs and Enemies.
    /// Uses composition with pluggable components (PatrolComponent, DetectionComponent, etc.)
    /// and a state machine for behavior control.
    /// 
    /// Setup:
    /// 1. Add this component to a GameObject
    /// 2. Set AIType to NPC or Enemy
    /// 3. Assign an AIData ScriptableObject for tuning
    /// 4. Add optional components: PatrolComponent, DetectionComponent, AICombatComponent
    /// 5. Ensure the GameObject has a MovementComponent (required)
    /// </summary>
    [RequireComponent(typeof(MovementComponent))]
    public class AIController : MonoBehaviour
    {
        [Header("AI Configuration")]
        [Tooltip("Type of AI behavior (NPC or Enemy)")]
        [SerializeField] private AIType m_aiType = AIType.Enemy;

        [Tooltip("AI configuration data (tuning values)")]
        [SerializeField] private AIData m_aiData;

        [Header("Optional Components")]
        [Tooltip("Reference to patrol component if using patrol behavior")]
        [SerializeField] private PatrolComponent m_patrolComponent;

        [Tooltip("Reference to detection component if using target detection")]
        [SerializeField] private DetectionComponent m_detectionComponent;

        [Tooltip("Reference to combat component if using combat behavior")]
        [SerializeField] private AICombatComponent m_combatComponent;

        [Header("Initial State")]
        [Tooltip("The state to start in (Idle or Patrol)")]
        [SerializeField] private bool m_startInPatrolState = true;

        private AIStateMachine m_stateMachine;
        private MovementComponent m_movementComponent;
        private HealthComponent m_healthComponent;

        /// <summary>
        /// Gets the AI type (NPC or Enemy).
        /// </summary>
        public AIType AIType => m_aiType;

        /// <summary>
        /// Sets the AI type. Useful for runtime configuration.
        /// </summary>
        /// <param name="aiType">The AI type to set</param>
        public void SetAIType(AIType aiType)
        {
            m_aiType = aiType;
        }

        /// <summary>
        /// Gets the AI configuration data.
        /// </summary>
        public AIData AIData => m_aiData;

        /// <summary>
        /// Gets the state machine managing this AI.
        /// </summary>
        public AIStateMachine StateMachine => m_stateMachine;

        /// <summary>
        /// Gets the movement component (required).
        /// </summary>
        public MovementComponent MovementComponent => m_movementComponent;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            // Initialize state machine
            m_stateMachine = new AIStateMachine(this);

            // Get required components
            m_movementComponent = GetComponent<MovementComponent>();
            if (m_movementComponent == null)
            {
                Debug.LogError($"[AIController] No MovementComponent found on {gameObject.name}. " +
                               "AIController requires MovementComponent to function. Please add one.");
            }

            // Get optional components if not assigned in inspector
            if (m_patrolComponent == null)
            {
                m_patrolComponent = GetComponent<PatrolComponent>();
            }

            if (m_detectionComponent == null)
            {
                m_detectionComponent = GetComponent<DetectionComponent>();
            }

            if (m_combatComponent == null)
            {
                m_combatComponent = GetComponent<AICombatComponent>();
            }

            m_healthComponent = GetComponent<HealthComponent>();
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            // Subscribe to detection events if component exists
            if (m_detectionComponent != null)
            {
                m_detectionComponent.OnTargetDetected += HandleTargetDetected;
                m_detectionComponent.OnTargetLost += HandleTargetLost;
            }

            // Subscribe to health events if component exists
            if (m_healthComponent != null)
            {
                m_healthComponent.OnHit += HandleDamaged;
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
                m_healthComponent.OnHit -= HandleDamaged;
            }
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            // Apply AIData settings to components
            if (m_aiData != null)
            {
                if (m_detectionComponent != null)
                {
                    m_detectionComponent.ApplyAIData(m_aiData);
                }

                if (m_combatComponent != null)
                {
                    m_combatComponent.ApplyAIData(m_aiData);
                }
            }
            else
            {
                Debug.LogWarning($"[AIController] No AIData assigned to {gameObject.name}. " +
                                 "Using default component settings.");
            }

            // Set initial state
            InitializeState();
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            // Don't update if dead
            if (m_healthComponent != null && m_healthComponent.IsDead)
            {
                if (m_movementComponent != null)
                {
                    m_movementComponent.Stop();
                }
                return;
            }

            // Update state machine
            if (m_stateMachine != null)
            {
                m_stateMachine.Tick();
            }
        }

        /// <summary>
        /// Gets the AICombatComponent if it exists.
        /// </summary>
        /// <returns>The AICombatComponent or null</returns>
        public AICombatComponent GetAICombatComponent()
        {
            return m_combatComponent;
        }

        /// <summary>
        /// Forces the AI to transition to a specific state.
        /// Useful for external control or scripted sequences.
        /// </summary>
        /// <param name="newState">The state to transition to</param>
        public void ForceStateChange(IAIState newState)
        {
            if (m_stateMachine != null && newState != null)
            {
                m_stateMachine.ChangeState(newState);
            }
        }

        /// <summary>
        /// Sends an event to the current state for handling.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Optional event data</param>
        public void TriggerEvent(string eventName, object eventData = null)
        {
            if (m_stateMachine != null)
            {
                m_stateMachine.TriggerEvent(eventName, eventData);
            }
        }

        private void InitializeState()
        {
            IAIState initialState;

            // Determine initial state based on configuration and available components
            if (m_startInPatrolState && m_patrolComponent != null && m_patrolComponent.WaypointCount > 0)
            {
                initialState = new PatrolState();
            }
            else
            {
                initialState = new IdleState();
            }

            m_stateMachine.ChangeState(initialState);
        }

        private void HandleTargetDetected(GameObject target)
        {
            Debug.Log($"[AIController] {gameObject.name} detected target: {target.name}");

            // Only enemies react aggressively to detection
            if (m_aiType == AIType.Enemy)
            {
                m_stateMachine.TriggerEvent("TargetDetected", target);
            }
            // NPCs might have different reactions or ignore targets
        }

        private void HandleTargetLost()
        {
            Debug.Log($"[AIController] {gameObject.name} lost target");
            m_stateMachine.TriggerEvent("TargetLost");
        }

        private void HandleDamaged()
        {
            Debug.Log($"[AIController] {gameObject.name} was damaged");
            
            // Both NPCs and Enemies might react to damage
            if (m_aiType == AIType.Enemy)
            {
                m_stateMachine.TriggerEvent("Damaged");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw AI type indicator
            Gizmos.color = m_aiType == AIType.Enemy ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}

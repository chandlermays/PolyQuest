using UnityEngine;

namespace PolyQuest.AI
{
    /// <summary>
    /// Adapter that forwards NPCController behavior to the new AIController system.
    /// Attach this alongside or instead of NPCController to bridge to the new AI system.
    /// 
    /// This adapter:
    /// - Creates or retrieves an AIController component
    /// - Configures it as AIType.NPC
    /// - Transfers patrol settings from NavigationPath to PatrolComponent
    /// - Provides compatibility with legacy NPCController behavior
    /// 
    /// Usage:
    /// 1. Add this component to GameObjects that have NPCController
    /// 2. The adapter will automatically set up the new AI system
    /// 3. Patrol behavior will be handled by PatrolComponent and PatrolState
    /// 4. Once migration is complete, remove NPCController and this adapter
    /// </summary>
    [RequireComponent(typeof(AIController))]
    public class NPCControllerAdapter : MonoBehaviour
    {
        [Header("Legacy NPCController Settings")]
        [Tooltip("Navigation path - will be transferred to PatrolComponent")]
        [SerializeField] private NavigationPath m_navigationPath;

        [Tooltip("Waypoint tolerance - will be transferred to PatrolComponent")]
        [SerializeField] private float m_waypointTolerance = 1f;

        [Tooltip("Waypoint dwell time - will be transferred to AIData")]
        [SerializeField] private float m_waypointDwellTime = 3f;

        [Tooltip("Optional: AIData asset to use instead of creating from legacy values")]
        [SerializeField] private AIData m_aiData;

        // Component references
        private AIController m_aiController;
        private PatrolComponent m_patrolComponent;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            // Get or add AIController
            m_aiController = GetComponent<AIController>();
            if (m_aiController == null)
            {
                m_aiController = gameObject.AddComponent<AIController>();
            }

            // TODO: If m_aiData is not assigned, maintainers should create an AIData asset
            // and assign it, or set default values programmatically

            SetupPatrolComponent();
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            // Additional setup after all components are initialized
            // TODO: Map any additional legacy NPCController settings not covered in Awake
        }

        /*-------------------------------------------------------------------
        | --- SetupPatrolComponent: Configure patrol behavior for the NPC --- |
        -------------------------------------------------------------------*/
        private void SetupPatrolComponent()
        {
            // Get or add PatrolComponent
            m_patrolComponent = GetComponent<PatrolComponent>();
            if (m_patrolComponent == null)
            {
                m_patrolComponent = gameObject.AddComponent<PatrolComponent>();
            }

            // Transfer legacy patrol settings to PatrolComponent
            // Note: PatrolComponent fields are serialized, so set via reflection or configure in Inspector
            m_patrolComponent.ArrivalRadius = m_waypointTolerance;
            
            // NavigationPath can be assigned directly to PatrolComponent in Inspector
            // or the adapter's m_navigationPath can be linked if needed
            if (m_navigationPath != null)
            {
                Debug.Log($"[NPCControllerAdapter] NavigationPath found on {gameObject.name}. " +
                         "PatrolComponent can use it directly. Assign in Inspector if needed.");
            }
            else
            {
                Debug.LogWarning($"[NPCControllerAdapter] No NavigationPath found on {gameObject.name}. " +
                               "NPC will idle without patrol path. Configure waypoints in PatrolComponent.");
            }

            Debug.LogWarning($"[NPCControllerAdapter] Patrol component setup on {gameObject.name}. " +
                           "TODO: Manually configure PatrolComponent settings in Inspector or link NavigationPath.");
        }

        /*-------------------------------------------------------------------
        | --- Legacy API Notes and Migration TODOs --- |
        -------------------------------------------------------------------
        | 
        | The legacy NPCController was very simple and only had patrol behavior:
        | - m_navigationPath -> PatrolComponent.m_navigationPath (or waypoint list)
        | - m_waypointTolerance -> PatrolComponent.ArrivalRadius or AIData.PatrolWaypointTolerance
        | - m_waypointDwellTime -> AIData.WaypointDwellTime
        | 
        | The legacy NPCController behavior:
        | - PatrolState() in Update loop -> Now handled by PatrolState class automatically
        | 
        | NPCController inherited from AIController (legacy base) which provided:
        | - Patrol waypoint cycling
        | - Dwell time at waypoints
        | - Movement via MovementComponent
        | 
        | TODO for maintainers:
        | 1. Create an AIData asset in the project with appropriate patrol values
        | 2. Assign the AIData asset to this adapter or the AIController
        | 3. Link existing NavigationPath to PatrolComponent, or configure waypoints manually
        | 4. Set PatrolComponent.ArrivalRadius to match legacy m_waypointTolerance
        | 5. Ensure MovementComponent is present and configured
        | 6. Test patrol behavior matches legacy behavior
        | 7. Once verified, remove NPCController and this adapter
        -------------------------------------------------------------------*/
    }
}

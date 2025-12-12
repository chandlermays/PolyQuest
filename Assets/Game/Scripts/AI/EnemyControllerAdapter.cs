using UnityEngine;
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /// <summary>
    /// Adapter that forwards EnemyController behavior to the new AIController system.
    /// Attach this alongside or instead of EnemyController to bridge to the new AI system.
    /// 
    /// This adapter:
    /// - Creates or retrieves an AIController component
    /// - Configures it as AIType.Enemy
    /// - Transfers settings from legacy fields to AIData and components
    /// - Provides API compatibility with legacy EnemyController public methods
    /// 
    /// Usage:
    /// 1. Add this component to GameObjects that have EnemyController
    /// 2. The adapter will automatically set up the new AI system
    /// 3. Legacy scripts calling EnemyController.Aggravate() will work through this adapter
    /// 4. Once migration is complete, remove EnemyController and this adapter
    /// </summary>
    [RequireComponent(typeof(AIController))]
    public class EnemyControllerAdapter : MonoBehaviour
    {
        [Header("Legacy EnemyController Settings")]
        [Tooltip("Detection range - will be transferred to AIData")]
        [SerializeField] private float m_detectionRange = 3f;

        [Tooltip("Alert range - will be transferred to AIData")]
        [SerializeField] private float m_alertRange = 5f;

        [Tooltip("Suspicion time - will be transferred to AIData")]
        [SerializeField] private float m_suspicionTime = 3f;

        [Tooltip("Aggro cooldown - will be transferred to AIData")]
        [SerializeField] private float m_aggroCooldown = 5f;

        [Tooltip("Optional: AIData asset to use instead of creating from legacy values")]
        [SerializeField] private AIData m_aiData;

        [Tooltip("Reference to player GameObject")]
        [SerializeField] private GameObject m_player;

        // Component references
        private AIController m_aiController;
        private DetectionComponent m_detectionComponent;

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

            // NOTE: AIController's AIType and AIData are serialized fields that should be
            // configured in the Inspector. This adapter provides component setup and basic
            // field mapping, but full migration requires:
            // 1. Creating an AIData asset with values from legacy fields
            // 2. Assigning it to AIController in Inspector
            // 3. Setting AIType to Enemy in Inspector
            // See migration docs for complete instructions.

            SetupDetectionComponent();
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            // Additional setup after all components are initialized
            // TODO: Map any additional legacy EnemyController settings not covered in Awake
        }

        /*-------------------------------------------------------------------
        | --- SetupDetectionComponent: Configure detection for the enemy --- |
        -------------------------------------------------------------------*/
        private void SetupDetectionComponent()
        {
            // Get or add DetectionComponent
            m_detectionComponent = GetComponent<DetectionComponent>();
            if (m_detectionComponent == null)
            {
                m_detectionComponent = gameObject.AddComponent<DetectionComponent>();
            }

            // Transfer legacy detection settings to DetectionComponent
            m_detectionComponent.SightRange = m_detectionRange;
            
            Debug.Log($"[EnemyControllerAdapter] DetectionComponent configured with SightRange={m_detectionRange}. " +
                     "Additional settings (FOV, layers) should be configured in Inspector or AIData.");
        }

        /*------------------------------------------------------- 
        | --- Aggravate: The Behavior of Aggravating the AI --- |
        | Legacy API compatibility method
        -------------------------------------------------------*/
        public void Aggravate()
        {
            if (m_aiController != null)
            {
                m_aiController.Aggravate();
            }
        }

        /*-------------------------------------------------------------------
        | --- Legacy API Notes and Migration TODOs --- |
        -------------------------------------------------------------------
        | 
        | The legacy EnemyController had these key fields that need migration:
        | - m_detectionRange -> AIData.SightRange or DetectionComponent.SightRange
        | - m_alertRange -> AIData.AlertRange
        | - m_suspicionTime -> AIData.SuspicionTime
        | - m_aggroCooldown -> AIData.AggroCooldown
        | - m_player -> AIController.m_playerTarget (or DetectionComponent target)
        | - m_enemyTracker -> Need to register with EnemyTracker on new system
        | 
        | The legacy EnemyController behavior:
        | - IsPlayerDetected() -> Now handled by DetectionComponent
        | - AttackState() -> Now handled by AttackState class
        | - SuspicionState() -> Now handled by SuspicionState class
        | - PatrolState() -> Now handled by PatrolState class
        | - AlertNearbyEnemies() -> Now in AttackState.AlertNearbyAllies()
        | 
        | TODO for maintainers:
        | 1. Create an AIData asset in the project with appropriate values
        | 2. Assign the AIData asset to this adapter or the AIController
        | 3. Configure DetectionComponent settings (sight range, FOV, layers)
        | 4. Ensure CombatComponent is configured properly
        | 5. If NavigationPath exists, add PatrolComponent and link it
        | 6. Handle EnemyTracker registration in the new system
        | 7. Test that Aggravate() and other public methods work correctly
        | 8. Once verified, remove EnemyController and this adapter
        -------------------------------------------------------------------*/
    }
}

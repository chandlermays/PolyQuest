using UnityEngine;
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /// <summary>
    /// Example script demonstrating how to configure AIController for NPC vs Enemy at runtime.
    /// This shows the component-based composition approach of the new AI system.
    /// 
    /// Usage:
    /// 1. Attach this script to an empty GameObject or call methods from your own code
    /// 2. Call CreateNPC() or CreateEnemy() to spawn configured AI
    /// 3. Review the code to understand how to set up AI manually or via prefabs
    /// </summary>
    public class ExampleAISetup : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("AIData asset to use for spawned AI")]
        [SerializeField] private AIData m_aiData;

        [Tooltip("Player reference for enemy detection")]
        [SerializeField] private GameObject m_player;

        [Tooltip("Prefab with visuals/animator for the AI")]
        [SerializeField] private GameObject m_aiPrefab;

        [Header("NPC Settings")]
        [Tooltip("Spawn position for test NPC")]
        [SerializeField] private Vector3 m_npcSpawnPosition = Vector3.zero;

        [Tooltip("Waypoints for NPC patrol")]
        [SerializeField] private Transform[] m_npcWaypoints;

        [Header("Enemy Settings")]
        [Tooltip("Spawn position for test Enemy")]
        [SerializeField] private Vector3 m_enemySpawnPosition = new Vector3(10, 0, 0);

        [Tooltip("Waypoints for Enemy patrol")]
        [SerializeField] private Transform[] m_enemyWaypoints;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            // Example: Create AI at startup (comment out if not needed)
            // CreateExampleNPC();
            // CreateExampleEnemy();
        }

        /*-------------------------------------------------------------------
        | --- CreateExampleNPC: Create an NPC with patrol behavior --- |
        -------------------------------------------------------------------*/
        public GameObject CreateExampleNPC()
        {
            Debug.Log("[ExampleAISetup] Creating example NPC...");

            // Create GameObject
            GameObject npcObject = CreateBaseAI("Example NPC", m_npcSpawnPosition);

            // Add/Configure AIController
            AIController aiController = npcObject.AddComponent<AIController>();
            
            // NOTE: AIType is a serialized field and should be configured in Inspector/prefab.
            // Runtime type changes after initialization are not recommended as the state machine
            // and components are initialized in Awake() based on the AIType.
            // Best practice: Create prefabs with AIController pre-configured to desired type.

            // Assign AIData
            if (m_aiData != null)
            {
                // NOTE: m_aiData is private SerializeField in AIController
                // For runtime assignment, AIController needs a public setter or constructor parameter
                // TODO: Add AIController.SetAIData(AIData data) method or make it assignable
                Debug.LogWarning("[ExampleAISetup] AIData assignment requires Inspector or AIController API extension.");
            }

            // Add PatrolComponent
            PatrolComponent patrolComponent = npcObject.AddComponent<PatrolComponent>();
            if (m_npcWaypoints != null && m_npcWaypoints.Length > 0)
            {
                // NOTE: PatrolComponent uses serialized waypoint list or NavigationPath.
                // For runtime waypoint configuration, assign to m_waypointTransforms via reflection,
                // or create a NavigationPath GameObject with waypoint children and assign it.
                // Best practice: Configure waypoints in Inspector or use prefabs with NavigationPath.
                Debug.LogWarning("[ExampleAISetup] Waypoint assignment requires Inspector configuration or NavigationPath setup.");
            }

            Debug.Log($"[ExampleAISetup] Created NPC: {npcObject.name}");
            return npcObject;
        }

        /*-------------------------------------------------------------------
        | --- CreateExampleEnemy: Create an Enemy with detection and combat --- |
        -------------------------------------------------------------------*/
        public GameObject CreateExampleEnemy()
        {
            Debug.Log("[ExampleAISetup] Creating example Enemy...");

            // Create GameObject
            GameObject enemyObject = CreateBaseAI("Example Enemy", m_enemySpawnPosition);

            // Add/Configure AIController
            AIController aiController = enemyObject.AddComponent<AIController>();
            // Set to Enemy type
            // TODO: Requires AIController.SetAIType(AIType.Enemy) method

            // Add DetectionComponent
            DetectionComponent detectionComponent = enemyObject.AddComponent<DetectionComponent>();
            if (m_aiData != null)
            {
                detectionComponent.SightRange = m_aiData.SightRange;
                detectionComponent.FovAngle = m_aiData.FovAngle;
                detectionComponent.CheckInterval = m_aiData.CheckInterval;
            }

            // Add PatrolComponent (enemies can patrol when not in combat)
            PatrolComponent patrolComponent = enemyObject.AddComponent<PatrolComponent>();
            if (m_enemyWaypoints != null && m_enemyWaypoints.Length > 0)
            {
                // NOTE: See NPC example above for waypoint configuration notes
                Debug.LogWarning("[ExampleAISetup] Waypoint assignment requires Inspector configuration or NavigationPath setup.");
            }

            // CombatComponent should already exist if using m_aiPrefab with combat setup
            CombatComponent combatComponent = enemyObject.GetComponent<CombatComponent>();
            if (combatComponent == null)
            {
                Debug.LogWarning("[ExampleAISetup] CombatComponent not found. Enemy will not be able to attack.");
            }

            Debug.Log($"[ExampleAISetup] Created Enemy: {enemyObject.name}");
            return enemyObject;
        }

        /*-------------------------------------------------------------------
        | --- CreateBaseAI: Create base GameObject with required components --- |
        -------------------------------------------------------------------*/
        private GameObject CreateBaseAI(string name, Vector3 position)
        {
            GameObject aiObject;

            // Use prefab if provided
            if (m_aiPrefab != null)
            {
                aiObject = Instantiate(m_aiPrefab, position, Quaternion.identity);
                aiObject.name = name;
            }
            else
            {
                // Create from scratch
                aiObject = new GameObject(name);
                aiObject.transform.position = position;

                // Add basic Unity components (normally from prefab)
                // NOTE: This is minimal - real AI needs Animator, Collider, etc.
                Debug.LogWarning("[ExampleAISetup] No prefab provided. Creating minimal AI without visuals/animator.");
            }

            // Ensure MovementComponent exists (required by AI)
            MovementComponent movementComponent = aiObject.GetComponent<MovementComponent>();
            if (movementComponent == null)
            {
                movementComponent = aiObject.AddComponent<MovementComponent>();
                Debug.LogWarning("[ExampleAISetup] Added MovementComponent. Ensure NavMeshAgent is configured.");
            }

            return aiObject;
        }

        /*-------------------------------------------------------------------
        | --- MIGRATION NOTES FOR MAINTAINERS --- |
        -------------------------------------------------------------------
        | 
        | This example demonstrates the conceptual approach, but has limitations
        | for runtime configuration:
        | 
        | 1. AIController.m_aiType and m_aiData are private SerializeField
        |    - Best configured in Inspector/prefab
        |    - For runtime setup, add public setters:
        |      public void SetAIType(AIType type) { m_aiType = type; }
        |      public void SetAIData(AIData data) { m_aiData = data; }
        | 
        | 2. PatrolComponent waypoints use SerializeField list or NavigationPath
        |    - For runtime setup, add:
        |      public void SetWaypoints(Vector3[] waypoints) or
        |      public void SetWaypoints(Transform[] waypoints)
        | 
        | 3. Recommended workflow:
        |    - Create prefabs with AIController + components configured
        |    - Instantiate prefabs instead of building at runtime
        |    - Use this script as reference for prefab configuration
        | 
        | 4. For complex setups, use ScriptableObject-based AI templates
        |    that define full AI configuration and spawn logic
        | 
        -------------------------------------------------------------------*/
    }
}

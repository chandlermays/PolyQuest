using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Example script demonstrating how to configure AIController for different AI types.
    /// This is a reference implementation showing proper component setup and configuration.
    /// Attach this to an empty GameObject to see example AI setup in action.
    /// </summary>
    public class ExampleAISetup : MonoBehaviour
    {
        [Header("Example Configuration")]
        [Tooltip("AI Data asset to use for configuration")]
        [SerializeField] private AIData m_exampleAIData;
        
        [Tooltip("Prefab with AIController to spawn")]
        [SerializeField] private GameObject m_aiPrefab;
        
        [Tooltip("Type of AI to configure")]
        [SerializeField] private AIType m_aiType = AIType.NPC;
        
        [Tooltip("Spawn example AI on start")]
        [SerializeField] private bool m_spawnOnStart = false;

        [Header("Patrol Configuration")]
        [Tooltip("Waypoint positions for patrol")]
        [SerializeField] private Vector3[] m_patrolWaypoints = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(5, 0, 0),
            new Vector3(5, 0, 5),
            new Vector3(0, 0, 5)
        };

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            if (m_spawnOnStart)
            {
                SpawnExampleAI();
            }
        }

        /// <summary>
        /// Spawns and configures an example AI based on the selected type.
        /// </summary>
        [ContextMenu("Spawn Example AI")]
        public void SpawnExampleAI()
        {
            GameObject aiObject;

            // Use prefab if available, otherwise create from scratch
            if (m_aiPrefab != null)
            {
                aiObject = Instantiate(m_aiPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                aiObject = new GameObject($"Example {m_aiType} AI");
                aiObject.transform.position = transform.position;
            }

            // Ensure required components exist
            AIController aiController = aiObject.GetComponent<AIController>();
            if (aiController == null)
            {
                aiController = aiObject.AddComponent<AIController>();
            }

            // Configure AI type and data
            // Note: These fields are private in AIController, so this example shows
            // how to configure via inspector or programmatically before instantiation
            Debug.Log($"Created {m_aiType} AI at {aiObject.transform.position}");

            // Add patrol component if waypoints are configured
            if (m_patrolWaypoints != null && m_patrolWaypoints.Length > 0)
            {
                SetupPatrolComponent(aiObject);
            }

            // Add detection and combat components for enemies
            if (m_aiType == AIType.Enemy)
            {
                SetupEnemyComponents(aiObject);
            }

            // Add movement component
            SetupMovementComponent(aiObject);

            Debug.Log($"Example AI setup complete. Type: {m_aiType}");
        }

        /*---------------------------------------------------------------------------- 
        | --- SetupPatrolComponent: Configures patrol behavior --- |
        ----------------------------------------------------------------------------*/
        private void SetupPatrolComponent(GameObject aiObject)
        {
            PatrolComponent patrol = aiObject.GetComponent<PatrolComponent>();
            if (patrol == null)
            {
                patrol = aiObject.AddComponent<PatrolComponent>();
            }

            // Note: PatrolComponent uses either a NavigationPath or custom waypoints
            // In a real scenario, you would create child GameObjects for waypoints
            // or use a NavigationPath ScriptableObject
            
            Debug.Log($"Added PatrolComponent with {m_patrolWaypoints.Length} waypoints");
        }

        /*---------------------------------------------------------------------------- 
        | --- SetupEnemyComponents: Adds detection and combat for enemies --- |
        ----------------------------------------------------------------------------*/
        private void SetupEnemyComponents(GameObject aiObject)
        {
            // Add detection component
            DetectionComponent detection = aiObject.GetComponent<DetectionComponent>();
            if (detection == null)
            {
                detection = aiObject.AddComponent<DetectionComponent>();
            }

            if (m_exampleAIData != null)
            {
                detection.Configure(m_exampleAIData);
            }

            // Add combat component
            AICombatComponent combat = aiObject.GetComponent<AICombatComponent>();
            if (combat == null)
            {
                combat = aiObject.AddComponent<AICombatComponent>();
            }

            if (m_exampleAIData != null)
            {
                combat.Configure(m_exampleAIData);
            }

            Debug.Log("Added DetectionComponent and AICombatComponent for Enemy");
        }

        /*---------------------------------------------------------------------------- 
        | --- SetupMovementComponent: Adds movement capability --- |
        ----------------------------------------------------------------------------*/
        private void SetupMovementComponent(GameObject aiObject)
        {
            AIMovementComponent movement = aiObject.GetComponent<AIMovementComponent>();
            if (movement == null)
            {
                movement = aiObject.AddComponent<AIMovementComponent>();
            }

            if (m_exampleAIData != null)
            {
                movement.Configure(m_exampleAIData);
            }

            Debug.Log("Added AIMovementComponent");
        }

        /*---------------------------------------------------------------------------- 
        | --- OnDrawGizmos: Visualize example waypoints --- |
        ----------------------------------------------------------------------------*/
        private void OnDrawGizmos()
        {
            if (m_patrolWaypoints == null || m_patrolWaypoints.Length == 0)
                return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < m_patrolWaypoints.Length; i++)
            {
                Vector3 worldPos = transform.position + m_patrolWaypoints[i];
                Gizmos.DrawWireSphere(worldPos, 0.5f);

                if (i < m_patrolWaypoints.Length - 1)
                {
                    Vector3 nextWorldPos = transform.position + m_patrolWaypoints[i + 1];
                    Gizmos.DrawLine(worldPos, nextWorldPos);
                }
            }
        }
    }
}

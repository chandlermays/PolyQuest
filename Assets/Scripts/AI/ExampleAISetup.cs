using UnityEngine;
using UnityEngine.AI;
//---------------------------------
using PolyQuest.AI;
using PolyQuest.AI.Components;
using PolyQuest.Components;

namespace PolyQuest.Examples
{
    /// <summary>
    /// Example script demonstrating how to set up an AIController for both NPC and Enemy configurations.
    /// This script shows how to programmatically create and configure AI entities at runtime.
    /// 
    /// Usage:
    /// 1. Attach this script to any GameObject in your scene
    /// 2. Assign an AIData asset in the inspector
    /// 3. Optionally assign a prefab with basic entity components
    /// 4. Run the scene to see AI entities spawned and configured
    /// </summary>
    public class ExampleAISetup : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("AIData asset with tuning parameters")]
        [SerializeField] private AIData m_aiData;

        [Tooltip("Optional prefab with entity components (MovementComponent, Animator, NavMeshAgent, etc.)")]
        [SerializeField] private GameObject m_entityPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Spawn an NPC example")]
        [SerializeField] private bool m_spawnNPC = true;

        [Tooltip("Spawn an Enemy example")]
        [SerializeField] private bool m_spawnEnemy = true;

        [Tooltip("Offset between spawned entities")]
        [SerializeField] private Vector3 m_spawnOffset = new Vector3(5f, 0f, 0f);

        private void Start()
        {
            if (m_aiData == null)
            {
                Debug.LogError("[ExampleAISetup] No AIData assigned. Please create an AIData asset and assign it.");
                return;
            }

            Vector3 spawnPosition = transform.position;

            if (m_spawnNPC)
            {
                CreateNPCExample(spawnPosition);
                spawnPosition += m_spawnOffset;
            }

            if (m_spawnEnemy)
            {
                CreateEnemyExample(spawnPosition);
            }
        }

        /// <summary>
        /// Creates and configures an NPC AI entity.
        /// NPCs typically patrol waypoints and don't engage in combat.
        /// </summary>
        private void CreateNPCExample(Vector3 position)
        {
            GameObject npcObject = CreateBaseEntity("Example NPC", position);
            if (npcObject == null)
                return;

            // Add AIController and configure as NPC
            AIController aiController = npcObject.AddComponent<AIController>();
            aiController.SetAIType(AIType.NPC);
            
            // Add PatrolComponent for waypoint navigation
            PatrolComponent patrolComponent = npcObject.AddComponent<PatrolComponent>();
            
            // Create some simple waypoints for demonstration
            CreatePatrolWaypoints(npcObject, patrolComponent, position, 3, 5f);

            Debug.Log($"[ExampleAISetup] Created NPC at {position} with patrol behavior.");
        }

        /// <summary>
        /// Creates and configures an Enemy AI entity.
        /// Enemies patrol, detect targets, and engage in combat.
        /// </summary>
        private void CreateEnemyExample(Vector3 position)
        {
            GameObject enemyObject = CreateBaseEntity("Example Enemy", position);
            if (enemyObject == null)
                return;

            // Add AIController (defaults to Enemy type)
            AIController aiController = enemyObject.AddComponent<AIController>();

            // Add PatrolComponent
            PatrolComponent patrolComponent = enemyObject.AddComponent<PatrolComponent>();
            CreatePatrolWaypoints(enemyObject, patrolComponent, position, 4, 4f);

            // Add DetectionComponent for target detection
            DetectionComponent detectionComponent = enemyObject.AddComponent<DetectionComponent>();
            detectionComponent.ApplyAIData(m_aiData);

            // Add AICombatComponent for combat behavior
            AICombatComponent combatComponent = enemyObject.AddComponent<AICombatComponent>();
            combatComponent.ApplyAIData(m_aiData);

            // NOTE: This example assumes the entity already has:
            // - MovementComponent (required by AIController)
            // - CombatComponent (for actual combat execution)
            // - HealthComponent (for damage/death handling)
            // - Animator (for animations)
            // - NavMeshAgent (for navigation)
            //
            // If using m_entityPrefab, these should already be on the prefab.
            // If creating from scratch, you would need to add these components here.

            Debug.Log($"[ExampleAISetup] Created Enemy at {position} with patrol, detection, and combat.");
        }

        /// <summary>
        /// Creates a base entity GameObject with necessary components.
        /// </summary>
        private GameObject CreateBaseEntity(string name, Vector3 position)
        {
            GameObject entity;

            if (m_entityPrefab != null)
            {
                // Use prefab if provided
                entity = Instantiate(m_entityPrefab, position, Quaternion.identity);
                entity.name = name;
            }
            else
            {
                // Create from scratch
                entity = new GameObject(name);
                entity.transform.position = position;

                // Add NavMeshAgent
                NavMeshAgent navAgent = entity.AddComponent<NavMeshAgent>();
                navAgent.radius = 0.5f;
                navAgent.height = 2f;
                navAgent.speed = 3.5f;

                // Add Animator (requires an actual animator controller)
                Animator animator = entity.AddComponent<Animator>();
                // Note: You would need to assign an animator controller for this to work properly

                // Add MovementComponent (required by AIController)
                MovementComponent movement = entity.AddComponent<MovementComponent>();

                // Add CapsuleCollider for physics
                CapsuleCollider collider = entity.AddComponent<CapsuleCollider>();
                collider.radius = 0.5f;
                collider.height = 2f;
                collider.center = new Vector3(0, 1f, 0);

                // Add Rigidbody for physics
                Rigidbody rb = entity.AddComponent<Rigidbody>();
                rb.isKinematic = true; // NavMeshAgent controls movement

                Debug.LogWarning($"[ExampleAISetup] Created {name} from scratch. " +
                                 "You may need to add HealthComponent, CombatComponent, and configure Animator for full functionality.");
            }

            return entity;
        }

        /// <summary>
        /// Creates waypoints in a circle pattern around the entity.
        /// </summary>
        private void CreatePatrolWaypoints(GameObject parent, PatrolComponent patrolComponent, Vector3 center, int waypointCount, float radius)
        {
            // Create a container for waypoints
            GameObject waypointsContainer = new GameObject("Patrol Waypoints");
            waypointsContainer.transform.SetParent(parent.transform);
            waypointsContainer.transform.position = center;

            // Create waypoints in a circle
            for (int i = 0; i < waypointCount; i++)
            {
                float angle = (360f / waypointCount) * i;
                float radians = angle * Mathf.Deg2Rad;
                
                Vector3 waypointPosition = center + new Vector3(
                    Mathf.Cos(radians) * radius,
                    0f,
                    Mathf.Sin(radians) * radius
                );

                GameObject waypoint = new GameObject($"Waypoint {i}");
                waypoint.transform.position = waypointPosition;
                waypoint.transform.SetParent(waypointsContainer.transform);

                // Note: PatrolComponent expects Transform references in its waypoint list
                // You would need to manually add these in the inspector or modify PatrolComponent
                // to accept a NavigationPath asset instead
            }

            Debug.Log($"[ExampleAISetup] Created {waypointCount} waypoints for {parent.name}. " +
                      "Add the waypoint Transforms to the PatrolComponent's waypoint list in the inspector.");
        }
    }
}

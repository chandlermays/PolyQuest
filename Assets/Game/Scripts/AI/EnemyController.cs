using UnityEngine;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Components;
using PolyQuest.Inventories;

namespace PolyQuest.AI
{
    /*------------------------------------------------------------- 
    | --- Responsible for Controlling an AI's Actions (kEnemy) --- |
    -------------------------------------------------------------*/
    public class EnemyController : AIController
    {
        [SerializeField] private EnemyTracker m_enemyTracker;

        [Header("AI Settings")]
        [SerializeField] private float m_detectionRange = 3f;
        [SerializeField] private float m_alertRange = 5f;
        [SerializeField] private float m_suspicionTime = 3f;
        [SerializeField] private float m_aggroCooldown = 5f;

        /* --- References --- */
        [SerializeField] private GameObject m_player;
        private GameObject m_currentTarget;
        private Transform m_playerTransform;
        private CombatComponent m_combatComponent;
        private HealthComponent m_healthComponent;
        private RandomDropper m_randomDropper;

        private float m_timeSincePlayerLastDetected = Mathf.Infinity;
        private float m_timeSinceAggrevated = Mathf.Infinity;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();

            Utilities.CheckForNull(m_player, nameof(m_player));

            m_combatComponent = GetComponent<CombatComponent>();
            Utilities.CheckForNull(m_combatComponent, nameof(m_combatComponent));

            m_healthComponent = GetComponent<HealthComponent>();
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));

            m_randomDropper = GetComponent<RandomDropper>();
            Utilities.CheckForNull(m_randomDropper, nameof(m_randomDropper));

            if (m_enemyTracker != null)
            {
                m_enemyTracker.RegisterEnemy(m_healthComponent);
            }
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            if (m_healthComponent.IsDead)
                return;

            m_healthComponent.OnHit += Aggrevate;
            m_healthComponent.OnDeath += () => m_randomDropper.RandomDrop();
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_healthComponent.OnHit -= Aggrevate;
            m_healthComponent.OnDeath -= () => m_randomDropper.RandomDrop();
        }

        /*--------------------------------------------------------------
        | --- OnDestroy: Called when the object is being destroyed --- |
        --------------------------------------------------------------*/
        private void OnDestroy()
        {
            if (m_enemyTracker != null)
            {
                m_enemyTracker.UnregisterEnemy(m_healthComponent);
            }
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        protected override void Start()
        {
            base.Start();

            m_playerTransform = m_player.transform;
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (m_healthComponent.IsDead)
                return;

            m_currentTarget = FindBestTarget();

            if (m_currentTarget != null && m_combatComponent.CanAttack(m_currentTarget))
            {
                AttackState();
            }
            else if (m_timeSincePlayerLastDetected < m_suspicionTime)
            {
                SuspicionState();
            }
            else
            {
                PatrolState();
            }

            m_timeSincePlayerLastDetected += Time.deltaTime;
            m_timeSinceArrivedAtWaypoint += Time.deltaTime;
            m_timeSinceAggrevated += Time.deltaTime;
        }

        /*------------------------------------------------------- 
        | --- Aggrevate: The Behavior of Aggrevating the AI --- |
        -------------------------------------------------------*/
        public void Aggrevate()
        {
            // Reset the timer to push the AI into AttackState
            m_timeSinceAggrevated = 0;
        }

        /*---------------------------------------------------------------------
        | --- FindBestTarget: Find the Best Target within Detection Range --- |
        ---------------------------------------------------------------------*/
        private GameObject FindBestTarget()
        {
            GameObject bestTarget = null;
            float closestDistance = Mathf.Infinity;

            // Check player first (priority target)
            float playerDistance = Vector3.Distance(m_playerTransform.position, m_transform.position);
            if (playerDistance < m_detectionRange || m_timeSinceAggrevated < m_aggroCooldown)
            {
                HealthComponent playerHealth = m_player.GetComponent<HealthComponent>();
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    bestTarget = m_player;
                    closestDistance = playerDistance;
                }
            }

            // Check for NPCs in range
            Collider[] colliders = Physics.OverlapSphere(m_transform.position, m_detectionRange, m_combatComponent.TargetLayers);
            foreach (var collider in colliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                    continue;

                // Check if it's an NPC
                NPCController npc = collider.GetComponent<NPCController>();
                if (npc == null)
                    continue;

                // Check if NPC is alive
                HealthComponent health = collider.GetComponent<HealthComponent>();
                if (health == null || health.IsDead)
                    continue;

                float distance = Vector3.Distance(m_transform.position, collider.transform.position);

                // If no player target yet, or NPC is closer, target the NPC
                if (bestTarget == null || distance < closestDistance)
                {
                    bestTarget = collider.gameObject;
                    closestDistance = distance;
                }
            }

            return bestTarget;
        }

        /*------------------------------------------------------ 
        | --- AttackState: The State of Attacking a Target --- |
        ------------------------------------------------------*/
        private void AttackState()
        {
            m_timeSincePlayerLastDetected = 0;
            m_combatComponent.SetTarget(m_player);

            AlertNearbyEnemies();
        }

        /*--------------------------------------------------------------
        | --- AlertNearbyEnemies: The Behavior of Alerting Enemies --- |
        --------------------------------------------------------------*/
        private void AlertNearbyEnemies()
        {
            RaycastHit[] hits = Physics.SphereCastAll(m_transform.position, m_alertRange, Vector3.up, 0f);
            foreach (RaycastHit hit in hits)
            {
                EnemyController enemyAI = hit.collider.GetComponent<EnemyController>();

                // This enemy instance should already be aggrevated at this point so there's no need to do it again
                if (enemyAI != null && enemyAI != this)
                {
                    enemyAI.Aggrevate();
                }
            }
        }

        /*----------------------------------------------------------------------------------------------- 
        | --- SuspicionState: The State of "Suspicion" when the Target has left the Detection Range --- |
        -----------------------------------------------------------------------------------------------*/
        private void SuspicionState()
        {
            m_movementComponent.Stop();
        }

        /*------------------------------------------------------------------------------- 
        | --- OnDrawGizmosSelected: Draw a Gizmo to Represent the AI's Chase Radius --- |
        -------------------------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            if (m_transform == null)
            {
                m_transform = transform;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(m_transform.position, m_detectionRange);
        }
    }
}
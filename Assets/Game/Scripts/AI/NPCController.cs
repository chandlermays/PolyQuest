using UnityEngine;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /*----------------------------------------------------------- 
    | --- Responsible for Controlling an AI's Actions (NPC) --- |
    -----------------------------------------------------------*/
    public class NPCController : AIController
    {
        [Header("Combat Settings")]
        [SerializeField] private float m_detectionRange = 5f;
        [SerializeField] private float m_suspicionTime = 3f;

        /* --- References --- */
        private CombatComponent m_combatComponent;
        private HealthComponent m_healthComponent;

        private float m_timeSinceEnemyLastDetected = Mathf.Infinity;
        private GameObject m_currentTarget;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();

            m_combatComponent = GetComponent<CombatComponent>();
            m_healthComponent = GetComponent<HealthComponent>();
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            if (m_healthComponent != null && !m_healthComponent.IsDead)
            {
                m_healthComponent.OnHit += Aggrevate;
            }
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            if (m_healthComponent != null)
            {
                m_healthComponent.OnHit -= Aggrevate;
            }
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (m_healthComponent != null && m_healthComponent.IsDead)
                return;

            if (m_combatComponent != null)
            {
                m_currentTarget = FindNearestEnemy();

                if (m_currentTarget != null && m_combatComponent.CanAttack(m_currentTarget))
                {
                    DefendState();
                }
                else if (m_timeSinceEnemyLastDetected < m_suspicionTime)
                {
                    SuspicionState();
                }
                else
                {
                    PatrolState();
                }

                m_timeSinceEnemyLastDetected += Time.deltaTime;
            }
            else
            {
                PatrolState();
            }

            m_timeSinceArrivedAtWaypoint += Time.deltaTime;
        }

        /*-------------------------------------------------------------------------
        | --- FindNearestEnemy: Find the Nearest Enemy within Detection Range --- |
        -------------------------------------------------------------------------*/
        private GameObject FindBestTarget()
        {
            // If we already have a target (e.g., from being attacked), keep attacking them
            if (m_currentTarget != null)
            {
                HealthComponent targetHealth = m_currentTarget.GetComponent<HealthComponent>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    // Check if target is still in range
                    float distance = Vector3.Distance(m_transform.position, m_currentTarget.transform.position);
                    if (distance <= m_detectionRange)
                    {
                        return m_currentTarget;
                    }
                }

                // Target is dead or out of range, clear it
                m_currentTarget = null;
            }

            GameObject nearestEnemy = null;
            float closestDistance = Mathf.Infinity;

            Collider[] colliders = Physics.OverlapSphere(m_transform.position, m_detectionRange, m_combatComponent.TargetLayers);

            foreach (var collider in colliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                    continue;

                // Check if it's an enemy
                EnemyController enemy = collider.GetComponent<EnemyController>();
                if (enemy == null)
                    continue;

                // Check if enemy is alive
                HealthComponent health = collider.GetComponent<HealthComponent>();
                if (health == null || health.IsDead)
                    continue;

                float distance = Vector3.Distance(m_transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    nearestEnemy = collider.gameObject;
                    closestDistance = distance;
                }
            }

            return nearestEnemy;
        }

        /*-------------------------------------------------------------- 
        | --- DefendState: The State of Defending Against an Enemy --- |
        --------------------------------------------------------------*/
        private void DefendState()
        {
            m_timeSinceEnemyLastDetected = 0;

            if (m_currentTarget != null)
            {
                m_combatComponent.SetTarget(m_currentTarget);
            }
        }

        /*--------------------------------------------------------------------------
        | --- SuspicionState: The State of "Suspicion" when Enemy Leaves Range --- |
        --------------------------------------------------------------------------*/
        private void SuspicionState()
        {
            m_movementComponent.Stop();
        }

        /*-----------------------------------------------
        | --- Aggrevate: Called when the NPC is Hit --- |
        -----------------------------------------------*/
        private void Aggrevate(GameObject instigator)
        {
            // Reset the timer to make NPC aggressive
            m_timeSinceEnemyLastDetected = 0;

            // If we were hit by an enemy, immediately target them
            if (instigator != null)
            {
                EnemyController enemy = instigator.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    // Force target switch to the enemy that attacked us
                    m_currentTarget = instigator;
                    if (m_combatComponent != null)
                    {
                        m_combatComponent.SetTarget(instigator);
                    }
                }
            }
            // If no specific attacker or not an enemy, find the nearest enemy
            else if (m_combatComponent != null && m_currentTarget == null)
            {
                m_currentTarget = FindNearestEnemy();
            }
        }

        /*-------------------------------------------------------------------------
        | --- FindNearestEnemy: Find the Nearest Enemy within Detection Range --- |
        -------------------------------------------------------------------------*/
        private GameObject FindNearestEnemy()
        {
            GameObject nearestEnemy = null;
            float closestDistance = Mathf.Infinity;

            Collider[] colliders = Physics.OverlapSphere(m_transform.position, m_detectionRange, m_combatComponent.TargetLayers);

            foreach (var collider in colliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                    continue;

                // Check if it's an enemy
                EnemyController enemy = collider.GetComponent<EnemyController>();
                if (enemy == null)
                    continue;

                // Check if enemy is alive
                HealthComponent health = collider.GetComponent<HealthComponent>();
                if (health == null || health.IsDead)
                    continue;

                float distance = Vector3.Distance(m_transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    nearestEnemy = collider.gameObject;
                    closestDistance = distance;
                }
            }

            return nearestEnemy;
        }

        /*------------------------------------------------------------------------------------ 
        | --- OnDrawGizmosSelected: Draw a Gizmo to Represent the NPC's Detection Radius --- |
        ------------------------------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            if (m_transform == null)
            {
                m_transform = transform;
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(m_transform.position, m_detectionRange);
        }
    }
}
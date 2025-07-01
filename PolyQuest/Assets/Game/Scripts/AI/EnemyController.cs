using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    /*------------------------------------------------------------- 
    | --- Responsible for Controlling an AI's Actions (Enemy) --- |
    -------------------------------------------------------------*/
    public class EnemyController : NonPlayerController
    {
        [SerializeField] private EnemyTracker m_enemyTracker;

        [Header("AI Settings")]
        [SerializeField] private float m_detectionRange = 3f;
        [SerializeField] private float m_alertRange = 5f;
        [SerializeField] private float m_suspicionTime = 3f;
        [SerializeField] private float m_aggroCooldown = 5f;

        /* --- References --- */
        [SerializeField] private GameObject m_player;
        private Transform m_playerTransform;
        private CombatComponent m_combatComponent;
        private HealthComponent m_healthComponent;

        private float m_timeSincePlayerLastDetected = Mathf.Infinity;
        private float m_timeSinceAggrevated = Mathf.Infinity;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();

            m_combatComponent = GetComponent<CombatComponent>();
            Utilities.CheckForNull(m_combatComponent, nameof(m_combatComponent));

            m_healthComponent = GetComponent<HealthComponent>();
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));

            m_healthComponent.OnHit += Aggrevate;

            m_enemyTracker.RegisterEnemy(m_healthComponent);
        }

        /*--------------------------------------------------------------
        | --- OnDestroy: Called when the object is being destroyed --- |
        --------------------------------------------------------------*/
        private void OnDestroy()
        {
            m_healthComponent.OnHit -= Aggrevate;

            m_enemyTracker.UnregisterEnemy(m_healthComponent);
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
            if (m_healthComponent.IsDead())
                return;

            if (IsPlayerDetected() && m_combatComponent.CanAttack(m_player))
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

                // The enemy should already be aggrevated at this point so there's no need to do it again
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

        /*------------------------------------------------------------------------------------------------------- 
        | --- IsPlayerDetected: Checks if the Player is within the Detection Range or has aggrovated the AI --- |
        -------------------------------------------------------------------------------------------------------*/
        private bool IsPlayerDetected()
        {
            float distance = Vector3.Distance(m_playerTransform.position, m_transform.position);
            
            return distance < m_detectionRange || m_timeSinceAggrevated < m_aggroCooldown;
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
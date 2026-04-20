/*---------------------------
File: AIController.cs
Author: Chandler Mays
----------------------------*/
using System;
using UnityEngine;
using UnityEngine.AI;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Core;
using PolyQuest.Inventories;

namespace PolyQuest.AI
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Central coordinator for AI behaviour; owns the state machine and exposes shared data.   *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Bootstraps and drives the AIStateMachine.                                              *
     *      - Exposes tuning parameters and cached component references to AI states.                *
     *      - Provides Aggrevate() so nearby enemies can alert this agent.                           *
     *      - Supports a Reset() path for scene reload / respawn systems.                            *
     * --------------------------------------------------------------------------------------------- */
    public class AIController : MonoBehaviour
    {
        [SerializeField] private EnemyTracker m_enemyTracker;

        [Header("Detection")]
        [SerializeField] private float m_detectionRange = 5f;
        [SerializeField] private float m_alertRange = 5f;

        [Header("Suspicion")]
        [SerializeField] private float m_suspicionTime = 3f;

        [Header("Aggro")]
        [SerializeField] private float m_agroCooldownTime = 5f;

        [Header("Patrol")]
        [SerializeField] private NavigationPath m_patrolPath;
        [SerializeField] private float m_waypointTolerance = 1f;
        [SerializeField] private float m_waypointDwellTime = 3f;
        [Range(0, 1)]
        [SerializeField] private float m_patrolSpeedFraction = 0.2f;

        /* --- Exposed Properties (read by states) --- */
        public float DetectionRange => m_detectionRange;
        public float AlertRange => m_alertRange;
        public float SuspicionTime => m_suspicionTime;
        public float AgroCooldownTime => m_agroCooldownTime;
        public NavigationPath PatrolPath => m_patrolPath;
        public float WaypointTolerance => m_waypointTolerance;
        public float WaypointDwellTime => m_waypointDwellTime;
        public float PatrolSpeedFraction => m_patrolSpeedFraction;

        /* --- Shared Mutable State (read/written by states) --- */
        public float TimeSinceLastSawTarget { get; set; } = Mathf.Infinity;
        public float TimeSinceAggrevated { get; set; } = Mathf.Infinity;

        /* --- Cached Component References --- */
        public AIStateMachine StateMachine { get; private set; }
        public CombatComponent Combat { get; private set; }
        public HealthComponent Health { get; private set; }
        public MovementComponent Movement { get; private set; }
        public RandomDropper Dropper { get; private set; }
        public GameObject Target { get; private set; }

        /* --- Guard Position --- */
        public Vector3 GuardPosition { get; private set; }

        private FactionComponent m_factionComponent;

        private event Action OnDeath;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Combat = GetComponent<CombatComponent>();
            Health = GetComponent<HealthComponent>();
            Movement = GetComponent<MovementComponent>();
            StateMachine = GetComponent<AIStateMachine>();
            m_factionComponent = GetComponent<FactionComponent>();

            TryGetComponent(out RandomDropper dropper);
            Dropper = dropper;

            if (Dropper != null)
            {
                OnDeath = () => Dropper.RandomDrop();
            }

            Utilities.CheckForNull(Combat, nameof(CombatComponent));
            Utilities.CheckForNull(Health, nameof(HealthComponent));
            Utilities.CheckForNull(Movement, nameof(MovementComponent));
            Utilities.CheckForNull(StateMachine, nameof(AIStateMachine));
            Utilities.CheckForNull(m_factionComponent, nameof(FactionComponent));

            GuardPosition = transform.position;

            if (m_enemyTracker != null)
            {
                m_enemyTracker.RegisterEnemy(Health);
            }
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            if (Health.IsDead)
                return;

            Health.OnHit += Aggrevate;

            if (Dropper != null)
            {
                Health.OnDeath += OnDeath;
            }
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            Health.OnHit -= Aggrevate;

            if (Dropper != null)
            {
                Health.OnDeath -= OnDeath;
            }
        }

        /*--------------------------------------------------------------
        | --- OnDestroy: Called when the object is being destroyed --- |
        --------------------------------------------------------------*/
        private void OnDestroy()
        {
            if (m_enemyTracker != null)
            {
                m_enemyTracker.UnregisterEnemy(Health);
            }
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            StateMachine.SetState(new PatrolState());
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (Health.IsDead)
                return;

            RefreshTarget();
            UpdateTimers();
        }

        /*----------------------------------------------------------------------
        | --- Aggrevate: Alert this agent — resets the aggro cooldown timer --- |
        ----------------------------------------------------------------------*/
        public void Aggrevate()
        {
            Aggrevate(null);
        }

        /*----------------------------------------------------------------------------------
        | --- Aggrevate: Alert this agent and optionally force the instigator as target --- |
        ----------------------------------------------------------------------------------*/
        public void Aggrevate(GameObject instigator)
        {
            TimeSinceAggrevated = 0f;

            if (IsValidTarget(instigator))
            {
                Target = instigator;
                TimeSinceLastSawTarget = 0f;
            }
        }

        /*-------------------------------------------------------------------
        | --- Reset: Warp back to guard position and clear all timers --- |
        -------------------------------------------------------------------*/
        public void Reset()
        {
            GetComponent<NavMeshAgent>().Warp(GuardPosition);
            Combat.Cancel();
            Target = null;
            TimeSinceLastSawTarget = Mathf.Infinity;
            TimeSinceAggrevated = Mathf.Infinity;
            StateMachine.SetState(new PatrolState());
        }

        /*----------------------------------------------------
        | --- IsAggrevated: True when the agent is alerted --- |
        ----------------------------------------------------*/
        public bool IsAggrevated()
        {
            if (!IsValidTarget(Target))
                return false;

            float distanceToTarget = Vector3.Distance(Target.transform.position, transform.position);
            return distanceToTarget <= m_detectionRange || TimeSinceAggrevated < m_agroCooldownTime;
        }

        /*------------------------------------------------------------------
        | --- RefreshTarget: Keep or acquire the best hostile target --- |
        ------------------------------------------------------------------*/
        private void RefreshTarget()
        {
            // Keep current target if it's still valid and we're still aggro'd
            if (IsValidTarget(Target) && IsAggrevated())
                return;

            Target = FindBestTarget();
        }

        /*---------------------------------------------------------------------
        | --- FindBestTarget: Returns the nearest valid hostile in range --- |
        ---------------------------------------------------------------------*/
        private GameObject FindBestTarget()
        {
            GameObject bestTarget = null;
            float closestDistance = Mathf.Infinity;

            Collider[] colliders = Physics.OverlapSphere(transform.position, m_detectionRange, Combat.TargetLayers);

            foreach (Collider collider in colliders)
            {
                GameObject candidate = collider.gameObject;

                if (!IsValidTarget(candidate))
                    continue;

                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < closestDistance)
                {
                    bestTarget = candidate;
                    closestDistance = distance;
                }
            }

            return bestTarget;
        }

        /*-------------------------------------------------------------------
        | --- IsValidTarget: Checks if a GameObject is a live hostile --- |
        -------------------------------------------------------------------*/
        private bool IsValidTarget(GameObject candidate)
        {
            if (candidate == null || candidate == gameObject)
                return false;

            if (!candidate.TryGetComponent(out HealthComponent targetHealth) || targetHealth.IsDead)
                return false;

            if (m_factionComponent == null)
                return true;

            if (!candidate.TryGetComponent(out FactionComponent targetFaction))
                return false;

            return m_factionComponent.IsHostileTo(targetFaction);
        }

        /*--------------------------------------------------------------------------
        | --- UpdateTimers: Advance all shared timers every frame --- |
        --------------------------------------------------------------------------*/
        private void UpdateTimers()
        {
            TimeSinceLastSawTarget += Time.deltaTime;
            TimeSinceAggrevated += Time.deltaTime;
        }

        /*---------------------------------------------------------------------
        | --- OnDrawGizmosSelected: Visualize detection radius in editor --- |
        ---------------------------------------------------------------------*/
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, m_detectionRange);
        }
    }
}
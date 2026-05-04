/*---------------------------
File: MovementComponent.cs
Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.AI;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Saving;

namespace PolyQuest.Components
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Handles navigation and movement logic for an entity using Unity's NavMesh system.      *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Controls movement, stopping, and rotation of the entity.                              *
     *      - Integrates with combat and health components to manage movement restrictions.         *
     *      - Calculates valid navigation paths and enforces path length limits.                    *
     *      - Updates animation parameters based on movement state.                                 *
     *      - Supports saving and restoring the entity's position for persistence.                  *
     * ------------------------------------------------------------------------------------------- */
    public class MovementComponent : EntityComponent, ISaveable, IAction
    {
        private enum MovementMode
        {
            kNone,
            kClickToMove,
            kWASD
        }

        private MovementMode m_movementMode = MovementMode.kNone;
        private float m_maxPathLength = 20f;

        private CombatComponent m_combatComponent;
        private HealthComponent m_healthComponent;
        private ActionManager m_actionManager;

        private static readonly int kSpeed = Animator.StringToHash("Speed");

        private bool IsAlive() => m_healthComponent == null || !m_healthComponent.IsDead;
        public bool IsWASDMoving => m_movementMode == MovementMode.kWASD;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();
            m_combatComponent = GetComponent<CombatComponent>();
            Utilities.CheckForNull(m_combatComponent, nameof(CombatComponent));

            m_healthComponent = GetComponent<HealthComponent>();
            Utilities.CheckForNull(m_healthComponent, nameof(HealthComponent));

            m_actionManager = GetComponent<ActionManager>();
            Utilities.CheckForNull(m_actionManager, nameof(ActionManager));
        }

        /*----------------------------------------- 
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (!IsAlive())
            {
                Stop();
                return;
            }
            UpdateAnimator();
        }

        /*----------------------------------------------------------------
        | --- StartMoveAction: The Behavior at the Start of Movement --- |
        ----------------------------------------------------------------*/
        public void StartMoveAction(Vector3 destination)
        {
            if (!IsAlive())
                return;

            m_movementMode = MovementMode.kClickToMove;
            m_actionManager.StartAction(this);
            MoveTo(destination);
        }

        /*---------------------------------------------------------------------
        | --- CanMoveTo: Checks if the Entity can Move to the Destination --- |
        ---------------------------------------------------------------------*/
        public bool CanMoveTo(Vector3 destination)
        {
            NavMeshPath navMeshPath = new();
            if (!NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, navMeshPath))
                return false;

            if (navMeshPath.status != NavMeshPathStatus.PathComplete)
                return false;

            if (GetPathLength(navMeshPath) > m_maxPathLength)
                return false;

            return true;
        }

        /*----------------------------------------------------- 
        | --- MoveTo: Moves the Entity to the Destination --- |
        -----------------------------------------------------*/
        public void MoveTo(Vector3 destination)
        {
            if (!IsAlive())
                return;

            if (!NavMeshAgent.isOnNavMesh)
            {
                Debug.LogWarning($"Agent {gameObject.name} is not on NavMesh");
                return;
            }

            NavMeshAgent.destination = destination;
            NavMeshAgent.isStopped = false;
        }

        /*--------------------------------------------------------- 
        | --- RotateTo: Rotates the Entity to the Destination --- |
        ---------------------------------------------------------*/
        public void RotateTo(Quaternion rotation)
        {
            Transform.rotation = rotation;
        }

        /*--------------------------------------------------- 
        | --- Cancel: Cancel the Movement of the Entity --- |
        ---------------------------------------------------*/
        public void Cancel()
        {
            m_movementMode = MovementMode.kNone;
            Stop();
            NavMeshAgent.ResetPath();
        }

        /*----------------------------------------------- 
        | --- Stop: Stop the Movement of the Entity --- |
        -----------------------------------------------*/
        public void Stop()
        {
            if (NavMeshAgent.enabled && NavMeshAgent.isOnNavMesh)
            {
                NavMeshAgent.isStopped = true;
            }
        }

        /*--------------------------------------------------------------------------------------------- 
        | --- MoveInDirection: Moves the Entity in the specified world direction (for WASD input) --- |
        ---------------------------------------------------------------------------------------------*/
        public void MoveInDirection(Vector3 worldDirection)
        {
            if (!IsAlive())
                return;

            if (!NavMeshAgent.isOnNavMesh)
                return;

            m_actionManager.CancelCurrentAction();

            m_movementMode = MovementMode.kWASD;
            NavMeshAgent.ResetPath();
            NavMeshAgent.isStopped = false;
            NavMeshAgent.velocity = worldDirection * NavMeshAgent.speed;
        }

        /*--------------------------------------------------------- 
        | --- StopWASD: Stops the WASD movement of the Entity --- |
        ---------------------------------------------------------*/
        public void StopWASD()
        {
            if (m_movementMode != MovementMode.kWASD)
                return;

            m_movementMode = MovementMode.kNone;
            NavMeshAgent.velocity = Vector3.zero;
            Stop();
        }

        /*-------------------------------------------------------------------------- 
        | --- CaptureState: Capture the current state of the Entity's position --- |
        --------------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            return Transform.position.ToToken();
        }

        /*-------------------------------------------------------------------------- 
        | --- RestoreState: Restore the Entity's position from the saved state --- |
        --------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            NavMeshAgent.enabled = false;
            Transform.position = state.ToVector3();
            NavMeshAgent.enabled = true;
            m_actionManager.CancelCurrentAction();
        }

        /*------------------------------------------------------------------------------ 
        | --- UpdateAnimator: Updates the Animator with the Entity's current speed --- |
        ------------------------------------------------------------------------------*/
        private void UpdateAnimator()
        {
            Vector3 velocity = NavMeshAgent.velocity;
            Vector3 localVelocity = Transform.InverseTransformDirection(velocity);
            float speed = localVelocity.z;
            Animator.SetFloat(kSpeed, speed);
        }

        /*-------------------------------------------------------------------------- 
        | --- GetPathLength: Calculate the length of the path (in Unity Units) --- |
        --------------------------------------------------------------------------*/
        private float GetPathLength(NavMeshPath path)
        {
            float total = 0;

            if (path.corners.Length < 2)
            {
                return total; // No path corners to calculate length
            }

            for (int i = 0; i < path.corners.Length - 1; ++i)
            {
                total += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }

            return total;
        }
    }
}
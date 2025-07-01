using UnityEngine;
using UnityEngine.AI;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest
{
    /*---------------------------------------------- 
    | --- The Movement Component for an Entity --- |
    ----------------------------------------------*/
    public class MovementComponent : EntityComponent, ISaveable
    {
        /* --- References --- */
        private CombatComponent m_combatComponent;
        private HealthComponent m_healthComponent;

        /* --- Animation Parameters --- */
        private const string kSpeed = "Speed";

        /* --- Navigation --- */
        [SerializeField] private float m_maxPathLength = 20f;

        public void Stop() => NavMeshAgent.isStopped = true;
        private bool IsAlive() => m_healthComponent == null || !m_healthComponent.IsDead();

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            // TODO: Find a different approach; every "Entity" has a movement component,
            // but will not always have a Health/Combat component, so while for now it
            // works to return null and proceed, it is not a good practice.
            // ...Unless it's fine as it is?

            base.Awake();
            m_combatComponent = GetComponent<CombatComponent>();
            m_healthComponent = GetComponent<HealthComponent>();
        }

        /*----------------------------------------- 
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (!IsAlive())
            {
                NavMeshAgent.isStopped = true;
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

            m_combatComponent?.Cancel();    // Null the Target before Moving
            MoveTo(destination);
        }

        /*---------------------------------------------------------------------
        | --- CanMoveTo: Checks if the Player can Move to the Destination --- |
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
        | --- MoveTo: Moves the Player to the Destination --- |
        -----------------------------------------------------*/
        public void MoveTo(Vector3 destination)
        {
            if (!IsAlive())
                return;

            NavMeshAgent.destination = destination;
            NavMeshAgent.isStopped = false;
        }

        /*--------------------------------------------------------- 
        | --- RotateTo: Rotates the Player to the Destination --- |
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
            NavMeshAgent.isStopped = true;
            NavMeshAgent.ResetPath();
        }

        /*------------------------------------------------------------------------------ 
        | --- UpdateAnimator: Updates the Animator with the Player's current kSpeed --- |
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

        /*----------------------------------------------------------------
        | --- CaptureState: Captures the current State of the Entity --- |
        ----------------------------------------------------------------*/
        public object CaptureState()
        {
            return new MySerializableVector3(Transform.position);
        }

        /*---------------------------------------------------------------
        | --- RestoreState: Restores the Entity to a previous State --- |
        ---------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            MySerializableVector3 position = (MySerializableVector3)state;
            NavMeshAgent.enabled = false;
            Transform.position = position.ToVector3();
            NavMeshAgent.enabled = true;
            m_combatComponent?.Cancel();
        }
    }
}
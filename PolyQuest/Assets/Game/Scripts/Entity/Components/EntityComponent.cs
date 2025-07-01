using UnityEngine;
using UnityEngine.AI;
//---------------------------------

namespace PolyQuest
{
    public abstract class EntityComponent : MonoBehaviour
    {
        protected GameObject Owner { get; private set; }
        protected Transform Transform { get; private set; }
        protected Animator Animator { get; private set; }
        protected NavMeshAgent NavMeshAgent { get; private set; }

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected virtual void Awake()
        {
            Owner = gameObject;
            Transform = transform;
            Animator = GetComponent<Animator>();
            NavMeshAgent = GetComponent<NavMeshAgent>();

            Utilities.CheckForNull(Animator, nameof(Animator));
            Utilities.CheckForNull(NavMeshAgent, nameof(NavMeshAgent));
        }
    }
}
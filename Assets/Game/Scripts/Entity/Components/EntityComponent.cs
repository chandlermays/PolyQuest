using UnityEngine;
using UnityEngine.AI;
//---------------------------------

namespace PolyQuest.Components
{
    /* -------------------------------------------------------------------------------------------------
     * Role: Serves as a base class for all entity-related components, providing common references.     *
     *                                                                                                  *
     * Responsibilities:                                                                                *
     *      - Provides access to core Unity components.                                                 *
     *      - Ensures required components are assigned and available for derived classes.               *
     *      - Establishes a consistent initialization pattern for entity components.                    *
     *      - Supports extension by specialized components.                                             *
     * ------------------------------------------------------------------------------------------------ */
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
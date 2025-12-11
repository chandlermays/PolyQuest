using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Interface for AI state behavior. States are plain C# objects (not MonoBehaviours) 
    /// that encapsulate specific behaviors like patrolling, attacking, or being suspicious.
    /// </summary>
    public interface IAIState
    {
        /// <summary>
        /// Called when entering this state. Use for initialization and setup.
        /// </summary>
        /// <param name="controller">The AIController managing this state</param>
        void Enter(AIController controller);

        /// <summary>
        /// Called every frame while in this state. Contains the main state logic.
        /// </summary>
        /// <param name="controller">The AIController managing this state</param>
        void Tick(AIController controller);

        /// <summary>
        /// Called when exiting this state. Use for cleanup.
        /// </summary>
        /// <param name="controller">The AIController managing this state</param>
        void Exit(AIController controller);

        /// <summary>
        /// Optional method for handling events while in this state.
        /// </summary>
        /// <param name="controller">The AIController managing this state</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Optional event data</param>
        void OnEvent(AIController controller, string eventName, object eventData = null);
    }
}

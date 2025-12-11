using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /// <summary>
    /// Interface defining the contract for all AI states.
    /// States handle specific behaviors (idle, patrol, attack, etc.) and can transition between each other.
    /// </summary>
    public interface IAIState
    {
        /// <summary>
        /// Called when the state is first entered.
        /// Use this to initialize state-specific variables and start behaviors.
        /// </summary>
        /// <param name="controller">The AIController that owns this state</param>
        void Enter(AIController controller);

        /// <summary>
        /// Called every frame while the state is active.
        /// Implement the main behavior logic here.
        /// </summary>
        /// <param name="controller">The AIController that owns this state</param>
        void Tick(AIController controller);

        /// <summary>
        /// Called when transitioning away from this state.
        /// Use this to clean up state-specific resources and stop behaviors.
        /// </summary>
        /// <param name="controller">The AIController that owns this state</param>
        void Exit(AIController controller);

        /// <summary>
        /// Called when an external event occurs that might affect this state.
        /// Examples: target detected, target lost, damage taken, etc.
        /// </summary>
        /// <param name="controller">The AIController that owns this state</param>
        /// <param name="eventName">The name of the event</param>
        /// <param name="eventData">Optional data associated with the event</param>
        void OnEvent(AIController controller, string eventName, object eventData = null);
    }
}

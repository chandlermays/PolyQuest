namespace PolyQuest.AI
{
    /// <summary>
    /// Interface for AI state implementations in the state machine.
    /// All states must implement Enter, Exit, Tick, and OnEvent methods.
    /// </summary>
    public interface IAIState
    {
        /// <summary>
        /// Called when the state is entered.
        /// Use this to initialize state-specific behavior and setup.
        /// </summary>
        void Enter();

        /// <summary>
        /// Called when the state is exited.
        /// Use this to cleanup state-specific resources and reset state.
        /// </summary>
        void Exit();

        /// <summary>
        /// Called every frame while the state is active.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        void Tick(float deltaTime);

        /// <summary>
        /// Called when an event is triggered while this state is active.
        /// Use this for event-driven state transitions or behavior changes.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="data">Optional event data</param>
        void OnEvent(string eventName, object data);
    }
}

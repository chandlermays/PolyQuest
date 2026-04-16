namespace PolyQuest.AI
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Defines the contract for all AI states used by the AIStateMachine.                     *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Provides lifecycle hooks: Initialize, Enter, Tick, and Exit.                           *
     *      - Decouples state logic from the state machine and AIController.                         *
     * --------------------------------------------------------------------------------------------- */
    public interface IAIState
    {
        void Initalize(AIController owner);
        void Enter();
        void Tick();
        void Exit();
    }
}
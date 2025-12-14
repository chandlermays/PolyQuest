namespace PolyQuest.AI
{
    public interface IAIState
    {
        void Initalize(AIController owner);

        void Enter();
        void Tick();
        void Exit();
    }
}
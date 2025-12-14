namespace PolyQuest.AI
{
    public class SuspicionState : IAIState
    {
        private AIController m_owner;

        public void Initalize(AIController owner)
        {
            m_owner = owner;
        }

        public void Enter()
        {
            //...
        }

        public void Tick()
        {

        }

        public void Exit()
        {
            //...
        }
    }
}
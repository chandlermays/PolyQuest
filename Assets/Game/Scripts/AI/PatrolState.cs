namespace PolyQuest.AI
{
    public class PatrolState : IAIState
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
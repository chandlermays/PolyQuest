using UnityEngine;

namespace PolyQuest
{
    public class ObjectiveCompletion : MonoBehaviour
    {
        [SerializeField] private QuestManager m_questManager;
        [SerializeField] private Quest m_quest;
        [SerializeField] private string m_objective;

        /*-----------------------------------------------------------------
        | --- CompleteObjective: Mark a quest's objective as complete --- |
        -----------------------------------------------------------------*/
        public void CompleteObjective()
        {
            m_questManager.CompleteObjective(m_quest, m_objective);
        }
    }
}
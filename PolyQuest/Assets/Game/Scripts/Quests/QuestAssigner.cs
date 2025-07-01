using UnityEngine;

namespace PolyQuest
{
    public class QuestAssigner : MonoBehaviour
    {
        [SerializeField] private GameObject m_player;
        [SerializeField] private Quest m_quest;

        /*-----------------------------------------------------
        | --- AssignQuest: Assign the quest to the player --- |
        -----------------------------------------------------*/
        public void AssignQuest()
        {
            QuestManager questManager = m_player.GetComponent<QuestManager>();
            questManager.AddQuest(m_quest);
        }
    }
}
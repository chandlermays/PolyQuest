using TMPro;
using UnityEngine;

namespace PolyQuest
{
    public class QuestUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_questTitle;
        [SerializeField] private TextMeshProUGUI m_questProgress;

        private QuestStatus m_status;
        public QuestStatus GetQuestStatus() => m_status;

        /*------------------------------------------------
        | --- Setup: Initialize the quest UI element --- |
        ------------------------------------------------*/
        public void Setup(QuestStatus status)
        {
            m_status = status;
            Quest quest = status.GetQuest();

            m_questTitle.text = quest.GetTitle();

            // Format the progression text
            m_questProgress.text = status.GetCompletedObjectiveCount() + "/" + quest.GetObjectiveCount();
        }
    }
}
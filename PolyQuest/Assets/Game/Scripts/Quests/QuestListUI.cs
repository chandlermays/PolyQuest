using UnityEngine;

namespace PolyQuest
{
    public class QuestListUI : MonoBehaviour
    {
        [SerializeField] private GameObject m_player;
        [SerializeField] private QuestUI m_questUIPrefab;

        private QuestManager m_questManager;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_questManager = m_player.GetComponent<QuestManager>();
            m_questManager.OnUpdate += RefreshQuestUI;
            RefreshQuestUI();
        }

        /*------------------------------------------------------
        | --- RefreshQuestUI: Update the Quest UI elements --- |
        ------------------------------------------------------*/
        private void RefreshQuestUI()
        {
            // Clear out any existing quest UI elements
            transform.DetachChildren();

            // Instantiate the quest UI for each quest
            foreach (QuestStatus status in m_questManager.GetStatuses())
            {
                QuestUI questUI = Instantiate<QuestUI>(m_questUIPrefab, transform);
                questUI.Setup(status);
            }
        }
    }
}
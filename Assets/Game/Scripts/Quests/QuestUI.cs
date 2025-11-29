using TMPro;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Quests
{
    /* --------------------------------------------------------------------------------------------
     * Role: Displays an individual quest's title and progress within the quest list UI.           *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - References UI elements for the quest's title and objective progress.                 *
     *      - Initializes and updates its display based on a given QuestStatus.                    *
     *      - Shows the quest name and the number of completed objectives out of the total.        *
     *      - Provides access to the associated QuestStatus for further UI or logic needs.         *
     *      - Designed to be instantiated by QuestListUI for each active quest.                    *
     * ------------------------------------------------------------------------------------------- */
    public class QuestUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_questTitle;
        [SerializeField] private TextMeshProUGUI m_questProgress;

        private QuestStatus m_status;
        public QuestStatus GetQuestStatus() => m_status;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_questTitle, nameof(m_questTitle));
            Utilities.CheckForNull(m_questProgress, nameof(m_questProgress));
        }

        /*------------------------------------------------
        | --- Setup: InitializeDecorationArray the quest UI element --- |
        ------------------------------------------------*/
        public void Setup(QuestStatus status)
        {
            m_status = status;
            Quest quest = status.Quest;

            m_questTitle.text = quest.Title;

            // Format the progression text
            m_questProgress.text = status.CompletedObjectiveCount + "/" + quest.ObjectiveCount;
        }
    }
}
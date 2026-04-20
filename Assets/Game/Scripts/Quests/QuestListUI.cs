/*---------------------------
File: QuestListUI.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.Quests
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Displays and manages the list of active quests in the player's UI.                    *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - References the player's QuestManager to access current quest statuses.               *
     *      - Instantiates and updates QuestUI elements for each active quest.                     *
     *      - Listens for quest updates and refreshes the UI accordingly.                          *
     *      - Clears and rebuilds the quest list UI when quests are added, removed, or updated.    *
     *      - Serves as the main container for all quest-related UI elements in the quest log.     *
     * ------------------------------------------------------------------------------------------- */
    public class QuestListUI : MonoBehaviour
    {
        [SerializeField] private QuestManager m_playerQuestMgr;
        [SerializeField] private QuestUI m_questUIPrefab;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_playerQuestMgr, nameof(m_playerQuestMgr));
            Utilities.CheckForNull(m_questUIPrefab, nameof(m_questUIPrefab));
        }

        /*---------------------------------------------------------------------
        | --- OnENable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_playerQuestMgr.OnQuestsUpdate += RefreshQuestUI;
            RefreshQuestUI();
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_playerQuestMgr.OnQuestsUpdate -= RefreshQuestUI;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            RefreshQuestUI();
        }

        /*------------------------------------------------------
        | --- RefreshQuestUI: Update the Quest UI elements --- |
        ------------------------------------------------------*/
        private void RefreshQuestUI()
        {
            // Clear out any existing quest UI elements
            foreach (Transform item in transform)
            {
                Destroy(item.gameObject);
            }

            // Instantiate the quest UI for each quest
            foreach (QuestStatus status in m_playerQuestMgr.ActiveQuests)
            {
                QuestUI questUI = Instantiate<QuestUI>(m_questUIPrefab, transform);
                questUI.Setup(status);
            }
        }
    }
}
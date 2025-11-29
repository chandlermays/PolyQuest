using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Quests
{
    /* --------------------------------------------------------------------------------------------
     * Role: Tracks the player's progress for a specific quest, including completed objectives.    *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Maintains a reference to the associated Quest asset.                                 *
     *      - Records which objectives have been completed by the player.                          *
     *      - Provides methods to check, complete, and count objectives.                           *
     *      - Determines if the quest is fully complete.                                           *
     *      - Supports saving and loading quest progress for persistence.                          *
     *      - Can be constructed from a Quest asset or a saved state for restoration.              *
     * ------------------------------------------------------------------------------------------- */
    public class QuestStatus
    {
        private Quest m_quest;
        private HashSet<string> m_completedObjectives = new();

        [System.Serializable]
        private class QuestStatusRecord
        {
            [SerializeField] private string m_questName;
            [SerializeField] private List<string> m_completedObjectives = new();

            public string QuestName
            { get => m_questName; set => m_questName = value; }

            public List<string> CompletedObjectives
            { get => m_completedObjectives; set => m_completedObjectives = value; }
        }

        public Quest Quest => m_quest;
        public int CompletedObjectiveCount => m_completedObjectives.Count;
        public bool IsObjectiveComplete(string objective) => m_completedObjectives.Contains(objective);

        /*---------------------------------------------------------------------
        | --- Constructor: InitializeDecorationArray the QuestStatus from a Quest object --- |
        ---------------------------------------------------------------------*/
        public QuestStatus(Quest quest)
        {
            this.m_quest = quest;
        }

        /*--------------------------------------------------------------------
        | --- Constructor: InitializeDecorationArray the QuestStatus from a saved state --- |
        --------------------------------------------------------------------*/
        public QuestStatus(object objectState)
        {
            QuestStatusRecord state = objectState as QuestStatusRecord;
            m_quest = Quest.GetByName(state.QuestName);

            m_completedObjectives = state.CompletedObjectives != null
                ? new HashSet<string>(state.CompletedObjectives)
                : new HashSet<string>();
        }

        /*--------------------------------------------------------------------
        | --- CompleteObjective: Adds an objective to the completed list --- |
        --------------------------------------------------------------------*/
        public void CompleteObjective(string objective)
        {
            if (m_quest.HasObjective(objective) && !m_completedObjectives.Contains(objective))
            {
                m_completedObjectives.Add(objective);
            }
        }

        /*--------------------------------------------------------------------
        | --- CaptureState: Save the Current State of the Quest's Status --- |
        --------------------------------------------------------------------*/
        public object CaptureState()
        {
            QuestStatusRecord state = new()
            {
                QuestName = m_quest.name,
                CompletedObjectives = new List<string>(m_completedObjectives)
            };
            return state;
        }


        /*----------------------------------------------------------------
        | --- IsQuestComplete: Check if all objectives are completed --- |
        ----------------------------------------------------------------*/
        public bool IsQuestComplete()
        {
            foreach (Quest.Objective objective in m_quest.Objectives)
            {
                if (!m_completedObjectives.Contains(objective.Identifier))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
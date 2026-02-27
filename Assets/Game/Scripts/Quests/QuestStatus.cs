using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
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
        private HashSet<QuestObjective> m_completedObjectives = new();

        private const string kQuestNameKey = "QuestName";
        private const string kCompletedObjectivesKey = "CompletedObjectives";

        public Quest Quest => m_quest;
        public int CompletedObjectiveCount => m_completedObjectives.Count;
        public bool IsObjectiveComplete(QuestObjective objective) => m_completedObjectives.Contains(objective);

        /*---------------------------------------------------------------------
        | --- Constructor: Initialize the QuestStatus from a Quest object --- |
        ---------------------------------------------------------------------*/
        public QuestStatus(Quest quest)
        {
            this.m_quest = quest;
        }

        /*--------------------------------------------------------------------
        | --- Constructor: Initialize the QuestStatus from a saved state --- |
        --------------------------------------------------------------------*/
        public QuestStatus(JToken objectState)
        {
            if (objectState is JObject state)
            {
                m_quest = Quest.GetByName(state[kQuestNameKey].ToObject<string>());
                m_completedObjectives.Clear();

                if (state[kCompletedObjectivesKey] is JArray completedState)
                {
                    foreach (JToken token in completedState)
                    {
                        string savedName = token.ToObject<string>();
                        QuestObjective obj = m_quest.Objectives.FirstOrDefault(o => o.name == savedName);
                        if (obj != null) m_completedObjectives.Add(obj);
                    }
                }
            }
        }

        /*--------------------------------------------------------------------
        | --- CompleteObjective: Adds an objective to the completed list --- |
        --------------------------------------------------------------------*/
        public void CompleteObjective(QuestObjective objective)
        {
            if (m_quest.HasObjective(objective) && !m_completedObjectives.Contains(objective))
            {
                m_completedObjectives.Add(objective);
            }
        }

        /*-----------------------------------------------------------------------
        | --- CaptureAsJToken: Save the Current State of the Quest's Status --- |
        -----------------------------------------------------------------------*/
        public JToken CaptureAsJToken()
        {
            JObject state = new()
            {
                [kQuestNameKey] = m_quest.name
            };

            JArray completedState = new();

            foreach (QuestObjective objective in m_completedObjectives)
            {
                completedState.Add(objective.name);
            }

            state[kCompletedObjectivesKey] = completedState;
            return state;
        }

        /*----------------------------------------------------------------
        | --- IsQuestComplete: Check if all objectives are completed --- |
        ----------------------------------------------------------------*/
        public bool IsQuestComplete()
        {
            foreach (QuestObjective objective in m_quest.Objectives)
            {
                if (!m_completedObjectives.Contains(objective))
                    return false;
            }
            return true;
        }
    }
}
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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

        private const string kQuestNameKey = "QuestName";
        private const string kCompletedObjectivesKey = "CompletedObjectives";

        public Quest Quest => m_quest;
        public int CompletedObjectiveCount => m_completedObjectives.Count;
        public bool IsObjectiveComplete(string objective) => m_completedObjectives.Contains(objective);

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
                IDictionary<string, JToken> stateDict = state;
                m_quest = Quest.GetByName(stateDict[kQuestNameKey].ToObject<string>());
                m_completedObjectives.Clear();

                if (stateDict[kCompletedObjectivesKey] is JArray completedState)
                {
                    IList<JToken> completedStateArray = completedState;
                    foreach (JToken objective in completedStateArray)
                    {
                        m_completedObjectives.Add(objective.ToObject<string>());
                    }
                }
            }
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
            foreach (string objective in m_completedObjectives)
            {
                completedState.Add(objective);
            }
            state[kCompletedObjectivesKey] = completedState;

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
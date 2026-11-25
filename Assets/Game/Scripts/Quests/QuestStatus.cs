/*---------------------------
| File: QuestStatus.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
     *      - Can be constructed from a Quest asset or restored from a saved state.                *
     * ------------------------------------------------------------------------------------------- */
    public class QuestStatus
    {
        private readonly Quest m_quest;
        private readonly HashSet<QuestObjective> m_completedObjectives = new();

        private const string kQuestNameKey = "QuestName";
        private const string kCompletedObjectivesKey = "CompletedObjectives";

        public Quest Quest => m_quest;
        public int CompletedObjectiveCount => m_completedObjectives.Count;
        public bool IsObjectiveComplete(QuestObjective objective) => objective != null && m_completedObjectives.Contains(objective);

        /*---------------------------------------------------------------------
        | --- Constructor: Initialize the QuestStatus from a Quest object --- |
        ---------------------------------------------------------------------*/
        public QuestStatus(Quest quest)
        {
            this.m_quest = quest;
        }

        /*------------------------------------------------------------------------------------------------------------
        | --- Restore: Rebuilds a status from saved data; null if it can't be read or its quest no longer exists --- |
        ------------------------------------------------------------------------------------------------------------*/
        public static QuestStatus Restore(JToken objectState)
        {
            if (objectState is not JObject state)
                return null;

            JToken questName = state[kQuestNameKey];
            if (questName == null || questName.Type != JTokenType.String)
                return null;

            Quest quest = Quest.GetByName(questName.Value<string>());
            if (quest == null)
                return null;

            // Objectives the quest no longer has are dropped; ones it gained since the save start incomplete
            QuestStatus status = new(quest);
            if (state[kCompletedObjectivesKey] is JArray completedState)
            {
                foreach (JToken token in completedState)
                {
                    if (token.Type != JTokenType.String)
                        continue;

                    string savedName = token.Value<string>();
                    foreach (QuestObjective objective in quest.Objectives)
                    {
                        if (objective != null && objective.name == savedName)
                        {
                            status.m_completedObjectives.Add(objective);
                            break;
                        }
                    }
                }
            }
            return status;
        }

        /*----------------------------------------------------------------------------------------------------
        | --- CompleteObjective: Adds an objective of this quest to the completed list; true if it's new --- |
        ----------------------------------------------------------------------------------------------------*/
        public bool CompleteObjective(QuestObjective objective)
        {
            if (objective == null || !m_quest.HasObjective(objective))
                return false;

            return m_completedObjectives.Add(objective);
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
using System.Collections.Generic;

namespace PolyQuest
{
    /*--------------------------------------------------------------------------------------
    | This represents the status of a specific Quest. It tracks which objectives of the    |
    | quest have been completed and provides methods to mark objectives as complete, check |
    | if a specific objective is complete, and determines if the entire quest is complete. |
    | It also supports saving and restoring its state.                                     |
    --------------------------------------------------------------------------------------*/

    public class QuestStatus
    {
        private Quest m_quest;
        private HashSet<string> m_completedObjectives = new();

        [System.Serializable]
        class QuestStatusRecord
        {
            public string m_questName;
            public List<string> m_completedObjectives = new();
        }

        public Quest GetQuest() => m_quest;
        public int GetCompletedObjectiveCount() => m_completedObjectives.Count;
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
        public QuestStatus(object objectState)
        {
            QuestStatusRecord state = objectState as QuestStatusRecord;
            m_quest = Quest.GetByName(state.m_questName);

            m_completedObjectives = state.m_completedObjectives != null
                ? new HashSet<string>(state.m_completedObjectives)
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
                m_questName = m_quest.name,
                m_completedObjectives = new List<string>(m_completedObjectives)
            };
            return state;
        }


        /*----------------------------------------------------------------
        | --- IsQuestComplete: Check if all objectives are completed --- |
        ----------------------------------------------------------------*/
        public bool IsQuestComplete()
        {
            foreach (Quest.Objective objective in m_quest.GetObjectives())
            {
                if (!m_completedObjectives.Contains(objective.m_identifier))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
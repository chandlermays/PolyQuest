/*---------------------------
File: ObjectiveCompletion.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.Quests
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Handles the completion of a specific quest objective, acting as a bridge between      *
     *       in-game events and the quest system.                                                  *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - References a specific quest and objective to be completed.                           *
     *      - Provides a method to mark the objective as complete via the QuestManager.            *
     *      - Designed to be triggered by in-game events, triggers, or interactions.               *
     *      - Enables modular and reusable objective completion logic for various quest types.     *
     * ------------------------------------------------------------------------------------------- */
    public class ObjectiveCompletion : MonoBehaviour
    {
        [SerializeField] private QuestManager m_questManager;
        [SerializeField] private Quest m_quest;
        [SerializeField] private QuestObjective m_objective;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_questManager, nameof(m_questManager));
            Utilities.CheckForNull(m_quest, nameof(m_quest));
            Utilities.CheckForNull(m_objective, nameof(m_objective));
        }

        /*-----------------------------------------------------------------
        | --- CompleteObjective: Mark a quest's objective as complete --- |
        -----------------------------------------------------------------*/
        public void CompleteObjective()
        {
            m_questManager.CompleteObjective(m_quest, m_objective);
        }
    }
}
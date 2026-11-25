/*---------------------------
| File: QuestObjective.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
//---------------------------------
using PolyQuest.Tools;

namespace PolyQuest.Quests
{
    /* --------------------------------------------------------------------------------------------
     * Role: Represents a single objective within a quest, providing metadata for quest progress   *
     *       tracking and display.                                                                 *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores a unique identifier for the objective.                                        *
     *      - Contains a description to inform the player of the objective's requirements.         *
     *      - Says whether it's a turn-in, which is never satisfied in advance.                    *
     *      - Holds an optional condition on the world that completes it (Complete When).          *
     *      - Lists the objectives in its quest that are done once it is (Also Completes).         *
     * ------------------------------------------------------------------------------------------- */
    [CreateAssetMenu(menuName = "PolyQuest/Quests/Quest Objective", fileName = "New Quest Objective")]
    public class QuestObjective : ScriptableObject
    {
        [SerializeField, TextArea] private string m_description;    // do I really want this property?

        [Tooltip("Finished by returning to someone (e.g. Return to Captain Flint). A turn-in is never satisfied in advance.")]
        [SerializeField] private bool m_isTurnIn = false;

        [Tooltip("Optional. While the quest is active, the objective completes as soon as this passes, including on receiving the quest, so work done beforehand counts (e.g. kWorldFlag camp_cleared, kHasItem <sword>). Leave empty for an objective that only completes from an event. Never on a turn-in.")]
        [FormerlySerializedAs("m_alreadySatisfied")]  // its name before the rename, so authored conditions load
        [SerializeField] private Conjunction m_completeWhen = new();

        [Tooltip("Other objectives in the same quest that are done once this one is (clearing the camp also completes finding it).")]
        [FormerlySerializedAs("m_implies")]           // its name before the rename, so authored lists load
        [SerializeField] private List<QuestObjective> m_alsoCompletes = new();

        public string Description => m_description;
        public bool IsTurnIn => m_isTurnIn;
        public IReadOnlyList<QuestObjective> AlsoCompletes => m_alsoCompletes;
        public bool HasCompleteWhenCondition => m_completeWhen != null && !m_completeWhen.IsEmpty;

        /*------------------------------------------------------------------------------------------------------------
        | --- IsCompleteWhenMet: Whether its Complete When condition passes; never for a turn-in or no condition --- |
        ------------------------------------------------------------------------------------------------------------*/
        public bool IsCompleteWhenMet(IEnumerable<IConditionChecker> checkers)
        {
            if (m_isTurnIn || !HasCompleteWhenCondition)
                return false;

            return m_completeWhen.Check(checkers);
        }

        /*------------------------------------------------------------------------------------
        | --- GetByName: Retrieve a QuestObjective by its name from the Resources folder --- |
        ------------------------------------------------------------------------------------*/
        public static QuestObjective GetByName(string objectiveName)
        {
            foreach (QuestObjective objective in Resources.LoadAll<QuestObjective>(""))
            {
                if (objective.name == objectiveName) return objective;
            }
            return null;
        }
    }
}
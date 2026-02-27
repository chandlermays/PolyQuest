using UnityEngine;

namespace PolyQuest.Quests
{
    /* --------------------------------------------------------------------------------------------
     * Role: Represents a single objective within a quest, providing metadata for quest progress   *
     *       tracking and display.                                                                 *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores a unique identifier for the objective.                                        *
     *      - Contains a description to inform the player of the objective's requirements.         *
     *      - Serves as a data asset to be referenced by quests and quest systems.                 *
     *      - Enables modular and reusable objective definitions across multiple quests.           *
     * ------------------------------------------------------------------------------------------- */
    [CreateAssetMenu(fileName = "New Quest Objective", menuName = "PolyQuest/Quests/Quest Objective", order = 0)]
    public class QuestObjective : ScriptableObject
    {
        [SerializeField, TextArea] private string m_description;    // do I really want this property?
        public string Description => m_description;

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
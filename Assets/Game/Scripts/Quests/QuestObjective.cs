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
    [CreateAssetMenu(fileName = "New Quest Objective", menuName = "PolyQuest/Quest Objective", order = 0)]
    public class QuestObjective : ScriptableObject
    {
        public string m_identifier;
        [TextArea] public string m_description;
    }
}
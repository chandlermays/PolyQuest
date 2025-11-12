using UnityEngine;
//---------------------------------

namespace PolyQuest.Quests
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Assigns a specific quest to the player's quest manager, typically triggered by NPCs,   *
     *       interactable objects, or scripted events in the game world.                            *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Holds a reference to a Quest asset and the player's QuestManager component.           *
     *      - Provides a method to assign the referenced quest to the player.                       *
     *      - Acts as a bridge between quest-giving game objects (e.g., NPCs) and the quest system. *
     *      - Can be attached to any GameObject that should offer or assign quests.                 *
     * -------------------------------------------------------------------------------------------- */
    public class QuestAssigner : MonoBehaviour
    {
        [SerializeField] private QuestManager m_playerQuestMgr;
        [SerializeField] private Quest m_quest;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_playerQuestMgr, nameof(m_playerQuestMgr));
            Utilities.CheckForNull(m_quest, nameof(m_quest));
        }

        /*-----------------------------------------------------
        | --- AssignQuest: Assign the quest to the player --- |
        -----------------------------------------------------*/
        public void AssignQuest()
        {
            m_playerQuestMgr.AddQuest(m_quest);
        }
    }
}
using UnityEngine;
//---------------------------------
using PolyQuest.Quests;

namespace PolyQuest.Dialogues
{
    [CreateAssetMenu(menuName = "PolyQuest/Dialogue/Events/Assign Quest", fileName = "New Assign Quest Event")]
    public class AssignQuestEvent : DialogueEvent
    {
        [SerializeField] private Quest m_quest;

        public override void Execute(GameObject instigator, GameObject target)
        {
            instigator.GetComponent<QuestManager>().AddQuest(m_quest);
        }
    }
}
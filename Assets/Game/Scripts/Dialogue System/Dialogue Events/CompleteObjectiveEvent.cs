/*---------------------------
File: CompleteObjectiveEvent.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------
using PolyQuest.Quests;

namespace PolyQuest.Dialogues
{
    [CreateAssetMenu(menuName = "PolyQuest/Dialogue/Events/Complete Objective", fileName = "New Complete Objective Event")]
    public class CompleteObjectiveEvent : DialogueEvent
    {
        [SerializeField] private Quest m_quest;
        [SerializeField] private QuestObjective m_objective;

        public override void Execute(GameObject instigator, GameObject target)
        {
            instigator.GetComponent<QuestManager>().CompleteObjective(m_quest, m_objective);
        }
    }
}
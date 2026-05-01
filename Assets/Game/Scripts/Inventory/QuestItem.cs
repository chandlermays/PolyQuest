/*---------------------------
File: QuestItem.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------
using PolyQuest.Quests;

namespace PolyQuest.Inventories
{
    [CreateAssetMenu(menuName = "PolyQuest/Items/Quest Item", fileName = "New Quest Item")]
    public class QuestItem : InventoryItem
    {
        [SerializeField] private Quest m_quest;
        [SerializeField] private QuestObjective m_objective;

        public Quest Quest => m_quest;
        public QuestObjective Objective => m_objective;

        public override void OnPickup(Inventory inventory, int quantity)
        {
            bool added = inventory.TryAddToAvailableSlot(this, quantity);
            if (added)
            {
                QuestManager questManager = inventory.GetComponent<QuestManager>();
                questManager.CompleteObjective(m_quest, m_objective);
            }
        }
    }
}
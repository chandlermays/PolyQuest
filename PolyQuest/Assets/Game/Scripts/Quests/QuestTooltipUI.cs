using TMPro;
using UnityEngine;

namespace PolyQuest
{
    public class QuestTooltipUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_title;
        [SerializeField] private Transform m_objectives;
        [SerializeField] private GameObject m_completedObjectivePrefab;
        [SerializeField] private GameObject m_incompleteObjectivePrefab;
        [SerializeField] private TextMeshProUGUI m_rewardText;

        /*--------------------------------------------------------
        | --- Setup: Initialize the quest tooltip UI element --- |
        --------------------------------------------------------*/
        public void Setup(QuestStatus status)
        {
            Quest quest = status.GetQuest();
            m_title.text = quest.GetTitle();
            m_objectives.DetachChildren();

            foreach (Quest.Objective objective in quest.GetObjectives())
            {
                GameObject objPrefab = m_incompleteObjectivePrefab;
                if (status.IsObjectiveComplete(objective.m_identifier))
                {
                    objPrefab = m_completedObjectivePrefab;
                }

                GameObject objInstance = Instantiate(objPrefab, m_objectives);
                TextMeshProUGUI objectiveText = objInstance.GetComponentInChildren<TextMeshProUGUI>();
                objectiveText.text = objective.m_description;
            }

            m_rewardText.text = GetRewardText(quest);
        }

        /*---------------------------------------------------------------
        | --- GetRewardText: Format the reward text for the tooltip --- |
        ---------------------------------------------------------------*/
        private string GetRewardText(Quest quest)
        {
            string rewardText = "";

            foreach (Quest.Reward reward in quest.GetRewards())
            {
                if (rewardText != "")
                {
                    rewardText += ", ";
                }
                if (reward.m_amount > 1)
                {
                    rewardText += reward.m_amount + " ";
                }
                rewardText += reward.m_item.GetName();
            }

            if (rewardText == "")
            {
                rewardText = "No reward";
            }

            rewardText += ".";
            return rewardText;
        }
    }
}
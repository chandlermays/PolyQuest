using TMPro;
using UnityEngine;
//---------------------------------
using PolyQuest.Quests;

namespace PolyQuest.UI.Tooltip
{
    /* --------------------------------------------------------------------------------------------
     * Role: Displays quest information, objectives, and rewards in a tooltip UI element.          *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Shows the quest title, objectives (completed/incomplete), and rewards.               *
     *      - Formats and updates the tooltip UI based on the current quest status.                *
     *      - Provides a setup method to initialize the tooltip with quest data.                   *
     * ------------------------------------------------------------------------------------------- */
    public class QuestTooltipUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_title;
        [SerializeField] private Transform m_objectives;
        [SerializeField] private GameObject m_completedObjectivePrefab;
        [SerializeField] private GameObject m_incompleteObjectivePrefab;
        [SerializeField] private TextMeshProUGUI m_rewardText;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_title, nameof(m_title));
            Utilities.CheckForNull(m_objectives, nameof(m_objectives));
            Utilities.CheckForNull(m_completedObjectivePrefab, nameof(m_completedObjectivePrefab));
            Utilities.CheckForNull(m_incompleteObjectivePrefab, nameof(m_incompleteObjectivePrefab));
            Utilities.CheckForNull(m_rewardText, nameof(m_rewardText));
        }

        /*--------------------------------------------------------
        | --- Setup: Initialize the quest tooltip UI element --- |
        --------------------------------------------------------*/
        public void Setup(QuestStatus status)
        {
            Quest quest = status.Quest;
            m_title.text = quest.Title;
            
            foreach (Transform item in m_objectives)
            {
                Destroy(item.gameObject);
            }

            foreach (QuestObjective objective in quest.Objectives)
            {
                GameObject objPrefab = status.IsObjectiveComplete(objective)
                    ? m_completedObjectivePrefab
                    : m_incompleteObjectivePrefab;

                GameObject objInstance = Instantiate(objPrefab, m_objectives);
                objInstance.GetComponentInChildren<TextMeshProUGUI>().text = objective.Description;
            }

            m_rewardText.text = GetRewardText(quest);
        }

        /*---------------------------------------------------------------
        | --- GetRewardText: Format the reward text for the tooltip --- |
        ---------------------------------------------------------------*/
        private string GetRewardText(Quest quest)
        {
            string rewardText = "";

            foreach (Quest.Reward reward in quest.Rewards)
            {
                if (rewardText != "")
                {
                    rewardText += ", ";
                }
                if (reward.Amount > 1)
                {
                    rewardText += reward.Amount + " ";
                }
                rewardText += reward.Item.Name;
            }

            if (rewardText == "")
            {
                rewardText = "No reward";
            }

            return rewardText;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Saving;
using PolyQuest.Tools;

namespace PolyQuest.Quests
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Manages all active quests and their progress for the player, acting as the central    *
     *       quest system component in the game.                                                   *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Tracks all active quests and their statuses for the player.                          *
     *      - Tracks all completed quests to prevent re-assignment.                                *
     *      - Adds new quests and checks for quest existence.                                      *
     *      - Handles objective completion and triggers reward distribution when quests complete.  *
     *      - Provides access to quest statuses for UI and other systems.                          *
     *      - Supports saving and restoring quest progress for persistence.                        *
     *      - Implements condition checking for quest-related predicates (e.g., for dialogue).     *
     *      - Raises update events to notify UI or other systems of quest changes.                 *
     * ------------------------------------------------------------------------------------------- */
    public class QuestManager : MonoBehaviour, ISaveable, IConditionChecker
    {
        private readonly Dictionary<Quest, QuestStatus> m_activeQuests = new();
        private readonly HashSet<string> m_completedQuests = new();

        [System.Serializable]
        private class QuestManagerSaveData
        {
            public List<object> activeQuests;
            public List<string> completedQuests;
        }

        public IEnumerable<QuestStatus> ActiveQuests => m_activeQuests.Values;

        public event Action OnUpdate;

        /*--------------------------------------------------
        | --- AddQuest: Add a new quest to the manager --- |
        --------------------------------------------------*/
        public void AddQuest(Quest quest)
        {
            // Don't add if already completed
            if (IsQuestCompleted(quest))
            {
                Debug.Log($"Quest '{quest.Title}' has already been completed and cannot be added again.");
                return;
            }

            // Don't add if already active
            if (m_activeQuests.ContainsKey(quest))
            {
                Debug.Log($"Quest '{quest.Title}' is already active.");
                return;
            }

            m_activeQuests[quest] = new QuestStatus(quest);
            OnUpdate?.Invoke();
        }

        /*-------------------------------------------------------
        | --- HasQuest: Check if the quest is already added --- |
        -------------------------------------------------------*/
        public bool HasQuest(Quest quest)
        {
            return GetQuestStatus(quest) != null;
        }

        /*---------------------------------------------------------------
        | --- IsQuestCompleted: Check if the quest has been completed --- |
        ---------------------------------------------------------------*/
        public bool IsQuestCompleted(Quest quest)
        {
            return quest != null && m_completedQuests.Contains(quest.name);
        }

        /*--------------------------------------------------------------------------------------------------
        | --- CompleteObjective: Mark an objective as complete and grants rewards if quest is complete --- |
        --------------------------------------------------------------------------------------------------*/
        public void CompleteObjective(Quest quest, string objective)
        {
            QuestStatus status = GetQuestStatus(quest);
            if (status == null)
                return;

            status.CompleteObjective(objective);

            if (status.IsQuestComplete())
            {
                GrantReward(quest);
                m_completedQuests.Add(quest.name);
                m_activeQuests.Remove(quest);
            }

            OnUpdate?.Invoke();
        }

        /*-------------------------------------------------------------------
        | --- CaptureState: Save the Current State of the Quest Manager --- |
        -------------------------------------------------------------------*/
        public object CaptureState()
        {
            QuestManagerSaveData saveData = new()
            {
                activeQuests = new List<object>(),
                completedQuests = new List<string>(m_completedQuests)
            };

            foreach (var pair in m_activeQuests)
            {
                saveData.activeQuests.Add(pair.Value.CaptureState());
            }

            return saveData;
        }

        /*-------------------------------------------------------------------
        | --- RestoreState: Load the Current State of the Quest Manager --- |
        -------------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            QuestManagerSaveData saveData = (QuestManagerSaveData)state;

            m_activeQuests.Clear();
            m_completedQuests.Clear();

            // Restore active quests
            foreach (object objectState in saveData.activeQuests)
            {
                QuestStatus status = new(objectState);
                Quest quest = status.Quest;
                if (quest != null)
                {
                    m_activeQuests[quest] = status;
                }
            }

            // Restore completed quests
            if (saveData.completedQuests != null)
            {
                foreach (string questName in saveData.completedQuests)
                {
                    m_completedQuests.Add(questName);
                }
            }

            OnUpdate?.Invoke();
        }

        /*-------------------------------------------------------------------
        | --- Evaluate: Check if the quest manager has a specific quest --- |
        -------------------------------------------------------------------*/
        public bool? Evaluate(PredicateType predicate, string[] parameters)
        {
            switch (predicate)
            {
                case PredicateType.kCompletedObjective:

                    QuestStatus questStatus = GetQuestStatus(Quest.GetByName(parameters[0]));
                    if (questStatus == null)
                        return false;

                    return questStatus.IsObjectiveComplete(parameters[1]);

                case PredicateType.kHasQuest:
                    return HasQuest(Quest.GetByName(parameters[0]));

                case PredicateType.kDoesNotHaveQuest:
                    return !HasQuest(Quest.GetByName(parameters[0]));

                case PredicateType.kCompletedQuest:
                    return IsQuestCompleted(Quest.GetByName(parameters[0]));

                default:
                    return null;
            }
        }

        /*-----------------------------------------------------------------
        | --- GetQuestStatus: Retrieve the status of a specific quest --- |
        -----------------------------------------------------------------*/
        private QuestStatus GetQuestStatus(Quest quest)
        {
            m_activeQuests.TryGetValue(quest, out var status);
            return status;
        }

        /*------------------------------------------------------------------
        | --- GrantReward: Distribute the rewards of a completed quest --- |
        ------------------------------------------------------------------*/
        private void GrantReward(Quest quest)
        {
            foreach (Quest.Reward reward in quest.Rewards)
            {
                bool success = GetComponent<Inventory>().TryAddToAvailableSlot(reward.Item, reward.Amount);
                if (!success)
                {
                    GetComponent<ItemDropper>().DropItem(reward.Item, reward.Amount);
                }
            }
        }
    }
}
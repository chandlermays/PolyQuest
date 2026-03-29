using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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

        private const string kActiveQuestsKey = "ActiveQuests";
        private const string kCompletedQuestsKey = "CompletedQuests";

        private readonly Queue<Quest> m_pendingCompletionNotifs = new();
        private readonly Queue<Quest> m_pendingRewards = new();

        public IEnumerable<QuestStatus> ActiveQuests => m_activeQuests.Values;

        public event Action<Quest> OnQuestCompleted;
        public event Action OnQuestsUpdate;

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
            OnQuestsUpdate?.Invoke();
        }

        /*-------------------------------------------------------
        | --- HasQuest: Check if the quest is already added --- |
        -------------------------------------------------------*/
        public bool HasQuest(Quest quest)
        {
            return GetQuestStatus(quest) != null;
        }

        /*-----------------------------------------------------------------
        | --- IsQuestCompleted: Check if the quest has been completed --- |
        -----------------------------------------------------------------*/
        public bool IsQuestCompleted(Quest quest)
        {
            return quest != null && m_completedQuests.Contains(quest.name);
        }

        /*--------------------------------------------------------------------------------------------------
        | --- CompleteObjective: Mark an objective as complete and grants rewards if quest is complete --- |
        --------------------------------------------------------------------------------------------------*/
        public void CompleteObjective(Quest quest, QuestObjective objective)
        {
            QuestStatus status = GetQuestStatus(quest);
            if (status == null)
                return;

            status.CompleteObjective(objective);
            SaveManager.Instance.Save();        // Auto-save after completing an objective

            if (status.IsQuestComplete())
            {
                CompleteQuest(quest);
            }

            OnQuestsUpdate?.Invoke();
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------
        | --- FlushCompletionNotifs: Process any pending quest completion notifications to ensure they are raised at the appropriate time --- |
        -------------------------------------------------------------------------------------------------------------------------------------*/
        public void FlushCompletionNotifs()
        {
            while (m_pendingRewards.Count > 0)
            {
                GrantReward(m_pendingRewards.Dequeue());
            }

            while (m_pendingCompletionNotifs.Count > 0)
            {
                OnQuestCompleted?.Invoke(m_pendingCompletionNotifs.Dequeue());
            }
        }

        /*-------------------------------------------------------------------
        | --- CaptureState: Save the Current State of the Quest Manager --- |
        -------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            JObject state = new();

            // Capture active quests
            JArray activeQuestsArray = new();
            foreach (QuestStatus status in m_activeQuests.Values)
            {
                activeQuestsArray.Add(status.CaptureAsJToken());
            }
            state[kActiveQuestsKey] = activeQuestsArray;

            // Capture completed quests
            JArray completedQuestsArray = new JArray();
            foreach (string questName in m_completedQuests)
            {
                completedQuestsArray.Add(questName);
            }
            state[kCompletedQuestsKey] = completedQuestsArray;

            return state;
        }

        /*-------------------------------------------------------------------
        | --- RestoreState: Load the Current State of the Quest Manager --- |
        -------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state is JObject stateObject)
            {
                m_activeQuests.Clear();
                m_completedQuests.Clear();

                // Restore active quests
                if (stateObject[kActiveQuestsKey] is JArray activeQuestsArray)
                {
                    foreach (JToken token in activeQuestsArray)
                    {
                        QuestStatus status = new QuestStatus(token);
                        Quest quest = status.Quest;
                        if (quest != null)
                        {
                            m_activeQuests[quest] = status;
                        }
                    }
                }

                // Restore completed quests
                if (stateObject[kCompletedQuestsKey] is JArray completedQuestsArray)
                {
                    foreach (JToken token in completedQuestsArray)
                    {
                        string questName = token.ToString();
                        m_completedQuests.Add(questName);
                    }
                }

                OnQuestsUpdate?.Invoke();
            }
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

                    QuestObjective objective = QuestObjective.GetByName(parameters[1]);
                    if (objective == null)
                        return false;

                    return questStatus.IsObjectiveComplete(objective);

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

        /*------------------------------------------------------------------------------------------------------------
        | --- CompleteQuest: Mark a quest as complete, grant rewards, and update the manager's state accordingly --- |
        ------------------------------------------------------------------------------------------------------------*/
        private void CompleteQuest(Quest quest)
        {
            m_completedQuests.Add(quest.name);
            m_activeQuests.Remove(quest);
            m_pendingRewards.Enqueue(quest);
            m_pendingCompletionNotifs.Enqueue(quest);
            SaveManager.Instance.Save();
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
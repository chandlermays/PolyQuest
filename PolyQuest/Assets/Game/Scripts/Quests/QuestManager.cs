using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest
{
    /*------------------------------------------------------------------------------------
    | This is a component attached to the Player that manages all active quests.         |
    | It keeps track of the player's current quests using a list of QuestStatus objects. |
    | It handles adding new quests, marking objectives as complete, and granting         |
    | rewards upon quest completion.                                                     |
    ------------------------------------------------------------------------------------*/

    public class QuestManager : MonoBehaviour, ISaveable, IConditionChecker
    {
        private Dictionary<Quest, QuestStatus> m_questStatuses = new();
        public IEnumerable<QuestStatus> GetStatuses() => m_questStatuses.Values;

        public event Action OnUpdate;

        /*--------------------------------------------------
        | --- AddQuest: Add a new quest to the manager --- |
        --------------------------------------------------*/
        public void AddQuest(Quest quest)
        {
            if (m_questStatuses.ContainsKey(quest)) return;
            m_questStatuses[quest] = new QuestStatus(quest);
            OnUpdate?.Invoke();
        }

        /*-------------------------------------------------------
        | --- HasQuest: Check if the quest is already added --- |
        -------------------------------------------------------*/
        public bool HasQuest(Quest quest)
        {
            return GetQuestStatus(quest) != null;
        }

        /*--------------------------------------------------------------------------------------------------
        | --- CompleteObjective: Mark an objective as complete and grants rewards if quest is complete --- |
        --------------------------------------------------------------------------------------------------*/
        public void CompleteObjective(Quest quest, string objective)
        {
            // Log the quest and objective being checked
            Debug.Log($"Attempting to complete objective: Quest='{quest.GetTitle()}', Objective='{objective}'");

            QuestStatus status = GetQuestStatus(quest);

            // Ignore if we don't have the quest yet
            if (status == null)
            {
                Debug.LogWarning($"Quest '{quest.GetTitle()}' not found in active quests.");
                return;
            }

            // Mark the objective as complete
            status.CompleteObjective(objective);

            // Log whether the objective was successfully completed
            if (status.IsObjectiveComplete(objective))
            {
                Debug.Log($"Objective '{objective}' in Quest '{quest.GetTitle()}' marked as complete.");
            }
            else
            {
                Debug.LogWarning($"Failed to mark Objective '{objective}' in Quest '{quest.GetTitle()}' as complete.");
            }

            // Check if the quest is complete and grant rewards
            if (status.IsQuestComplete())
            {
                Debug.Log($"Quest '{quest.GetTitle()}' is complete. Granting rewards.");
                GrantReward(quest);
            }

            OnUpdate?.Invoke();
        }

        /*-----------------------------------------------------------------
        | --- GetQuestStatus: Retrieve the status of a specific quest --- |
        -----------------------------------------------------------------*/
        private QuestStatus GetQuestStatus(Quest quest)
        {
            m_questStatuses.TryGetValue(quest, out var status);
            return status;
        }

        /*------------------------------------------------------------------
        | --- GrantReward: Distribute the rewards of a completed quest --- |
        ------------------------------------------------------------------*/
        private void GrantReward(Quest quest)
        {
            foreach (Quest.Reward reward in quest.GetRewards())
            {
                Debug.Log($"Granting reward: Item='{reward.m_item.GetName()}', Amount={reward.m_amount}");

                // If the reward is not stackable
                if (!reward.m_item.IsStackable())
                {
                    int rewarded = 0;

                    // Fill all available slots
                    for (int i = 0; i < reward.m_amount; ++i)
                    {
                        bool success = GetComponent<Inventory>().AddItemToNextAvailableSlot(reward.m_item, 1);
                        if (success)
                        {
                            ++rewarded;
                            Debug.Log($"Successfully added 1 '{reward.m_item.GetName()}' to inventory. Total added: {rewarded}/{reward.m_amount}");
                        }
                        else
                        {
                            // GetComponent<ItemDropHandler>().DropItem(reward.m_item, reward.m_amount);
                            Debug.LogWarning($"Failed to add 1 '{reward.m_item.GetName()}' to inventory. Inventory might be full.");
                        }
                    }

                    // If all was given, proceed to the next
                    if (rewarded == reward.m_amount)
                    {
                        Debug.Log($"All '{reward.m_item.GetName()}' items successfully added to inventory.");
                        continue;
                    }

                    // Drop any remaining reward items that couldn't be added
                    for (int i = rewarded; i < reward.m_amount; ++i)
                    {
                        Debug.LogWarning($"Dropping '{reward.m_item.GetName()}' as inventory is full.");
                        // GetComponent<ItemDropHandler>().DropItem(reward.m_item, 1);
                    }
                }
                // If the reward is stackable
                else
                {
                    bool isRewarded = GetComponent<Inventory>().AddItemToNextAvailableSlot(reward.m_item, reward.m_amount);
                    if (isRewarded)
                    {
                        Debug.Log($"Successfully added {reward.m_amount} '{reward.m_item.GetName()}' to inventory.");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to add {reward.m_amount} '{reward.m_item.GetName()}' to inventory. Inventory might be full. Dropping items.");

                        for (int i = 0; i < reward.m_amount; ++i)
                        {
                            Debug.LogWarning($"Dropping '{reward.m_item.GetName()}' as inventory is full.");
                            // GetComponent<ItemDropHandler>().DropItem(reward.m_item, 1);
                        }
                    }
                }
            }
        }

        /*-------------------------------------------------------------------
        | --- CaptureState: Save the Current State of the Quest Manager --- |
        -------------------------------------------------------------------*/
        public object CaptureState()
        {
            List<object> state = new();
            foreach (var pair in m_questStatuses)
            {
                state.Add(pair.Value.CaptureState());
            }
            return state;
        }

        /*-------------------------------------------------------------------
        | --- RestoreState: Load the Current State of the Quest Manager --- |
        -------------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            if (state is not List<object> stateList)
                return;

            m_questStatuses.Clear();

            foreach (object objectState in stateList)
            {
                QuestStatus status = new(objectState);
                Quest quest = status.GetQuest();
                if (quest != null)
                {
                    m_questStatuses[quest] = status;
                }
            }

            OnUpdate?.Invoke();
        }

        /*-------------------------------------------------------------------
        | --- Evaluate: Check if the quest manager has a specific quest --- |
        -------------------------------------------------------------------*/
        public bool? Evaluate(string predicate, string[] parameters)
        {
            switch (predicate)
            {
                case "HasCompletedObjective":
                    // Ensure parameters are valid
                    if (parameters.Length < 2)
                        return null;

                    string questName = parameters[0];
                    string objective = parameters[1];

                    // Find the quest by name
                    Quest quest = Quest.GetByName(questName);
                    if (quest == null)
                    {
                        Debug.Log("Quest not found: " + questName);
                        return false;
                    }

                    // Get the quest status
                    QuestStatus status = GetQuestStatus(quest);
                    if (status == null)
                    {
                        Debug.Log("Quest status not found for: " + questName);
                        return false;
                    }

                    // Check if the objective is complete
                    bool isComplete = status.IsObjectiveComplete(objective);
                    Debug.Log($"Evaluating condition: Quest={questName}, Objective={objective}, IsComplete={isComplete}");
                    return isComplete;

                case "HasQuest":
                    return HasQuest(Quest.GetByName(parameters[0]));

                case "DoesNotHaveQuest":
                    return !HasQuest(Quest.GetByName(parameters[0]));

                default:
                    return null;
            }
        }
    }
}
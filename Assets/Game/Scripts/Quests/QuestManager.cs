/*---------------------------
| File: QuestManager.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Pickups;
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
     *      - Adds new quests and catches up objectives the world already satisfies.               *
     *      - Completes objectives (and the ones they imply), and completes quests in one step:    *
     *        mark complete, grant rewards, raise the event, then autosave.                        *
     *      - Re-checks active objectives when the world changes (flags, inventory, quests, load). *
     *      - Supports saving and restoring quest progress for persistence.                        *
     *      - Implements condition checking for quest-related conditions (e.g., for dialogue).     *
     *      - Raises update events to notify UI or other systems of quest changes.                 *
     * ------------------------------------------------------------------------------------------- */
    public class QuestManager : MonoBehaviour, ISaveable, IConditionChecker
    {
        private readonly Dictionary<Quest, QuestStatus> m_activeQuests = new();
        private readonly HashSet<string> m_completedQuests = new();

        private const string kActiveQuestsKey = "ActiveQuests";
        private const string kCompletedQuestsKey = "CompletedQuests";

        private WorldFlags m_worldFlags;
        private Inventory m_inventory;
        private bool m_isRechecking = false;
        private int m_completionDepth = 0;          // Inside CompleteObjective: re-checks wait until the step is done

        public IEnumerable<QuestStatus> ActiveQuests => m_activeQuests.Values;

        public event Action<Quest> OnQuestCompleted;
        public event Action<Quest> OnQuestStarted;
        public event Action OnQuestsUpdate;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            // The world changing can satisfy an active objective: re-check when it does
            m_worldFlags = GetComponent<WorldFlags>();
            if (m_worldFlags != null)
                m_worldFlags.OnFlagSet += HandleFlagSet;

            m_inventory = GetComponent<Inventory>();
            if (m_inventory != null)
                m_inventory.OnInventoryChanged += RecheckActiveObjectives;

            SavingSystem.RestoreFinished += HandleRestoreFinished;
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            if (m_worldFlags != null)
                m_worldFlags.OnFlagSet -= HandleFlagSet;

            if (m_inventory != null)
                m_inventory.OnInventoryChanged -= RecheckActiveObjectives;

            SavingSystem.RestoreFinished -= HandleRestoreFinished;
        }

        /*--------------------------------------------------
        | --- AddQuest: Add a new quest to the manager --- |
        --------------------------------------------------*/
        public void AddQuest(Quest quest)
        {
            if (quest == null)
            {
                WarnInEditor($"QuestManager on '{name}': AddQuest was given no quest, so nothing was added. Check the assigner or dialogue event that called it.");
                return;
            }

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
            OnQuestStarted?.Invoke(quest);
            OnQuestsUpdate?.Invoke();

            // Catch up: objectives the world already satisfies complete now, after "started"
            RecheckActiveObjectives();
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

        /*---------------------------------------------------------------------------------------------------------------
        | --- CompleteObjective: Marks an objective (and its Also Completes) complete, completing the quest if done --- |
        ---------------------------------------------------------------------------------------------------------------*/
        public void CompleteObjective(Quest quest, QuestObjective objective)
        {
            if (quest == null || objective == null)
            {
                WarnInEditor($"QuestManager on '{name}': CompleteObjective was given no {(quest == null ? "quest" : "objective")}, so nothing was completed. Check the trigger, item or dialogue event that called it.");
                return;
            }

            // Not active (not received yet, or already finished): nothing to do. The world state the event left
            // behind (a flag, an item) completes the objective through its condition once the quest is received.
            QuestStatus status = GetQuestStatus(quest);
            if (status == null)
                return;

            ++m_completionDepth;
            try
            {
                if (!CompleteWithAlsoCompletes(status, objective))
                    return;

                if (status.IsQuestComplete())
                {
                    CompleteQuest(quest);
                }

                SaveManager.Instance.AutoSave();        // Auto-save once the step is done, rewards included
                OnQuestsUpdate?.Invoke();
            }
            finally
            {
                --m_completionDepth;
            }

            // Quest state (and possibly the inventory, through rewards) changed: other objectives may now pass
            RecheckActiveObjectives();
        }

        /*----------------------------------------------------------------------------------------------------------------
        | --- RecheckActiveObjectives: Completes active, non-turn-in objectives whose Complete When condition passes --- |
        ----------------------------------------------------------------------------------------------------------------*/
        public void RecheckActiveObjectives()
        {
            // Already re-checking (its loop picks the change up), mid-step (CompleteObjective re-checks when it's
            // done), or mid-load (SavingSystem.RestoreFinished re-checks once the whole load is done)
            if (m_isRechecking || m_completionDepth > 0 || SavingSystem.IsRestoring)
                return;

            m_isRechecking = true;
            try
            {
                // Completing one objective can satisfy another (rewards, Also Completes): loop until a pass adds nothing
                bool progressed = true;
                while (progressed)
                {
                    progressed = false;
                    IConditionChecker[] checkers = GetComponents<IConditionChecker>();

                    foreach (QuestStatus status in m_activeQuests.Values.ToList())
                    {
                        if (!m_activeQuests.ContainsKey(status.Quest))
                            continue;

                        foreach (QuestObjective objective in status.Quest.Objectives)
                        {
                            if (objective == null || status.IsObjectiveComplete(objective))
                                continue;

                            // One way: a condition that later turns false never reverts a completed objective
                            if (!objective.IsCompleteWhenMet(checkers))
                                continue;

                            CompleteObjective(status.Quest, objective);
                            progressed = true;
                            break;
                        }
                    }
                }
            }
            finally
            {
                m_isRechecking = false;
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

        /*--------------------------------------------------------------------------------------------------
        | --- RestoreState: Load the quests; a quest that can't be read or no longer exists is skipped --- |
        --------------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            // The whole save is the wrong shape: ignore it, keep the current quests, and raise nothing
            if (state is not JObject stateObject)
                return;

            m_activeQuests.Clear();
            m_completedQuests.Clear();

            // Restore active quests
            if (stateObject[kActiveQuestsKey] is JArray activeQuestsArray)
            {
                foreach (JToken token in activeQuestsArray)
                {
                    QuestStatus status = QuestStatus.Restore(token);
                    if (status == null)
                    {
                        WarnInEditor($"QuestManager on '{name}': skipped a saved active quest that can't be read or no longer exists ({token["QuestName"]}). The other quests loaded.");
                        continue;
                    }

                    m_activeQuests[status.Quest] = status;
                }
            }

            // Restore completed quests: a quest gained objectives in a patch stays completed
            if (stateObject[kCompletedQuestsKey] is JArray completedQuestsArray)
            {
                foreach (JToken token in completedQuestsArray)
                {
                    string questName = token.Type == JTokenType.String ? token.Value<string>() : null;
                    if (Quest.GetByName(questName) == null)
                    {
                        WarnInEditor($"QuestManager on '{name}': skipped saved completed quest '{token}' because no quest has that name. The other quests loaded.");
                        continue;
                    }

                    m_completedQuests.Add(questName);
                }
            }

            OnQuestsUpdate?.Invoke();
        }

        /*-------------------------------------------------------------------------------------------------------
        | --- Evaluate: Answers quest conditions; unknown names or missing parameters are unanswered (null) --- |
        -------------------------------------------------------------------------------------------------------*/
        public bool? Evaluate(ConditionType predicate, string[] parameters)
        {
            if (predicate != ConditionType.kHasQuest && predicate != ConditionType.kCompletedQuest && predicate != ConditionType.kCompletedObjective)
                return null;

            if (parameters == null || parameters.Length == 0 || string.IsNullOrWhiteSpace(parameters[0]))
                return null;

            Quest quest = Quest.GetByName(parameters[0]);
            if (quest == null)
                return null;

            switch (predicate)
            {
                // Active: received and not yet finished
                case ConditionType.kHasQuest:
                    return HasQuest(quest);

                // Finished
                case ConditionType.kCompletedQuest:
                    return IsQuestCompleted(quest);

                // Done, including after the quest itself is finished
                case ConditionType.kCompletedObjective:
                    {
                        if (parameters.Length < 2 || string.IsNullOrWhiteSpace(parameters[1]))
                            return null;

                        QuestObjective objective = quest.Objectives.FirstOrDefault(o => o != null && o.name == parameters[1]);
                        if (objective == null)
                            return null;

                        if (IsQuestCompleted(quest))
                            return true;

                        QuestStatus status = GetQuestStatus(quest);
                        return status != null && status.IsObjectiveComplete(objective);
                    }

                default:
                    return null;
            }
        }

        /*-----------------------------------------------------------------------------------------------------------------------
        | --- CompleteWithAlsoCompletes: Completes the objective and, transitively, the ones it also completes in its quest --- |
        -----------------------------------------------------------------------------------------------------------------------*/
        private static bool CompleteWithAlsoCompletes(QuestStatus status, QuestObjective objective)
        {
            bool completedAny = false;
            Stack<QuestObjective> toComplete = new();
            toComplete.Push(objective);

            while (toComplete.Count > 0)
            {
                QuestObjective next = toComplete.Pop();
                if (!status.CompleteObjective(next))
                    continue;       // already done, or not part of this quest

                completedAny = true;
                foreach (QuestObjective alsoCompleted in next.AlsoCompletes)
                {
                    if (alsoCompleted != null)
                        toComplete.Push(alsoCompleted);
                }
            }
            return completedAny;
        }

        /*------------------------------------------------------------------------------------------------------
        | --- CompleteQuest: In one step: mark the quest complete, grant its rewards, then raise the event --- |
        ------------------------------------------------------------------------------------------------------*/
        private void CompleteQuest(Quest quest)
        {
            m_completedQuests.Add(quest.name);
            m_activeQuests.Remove(quest);
            GrantReward(quest);
            OnQuestCompleted?.Invoke(quest);
        }

        /*-----------------------------------------------------------------
        | --- GetQuestStatus: Retrieve the status of a specific quest --- |
        -----------------------------------------------------------------*/
        private QuestStatus GetQuestStatus(Quest quest)
        {
            if (quest == null)
                return null;

            m_activeQuests.TryGetValue(quest, out var status);
            return status;
        }

        /*---------------------------------------------------------------------------------------------
        | --- GrantReward: Distribute the rewards; items that don't fit drop at the Player's feet --- |
        ---------------------------------------------------------------------------------------------*/
        private void GrantReward(Quest quest)
        {
            foreach (Quest.Reward reward in quest.Rewards)
            {
                if (reward.Item == null || reward.Amount <= 0)
                    continue;

                if (reward.Item is CurrencyItem)
                {
                    GetComponent<Wallet>().UpdateSilver(reward.Amount);
                    continue;
                }

                if (GetComponent<Inventory>().TryAddToAvailableSlot(reward.Item, reward.Amount))
                    continue;

                ItemDropper dropper = GetComponent<ItemDropper>();
                if (dropper == null)
                {
                    WarnInEditor($"QuestManager on '{name}': '{quest.Title}' rewards {reward.Amount} x {reward.Item.Name}, which didn't fit, and there's no ItemDropper to drop it.");
                    continue;
                }

                // A stack drops as one pickup; each copy of a non-stackable needs its own
                if (reward.Item.IsStackable)
                {
                    dropper.DropItem(reward.Item, reward.Amount);
                }
                else
                {
                    for (int i = 0; i < reward.Amount; ++i)
                        dropper.DropItem(reward.Item);
                }
            }
        }

        /*--------------------------------------------------------------------------------
        | --- HandleFlagSet: A fact about the world changed: re-check the objectives --- |
        --------------------------------------------------------------------------------*/
        private void HandleFlagSet(WorldFlag flag)
        {
            RecheckActiveObjectives();
        }

        /*----------------------------------------------------------------------------------------------------
        | --- HandleRestoreFinished: The whole load is done: objectives the world now satisfies complete --- |
        ----------------------------------------------------------------------------------------------------*/
        private void HandleRestoreFinished()
        {
            // A static event can outlive a manager that was destroyed without OnDestroy running
            if (this == null)
            {
                SavingSystem.RestoreFinished -= HandleRestoreFinished;
                return;
            }

            RecheckActiveObjectives();
        }

        /*-------------------------------------------
        | --- WarnInEditor: Editor-only warning --- |
        -------------------------------------------*/
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void WarnInEditor(string message)
        {
            Debug.LogWarning(message, this);
        }
    }
}
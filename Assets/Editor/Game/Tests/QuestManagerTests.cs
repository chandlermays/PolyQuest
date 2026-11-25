/*---------------------------
| File: QuestManagerTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Pickups;
using PolyQuest.Quests;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    public class QuestManagerTests
    {
        private const int kSilverReward = 50;
        private const int kPotionReward = 2;

        private InventoryTestKit m_kit;
        private QuestManager m_manager;
        private Inventory m_inventory;
        private Wallet m_wallet;
        private WorldFlags m_flags;

        private WorldFlag m_campFound, m_campCleared;
        private QuestObjective m_find, m_kill, m_return, m_unrelated;
        private Quest m_quest;
        private CurrencyItem m_silver;
        private ActionItem m_potion;

        private int m_started, m_completed, m_updated;

        /*---------------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for quest manager testing --- |
        ---------------------------------------------------------------------------------*/
        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            m_kit.IsolateSaves();

            m_silver = m_kit.Item<CurrencyItem>("silver", stackable: true);
            m_potion = m_kit.Item<ActionItem>("potion", stackable: true);

            m_find = Objective("Find the Camp");
            m_kill = Objective("Kill the Bandits");
            m_return = Objective("Return to the Captain");
            m_unrelated = Objective("Some Other Quest's Objective");

            // Finding and clearing the camp read the world; returning to the captain is a turn-in
            m_campFound = m_kit.Flag("camp_found");
            m_campCleared = m_kit.Flag("camp_cleared");
            InventoryTestKit.ConfigureObjective(m_find, InventoryTestKit.When(ConditionType.kWorldFlag, m_campFound.ID));
            InventoryTestKit.ConfigureObjective(m_kill, InventoryTestKit.When(ConditionType.kWorldFlag, m_campCleared.ID));
            InventoryTestKit.ConfigureObjective(m_return, isTurnIn: true);

            // Three unordered objectives, rewarding silver and a stack of potions
            m_quest = NewQuest("Clear the Camp", new[] { m_find, m_kill, m_return }, (m_silver, kSilverReward), (m_potion, kPotionReward));

            GameObject player = m_kit.NewObject("Player");
            m_inventory = m_kit.NewInventory(player, 4);
            m_wallet = player.AddComponent<Wallet>();
            m_flags = player.AddComponent<WorldFlags>();
            m_manager = player.AddComponent<QuestManager>();
            InventoryTestKit.InvokeAwake(m_manager);            // listens for flags and inventory changes

            m_started = m_completed = m_updated = 0;
            m_manager.OnQuestStarted += _ => ++m_started;
            m_manager.OnQuestCompleted += _ => ++m_completed;
            m_manager.OnQuestsUpdate += () => ++m_updated;
        }

        /*-------------------------------------------------------------------
        | --- TearDown: Release test fixtures and game object resources --- |
        -------------------------------------------------------------------*/
        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }


        /*-----------------
        | --- Helpers --- |
        -----------------*/
        /*----------------------------------------------------
        | --- Objective: Creates a named quest objective --- |
        ----------------------------------------------------*/
        private QuestObjective Objective(string name)
        {
            QuestObjective objective = m_kit.Track(ScriptableObject.CreateInstance<QuestObjective>());
            objective.name = name;
            return objective;
        }

        /*-------------------------------------------------------------------------
        | --- NewQuest: Creates a quest with the given objectives and rewards --- |
        -------------------------------------------------------------------------*/
        private Quest NewQuest(string name, QuestObjective[] objectives, params (InventoryItem item, int amount)[] rewards)
        {
            Quest quest = m_kit.Track(ScriptableObject.CreateInstance<Quest>());
            quest.name = name;

            SerializedObject so = new SerializedObject(quest);
            SerializedProperty objectiveList = so.FindProperty("m_objectives");
            objectiveList.arraySize = objectives.Length;
            for (int i = 0; i < objectives.Length; ++i)
                objectiveList.GetArrayElementAtIndex(i).objectReferenceValue = objectives[i];

            SerializedProperty rewardList = so.FindProperty("m_rewards");
            rewardList.arraySize = rewards.Length;
            for (int i = 0; i < rewards.Length; ++i)
            {
                SerializedProperty reward = rewardList.GetArrayElementAtIndex(i);
                reward.FindPropertyRelative("m_item").objectReferenceValue = rewards[i].item;
                reward.FindPropertyRelative("m_amount").intValue = rewards[i].amount;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return quest;
        }

        /*-------------------------------------------------------------------------
        | --- StatusOf: The quest's active status, or null if it isn't active --- |
        -------------------------------------------------------------------------*/
        private QuestStatus StatusOf(Quest quest) => m_manager.ActiveQuests.SingleOrDefault(s => s.Quest == quest);

        /*-------------------------------------------------------------
        | --- CompleteAll: Completes every objective of the quest --- |
        -------------------------------------------------------------*/
        private void CompleteAll(Quest quest)
        {
            foreach (QuestObjective objective in quest.Objectives)
                m_manager.CompleteObjective(quest, objective);
        }

        /*----------------------------------------------------------------------
        | --- Owned: Totals an item's quantity across every inventory slot --- |
        ----------------------------------------------------------------------*/
        private int Owned(InventoryItem item)
        {
            int total = 0;
            for (int i = 0; i < m_inventory.Size; ++i)
                if (m_inventory.GetItemAtSlot(i) == item) total += m_inventory.GetQuantityAtSlot(i);
            return total;
        }


        /*---------------------------
        | --- Acquiring a Quest --- |
        ---------------------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------------
        | --- AddQuest_MakesTheQuestActive_WithNoObjectivesComplete: AddQuest makes the quest active with none of its objectives complete --- |
        -------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("AddQuest makes the quest active with none of its objectives complete.")]
        public void AddQuest_MakesTheQuestActive_WithNoObjectivesComplete()
        {
            m_manager.AddQuest(m_quest);

            Assert.That(m_manager.HasQuest(m_quest), Is.True);
            Assert.That(m_manager.IsQuestCompleted(m_quest), Is.False);
            Assert.That(StatusOf(m_quest), Is.Not.Null);
            Assert.That(StatusOf(m_quest).CompletedObjectiveCount, Is.EqualTo(0));
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------
        | --- AddQuest_RaisesOnQuestStarted_InTheSameStep: AddQuest raises OnQuestStarted once, in the same step, and OnQuestsUpdate once --- |
        -------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("AddQuest raises OnQuestStarted once, in the same step, and OnQuestsUpdate once.")]
        public void AddQuest_RaisesOnQuestStarted_InTheSameStep()
        {
            m_manager.AddQuest(m_quest);

            Assert.That(m_started, Is.EqualTo(1));
            Assert.That(m_updated, Is.EqualTo(1));
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- AddQuest_WhenAlreadyActive_KeepsProgress_AndDoesNotStartItAgain: Adding a quest that is already active keeps its progress, keeps a single entry, and doesn't start it again --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Adding a quest that is already active keeps its progress, keeps a single entry, and doesn't start it again.")]
        public void AddQuest_WhenAlreadyActive_KeepsProgress_AndDoesNotStartItAgain()
        {
            m_manager.AddQuest(m_quest);
            m_manager.CompleteObjective(m_quest, m_find);
            int startedBefore = m_started;

            m_manager.AddQuest(m_quest);

            Assert.That(m_manager.ActiveQuests.Count(), Is.EqualTo(1));
            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_find), Is.True);
            Assert.That(m_started, Is.EqualTo(startedBefore));
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- AddQuest_AlreadyCompletedQuest_IsNotAcquiredAgain: A completed quest can't be acquired again: it stays completed, isn't active, isn't started again and pays no second reward --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A completed quest can't be acquired again: it stays completed, isn't active, isn't started again and pays no second reward.")]
        public void AddQuest_AlreadyCompletedQuest_IsNotAcquiredAgain()
        {
            m_manager.AddQuest(m_quest);
            CompleteAll(m_quest);
            int startedBefore = m_started;
            int silverBefore = m_wallet.CurrentSilver;

            m_manager.AddQuest(m_quest);

            Assert.That(m_manager.HasQuest(m_quest), Is.False);
            Assert.That(m_manager.IsQuestCompleted(m_quest), Is.True);
            Assert.That(m_started, Is.EqualTo(startedBefore));
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(silverBefore));
        }

        /*---------------------------------------------------------------------------------------------------------------------
        | --- AddQuest_Null_DoesNothing: AddQuest with no quest (a misconfigured assigner or dialogue event) does nothing --- |
        ---------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("AddQuest with no quest (a misconfigured assigner or dialogue event) does nothing, with an editor warning.")]
        public void AddQuest_Null_DoesNothing()
        {
            LogAssert.Expect(LogType.Warning, new Regex("AddQuest was given no quest"));

            Assert.DoesNotThrow(() => m_manager.AddQuest(null));

            Assert.That(m_manager.ActiveQuests, Is.Empty);
            Assert.That(m_started, Is.EqualTo(0));
        }


        /*-------------------------------
        | --- Completing Objectives --- |
        -------------------------------*/
        /*------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompleteObjective_MarksIt_AndTheQuestStaysActive: Completing one objective marks it complete; the quest stays active until every objective is done --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing one objective marks it complete; the quest stays active until every objective is done.")]
        public void CompleteObjective_MarksIt_AndTheQuestStaysActive()
        {
            m_manager.AddQuest(m_quest);

            m_manager.CompleteObjective(m_quest, m_find);

            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_find), Is.True);
            Assert.That(StatusOf(m_quest).CompletedObjectiveCount, Is.EqualTo(1));
            Assert.That(m_manager.HasQuest(m_quest), Is.True);
            Assert.That(m_manager.IsQuestCompleted(m_quest), Is.False);
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompleteObjective_InAnyOrder_CompletesTheQuestWhenAllAreDone: Objectives are unordered: completing them in any order completes the quest once all are done --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Objectives are unordered: completing them in any order completes the quest once all are done.")]
        public void CompleteObjective_InAnyOrder_CompletesTheQuestWhenAllAreDone()
        {
            m_manager.AddQuest(m_quest);

            m_manager.CompleteObjective(m_quest, m_return);
            m_manager.CompleteObjective(m_quest, m_kill);
            Assert.That(m_manager.IsQuestCompleted(m_quest), Is.False);

            m_manager.CompleteObjective(m_quest, m_find);

            Assert.That(m_manager.IsQuestCompleted(m_quest), Is.True);
            Assert.That(m_manager.HasQuest(m_quest), Is.False);
        }

        /*------------------------------------------------------------------------------------------------
        | --- CompleteObjective_Twice_CountsOnce: Completing the same objective twice counts it once --- |
        ------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing the same objective twice counts it once.")]
        public void CompleteObjective_Twice_CountsOnce()
        {
            m_manager.AddQuest(m_quest);

            m_manager.CompleteObjective(m_quest, m_find);
            m_manager.CompleteObjective(m_quest, m_find);

            Assert.That(StatusOf(m_quest).CompletedObjectiveCount, Is.EqualTo(1));
        }

        /*-------------------------------------------------------------------------------------------------------------
        | --- CompleteObjective_NotInTheQuest_IsIgnored: An objective that doesn't belong to the quest is ignored --- |
        -------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("An objective that doesn't belong to the quest is ignored.")]
        public void CompleteObjective_NotInTheQuest_IsIgnored()
        {
            m_manager.AddQuest(m_quest);

            m_manager.CompleteObjective(m_quest, m_unrelated);

            Assert.That(StatusOf(m_quest).CompletedObjectiveCount, Is.EqualTo(0));
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompleteObjective_NullQuestOrObjective_DoesNothing: CompleteObjective with no quest or no objective (a misconfigured trigger, quest item or dialogue event) does nothing --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("CompleteObjective with no quest or no objective (a misconfigured trigger, quest item or dialogue event) does nothing, with an editor warning.")]
        public void CompleteObjective_NullQuestOrObjective_DoesNothing()
        {
            m_manager.AddQuest(m_quest);
            LogAssert.Expect(LogType.Warning, new Regex("CompleteObjective was given no quest"));
            LogAssert.Expect(LogType.Warning, new Regex("CompleteObjective was given no objective"));

            Assert.DoesNotThrow(() => m_manager.CompleteObjective(null, m_find));
            Assert.DoesNotThrow(() => m_manager.CompleteObjective(m_quest, null));

            Assert.That(StatusOf(m_quest).CompletedObjectiveCount, Is.EqualTo(0));
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------
        | --- CompleteObjective_AutoSaves: Completing an objective autosaves: it writes the autosave and leaves the Player's own save alone --- |
        ---------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing an objective autosaves: it writes the autosave and leaves the Player's own save alone.")]
        public void CompleteObjective_AutoSaves()
        {
            m_manager.AddQuest(m_quest);

            m_manager.CompleteObjective(m_quest, m_find);

            Assert.That(InventoryTestKit.TestAutoSaveFileExists(), Is.True);
            Assert.That(InventoryTestKit.TestSaveFileExists(), Is.False, "an automatic save wrote the Player's own save");
        }


        /*------------------------------------
        | --- Work Done Before Acquiring --- |
        ------------------------------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- ObjectivesCompletedBeforeAcquiring_Count_AndTheTurnInIsStillNeeded: Objectives completed before the quest is acquired count once it is acquired; a quest with its turn-in still open stays active and waits for the turn-in --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Objectives the world already satisfies when the quest is acquired complete then; a quest with its turn-in still open stays active and waits for the turn-in.")]
        public void ObjectivesCompletedBeforeAcquiring_Count_AndTheTurnInIsStillNeeded()
        {
            m_flags.Set(m_campFound);
            m_flags.Set(m_campCleared);
            Assert.That(m_manager.HasQuest(m_quest), Is.False, "the world changing doesn't acquire the quest");

            m_manager.AddQuest(m_quest);

            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_find), Is.True);
            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_kill), Is.True);
            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_return), Is.False);
            Assert.That(m_manager.IsQuestCompleted(m_quest), Is.False);
        }

        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- QuestWithoutATurnIn_AllDoneBeforehand_CompletesWhenAcquired_StartedBeforeCompleted_PaidOnce: A quest without a turn-in whose objectives were all done beforehand completes when acquired: started is raised before completed, and the rewards are paid exactly once --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A quest without a turn-in whose objectives were all done beforehand completes when acquired: started is raised before completed, and the rewards are paid exactly once.")]
        public void QuestWithoutATurnIn_AllDoneBeforehand_CompletesWhenAcquired_StartedBeforeCompleted_PaidOnce()
        {
            Quest clearTheCamp = NewQuest("Clear the Camp, Done", new[] { m_find, m_kill }, (m_silver, kSilverReward), (m_potion, kPotionReward));
            var order = new System.Collections.Generic.List<string>();
            m_manager.OnQuestStarted += _ => order.Add("started");
            m_manager.OnQuestCompleted += _ => order.Add("completed");
            m_flags.Set(m_campFound);
            m_flags.Set(m_campCleared);

            m_manager.AddQuest(clearTheCamp);

            Assert.That(m_manager.IsQuestCompleted(clearTheCamp), Is.True);
            Assert.That(m_manager.HasQuest(clearTheCamp), Is.False);
            Assert.That(order, Is.EqualTo(new[] { "started", "completed" }));
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(kSilverReward));
            Assert.That(Owned(m_potion), Is.EqualTo(kPotionReward));
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompleteObjective_BeforeAcquiring_IsNotRemembered: Completing an objective of a quest that isn't active does nothing: only the world's state carries over --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing an objective of a quest that isn't active does nothing: only the world's state carries over to when the quest is acquired.")]
        public void CompleteObjective_BeforeAcquiring_IsNotRemembered()
        {
            m_manager.CompleteObjective(m_quest, m_return);

            m_manager.AddQuest(m_quest);

            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_return), Is.False);
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- WorldStateBeforeAcquiring_SurvivesSaveAndLoad: A fact recorded before the quest is acquired survives a save and load, and counts when the quest is acquired --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A fact recorded before the quest is acquired survives a save and load, and counts when the quest is acquired afterwards.")]
        public void WorldStateBeforeAcquiring_SurvivesSaveAndLoad()
        {
            m_flags.Set(m_campFound);
            JToken saved = JToken.Parse(m_flags.CaptureState().ToString());

            GameObject freshPlayer = m_kit.NewObject("FreshPlayer");
            m_kit.NewInventory(freshPlayer, 4);
            freshPlayer.AddComponent<Wallet>();
            WorldFlags freshFlags = freshPlayer.AddComponent<WorldFlags>();
            QuestManager freshManager = freshPlayer.AddComponent<QuestManager>();
            InventoryTestKit.InvokeAwake(freshManager);
            freshFlags.RestoreState(saved);

            freshManager.AddQuest(m_quest);

            QuestStatus status = freshManager.ActiveQuests.Single(s => s.Quest == m_quest);
            Assert.That(status.IsObjectiveComplete(m_find), Is.True);
            Assert.That(status.IsObjectiveComplete(m_kill), Is.False);
        }


        /*----------------------------
        | --- The World Changing --- |
        ----------------------------*/
        /*--------------------------------------------------------------------------------------------------------------------------------------------
        | --- SettingAFlag_WhileTheQuestIsActive_CompletesTheObjectiveThatReadsIt: Recording a fact completes the active objective that reads it --- |
        --------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Recording a fact completes the active objective that reads it, and only that one.")]
        public void SettingAFlag_WhileTheQuestIsActive_CompletesTheObjectiveThatReadsIt()
        {
            m_manager.AddQuest(m_quest);

            m_flags.Set(m_campFound);

            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_find), Is.True);
            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_kill), Is.False);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------------------
        | --- InventoryChange_WhileTheQuestIsActive_CompletesAnItemObjective: Getting the item an objective asks for completes it, however it's obtained --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Getting the item an objective asks for completes it, however it's obtained.")]
        public void InventoryChange_WhileTheQuestIsActive_CompletesAnItemObjective()
        {
            ActionItem sword = m_kit.Item<ActionItem>("sword");
            QuestObjective findTheSword = Objective("Find the Sword");
            InventoryTestKit.ConfigureObjective(findTheSword, InventoryTestKit.When(ConditionType.kHasItem, sword.ID));
            Quest heirloom = NewQuest("Heirloom", new[] { findTheSword, Objective("Return the Sword") });
            m_manager.AddQuest(heirloom);

            m_inventory.TryAddToAvailableSlot(sword);

            Assert.That(StatusOf(heirloom).IsObjectiveComplete(findTheSword), Is.True);
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompletedObjective_StaysComplete_WhenItsConditionTurnsFalse: Completion is one way: losing the item afterwards doesn't undo the objective --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completion is one way: losing the item afterwards doesn't undo the objective.")]
        public void CompletedObjective_StaysComplete_WhenItsConditionTurnsFalse()
        {
            ActionItem sword = m_kit.Item<ActionItem>("sword");
            QuestObjective findTheSword = Objective("Find the Sword");
            InventoryTestKit.ConfigureObjective(findTheSword, InventoryTestKit.When(ConditionType.kHasItem, sword.ID));
            Quest heirloom = NewQuest("Heirloom", new[] { findTheSword, Objective("Return the Sword") });
            m_manager.AddQuest(heirloom);
            m_inventory.TryAddToAvailableSlot(sword);

            m_inventory.RemoveItemsFromSlot(0, 1);

            Assert.That(StatusOf(heirloom).IsObjectiveComplete(findTheSword), Is.True);
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompletingAnObjective_AlsoCompletesTheObjectivesItLists: Completing an objective also completes the ones in its Also Completes (clearing the camp also completes finding it) --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing an objective also completes the ones in its Also Completes (clearing the camp also completes finding it), and nothing else.")]
        public void CompletingAnObjective_AlsoCompletesTheObjectivesItLists()
        {
            InventoryTestKit.ConfigureObjective(m_kill, InventoryTestKit.When(ConditionType.kWorldFlag, m_campCleared.ID), false, m_find);
            m_manager.AddQuest(m_quest);

            m_flags.Set(m_campCleared);

            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_kill), Is.True);
            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_find), Is.True);
            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_return), Is.False);
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- ARewardThatSatisfiesAnotherObjective_CompletesItInTheSameStep: A reward that another active quest's objective asks for completes that objective right away --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A reward that another active quest's objective asks for completes that objective right away.")]
        public void ARewardThatSatisfiesAnotherObjective_CompletesItInTheSameStep()
        {
            QuestObjective gatherPotions = Objective("Gather Potions");
            InventoryTestKit.ConfigureObjective(gatherPotions, InventoryTestKit.When(ConditionType.kHasItems, m_potion.ID, kPotionReward.ToString()));
            Quest apothecary = NewQuest("Apothecary", new[] { gatherPotions, Objective("Deliver the Potions") });
            m_manager.AddQuest(apothecary);
            m_manager.AddQuest(m_quest);

            CompleteAll(m_quest);

            Assert.That(StatusOf(apothecary).IsObjectiveComplete(gatherPotions), Is.True);
        }


        /*------------------------------
        | --- Completing the Quest --- |
        ------------------------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompletingTheQuest_GrantsItsRewards_InTheSameStep: Completing the last objective grants every reward in the same step: currency to the wallet, items to the inventory --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing the last objective grants every reward in the same step: currency to the wallet, items to the inventory.")]
        public void CompletingTheQuest_GrantsItsRewards_InTheSameStep()
        {
            m_manager.AddQuest(m_quest);

            CompleteAll(m_quest);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(kSilverReward));
            Assert.That(Owned(m_potion), Is.EqualTo(kPotionReward));
        }

        /*-----------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompletingTheQuest_RaisesOnQuestCompleted_InTheSameStep: Completing the last objective raises OnQuestCompleted once, in the same step --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing the last objective raises OnQuestCompleted once, in the same step.")]
        public void CompletingTheQuest_RaisesOnQuestCompleted_InTheSameStep()
        {
            m_manager.AddQuest(m_quest);

            CompleteAll(m_quest);

            Assert.That(m_completed, Is.EqualTo(1));
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompletingAnObjectiveAfterTheQuestIsDone_GrantsNothingMore: Completing an objective again after the quest is done (e.g. a turn-in node reached twice) pays nothing more and doesn't complete it again --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completing an objective again after the quest is done (e.g. a turn-in node reached twice) pays nothing more and doesn't complete it again.")]
        public void CompletingAnObjectiveAfterTheQuestIsDone_GrantsNothingMore()
        {
            m_manager.AddQuest(m_quest);
            CompleteAll(m_quest);
            int silverBefore = m_wallet.CurrentSilver;
            int potionsBefore = Owned(m_potion);
            int completedBefore = m_completed;

            m_manager.CompleteObjective(m_quest, m_return);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(silverBefore));
            Assert.That(Owned(m_potion), Is.EqualTo(potionsBefore));
            Assert.That(m_completed, Is.EqualTo(completedBefore));
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------
        | --- RewardWithNoItem_IsSkipped_TheOthersAreGranted: A reward entry with no item is skipped; the other rewards are still granted --- |
        -------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A reward entry with no item is skipped; the other rewards are still granted.")]
        public void RewardWithNoItem_IsSkipped_TheOthersAreGranted()
        {
            Quest quest = NewQuest("Unfinished Rewards", new[] { m_find }, (null, 1), (m_silver, kSilverReward));
            m_manager.AddQuest(quest);

            Assert.DoesNotThrow(() => m_manager.CompleteObjective(quest, m_find));

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(kSilverReward));
        }
    }
}
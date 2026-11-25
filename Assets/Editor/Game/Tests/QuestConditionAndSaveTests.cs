/*---------------------------
| File: QuestConditionAndSaveTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Quests;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    // Conditions and saves find quests and objectives by asset name through Resources, so these tests use the shipped quests:
    //   "A Family Heirloom":     Find the Sword, Return the Sword
    //   "Clear the Bandit Camp": Find the Bandit Camp, Kill the Bandits, Return to Captain Flint
    public class QuestConditionAndSaveTests
    {
        private const string kHeirloom = "A Family Heirloom";
        private const string kFindTheSword = "Find the Sword";
        private const string kReturnTheSword = "Return the Sword";
        private const string kBanditCamp = "Clear the Bandit Camp";

        private InventoryTestKit m_kit;
        private QuestManager m_manager;

        private Quest m_heirloom, m_banditCamp;
        private QuestObjective m_findTheSword, m_returnTheSword;

        /*--------------------------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for quest condition and save testing --- |
        --------------------------------------------------------------------------------------------*/
        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            m_kit.IsolateSaves();

            m_heirloom = Quest.GetByName(kHeirloom);
            m_banditCamp = Quest.GetByName(kBanditCamp);
            Assert.That(m_heirloom, Is.Not.Null, $"fixture: the '{kHeirloom}' quest asset is missing from Resources");
            Assert.That(m_banditCamp, Is.Not.Null, $"fixture: the '{kBanditCamp}' quest asset is missing from Resources");
            m_findTheSword = m_heirloom.Objectives.Single(o => o.name == kFindTheSword);
            m_returnTheSword = m_heirloom.Objectives.Single(o => o.name == kReturnTheSword);

            m_manager = NewPlayerQuestManager("Player");
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
        /*------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- NewPlayerQuestManager: Creates a Player QuestManager with an inventory and wallet, since completing a shipped quest pays its shipped rewards --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------*/
        private QuestManager NewPlayerQuestManager(string name)
        {
            GameObject player = m_kit.NewObject(name);
            m_kit.NewInventory(player, 8);
            player.AddComponent<Wallet>();
            return player.AddComponent<QuestManager>();
        }

        /*-------------------------------------------------------------
        | --- CompleteAll: Completes every objective of the quest --- |
        -------------------------------------------------------------*/
        private void CompleteAll(QuestManager manager, Quest quest)
        {
            foreach (QuestObjective objective in quest.Objectives)
                manager.CompleteObjective(quest, objective);
        }

        /*---------------------------------------------------------------------------
        | --- Evaluate: Evaluates a condition against the Player's QuestManager --- |
        ---------------------------------------------------------------------------*/
        private bool? Evaluate(ConditionType condition, params string[] parameters) => m_manager.Evaluate(condition, parameters);

        /*-------------------------------------------------------------------------
        | --- StatusOf: The quest's active status, or null if it isn't active --- |
        -------------------------------------------------------------------------*/
        private static QuestStatus StatusOf(QuestManager manager, Quest quest) => manager.ActiveQuests.SingleOrDefault(s => s.Quest == quest);

        /*--------------------------------------------------------------------------------------
        | --- ActiveEntry: Builds a saved active-quest entry with its completed objectives --- |
        --------------------------------------------------------------------------------------*/
        private static JObject ActiveEntry(string questName, params string[] completedObjectives)
            => new JObject { ["QuestName"] = questName, ["CompletedObjectives"] = new JArray(completedObjectives) };

        /*------------------------------------------------------------------------------------------------
        | --- State: Builds a saved QuestManager state from active entries and completed quest names --- |
        ------------------------------------------------------------------------------------------------*/
        private static JObject State(JObject[] active, params string[] completed)
            => new JObject { ["ActiveQuests"] = new JArray(active), ["CompletedQuests"] = new JArray(completed) };


        /*--------------------
        | --- Conditions --- |
        --------------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- HasQuest_IsTrueOnlyWhileTheQuestIsActive: kHasQuest is true only while the quest is active: not before it is acquired and not after it is completed --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("kHasQuest is true only while the quest is active: not before it is acquired and not after it is completed.")]
        public void HasQuest_IsTrueOnlyWhileTheQuestIsActive()
        {
            Assert.That(Evaluate(ConditionType.kHasQuest, kHeirloom), Is.False);

            m_manager.AddQuest(m_heirloom);
            Assert.That(Evaluate(ConditionType.kHasQuest, kHeirloom), Is.True);

            CompleteAll(m_manager, m_heirloom);
            Assert.That(Evaluate(ConditionType.kHasQuest, kHeirloom), Is.False);
        }

        /*---------------------------------------------------------------------------------------------------------------------------
        | --- CompletedQuest_BecomesTrueWhenTheQuestCompletes: kCompletedQuest is false until the quest is completed, then true --- |
        ---------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("kCompletedQuest is false until the quest is completed, then true.")]
        public void CompletedQuest_BecomesTrueWhenTheQuestCompletes()
        {
            m_manager.AddQuest(m_heirloom);
            Assert.That(Evaluate(ConditionType.kCompletedQuest, kHeirloom), Is.False);

            CompleteAll(m_manager, m_heirloom);

            Assert.That(Evaluate(ConditionType.kCompletedQuest, kHeirloom), Is.True);
        }

        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompletedObjective_IsTrueOnceThatObjectiveIsDone: kCompletedObjective is false until that objective is done, then true; other objectives are unaffected --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("kCompletedObjective is false until that objective is done, then true; other objectives are unaffected.")]
        public void CompletedObjective_IsTrueOnceThatObjectiveIsDone()
        {
            m_manager.AddQuest(m_heirloom);
            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, kFindTheSword), Is.False);

            m_manager.CompleteObjective(m_heirloom, m_findTheSword);

            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, kFindTheSword), Is.True);
            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, kReturnTheSword), Is.False);
        }

        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompletedObjective_StaysTrueAfterTheQuestIsCompleted: Completed objectives are permanent history: kCompletedObjective stays true after the quest is completed --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Completed objectives are permanent history: kCompletedObjective stays true after the quest is completed.")]
        public void CompletedObjective_StaysTrueAfterTheQuestIsCompleted()
        {
            m_manager.AddQuest(m_heirloom);

            CompleteAll(m_manager, m_heirloom);

            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, kFindTheSword), Is.True);
            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, kReturnTheSword), Is.True);
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- UnknownQuestName_IsUnanswered: A quest name that matches no quest asset is unanswered (null), so the condition fails closed even when negated --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [TestCase(ConditionType.kHasQuest)]
        [TestCase(ConditionType.kCompletedQuest)]
        [TestCase(ConditionType.kCompletedObjective)]
        [Description("A quest name that matches no quest asset is unanswered (null), so the condition fails closed even when negated.")]
        public void UnknownQuestName_IsUnanswered(ConditionType condition)
        {
            Assert.That(Evaluate(condition, "No Such Quest", kFindTheSword), Is.Null);
        }

        /*-------------------------------------------------------------------------------------------------------------------
        | --- UnknownObjectiveName_IsUnanswered: An objective name that matches no objective asset is unanswered (null) --- |
        -------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("An objective name that matches no objective asset is unanswered (null).")]
        public void UnknownObjectiveName_IsUnanswered()
        {
            m_manager.AddQuest(m_heirloom);

            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, "No Such Objective"), Is.Null);
        }

        /*-----------------------------------------------------------------------------------------------------------------
        | --- MissingParameters_AreUnanswered: A quest condition authored without its parameters is unanswered (null) --- |
        -----------------------------------------------------------------------------------------------------------------*/
        [TestCase(ConditionType.kHasQuest)]
        [TestCase(ConditionType.kCompletedQuest)]
        [TestCase(ConditionType.kCompletedObjective)]
        [Description("A quest condition authored without its parameters is unanswered (null).")]
        public void MissingParameters_AreUnanswered(ConditionType condition)
        {
            Assert.That(Evaluate(condition), Is.Null);
        }

        /*------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompletedObjective_WithoutAnObjectiveParameter_IsUnanswered: kCompletedObjective authored with a quest but no objective is unanswered (null) --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("kCompletedObjective authored with a quest but no objective is unanswered (null).")]
        public void CompletedObjective_WithoutAnObjectiveParameter_IsUnanswered()
        {
            m_manager.AddQuest(m_heirloom);

            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom), Is.Null);
        }

        /*----------------------------------------------------------------------------------------------------------------
        | --- NonQuestCondition_IsNotAnswered: Conditions that aren't about quests are left to other checkers (null) --- |
        ----------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Conditions that aren't about quests are left to other checkers (null).")]
        public void NonQuestCondition_IsNotAnswered()
        {
            Assert.That(Evaluate(ConditionType.kHasItem, "sword"), Is.Null);
        }


        /*----------------
        | --- Saving --- |
        ----------------*/
        /*------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CaptureThenRestore_IntoAFreshManager_RestoresProgressAndCompletedQuests: Captured state restored into a fresh QuestManager restores the active quests with their progress and the completed quests --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Captured state restored into a fresh QuestManager restores the active quests with their progress and the completed quests.")]
        public void CaptureThenRestore_IntoAFreshManager_RestoresProgressAndCompletedQuests()
        {
            m_manager.AddQuest(m_heirloom);
            m_manager.CompleteObjective(m_heirloom, m_findTheSword);
            m_manager.AddQuest(m_banditCamp);
            CompleteAll(m_manager, m_banditCamp);
            JToken saved = JToken.Parse(m_manager.CaptureState().ToString());

            QuestManager restored = NewPlayerQuestManager("FreshPlayer");
            restored.RestoreState(saved);

            Assert.That(restored.HasQuest(m_heirloom), Is.True);
            Assert.That(StatusOf(restored, m_heirloom).IsObjectiveComplete(m_findTheSword), Is.True);
            Assert.That(StatusOf(restored, m_heirloom).IsObjectiveComplete(m_returnTheSword), Is.False);
            Assert.That(restored.IsQuestCompleted(m_banditCamp), Is.True);
            Assert.That(restored.HasQuest(m_banditCamp), Is.False);
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_RaisesOnlyOnQuestsUpdate: RestoreState raises OnQuestsUpdate once and does not raise OnQuestStarted or OnQuestCompleted: loading isn't starting or finishing a quest --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("RestoreState raises OnQuestsUpdate once and does not raise OnQuestStarted or OnQuestCompleted: loading isn't starting or finishing a quest.")]
        public void RestoreState_RaisesOnlyOnQuestsUpdate()
        {
            QuestManager restored = NewPlayerQuestManager("FreshPlayer");
            int started = 0, completed = 0, updated = 0;
            restored.OnQuestStarted += _ => ++started;
            restored.OnQuestCompleted += _ => ++completed;
            restored.OnQuestsUpdate += () => ++updated;

            restored.RestoreState(State(new[] { ActiveEntry(kHeirloom, kFindTheSword) }, kBanditCamp));

            Assert.That(updated, Is.EqualTo(1));
            Assert.That(started, Is.EqualTo(0));
            Assert.That(completed, Is.EqualTo(0));
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_ReplacesTheCurrentQuests: RestoreState replaces the current quests; quests absent from the save are no longer active --- |
        -------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("RestoreState replaces the current quests; quests absent from the save are no longer active.")]
        public void RestoreState_ReplacesTheCurrentQuests()
        {
            m_manager.AddQuest(m_banditCamp);

            m_manager.RestoreState(State(new[] { ActiveEntry(kHeirloom) }));

            Assert.That(m_manager.HasQuest(m_banditCamp), Is.False);
            Assert.That(m_manager.HasQuest(m_heirloom), Is.True);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_UnknownQuest_IsSkipped_TheRestLoad: A saved active quest whose asset no longer exists is skipped; the other saved quests still load --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A saved active quest whose asset no longer exists is skipped; the other saved quests still load.")]
        public void RestoreState_UnknownQuest_IsSkipped_TheRestLoad()
        {
            JObject state = State(new[] { ActiveEntry("A Quest That Was Removed", "Some Objective"), ActiveEntry(kHeirloom, kFindTheSword) }, kBanditCamp);

            Assert.DoesNotThrow(() => m_manager.RestoreState(state));

            Assert.That(m_manager.ActiveQuests.Count(), Is.EqualTo(1));
            Assert.That(StatusOf(m_manager, m_heirloom).IsObjectiveComplete(m_findTheSword), Is.True);
            Assert.That(m_manager.IsQuestCompleted(m_banditCamp), Is.True);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_UnknownObjective_IsSkipped_TheRestLoad: A saved completed objective that no longer belongs to its quest is skipped; the quest's other saved objectives still load --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A saved completed objective that no longer belongs to its quest is skipped; the quest's other saved objectives still load.")]
        public void RestoreState_UnknownObjective_IsSkipped_TheRestLoad()
        {
            m_manager.RestoreState(State(new[] { ActiveEntry(kHeirloom, "An Objective That Was Removed", kFindTheSword) }));

            QuestStatus status = StatusOf(m_manager, m_heirloom);
            Assert.That(status.IsObjectiveComplete(m_findTheSword), Is.True);
            Assert.That(status.CompletedObjectiveCount, Is.EqualTo(1));
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_NonObject_IsIgnored: A null or non-object state is ignored, as if the save never held quests: current quests are kept and no event fires --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A null or non-object state is ignored, as if the save never held quests: current quests are kept and no event fires.")]
        public void RestoreState_NonObject_IsIgnored()
        {
            m_manager.AddQuest(m_heirloom);
            int updated = 0;
            m_manager.OnQuestsUpdate += () => ++updated;

            m_manager.RestoreState(JValue.CreateNull());
            m_manager.RestoreState(new JArray());

            Assert.That(m_manager.HasQuest(m_heirloom), Is.True);
            Assert.That(updated, Is.EqualTo(0));
        }

        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_QuestThatGainedAnObjectiveSinceTheSave_ActiveGetsItIncomplete_CompletedStaysCompleted: An objective added after the save starts incomplete in an active quest; a completed quest stays completed --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("An objective added to a quest after the save starts incomplete in an active quest, which stays active; a completed quest stays completed.")]
        public void RestoreState_QuestThatGainedAnObjectiveSinceTheSave_ActiveGetsItIncomplete_CompletedStaysCompleted()
        {
            QuestObjective added = m_banditCamp.Objectives.Last();
            string[] savedBeforeThePatch = m_banditCamp.Objectives.Where(o => o != added).Select(o => o.name).ToArray();

            m_manager.RestoreState(State(new[] { ActiveEntry(kBanditCamp, savedBeforeThePatch) }, kHeirloom));

            Assert.That(m_manager.HasQuest(m_banditCamp), Is.True);
            Assert.That(StatusOf(m_manager, m_banditCamp).IsObjectiveComplete(added), Is.False);
            Assert.That(StatusOf(m_manager, m_banditCamp).CompletedObjectiveCount, Is.EqualTo(savedBeforeThePatch.Length));
            Assert.That(m_manager.IsQuestCompleted(m_heirloom), Is.True);
        }
    }
}
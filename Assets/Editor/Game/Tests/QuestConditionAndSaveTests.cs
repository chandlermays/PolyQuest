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

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        // Completing a shipped quest pays its shipped rewards, so the owner needs somewhere to put them
        private QuestManager NewPlayerQuestManager(string name)
        {
            GameObject player = m_kit.NewObject(name);
            m_kit.NewInventory(player, 8);
            player.AddComponent<Wallet>();
            return player.AddComponent<QuestManager>();
        }

        private void CompleteAll(QuestManager manager, Quest quest)
        {
            foreach (QuestObjective objective in quest.Objectives)
                manager.CompleteObjective(quest, objective);
        }

        private bool? Evaluate(ConditionType condition, params string[] parameters) => m_manager.Evaluate(condition, parameters);

        private static QuestStatus StatusOf(QuestManager manager, Quest quest) => manager.ActiveQuests.SingleOrDefault(s => s.Quest == quest);

        private static JObject ActiveEntry(string questName, params string[] completedObjectives)
            => new JObject { ["QuestName"] = questName, ["CompletedObjectives"] = new JArray(completedObjectives) };

        private static JObject State(JObject[] active, params string[] completed)
            => new JObject { ["ActiveQuests"] = new JArray(active), ["CompletedQuests"] = new JArray(completed) };

        /* ============================== Conditions ============================== */

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

        [Test]
        [Description("kCompletedQuest is false until the quest is completed, then true.")]
        public void CompletedQuest_BecomesTrueWhenTheQuestCompletes()
        {
            m_manager.AddQuest(m_heirloom);
            Assert.That(Evaluate(ConditionType.kCompletedQuest, kHeirloom), Is.False);

            CompleteAll(m_manager, m_heirloom);

            Assert.That(Evaluate(ConditionType.kCompletedQuest, kHeirloom), Is.True);
        }

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

        [Test]
        [Description("Completed objectives are permanent history: kCompletedObjective stays true after the quest is completed.")]
        public void CompletedObjective_StaysTrueAfterTheQuestIsCompleted()
        {
            m_manager.AddQuest(m_heirloom);

            CompleteAll(m_manager, m_heirloom);

            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, kFindTheSword), Is.True);
            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, kReturnTheSword), Is.True);
        }

        [Test]
        [Description("An objective done before its quest is acquired is part of the history: kCompletedObjective is true for it.")]
        public void CompletedObjective_IsTrueForWorkDoneBeforeAcquiringTheQuest()
        {
            m_manager.CompleteObjective(m_heirloom, m_findTheSword);

            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, kFindTheSword), Is.True);
        }

        [TestCase(ConditionType.kHasQuest)]
        [TestCase(ConditionType.kCompletedQuest)]
        [TestCase(ConditionType.kCompletedObjective)]
        [Description("A quest name that matches no quest asset is unanswered (null), so the condition fails closed even when negated.")]
        public void UnknownQuestName_IsUnanswered(ConditionType condition)
        {
            Assert.That(Evaluate(condition, "No Such Quest", kFindTheSword), Is.Null);
        }

        [Test]
        [Description("An objective name that matches no objective asset is unanswered (null).")]
        public void UnknownObjectiveName_IsUnanswered()
        {
            m_manager.AddQuest(m_heirloom);

            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom, "No Such Objective"), Is.Null);
        }

        [TestCase(ConditionType.kHasQuest)]
        [TestCase(ConditionType.kCompletedQuest)]
        [TestCase(ConditionType.kCompletedObjective)]
        [Description("A quest condition authored without its parameters is unanswered (null).")]
        public void MissingParameters_AreUnanswered(ConditionType condition)
        {
            Assert.That(Evaluate(condition), Is.Null);
        }

        [Test]
        [Description("kCompletedObjective authored with a quest but no objective is unanswered (null).")]
        public void CompletedObjective_WithoutAnObjectiveParameter_IsUnanswered()
        {
            m_manager.AddQuest(m_heirloom);

            Assert.That(Evaluate(ConditionType.kCompletedObjective, kHeirloom), Is.Null);
        }

        [Test]
        [Description("Conditions that aren't about quests are left to other checkers (null).")]
        public void NonQuestCondition_IsNotAnswered()
        {
            Assert.That(Evaluate(ConditionType.kHasItem, "sword"), Is.Null);
        }

        /* ============================== Save / restore ============================== */

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

        [Test]
        [Description("RestoreState replaces the current quests; quests absent from the save are no longer active.")]
        public void RestoreState_ReplacesTheCurrentQuests()
        {
            m_manager.AddQuest(m_banditCamp);

            m_manager.RestoreState(State(new[] { ActiveEntry(kHeirloom) }));

            Assert.That(m_manager.HasQuest(m_banditCamp), Is.False);
            Assert.That(m_manager.HasQuest(m_heirloom), Is.True);
        }

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

        [Test]
        [Description("A saved completed objective that no longer belongs to its quest is skipped; the quest's other saved objectives still load.")]
        public void RestoreState_UnknownObjective_IsSkipped_TheRestLoad()
        {
            m_manager.RestoreState(State(new[] { ActiveEntry(kHeirloom, "An Objective That Was Removed", kFindTheSword) }));

            QuestStatus status = StatusOf(m_manager, m_heirloom);
            Assert.That(status.IsObjectiveComplete(m_findTheSword), Is.True);
            Assert.That(status.CompletedObjectiveCount, Is.EqualTo(1));
        }

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

        [Test]
        [Description("Work done before a quest is acquired survives a save and load, and counts when the quest is acquired afterwards.")]
        public void WorkDoneBeforeAcquiring_SurvivesSaveAndLoad()
        {
            m_manager.CompleteObjective(m_heirloom, m_findTheSword);
            JToken saved = JToken.Parse(m_manager.CaptureState().ToString());

            QuestManager restored = NewPlayerQuestManager("FreshPlayer");
            restored.RestoreState(saved);
            restored.AddQuest(m_heirloom);

            Assert.That(StatusOf(restored, m_heirloom).IsObjectiveComplete(m_findTheSword), Is.True);
        }
    }
}
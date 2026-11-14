/*---------------------------
| File: QuestManagerTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Pickups;
using PolyQuest.Quests;

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

        private QuestObjective m_find, m_kill, m_return, m_unrelated;
        private Quest m_quest;
        private CurrencyItem m_silver;
        private ActionItem m_potion;

        private int m_started, m_completed, m_updated;

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

            // Three unordered objectives, rewarding silver and a stack of potions
            m_quest = NewQuest("Clear the Camp", new[] { m_find, m_kill, m_return }, (m_silver, kSilverReward), (m_potion, kPotionReward));

            GameObject player = m_kit.NewObject("Player");
            m_inventory = m_kit.NewInventory(player, 4);
            m_wallet = player.AddComponent<Wallet>();
            m_manager = player.AddComponent<QuestManager>();

            m_started = m_completed = m_updated = 0;
            m_manager.OnQuestStarted += _ => ++m_started;
            m_manager.OnQuestCompleted += _ => ++m_completed;
            m_manager.OnQuestsUpdate += () => ++m_updated;
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        private QuestObjective Objective(string name)
        {
            QuestObjective objective = m_kit.Track(ScriptableObject.CreateInstance<QuestObjective>());
            objective.name = name;
            return objective;
        }

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

        private QuestStatus StatusOf(Quest quest) => m_manager.ActiveQuests.SingleOrDefault(s => s.Quest == quest);

        private void CompleteAll(Quest quest)
        {
            foreach (QuestObjective objective in quest.Objectives)
                m_manager.CompleteObjective(quest, objective);
        }

        private int Owned(InventoryItem item)
        {
            int total = 0;
            for (int i = 0; i < m_inventory.Size; ++i)
                if (m_inventory.GetItemAtSlot(i) == item) total += m_inventory.GetQuantityAtSlot(i);
            return total;
        }

        /* ============================== Acquiring a quest ============================== */

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

        [Test]
        [Description("AddQuest raises OnQuestStarted once, in the same step, and OnQuestsUpdate once.")]
        public void AddQuest_RaisesOnQuestStarted_InTheSameStep()
        {
            m_manager.AddQuest(m_quest);

            Assert.That(m_started, Is.EqualTo(1));
            Assert.That(m_updated, Is.EqualTo(1));
        }

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

        [Test]
        [Description("AddQuest with no quest (a misconfigured assigner or dialogue event) does nothing.")]
        public void AddQuest_Null_DoesNothing()
        {
            Assert.DoesNotThrow(() => m_manager.AddQuest(null));

            Assert.That(m_manager.ActiveQuests, Is.Empty);
            Assert.That(m_started, Is.EqualTo(0));
        }

        /* ============================== Completing objectives ============================== */

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

        [Test]
        [Description("Completing the same objective twice counts it once.")]
        public void CompleteObjective_Twice_CountsOnce()
        {
            m_manager.AddQuest(m_quest);

            m_manager.CompleteObjective(m_quest, m_find);
            m_manager.CompleteObjective(m_quest, m_find);

            Assert.That(StatusOf(m_quest).CompletedObjectiveCount, Is.EqualTo(1));
        }

        [Test]
        [Description("An objective that doesn't belong to the quest is ignored.")]
        public void CompleteObjective_NotInTheQuest_IsIgnored()
        {
            m_manager.AddQuest(m_quest);

            m_manager.CompleteObjective(m_quest, m_unrelated);

            Assert.That(StatusOf(m_quest).CompletedObjectiveCount, Is.EqualTo(0));
        }

        [Test]
        [Description("CompleteObjective with no quest or no objective (a misconfigured trigger, quest item or dialogue event) does nothing.")]
        public void CompleteObjective_NullQuestOrObjective_DoesNothing()
        {
            m_manager.AddQuest(m_quest);

            Assert.DoesNotThrow(() => m_manager.CompleteObjective(null, m_find));
            Assert.DoesNotThrow(() => m_manager.CompleteObjective(m_quest, null));

            Assert.That(StatusOf(m_quest).CompletedObjectiveCount, Is.EqualTo(0));
        }

        [Test]
        [Description("Completing an objective autosaves the game.")]
        public void CompleteObjective_WritesTheSaveFile()
        {
            m_manager.AddQuest(m_quest);

            m_manager.CompleteObjective(m_quest, m_find);

            Assert.That(InventoryTestKit.TestSaveFileExists(), Is.True);
        }

        /* ============================== Work done before acquiring the quest ============================== */

        [Test]
        [Description("Objectives completed before the quest is acquired count once it is acquired; a quest with its turn-in still open stays active and waits for the turn-in.")]
        public void ObjectivesCompletedBeforeAcquiring_Count_AndTheTurnInIsStillNeeded()
        {
            m_manager.CompleteObjective(m_quest, m_find);
            m_manager.CompleteObjective(m_quest, m_kill);
            Assert.That(m_manager.HasQuest(m_quest), Is.False, "completing an objective doesn't acquire the quest");

            m_manager.AddQuest(m_quest);

            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_find), Is.True);
            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_kill), Is.True);
            Assert.That(StatusOf(m_quest).IsObjectiveComplete(m_return), Is.False);
            Assert.That(m_manager.IsQuestCompleted(m_quest), Is.False);
        }

        [Test]
        [Description("A quest without a turn-in whose objectives were all done beforehand completes when acquired: started is raised before completed, and the rewards are paid exactly once.")]
        public void QuestWithoutATurnIn_AllDoneBeforehand_CompletesWhenAcquired_StartedBeforeCompleted_PaidOnce()
        {
            Quest clearTheCamp = NewQuest("Clear the Camp, Done", new[] { m_find, m_kill }, (m_silver, kSilverReward), (m_potion, kPotionReward));
            var order = new System.Collections.Generic.List<string>();
            m_manager.OnQuestStarted += _ => order.Add("started");
            m_manager.OnQuestCompleted += _ => order.Add("completed");
            m_manager.CompleteObjective(clearTheCamp, m_find);
            m_manager.CompleteObjective(clearTheCamp, m_kill);

            m_manager.AddQuest(clearTheCamp);

            Assert.That(m_manager.IsQuestCompleted(clearTheCamp), Is.True);
            Assert.That(m_manager.HasQuest(clearTheCamp), Is.False);
            Assert.That(order, Is.EqualTo(new[] { "started", "completed" }));
            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(kSilverReward));
            Assert.That(Owned(m_potion), Is.EqualTo(kPotionReward));
        }

        /* ============================== Completing the quest ============================== */

        [Test]
        [Description("Completing the last objective grants every reward in the same step: currency to the wallet, items to the inventory.")]
        public void CompletingTheQuest_GrantsItsRewards_InTheSameStep()
        {
            m_manager.AddQuest(m_quest);

            CompleteAll(m_quest);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(kSilverReward));
            Assert.That(Owned(m_potion), Is.EqualTo(kPotionReward));
        }

        [Test]
        [Description("Completing the last objective raises OnQuestCompleted once, in the same step.")]
        public void CompletingTheQuest_RaisesOnQuestCompleted_InTheSameStep()
        {
            m_manager.AddQuest(m_quest);

            CompleteAll(m_quest);

            Assert.That(m_completed, Is.EqualTo(1));
        }

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
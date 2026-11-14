/*---------------------------
| File: QuestHookTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Components;
using PolyQuest.Core;
using PolyQuest.Dialogues;
using PolyQuest.Inventories;
using PolyQuest.Quests;

namespace PolyQuest.Tests
{
    // Everything in the game that acquires a quest or completes an objective: dialogue events, assigners,
    // trigger volumes, enemy trackers and quest items.
    public class QuestHookTests
    {
        private const BindingFlags kAny = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private InventoryTestKit m_kit;
        private GameObject m_player;
        private QuestManager m_manager;
        private Inventory m_inventory;

        private QuestObjective m_first, m_second;
        private Quest m_quest;

        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            m_kit.IsolateSaves();

            m_first = Objective("First Objective");
            m_second = Objective("Second Objective");
            m_quest = m_kit.Track(ScriptableObject.CreateInstance<Quest>());
            m_quest.name = "Hook Test Quest";
            SerializedObject so = new SerializedObject(m_quest);
            SerializedProperty objectives = so.FindProperty("m_objectives");
            objectives.arraySize = 2;
            objectives.GetArrayElementAtIndex(0).objectReferenceValue = m_first;
            objectives.GetArrayElementAtIndex(1).objectReferenceValue = m_second;
            so.FindProperty("m_rewards").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            m_player = m_kit.NewObject("Player");
            m_player.tag = "Player";
            m_inventory = m_kit.NewInventory(m_player, 2);
            m_player.AddComponent<Wallet>();
            m_manager = m_player.AddComponent<QuestManager>();
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

        private bool IsDone(QuestObjective objective)
        {
            QuestStatus status = m_manager.ActiveQuests.SingleOrDefault(s => s.Quest == m_quest);
            return status != null && status.IsObjectiveComplete(objective);
        }

        private ObjectiveCompletion AddObjectiveCompletion(GameObject go, QuestObjective objective)
        {
            ObjectiveCompletion completion = go.AddComponent<ObjectiveCompletion>();
            SerializedTestUtil.SetObject(completion, "m_questManager", m_manager);
            SerializedTestUtil.SetObject(completion, "m_quest", m_quest);
            SerializedTestUtil.SetObject(completion, "m_objective", objective);
            InventoryTestKit.InvokeAwake(completion);
            return completion;
        }

        /* ============================== Dialogue events ============================== */

        [Test]
        [Description("An AssignQuestEvent gives its quest to the instigator's QuestManager.")]
        public void AssignQuestEvent_GivesTheQuestToTheInstigator()
        {
            AssignQuestEvent assign = m_kit.Track(ScriptableObject.CreateInstance<AssignQuestEvent>());
            SerializedTestUtil.SetObject(assign, "m_quest", m_quest);

            assign.Execute(m_player, m_kit.NewObject("NPC"));

            Assert.That(m_manager.HasQuest(m_quest), Is.True);
        }

        [Test]
        [Description("A CompleteObjectiveEvent completes its objective in the instigator's QuestManager.")]
        public void CompleteObjectiveEvent_CompletesTheObjectiveForTheInstigator()
        {
            m_manager.AddQuest(m_quest);
            CompleteObjectiveEvent complete = m_kit.Track(ScriptableObject.CreateInstance<CompleteObjectiveEvent>());
            SerializedTestUtil.SetObject(complete, "m_quest", m_quest);
            SerializedTestUtil.SetObject(complete, "m_objective", m_second);

            complete.Execute(m_player, m_kit.NewObject("NPC"));

            Assert.That(IsDone(m_second), Is.True);
            Assert.That(IsDone(m_first), Is.False);
        }

        /* ============================== QuestAssigner / ObjectiveCompletion ============================== */

        [Test]
        [Description("QuestAssigner.AssignQuest gives its quest to the player's QuestManager.")]
        public void QuestAssigner_GivesTheQuestToThePlayer()
        {
            QuestAssigner assigner = m_kit.NewObject("Assigner").AddComponent<QuestAssigner>();
            SerializedTestUtil.SetObject(assigner, "m_playerQuestMgr", m_manager);
            SerializedTestUtil.SetObject(assigner, "m_quest", m_quest);

            assigner.AssignQuest();

            Assert.That(m_manager.HasQuest(m_quest), Is.True);
        }

        [Test]
        [Description("ObjectiveCompletion.CompleteObjective completes its configured objective, and only that one.")]
        public void ObjectiveCompletion_CompletesItsObjective()
        {
            m_manager.AddQuest(m_quest);
            ObjectiveCompletion completion = AddObjectiveCompletion(m_kit.NewObject("Completion"), m_first);

            completion.CompleteObjective();

            Assert.That(IsDone(m_first), Is.True);
            Assert.That(IsDone(m_second), Is.False);
        }

        /* ============================== TriggerObjective ============================== */

        private GameObject BuildTriggerVolume(QuestObjective objective)
        {
            GameObject volume = m_kit.NewObject("TriggerVolume");
            volume.AddComponent<BoxCollider>().isTrigger = true;
            AddObjectiveCompletion(volume, objective);
            volume.AddComponent<TriggerObjective>();
            return volume;
        }

        private static void Enter(GameObject volume, Collider other)
        {
            TriggerObjective trigger = volume.GetComponent<TriggerObjective>();
            typeof(TriggerObjective).GetMethod("OnTriggerEnter", kAny).Invoke(trigger, new object[] { other });
        }

        [Test]
        [Description("The Player entering a TriggerObjective volume completes the volume's objective.")]
        public void TriggerObjective_PlayerEntering_CompletesTheObjective()
        {
            m_manager.AddQuest(m_quest);
            GameObject volume = BuildTriggerVolume(m_first);

            Enter(volume, m_player.AddComponent<CapsuleCollider>());

            Assert.That(IsDone(m_first), Is.True);
        }

        [Test]
        [Description("Anything other than the Player entering a TriggerObjective volume completes nothing.")]
        public void TriggerObjective_NonPlayerEntering_CompletesNothing()
        {
            m_manager.AddQuest(m_quest);
            GameObject volume = BuildTriggerVolume(m_first);
            GameObject wanderer = m_kit.NewObject("Wanderer");

            Enter(volume, wanderer.AddComponent<CapsuleCollider>());

            Assert.That(IsDone(m_first), Is.False);
        }

        /* ============================== EnemyTracker ============================== */

        private EnemyTracker BuildTracker(QuestObjective objective)
        {
            GameObject trackerObject = m_kit.NewObject("EnemyTracker");
            AddObjectiveCompletion(trackerObject, objective);
            EnemyTracker tracker = trackerObject.AddComponent<EnemyTracker>();
            InventoryTestKit.InvokeAwake(tracker);
            return tracker;
        }

        private HealthComponent BuildEnemy(string name)
        {
            AIProgression progression = m_kit.Track(ScriptableObject.CreateInstance<AIProgression>());
            SerializedTestUtil.SetIntArray(progression, "m_maxHealthAmounts", 10);

            GameObject enemy = m_kit.NewObject(name);
            enemy.AddComponent<Animator>();
            enemy.AddComponent<NavMeshAgent>();
            BaseStats stats = enemy.AddComponent<BaseStats>();
            SerializedTestUtil.SetObject(stats, "m_baseProgression", progression);
            HealthComponent health = enemy.AddComponent<HealthComponent>();
            InventoryTestKit.InvokeAwake(health);
            typeof(HealthComponent).GetMethod("Start", kAny).Invoke(health, null);
            return health;
        }

        private void Kill(HealthComponent enemy) => enemy.TakeDamage(m_player, 1000f);

        [Test]
        [Description("An EnemyTracker completes its objective when the last of its tracked enemies dies, not before.")]
        public void EnemyTracker_CompletesItsObjective_WhenEveryTrackedEnemyIsDead()
        {
            m_manager.AddQuest(m_quest);
            EnemyTracker tracker = BuildTracker(m_first);
            HealthComponent a = BuildEnemy("BanditA");
            HealthComponent b = BuildEnemy("BanditB");
            tracker.RegisterEnemy(a);
            tracker.RegisterEnemy(b);

            Kill(a);
            Assert.That(IsDone(m_first), Is.False, "one enemy is still alive");

            Kill(b);
            Assert.That(IsDone(m_first), Is.True);
        }

        [Test]
        [Description("Enemies a save restores as dead count as killed: after a reload, killing the remaining enemies still completes the objective.")]
        public void EnemyTracker_EnemiesRestoredDeadFromASave_CountAsKilled()
        {
            m_manager.AddQuest(m_quest);
            EnemyTracker tracker = BuildTracker(m_first);
            HealthComponent killedLastSession = BuildEnemy("BanditA");
            HealthComponent stillAlive = BuildEnemy("BanditB");
            tracker.RegisterEnemy(killedLastSession);           // enemies register in Awake, before the save is applied
            tracker.RegisterEnemy(stillAlive);

            killedLastSession.RestoreState(new JObject { ["health"] = 0f, ["isDead"] = true });
            Kill(stillAlive);

            Assert.That(IsDone(m_first), Is.True);
        }

        /* ============================== QuestItem ============================== */

        private QuestItem NewQuestItem(string id, QuestObjective objective)
        {
            QuestItem item = m_kit.Item<QuestItem>(id);
            SerializedTestUtil.SetObject(item, "m_quest", m_quest);
            SerializedTestUtil.SetObject(item, "m_objective", objective);
            return item;
        }

        [Test]
        [Description("Picking up a quest item puts it in the inventory and completes its objective.")]
        public void QuestItem_PickedUp_GoesToTheInventory_AndCompletesItsObjective()
        {
            m_manager.AddQuest(m_quest);
            QuestItem sword = NewQuestItem("family-sword", m_first);

            sword.OnPickup(m_inventory, 1);

            Assert.That(m_inventory.ContainsItem(sword), Is.True);
            Assert.That(IsDone(m_first), Is.True);
        }

        [Test]
        [Description("A quest item that doesn't fit in the inventory isn't picked up and doesn't complete its objective.")]
        public void QuestItem_ThatDoesNotFit_DoesNotCompleteItsObjective()
        {
            m_manager.AddQuest(m_quest);
            for (int i = 0; i < m_inventory.Size; ++i)
                m_inventory.TryAddToAvailableSlot(m_kit.Item<ActionItem>("filler" + i), 1);
            QuestItem sword = NewQuestItem("family-sword", m_first);

            sword.OnPickup(m_inventory, 1);

            Assert.That(m_inventory.ContainsItem(sword), Is.False);
            Assert.That(IsDone(m_first), Is.False);
        }
    }
}
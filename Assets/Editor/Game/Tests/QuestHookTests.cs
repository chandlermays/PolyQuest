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
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    // Everything in the game that acquires a quest, completes an objective or records a fact objectives read:
    // dialogue events, assigners, flag trigger volumes, enemy trackers and quest items.
    public class QuestHookTests
    {
        private const BindingFlags kAny = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private InventoryTestKit m_kit;
        private GameObject m_player;
        private QuestManager m_manager;
        private Inventory m_inventory;

        private WorldFlags m_flags;
        private WorldFlag m_campEntered, m_campCleared;
        private QuestObjective m_first, m_second;
        private Quest m_quest;

        /*------------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for quest hook testing --- |
        ------------------------------------------------------------------------------*/
        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            m_kit.IsolateSaves();

            m_first = Objective("First Objective");
            m_second = Objective("Second Objective");

            // The first objective reads "camp entered", the second "camp cleared"
            m_campEntered = m_kit.Flag("camp_entered");
            m_campCleared = m_kit.Flag("camp_cleared");
            InventoryTestKit.ConfigureObjective(m_first, InventoryTestKit.When(ConditionType.kWorldFlag, m_campEntered.ID));
            InventoryTestKit.ConfigureObjective(m_second, InventoryTestKit.When(ConditionType.kWorldFlag, m_campCleared.ID));
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
            m_flags = m_player.AddComponent<WorldFlags>();
            m_manager = m_player.AddComponent<QuestManager>();
            InventoryTestKit.InvokeAwake(m_manager);            // listens for flags and inventory changes
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

        /*-------------------------------------------------------------------------------------
        | --- IsDone: Whether the objective is complete in the test quest's active status --- |
        -------------------------------------------------------------------------------------*/
        private bool IsDone(QuestObjective objective)
        {
            QuestStatus status = m_manager.ActiveQuests.SingleOrDefault(s => s.Quest == m_quest);
            return status != null && status.IsObjectiveComplete(objective);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------
        | --- AddObjectiveCompletion: Adds an ObjectiveCompletion wired to the Player's QuestManager, the test quest and the objective --- |
        ----------------------------------------------------------------------------------------------------------------------------------*/
        private ObjectiveCompletion AddObjectiveCompletion(GameObject go, QuestObjective objective)
        {
            ObjectiveCompletion completion = go.AddComponent<ObjectiveCompletion>();
            SerializedTestUtil.SetObject(completion, "m_questManager", m_manager);
            SerializedTestUtil.SetObject(completion, "m_quest", m_quest);
            SerializedTestUtil.SetObject(completion, "m_objective", objective);
            InventoryTestKit.InvokeAwake(completion);
            return completion;
        }


        /*-------------------------
        | --- Dialogue Events --- |
        -------------------------*/
        /*-----------------------------------------------------------------------------------------------------------------------------
        | --- AssignQuestEvent_GivesTheQuestToTheInstigator: An AssignQuestEvent gives its quest to the instigator's QuestManager --- |
        -----------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("An AssignQuestEvent gives its quest to the instigator's QuestManager.")]
        public void AssignQuestEvent_GivesTheQuestToTheInstigator()
        {
            AssignQuestEvent assign = m_kit.Track(ScriptableObject.CreateInstance<AssignQuestEvent>());
            SerializedTestUtil.SetObject(assign, "m_quest", m_quest);

            assign.Execute(m_player, m_kit.NewObject("NPC"));

            Assert.That(m_manager.HasQuest(m_quest), Is.True);
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CompleteObjectiveEvent_CompletesTheObjectiveForTheInstigator: A CompleteObjectiveEvent completes its objective in the instigator's QuestManager --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------*/
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


        /*---------------------------------------------
        | --- QuestAssigner & ObjectiveCompletion --- |
        ---------------------------------------------*/
        /*------------------------------------------------------------------------------------------------------------------------
        | --- QuestAssigner_GivesTheQuestToThePlayer: QuestAssigner.AssignQuest gives its quest to the player's QuestManager --- |
        ------------------------------------------------------------------------------------------------------------------------*/
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

        /*------------------------------------------------------------------------------------------------------------------------------------------------
        | --- ObjectiveCompletion_CompletesItsObjective: ObjectiveCompletion.CompleteObjective completes its configured objective, and only that one --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------*/
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


        /*--------------------------
        | --- WorldFlagTrigger --- |
        --------------------------*/
        /*-----------------------------------------------------------------------------------
        | --- BuildFlagVolume: Creates a trigger volume that sets the flag when entered --- |
        -----------------------------------------------------------------------------------*/
        private GameObject BuildFlagVolume(WorldFlag flag)
        {
            GameObject volume = m_kit.NewObject("FlagVolume");
            volume.AddComponent<BoxCollider>().isTrigger = true;
            WorldFlagTrigger trigger = volume.AddComponent<WorldFlagTrigger>();
            SerializedTestUtil.SetObject(trigger, "m_flag", flag);
            return volume;
        }

        /*------------------------------------------------------------------------------------
        | --- Enter: Invokes the volume's private OnTriggerEnter with the given collider --- |
        ------------------------------------------------------------------------------------*/
        private static void Enter(GameObject volume, Collider other)
        {
            WorldFlagTrigger trigger = volume.GetComponent<WorldFlagTrigger>();
            typeof(WorldFlagTrigger).GetMethod("OnTriggerEnter", kAny).Invoke(trigger, new object[] { other });
        }

        /*------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- WorldFlagTrigger_PlayerEntering_SetsItsFlag_AndTheObjectiveReadingItCompletes: The Player entering the volume records its fact, and the active objective that reads it completes --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("The Player entering a WorldFlagTrigger volume records its fact, and the active objective that reads it completes.")]
        public void WorldFlagTrigger_PlayerEntering_SetsItsFlag_AndTheObjectiveReadingItCompletes()
        {
            m_manager.AddQuest(m_quest);
            GameObject volume = BuildFlagVolume(m_campEntered);

            Enter(volume, m_player.AddComponent<CapsuleCollider>());

            Assert.That(m_flags.IsSet(m_campEntered), Is.True);
            Assert.That(IsDone(m_first), Is.True);
            Assert.That(IsDone(m_second), Is.False);
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------
        | --- WorldFlagTrigger_BeforeTheQuest_RecordsTheFact_ForWhenItIsAcquired: Entering before the quest still counts once it's acquired --- |
        ---------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Entering the volume before the quest is acquired records the fact, so the objective completes when the quest is acquired.")]
        public void WorldFlagTrigger_BeforeTheQuest_RecordsTheFact_ForWhenItIsAcquired()
        {
            GameObject volume = BuildFlagVolume(m_campEntered);
            Enter(volume, m_player.AddComponent<CapsuleCollider>());

            m_manager.AddQuest(m_quest);

            Assert.That(IsDone(m_first), Is.True);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------
        | --- WorldFlagTrigger_NonPlayerEntering_SetsNothing: Anything other than the Player entering a WorldFlagTrigger volume sets nothing --- |
        ----------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Anything other than the Player entering a WorldFlagTrigger volume sets nothing and completes nothing.")]
        public void WorldFlagTrigger_NonPlayerEntering_SetsNothing()
        {
            m_manager.AddQuest(m_quest);
            GameObject volume = BuildFlagVolume(m_campEntered);
            GameObject wanderer = m_kit.NewObject("Wanderer");

            Enter(volume, wanderer.AddComponent<CapsuleCollider>());

            Assert.That(m_flags.IsSet(m_campEntered), Is.False);
            Assert.That(IsDone(m_first), Is.False);
        }


        /*----------------------
        | --- EnemyTracker --- |
        ----------------------*/
        /*----------------------------------------------------------------------------------
        | --- BuildTracker: Creates an EnemyTracker that records its flag once cleared --- |
        ----------------------------------------------------------------------------------*/
        private EnemyTracker BuildTracker(WorldFlag clearedFlag)
        {
            GameObject trackerObject = m_kit.NewObject("EnemyTracker");
            EnemyTracker tracker = trackerObject.AddComponent<EnemyTracker>();
            SerializedTestUtil.SetObject(tracker, "m_clearedFlag", clearedFlag);
            InventoryTestKit.InvokeAwake(tracker);
            return tracker;
        }

        /*--------------------------------------------------------------------------------
        | --- BuildEnemy: Creates a 10-health enemy with its HealthComponent started --- |
        --------------------------------------------------------------------------------*/
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

        /*-----------------------------------------------------------------------
        | --- Kill: Kills an enemy with an overwhelming hit from the Player --- |
        -----------------------------------------------------------------------*/
        private void Kill(HealthComponent enemy) => enemy.TakeDamage(m_player, 1000f);

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- EnemyTracker_SetsItsFlag_WhenEveryTrackedEnemyIsDead: An EnemyTracker records its fact when the last of its enemies dies, not before, and the objective reading it completes --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("An EnemyTracker records its fact when the last of its tracked enemies dies, not before, and the active objective reading it completes.")]
        public void EnemyTracker_SetsItsFlag_WhenEveryTrackedEnemyIsDead()
        {
            m_manager.AddQuest(m_quest);
            EnemyTracker tracker = BuildTracker(m_campCleared);
            HealthComponent a = BuildEnemy("BanditA");
            HealthComponent b = BuildEnemy("BanditB");
            tracker.RegisterEnemy(a);
            tracker.RegisterEnemy(b);

            Kill(a);
            Assert.That(m_flags.IsSet(m_campCleared), Is.False, "one enemy is still alive");

            Kill(b);
            Assert.That(m_flags.IsSet(m_campCleared), Is.True);
            Assert.That(IsDone(m_second), Is.True);
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- EnemyTracker_EnemiesRestoredDeadFromASave_CountAsKilled: Enemies a save restores as dead count as killed: after a reload, killing the remaining enemies still clears the encounter --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Enemies a save restores as dead count as killed: after a reload, killing the remaining enemies still clears the encounter.")]
        public void EnemyTracker_EnemiesRestoredDeadFromASave_CountAsKilled()
        {
            m_manager.AddQuest(m_quest);
            EnemyTracker tracker = BuildTracker(m_campCleared);
            HealthComponent killedLastSession = BuildEnemy("BanditA");
            HealthComponent stillAlive = BuildEnemy("BanditB");
            tracker.RegisterEnemy(killedLastSession);           // enemies register in Awake, before the save is applied
            tracker.RegisterEnemy(stillAlive);

            killedLastSession.RestoreState(new JObject { ["health"] = 0f, ["isDead"] = true });
            Assert.That(m_flags.IsSet(m_campCleared), Is.False, "one enemy is still alive");
            Kill(stillAlive);

            Assert.That(m_flags.IsSet(m_campCleared), Is.True);
            Assert.That(IsDone(m_second), Is.True);
        }

        /*---------------------------------------------------------------------------------------------------------------------------------
        | --- EnemyTracker_EveryEnemyRestoredDead_IsCleared: When a restore leaves every tracked enemy dead, the encounter is cleared --- |
        ---------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("When a restore leaves every tracked enemy dead, the encounter is cleared without anything dying now.")]
        public void EnemyTracker_EveryEnemyRestoredDead_IsCleared()
        {
            EnemyTracker tracker = BuildTracker(m_campCleared);
            HealthComponent a = BuildEnemy("BanditA");
            HealthComponent b = BuildEnemy("BanditB");
            tracker.RegisterEnemy(a);
            tracker.RegisterEnemy(b);

            a.RestoreState(new JObject { ["health"] = 0f, ["isDead"] = true });
            b.RestoreState(new JObject { ["health"] = 0f, ["isDead"] = true });

            Assert.That(m_flags.IsSet(m_campCleared), Is.True);
        }


        /*-------------------
        | --- QuestItem --- |
        -------------------*/
        /*---------------------------------------------------------------------------------------------------
        | --- NewQuestItem: Creates a registered quest item that completes the objective when picked up --- |
        ---------------------------------------------------------------------------------------------------*/
        private QuestItem NewQuestItem(string id, QuestObjective objective)
        {
            QuestItem item = m_kit.Item<QuestItem>(id);
            SerializedTestUtil.SetObject(item, "m_quest", m_quest);
            SerializedTestUtil.SetObject(item, "m_objective", objective);
            return item;
        }

        /*------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- QuestItem_PickedUp_GoesToTheInventory_AndCompletesItsObjective: Picking up a quest item puts it in the inventory and completes its objective --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------*/
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

        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- QuestItem_ThatDoesNotFit_DoesNotCompleteItsObjective: A quest item that doesn't fit in the inventory isn't picked up and doesn't complete its objective --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------*/
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
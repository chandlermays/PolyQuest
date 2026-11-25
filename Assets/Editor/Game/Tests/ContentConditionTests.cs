/*---------------------------
| File: ContentConditionTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Edit;
using PolyQuest.Quests;
using PolyQuest.Shops;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    // Every condition authored in the game's assets (dialogue nodes, equipable items, anything holding a Conjunction)
    // has a real condition type and every parameter that type reads, every item ID resolves through FindByID, a kHasItems
    // quantity is a whole number above 0, a kHasSpokenTo names an NPC who exists, and a kWorldFlag names a WorldFlag in
    // Resources. Every NPC's and every WorldFlag's ID is unique, and quests are well formed: each objective belongs to one
    // quest, its Also Completes stays in that quest without cycles, and a turn-in has no Complete When condition.
    // A condition that breaks any of these can never pass, so it is broken content. Every NPC's ID is unique.
    public class ContentConditionTests
    {
        private const string kGameFolder = "Assets/Game";

        /*-----------------------------------------------------------------------------------------
        | --- AuthoredConditions: Every serialized Conjunction.Condition in the game's assets --- |
        -----------------------------------------------------------------------------------------*/
        private static IEnumerable<(string where, SerializedProperty condition)> AuthoredConditions()
        {
            HashSet<string> paths = new();
            foreach (string filter in new[] { "t:ScriptableObject", "t:Prefab" })
            {
                foreach (string guid in AssetDatabase.FindAssets(filter, new[] { kGameFolder }))
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (string path in paths)
            {
                // Sub-assets too: dialogue nodes live inside their dialogue's asset file
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!(asset is ScriptableObject) && !(asset is MonoBehaviour))
                        continue;

                    SerializedObject serialized = new SerializedObject(asset);
                    SerializedProperty iterator = serialized.GetIterator();
                    while (iterator.Next(true))
                    {
                        if (iterator.propertyType != SerializedPropertyType.Generic || iterator.type != nameof(Conjunction.Condition))
                            continue;

                        if (iterator.FindPropertyRelative("m_condition") == null || iterator.FindPropertyRelative("m_parameters") == null)
                            continue;

                        yield return ($"{path} › {asset.name} › {iterator.propertyPath}", iterator.Copy());
                    }
                }
            }
        }

        /*-------------------------------------------------------------------------------------------------------------------
        | --- EveryAuthoredCondition_HasAConditionType_AndItsRequiredParameters: Lists every broken condition in one go --- |
        -------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Every condition in the game's assets has a condition type other than Select, every parameter its type reads is filled in, every item ID resolves through FindByID, every kHasItems quantity is a whole number above 0, every kHasSpokenTo names an existing NPC ID, and every kWorldFlag names a WorldFlag in Resources.")]
        public void EveryAuthoredCondition_HasAConditionType_AndItsRequiredParameters()
        {
            int checkedCount = 0;
            List<string> problems = new();

            foreach ((string where, SerializedProperty condition) in AuthoredConditions())
            {
                ++checkedCount;
                string problem = ConditionValidation.Problem(condition);
                if (problem != null)
                    problems.Add($"{where}: {problem}");
            }

            Assert.That(checkedCount, Is.GreaterThan(0), "fixture: no authored conditions were found, so nothing was checked");
            Assert.That(problems, Is.Empty, "Broken conditions:\n" + string.Join("\n", problems));
        }

        /*-------------------------------------------------------------------------------------------------------------------
        | --- EveryNpc_HasAUniqueNonEmptyID: Dialogue history and kHasSpokenTo find an NPC by its ID, so it must be one --- |
        -------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Every NPC in the game's prefabs has a non-empty NPC ID, and no two NPCs share one.")]
        public void EveryNpc_HasAUniqueNonEmptyID()
        {
            List<NpcCatalog.Entry> npcs = NpcCatalog.FindAll();
            Assert.That(npcs, Is.Not.Empty, "fixture: no NPCs were found, so nothing was checked");

            List<string> problems = new();
            Dictionary<string, List<string>> prefabsByID = new();
            foreach (NpcCatalog.Entry npc in npcs)
            {
                if (string.IsNullOrWhiteSpace(npc.ID))
                {
                    problems.Add($"{npc.PrefabPath}: {npc.DisplayName} has no NPC ID");
                    continue;
                }

                if (!prefabsByID.TryGetValue(npc.ID, out List<string> prefabs))
                    prefabsByID[npc.ID] = prefabs = new List<string>();
                prefabs.Add(npc.PrefabPath);
            }

            foreach (KeyValuePair<string, List<string>> pair in prefabsByID)
            {
                if (pair.Value.Count > 1)
                    problems.Add($"'{pair.Key}' is the ID of {pair.Value.Count} NPCs: {string.Join(", ", pair.Value)}");
            }

            Assert.That(problems, Is.Empty, "NPC ID problems:\n" + string.Join("\n", problems));
        }

        /*---------------------------------------------------------------------------------------------------------------------------------
        | --- EveryWorldFlag_HasAUniqueNonEmptyID_AndIsInResources: Saves and kWorldFlag find a flag by its ID, and only in Resources --- |
        ---------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Every WorldFlag in the game's assets has a non-empty ID that no other flag shares, and lives under a Resources folder so the game can find it.")]
        public void EveryWorldFlag_HasAUniqueNonEmptyID_AndIsInResources()
        {
            List<string> problems = new();
            Dictionary<string, List<string>> pathsByID = new();
            foreach (WorldFlag flag in ConditionValidation.FindWorldFlags(resourcesOnly: false))
            {
                string path = AssetDatabase.GetAssetPath(flag);
                if (!path.Contains("/Resources/"))
                    problems.Add($"{path}: isn't under a Resources folder, so the game can't find it");

                if (string.IsNullOrWhiteSpace(flag.ID))
                {
                    problems.Add($"{path}: has no ID");
                    continue;
                }

                if (!pathsByID.TryGetValue(flag.ID, out List<string> paths))
                    pathsByID[flag.ID] = paths = new List<string>();
                paths.Add(path);
            }

            foreach (KeyValuePair<string, List<string>> pair in pathsByID.Where(p => p.Value.Count > 1))
                problems.Add($"'{pair.Key}' is the ID of {pair.Value.Count} flags: {string.Join(", ", pair.Value)}");

            Assert.That(problems, Is.Empty, "WorldFlag problems:\n" + string.Join("\n", problems));
        }


        /*----------------
        | --- Quests --- |
        ----------------*/
        /*-------------------------------------------------------------
        | --- LoadAll: Every asset of a type in the game's folder --- |
        -------------------------------------------------------------*/
        private static List<T> LoadAll<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { kGameFolder })
                                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                                .Where(asset => asset != null)
                                .ToList();
        }

        /*----------------------------------------------------------------------------------------------------------------------------------
        | --- EveryObjective_BelongsToExactlyOneQuest: An objective's Also Completes and progress are per quest, so it can't be shared --- |
        ----------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Every quest objective in the game's assets is listed by exactly one quest, and no quest lists an empty objective slot.")]
        public void EveryObjective_BelongsToExactlyOneQuest()
        {
            List<Quest> quests = LoadAll<Quest>();
            Assert.That(quests, Is.Not.Empty, "fixture: no quests were found, so nothing was checked");
            List<string> problems = new();

            foreach (Quest quest in quests.Where(q => q.Objectives.Any(o => o == null)))
                problems.Add($"{quest.name}: has an empty objective slot");

            foreach (QuestObjective objective in LoadAll<QuestObjective>())
            {
                List<string> owners = quests.Where(q => q.HasObjective(objective)).Select(q => q.name).ToList();
                if (owners.Count != 1)
                    problems.Add($"{objective.name}: is listed by {owners.Count} quests{(owners.Count > 0 ? ": " + string.Join(", ", owners) : "")}");
            }

            Assert.That(problems, Is.Empty, "Objective problems:\n" + string.Join("\n", problems));
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- EveryAlsoCompletes_StaysInItsQuest_WithNoCycles: An objective only also completes objectives of its own quest, and never (through others) itself --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("An objective's Also Completes only lists objectives of its own quest, and never leads back to itself, directly or through others.")]
        public void EveryAlsoCompletes_StaysInItsQuest_WithNoCycles()
        {
            List<string> problems = new();
            foreach (Quest quest in LoadAll<Quest>())
            {
                foreach (QuestObjective objective in quest.Objectives.Where(o => o != null))
                {
                    foreach (QuestObjective alsoCompleted in objective.AlsoCompletes)
                    {
                        if (alsoCompleted == null || !quest.HasObjective(alsoCompleted))
                            problems.Add($"{quest.name} › {objective.name}: Also Completes {(alsoCompleted == null ? "an empty slot" : alsoCompleted.name)}, which isn't in {quest.name}");
                    }

                    if (AlsoCompletesItself(objective))
                        problems.Add($"{quest.name} › {objective.name}: Also Completes leads back to itself through a cycle");
                }
            }

            Assert.That(problems, Is.Empty, "Also Completes problems:\n" + string.Join("\n", problems));
        }

        /*---------------------------------------------------------------------------------------
        | --- AlsoCompletesItself: Whether following Also Completes leads back to the start --- |
        ---------------------------------------------------------------------------------------*/
        private static bool AlsoCompletesItself(QuestObjective start)
        {
            HashSet<QuestObjective> visited = new();
            Stack<QuestObjective> toVisit = new(start.AlsoCompletes.Where(o => o != null));
            while (toVisit.Count > 0)
            {
                QuestObjective next = toVisit.Pop();
                if (next == start)
                    return true;

                if (!visited.Add(next))
                    continue;

                foreach (QuestObjective alsoCompleted in next.AlsoCompletes.Where(o => o != null))
                    toVisit.Push(alsoCompleted);
            }
            return false;
        }

        /*----------------------------------------------------------------------------------------------------------------------------
        | --- EveryTurnIn_HasNoCompleteWhenCondition: A turn-in is never completed in advance, so a condition on it is a mistake --- |
        ----------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A turn-in objective has no Complete When condition: a turn-in is never completed in advance.")]
        public void EveryTurnIn_HasNoCompleteWhenCondition()
        {
            List<string> problems = LoadAll<QuestObjective>()
                                        .Where(o => o.IsTurnIn && o.HasCompleteWhenCondition)
                                        .Select(o => $"{AssetDatabase.GetAssetPath(o)}: is a turn-in with a Complete When condition")
                                        .ToList();

            Assert.That(problems, Is.Empty, "Turn-in problems:\n" + string.Join("\n", problems));
        }


        /*---------------
        | --- Shops --- |
        ---------------*/
        /*------------------------------------------------------------------------------------------------------------------
        | --- EveryShopConfig_HasAnItemInEveryEntry: An entry with no item is left out of the shop, so it's a mistake --- |
        ------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("No ShopConfig in the game's assets has an entry with no item; the shop would leave it out.")]
        public void EveryShopConfig_HasAnItemInEveryEntry()
        {
            List<string> problems = new();
            foreach (ShopConfig config in LoadAll<ShopConfig>())
            {
                ShopConfig.ShopItemEntry[] entries = config.ShopInventory ?? new ShopConfig.ShopItemEntry[0];
                for (int i = 0; i < entries.Length; ++i)
                {
                    if (entries[i] == null || entries[i].Item == null)
                        problems.Add($"{AssetDatabase.GetAssetPath(config)}: entry #{i} has no item");
                }
            }

            Assert.That(problems, Is.Empty, "Shop config problems:\n" + string.Join("\n", problems));
        }

        /*--------------------------------------------------------------------------------------------------------------
        | --- Validation_FlagsSelect_AndEachMissingRequiredParameter: The rule the content test and the drawer use --- |
        --------------------------------------------------------------------------------------------------------------*/
        [TestCase(ConditionType.kSelect, new[] { "Clear the Bandit Camp" })]
        [TestCase(ConditionType.kHasQuest, new[] { "" })]
        [TestCase(ConditionType.kCompletedQuest, new string[0])]
        [TestCase(ConditionType.kCompletedObjective, new[] { "Clear the Bandit Camp", "" })]
        [TestCase(ConditionType.kHasLevel, new[] { " " })]
        [TestCase(ConditionType.kHasItem, new[] { "" })]
        [TestCase(ConditionType.kHasItems, new[] { "potion-id", "" })]
        [TestCase(ConditionType.kHasItems, new[] { "potion-id", "lots" })]
        [TestCase(ConditionType.kHasItems, new[] { "potion-id", "0" })]
        [TestCase(ConditionType.kHasItemEquipped, new string[0])]
        [TestCase(ConditionType.kHasSpokenTo, new[] { "" })]
        [TestCase(ConditionType.kWorldFlag, new[] { "" })]
        [Description("A condition left on Select, missing any parameter its type reads, or with a kHasItems quantity that isn't a whole number above 0, is flagged.")]
        public void Validation_FlagsSelect_AndEachMissingRequiredParameter(ConditionType type, string[] parameters)
        {
            Assert.That(ConditionValidation.Problem(type, parameters), Is.Not.Null);
        }

        /*--------------------------------------------------------------------------------------------------------------
        | --- Validation_AcceptsACompleteCondition: Only the parameters a type reads are required, extras are fine --- |
        --------------------------------------------------------------------------------------------------------------*/
        [TestCase(ConditionType.kHasQuest, new[] { "Clear the Bandit Camp", "" })]
        [TestCase(ConditionType.kCompletedObjective, new[] { "Clear the Bandit Camp", "Kill the Bandits" })]
        [TestCase(ConditionType.kHasItems, new[] { "potion-id", "3" })]
        [TestCase(ConditionType.kHasSpokenTo, new[] { "elira" })]
        [Description("A condition with a type and every parameter that type reads is accepted; an unused empty second parameter is fine.")]
        public void Validation_AcceptsACompleteCondition(ConditionType type, string[] parameters)
        {
            Assert.That(ConditionValidation.Problem(type, parameters), Is.Null);
        }
    }
}
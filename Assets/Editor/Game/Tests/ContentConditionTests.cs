/*---------------------------
| File: ContentConditionTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Edit;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    // Every condition authored in the game's assets (dialogue nodes, equipable items, anything holding a Conjunction)
    // has a real condition type and every parameter that type reads. A condition left on Select or missing a parameter
    // can never pass, so it is broken content.
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
        [Description("Every condition in the game's assets has a condition type other than Select, and every parameter its type reads is filled in.")]
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
        [TestCase(ConditionType.kHasItemEquipped, new string[0])]
        [TestCase(ConditionType.kHasSpokenTo, new[] { "" })]
        [Description("A condition left on Select, or missing any parameter its type reads, is flagged.")]
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
        [TestCase(ConditionType.kHasSpokenTo, new[] { "Elira" })]
        [Description("A condition with a type and every parameter that type reads is accepted; an unused empty second parameter is fine.")]
        public void Validation_AcceptsACompleteCondition(ConditionType type, string[] parameters)
        {
            Assert.That(ConditionValidation.Problem(type, parameters), Is.Null);
        }
    }
}
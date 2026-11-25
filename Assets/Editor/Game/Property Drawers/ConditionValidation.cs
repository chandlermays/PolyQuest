/*---------------------------
| File: ConditionValidation.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Tools;

namespace PolyQuest.Edit
{
    // Authoring rules for a Conjunction.Condition: a real condition type, with every parameter that type reads filled in,
    // a kHasItems quantity that's a whole number above 0, every item ID resolving to an item, and a kHasSpokenTo that
    // names an NPC who exists. Shared by the ConditionPropertyDrawer's warning and the content test that checks every
    // condition in the game.
    public static class ConditionValidation
    {
        /*-----------------------------------------------------------------------------------
        | --- RequiredParameters: The parameters a condition type reads, in their order --- |
        -----------------------------------------------------------------------------------*/
        public static string[] RequiredParameters(ConditionType type)
        {
            switch (type)
            {
                case ConditionType.kHasQuest:
                case ConditionType.kCompletedQuest:
                    return new[] { "Quest" };

                case ConditionType.kCompletedObjective:
                    return new[] { "Quest", "Objective" };

                case ConditionType.kHasLevel:
                    return new[] { "Minimum Level" };

                case ConditionType.kHasItem:
                case ConditionType.kHasItemEquipped:
                    return new[] { "Item" };

                case ConditionType.kHasItems:
                    return new[] { "Item", "Quantity" };

                case ConditionType.kHasSpokenTo:
                    return new[] { "NPC ID" };

                default:
                    return Array.Empty<string>();
            }
        }

        /*-------------------------------------------------------------------------------------------
        | --- Problem: What's wrong with a condition as authored, or null when nothing is wrong --- |
        -------------------------------------------------------------------------------------------*/
        public static string Problem(ConditionType type, IReadOnlyList<string> parameters)
        {
            if (type == ConditionType.kSelect)
                return "No condition selected — this condition will never pass.";

            string[] required = RequiredParameters(type);
            for (int i = 0; i < required.Length; ++i)
            {
                if (parameters == null || parameters.Count <= i || string.IsNullOrWhiteSpace(parameters[i]))
                    return $"{required[i]} is empty — this condition will never pass.";
            }

            if (type == ConditionType.kHasItems && (!int.TryParse(parameters[1], out int quantity) || quantity <= 0))
                return $"Quantity '{parameters[1]}' isn't a whole number above 0 — this condition will never pass.";

            return null;
        }

        /*---------------------------------------------------------------------------------------------------------------
        | --- Problem: The same check, read from a serialized Conjunction.Condition, plus whether its NPC ID exists --- |
        ---------------------------------------------------------------------------------------------------------------*/
        public static string Problem(SerializedProperty condition)
        {
            SerializedProperty type = condition.FindPropertyRelative("m_condition");
            SerializedProperty parameters = condition.FindPropertyRelative("m_parameters");
            if (type == null || parameters == null)
                return null;

            List<string> values = new();
            for (int i = 0; i < parameters.arraySize; ++i)
                values.Add(parameters.GetArrayElementAtIndex(i).stringValue);

            ConditionType conditionType = (ConditionType)type.enumValueIndex;
            string problem = Problem(conditionType, values);
            if (problem != null)
                return problem;

            if (conditionType == ConditionType.kHasSpokenTo && !NpcCatalog.Exists(values[0]))
                return $"No NPC has the ID '{values[0]}' — this condition will never pass.";

            if (IsItemCondition(conditionType) && !ItemExists(values[0]))
                return $"No item has the ID '{values[0]}' — this condition will never pass.";

            return null;
        }

        /*--------------------------------------------------------------------------------
        | --- IsItemCondition: Whether the condition's first parameter is an item ID --- |
        --------------------------------------------------------------------------------*/
        private static bool IsItemCondition(ConditionType type)
        {
            return type == ConditionType.kHasItem || type == ConditionType.kHasItems || type == ConditionType.kHasItemEquipped;
        }

        /*--------------------------------------------------------------------------------------------------------------
        | --- ItemExists: Whether FindByID resolves the ID, rechecking Resources for items made since it was built --- |
        --------------------------------------------------------------------------------------------------------------*/
        private static bool ItemExists(string itemID)
        {
            if (InventoryItem.FindByID(itemID) != null)
                return true;

            // FindByID's registry is built once per domain; an item created in this editor session isn't in it yet
            foreach (InventoryItem item in Resources.LoadAll<InventoryItem>(""))
            {
                if (item.ID == itemID)
                    return true;
            }
            return false;
        }
    }
}
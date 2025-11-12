using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
//---------------------------------
using PolyQuest.Tools;
using PolyQuest.Quests;
using PolyQuest.Inventories;

namespace PolyQuest.Edit
{
    [CustomPropertyDrawer(typeof(Condition.Predicate))]
    public class ConditionPropertyDrawer : PropertyDrawer
    {
        private Dictionary<string, Quest> m_quests;
        private Dictionary<string, InventoryItem> m_items;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty predicate = property.FindPropertyRelative("m_predicate");
            SerializedProperty parameters = property.FindPropertyRelative("m_parameters");
            SerializedProperty negate = property.FindPropertyRelative("m_negate");
            float propertyHeight = EditorGUI.GetPropertyHeight(predicate);
            position.height = propertyHeight;
            EditorGUI.PropertyField(position, predicate);

            PredicateType selectedPredicate = (PredicateType)predicate.enumValueIndex;

            if (selectedPredicate == PredicateType.kSelect)
                return;

            while (parameters.arraySize < 2)
            {
                parameters.InsertArrayElementAtIndex(0);
            }
            SerializedProperty param0 = parameters.GetArrayElementAtIndex(0);
            SerializedProperty param1 = parameters.GetArrayElementAtIndex(1);

            if (selectedPredicate == PredicateType.kHasQuest || selectedPredicate == PredicateType.kCompletedQuest ||
                selectedPredicate == PredicateType.kCompletedObjective || selectedPredicate == PredicateType.kDoesNotHaveQuest)
            {
                position.y += propertyHeight;
                DrawQuest(position, param0);
                if (selectedPredicate == PredicateType.kCompletedObjective)
                {
                    position.y += propertyHeight;
                    DrawObjective(position, param1, param0);
                }
            }

            if (selectedPredicate == PredicateType.kHasItem || selectedPredicate == PredicateType.kHasItems ||
                    selectedPredicate == PredicateType.kHasItemEquipped)
            {
                position.y += propertyHeight;
                DrawInventoryItemList(position, param0, selectedPredicate == PredicateType.kHasItems, selectedPredicate == PredicateType.kHasItemEquipped);
                if (selectedPredicate == PredicateType.kHasItems)
                {
                    position.y += propertyHeight;
                    DrawIntSlider(position, "Quantity Needed:", param1, 1, 100);
                }
            }

            if (selectedPredicate == PredicateType.kHasLevel)
            {
                position.y += propertyHeight;
                DrawIntSlider(position, "Minimum Level:", param0, 1, 100);
            }

            position.y += propertyHeight;
            EditorGUI.PropertyField(position, negate);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty predicate = property.FindPropertyRelative("m_predicate");
            float propertyHeight = EditorGUI.GetPropertyHeight(predicate);
            PredicateType selectedPredicate = (PredicateType)predicate.enumValueIndex;
            switch (selectedPredicate)
            {
                // No parameters
                case PredicateType.kSelect:
                    return propertyHeight;

                // Predicate + One parameter + Negate
                case PredicateType.kHasQuest:
                case PredicateType.kDoesNotHaveQuest:
                case PredicateType.kCompletedQuest:
                case PredicateType.kHasItem:
                case PredicateType.kHasItemEquipped:
                case PredicateType.kHasLevel:
                    return propertyHeight * 3.0f;

                // Predicate + Two parameters + Negate
                case PredicateType.kCompletedObjective:
                case PredicateType.kHasItems:
                    return propertyHeight * 4.0f;
            }
            // default
            return propertyHeight * 2;
        }

        private void LoadQuestDictionary()
        {
            if (m_quests != null)
                return;

            m_quests = new();
            foreach (Quest quest in Resources.LoadAll<Quest>(""))
            {
                m_quests[quest.name] = quest;
            }
        }

        private void DrawQuest(Rect position, SerializedProperty element)
        {
            LoadQuestDictionary();
            List<string> names = m_quests.Keys.ToList();
            int index = names.IndexOf(element.stringValue);

            EditorGUI.BeginProperty(position, new GUIContent("Quest:"), element);
            int newIndex = EditorGUI.Popup(position, "Quest:", index, names.ToArray());
            if (newIndex != index)
            {
                element.stringValue = names[newIndex];
            }

            EditorGUI.EndProperty();
        }

        private void DrawObjective(Rect position, SerializedProperty element, SerializedProperty selectedQuest)
        {
            string questName = selectedQuest.stringValue;
            if (!m_quests.ContainsKey(questName))
                return;

            List<string> references = new();
            List<string> descriptions = new();
            foreach (Quest.Objective objective in m_quests[questName].Objectives)
            {
                references.Add(objective.Identifier);
                descriptions.Add(objective.Description);
            }

            int index = references.IndexOf(element.stringValue);
            EditorGUI.BeginProperty(position, new GUIContent("Objective:"), element);
            int newIndex = EditorGUI.Popup(position, "Objective:", index, descriptions.ToArray());
            if (newIndex != index)
            {
                element.stringValue = references[newIndex];
            }
            EditorGUI.EndProperty();
        }

        private void LoadItemDictionary()
        {
            if (m_items != null)
                return;

            m_items = new();
            foreach (InventoryItem item in Resources.LoadAll<InventoryItem>(""))
            {
                m_items[item.ID] = item;
            }
        }

        private void DrawInventoryItemList(Rect position, SerializedProperty element, bool stackable = false, bool equipment = false)
        {
            LoadItemDictionary();
            List<string> itemIDs = m_items.Keys.ToList();
            if (stackable)
                itemIDs = itemIDs.Where(id => m_items[id].IsStackable).ToList();

            if (equipment)
                itemIDs = itemIDs.Where(id => m_items[id] is EquipableItem eqItem).ToList();

            List<string> itemNames = new();
            foreach (string id in itemIDs)
            {
                itemNames.Add(m_items[id].name);
            }

            int index = itemIDs.IndexOf(element.stringValue);
            EditorGUI.BeginProperty(position, new GUIContent("Item:"), element);
            int newIndex = EditorGUI.Popup(position, "Item:", index, itemNames.ToArray());
            if (newIndex != index)
            {
                element.stringValue = itemIDs[newIndex];
            }
        }

        private static void DrawIntSlider(Rect position, string caption, SerializedProperty intParam, int minLevel = 1, int maxLevel = 100)
        {
            EditorGUI.BeginProperty(position, new GUIContent(caption), intParam);
            if (!int.TryParse(intParam.stringValue, out int value))
            {
                value = 1;
            }

            EditorGUI.BeginChangeCheck();
            int result = EditorGUI.IntSlider(position, caption, value, minLevel, maxLevel);
            if (EditorGUI.EndChangeCheck())
            {
                intParam.stringValue = $"{result}";
            }

            EditorGUI.EndProperty();
        }
    }
}
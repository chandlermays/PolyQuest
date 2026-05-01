/*---------------------------
File: ConditionPropertyDrawer.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Quests;
using PolyQuest.Tools;

namespace PolyQuest.Edit
{
    [CustomPropertyDrawer(typeof(Conjunction.Condition))]
    public class ConditionPropertyDrawer : PropertyDrawer
    {
        private Dictionary<string, Quest> m_quests;
        private Dictionary<string, InventoryItem> m_items;
        private Dictionary<string, Dictionary<string, QuestObjective>> m_objectives;

        // Persists the selected quest filter per-property across repaints (editor session only)
        private readonly Dictionary<string, string> m_itemQuestFilter = new();

        /*-------------------------------------------------------------
        | --- OnGUI: Called for rendering and handling GUI events --- |
        -------------------------------------------------------------*/
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty predicate = property.FindPropertyRelative("m_predicate");
            SerializedProperty parameters = property.FindPropertyRelative("m_parameters");
            SerializedProperty negate = property.FindPropertyRelative("m_negate");

            float propertyHeight = EditorGUI.GetPropertyHeight(predicate);
            position.height = propertyHeight;
            EditorGUI.PropertyField(position, predicate);

            ConditionType selectedPredicate = (ConditionType)predicate.enumValueIndex;

            if (selectedPredicate == ConditionType.kSelect)
                return;

            while (parameters.arraySize < 2)
                parameters.InsertArrayElementAtIndex(0);

            SerializedProperty param0 = parameters.GetArrayElementAtIndex(0);
            SerializedProperty param1 = parameters.GetArrayElementAtIndex(1);

            // ---- Quest / Objective predicates (unchanged) ----
            if (selectedPredicate == ConditionType.kHasQuest ||
                selectedPredicate == ConditionType.kDoesNotHaveQuest ||
                selectedPredicate == ConditionType.kCompletedQuest ||
                selectedPredicate == ConditionType.kCompletedObjective)
            {
                position.y += propertyHeight;
                DrawQuest(position, param0);

                if (selectedPredicate == ConditionType.kCompletedObjective)
                {
                    position.y += propertyHeight;
                    DrawObjective(position, param1, param0);
                }
            }

            // ---- Item predicates — quest-filtered item list ----
            if (selectedPredicate == ConditionType.kHasItem ||
                selectedPredicate == ConditionType.kHasItems ||
                selectedPredicate == ConditionType.kHasItemEquipped)
            {
                bool stackable = selectedPredicate == ConditionType.kHasItems;
                bool equipment = selectedPredicate == ConditionType.kHasItemEquipped;
                string filterKey = property.propertyPath;

                // Row 1: Optional quest scope (narrows item list to that quest's QuestItems)
                position.y += propertyHeight;
                string selectedQuestName = DrawQuestFilter(position, filterKey);

                // Row 2: Item list, optionally narrowed to QuestItems linked to the chosen quest
                position.y += propertyHeight;
                DrawInventoryItemList(position, param0, stackable, equipment, selectedQuestName);

                if (stackable)
                {
                    position.y += propertyHeight;
                    DrawIntSlider(position, "Quantity Needed:", param1, 1, 100);
                }
            }

            if (selectedPredicate == ConditionType.kHasLevel)
            {
                position.y += propertyHeight;
                DrawIntSlider(position, "Minimum Level:", param0, 1, 100);
            }

            position.y += propertyHeight;
            EditorGUI.PropertyField(position, negate);
        }

        /*------------------------------------------------------------------------
        | --- GetProperyHeight: Returns the height of the property in pixels --- |
        ------------------------------------------------------------------------*/
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty predicate = property.FindPropertyRelative("m_predicate");
            float propertyHeight = EditorGUI.GetPropertyHeight(predicate);
            ConditionType selectedPredicate = (ConditionType)predicate.enumValueIndex;

            switch (selectedPredicate)
            {
                case ConditionType.kSelect:
                    return propertyHeight;

                // Condition + Quest filter + Item + Negate
                case ConditionType.kHasItem:
                case ConditionType.kHasItemEquipped:
                    return propertyHeight * 4.0f;

                // Condition + Quest filter + Item + Quantity + Negate
                case ConditionType.kHasItems:
                    return propertyHeight * 5.0f;

                // Condition + Quest + Negate
                case ConditionType.kHasQuest:
                case ConditionType.kDoesNotHaveQuest:
                case ConditionType.kCompletedQuest:
                case ConditionType.kHasLevel:
                    return propertyHeight * 3.0f;

                // Condition + Quest + Objective + Negate
                case ConditionType.kCompletedObjective:
                    return propertyHeight * 4.0f;
            }

            return propertyHeight * 2;
        }

        /*-------------------------------------------------------------------------------------------------------
        | --- LoadQuestDictionary: Caches all Quests in a name-to-asset dictionary for quick lookup by name --- |
        -------------------------------------------------------------------------------------------------------*/
        private void LoadQuestDictionary()
        {
            if (m_quests != null)
                return;

            m_quests = new Dictionary<string, Quest>();
            foreach (Quest quest in Resources.LoadAll<Quest>(""))
                m_quests[quest.name] = quest;
        }

        /*---------------------------------------------------------------------------------------------------------------------------------
        | --- LoadObjectiveDictionary: Caches all QuestObjectives in a nested dictionary for quick lookup by quest and objective name --- |
        ---------------------------------------------------------------------------------------------------------------------------------*/
        private void LoadObjectiveDictionary()
        {
            if (m_objectives != null)
                return;

            LoadQuestDictionary();
            m_objectives = new Dictionary<string, Dictionary<string, QuestObjective>>();

            foreach (var (questName, quest) in m_quests)
            {
                m_objectives[questName] = new Dictionary<string, QuestObjective>();
                foreach (QuestObjective objective in quest.Objectives)
                    m_objectives[questName][objective.name] = objective;
            }
        }

        /*---------------------------------------------------------------------
        | --- DrawQuest: Renders a dropdown for selecting a Quest by name --- |
        ---------------------------------------------------------------------*/
        private void DrawQuest(Rect position, SerializedProperty element)
        {
            LoadQuestDictionary();
            List<string> names = m_quests.Keys.ToList();
            int index = names.IndexOf(element.stringValue);

            EditorGUI.BeginProperty(position, new GUIContent("Quest:"), element);
            int newIndex = EditorGUI.Popup(position, "Quest:", index, names.ToArray());
            if (newIndex != index)
                element.stringValue = names[newIndex];

            EditorGUI.EndProperty();
        }

        /*--------------------------------------------------------------------------------------------------
        | --- DrawObjective: Renders a dropdown of objectives filtered to the currently selected Quest --- |
        --------------------------------------------------------------------------------------------------*/
        private void DrawObjective(Rect position, SerializedProperty element, SerializedProperty selectedQuest)
        {
            LoadObjectiveDictionary();

            string questName = selectedQuest.stringValue;
            if (!m_objectives.ContainsKey(questName))
                return;

            Dictionary<string, QuestObjective> questObjectives = m_objectives[questName];
            List<string> assetNames = questObjectives.Keys.ToList();
            List<string> displayLabels = assetNames.Select(n => questObjectives[n].name).ToList();

            int index = assetNames.IndexOf(element.stringValue);

            EditorGUI.BeginProperty(position, new GUIContent("Objective:"), element);
            int newIndex = EditorGUI.Popup(position, "Objective:", index, displayLabels.ToArray());
            if (newIndex != index)
                element.stringValue = assetNames[newIndex];

            EditorGUI.EndProperty();
        }

        /*-----------------------------------------------------------------------------------------------------------
        | --- LoadItemDictionary: Caches all InventoryItems in an ID-to-asset dictionary for quick lookup by ID --- |
        -----------------------------------------------------------------------------------------------------------*/
        private void LoadItemDictionary()
        {
            if (m_items != null)
                return;

            m_items = new Dictionary<string, InventoryItem>();
            foreach (InventoryItem item in Resources.LoadAll<InventoryItem>(""))
                m_items[item.ID] = item;
        }

        /*--------------------------------------------------------------------------------------------------------
        | --- DrawQuestFilter: Renders an optional Quest filter dropdown and returns the selected Quest name --- |
        --------------------------------------------------------------------------------------------------------*/
        private string DrawQuestFilter(Rect position, string filterKey)
        {
            LoadQuestDictionary();

            List<string> questNames = m_quests.Keys.ToList();
            List<string> options = new List<string> { "(Any)" };
            options.AddRange(questNames);

            if (!m_itemQuestFilter.ContainsKey(filterKey))
                m_itemQuestFilter[filterKey] = null;

            string current = m_itemQuestFilter[filterKey];
            int currentIndex = current != null ? questNames.IndexOf(current) + 1 : 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, "Filter by Quest:", currentIndex, options.ToArray());
            if (EditorGUI.EndChangeCheck())
                m_itemQuestFilter[filterKey] = newIndex == 0 ? null : questNames[newIndex - 1];

            return m_itemQuestFilter[filterKey];
        }

        /*------------------------------------------------------------------------------------------------------
        | --- DrawInventoryItemList: Renders a dropdown of InventoryItems filtered by type and Quest scope --- |
        ------------------------------------------------------------------------------------------------------*/
        private void DrawInventoryItemList(Rect position, SerializedProperty element, bool stackable = false, bool equipment = false, string questFilter = null)
        {
            LoadItemDictionary();

            IEnumerable<string> itemIDs = m_items.Keys;

            if (questFilter != null && m_quests.TryGetValue(questFilter, out Quest filterQuest))
            {
                itemIDs = itemIDs.Where(id => m_items[id] is QuestItem qi && qi.Quest == filterQuest);
            }

            if (stackable)
                itemIDs = itemIDs.Where(id => m_items[id].IsStackable);

            if (equipment)
                itemIDs = itemIDs.Where(id => m_items[id] is EquipableItem);

            List<string> filteredIDs = itemIDs.ToList();
            List<string> displayNames = filteredIDs.Select(id => m_items[id].name).ToList();

            if (filteredIDs.Count == 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(position, "Item:", 0, new[] { questFilter != null
                    ? "No Quest Items linked to selected Quest"
                    : "No items found" });
                EditorGUI.EndDisabledGroup();
                return;
            }

            int index = filteredIDs.IndexOf(element.stringValue);

            EditorGUI.BeginProperty(position, new GUIContent("Item:"), element);
            int newIndex = EditorGUI.Popup(position, "Item:", index, displayNames.ToArray());
            if (newIndex != index)
                element.stringValue = filteredIDs[newIndex];

            EditorGUI.EndProperty();
        }

        /*-------------------------------------------------------------------------
        | --- DrawIntSlider: Renders an integer slider for numeric parameters --- |
        -------------------------------------------------------------------------*/
        private static void DrawIntSlider(Rect position, string caption, SerializedProperty intParam, int minLevel = 1, int maxLevel = 100)
        {
            EditorGUI.BeginProperty(position, new GUIContent(caption), intParam);

            if (!int.TryParse(intParam.stringValue, out int value))
                value = 1;

            EditorGUI.BeginChangeCheck();
            int result = EditorGUI.IntSlider(position, caption, value, minLevel, maxLevel);
            if (EditorGUI.EndChangeCheck())
                intParam.stringValue = $"{result}";

            EditorGUI.EndProperty();
        }
    }
}
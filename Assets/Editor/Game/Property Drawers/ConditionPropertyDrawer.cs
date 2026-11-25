/*---------------------------
| File: ConditionPropertyDrawer.cs
| Author: Chandler Mays
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

        // Height of the warning shown under the dropdown when the condition can never pass
        private static float WarningHeight => EditorGUIUtility.singleLineHeight * 2f;

        /*-------------------------------------------------------------
        | --- OnGUI: Called for rendering and handling GUI events --- |
        -------------------------------------------------------------*/
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty condition = property.FindPropertyRelative("m_condition");
            SerializedProperty parameters = property.FindPropertyRelative("m_parameters");
            SerializedProperty negate = property.FindPropertyRelative("m_negate");

            float propertyHeight = EditorGUI.GetPropertyHeight(condition);
            position.height = propertyHeight;
            EditorGUI.PropertyField(position, condition);

            // A condition left on Select, or missing a parameter it reads, can never pass: say so under the dropdown
            string problem = ConditionValidation.Problem(property);
            if (problem != null)
            {
                Rect warning = new Rect(position.x, position.y + propertyHeight, position.width, WarningHeight);
                EditorGUI.HelpBox(warning, problem, MessageType.Error);

                // The rows below start under the warning instead of under the dropdown
                position.y += WarningHeight;
            }

            ConditionType selectedCondition = (ConditionType)condition.enumValueIndex;

            if (selectedCondition == ConditionType.kSelect)
                return;

            while (parameters.arraySize < 2)
                parameters.InsertArrayElementAtIndex(0);

            SerializedProperty param0 = parameters.GetArrayElementAtIndex(0);
            SerializedProperty param1 = parameters.GetArrayElementAtIndex(1);

            // ---- Quest / Objective conditions ----
            if (selectedCondition == ConditionType.kHasQuest ||
                selectedCondition == ConditionType.kCompletedQuest ||
                selectedCondition == ConditionType.kCompletedObjective)
            {
                position.y += propertyHeight;
                DrawQuest(position, param0);

                if (selectedCondition == ConditionType.kCompletedObjective)
                {
                    position.y += propertyHeight;
                    DrawObjective(position, param1, param0);
                }
            }

            // ---- Item conditions � quest-filtered item list ----
            if (selectedCondition == ConditionType.kHasItem ||
                selectedCondition == ConditionType.kHasItems ||
                selectedCondition == ConditionType.kHasItemEquipped)
            {
                bool stackable = selectedCondition == ConditionType.kHasItems;
                bool equipment = selectedCondition == ConditionType.kHasItemEquipped;
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

            if (selectedCondition == ConditionType.kHasLevel)
            {
                position.y += propertyHeight;
                DrawIntSlider(position, "Minimum Level:", param0, 1, 100);
            }

            if (selectedCondition == ConditionType.kHasSpokenTo)
            {
                position.y += propertyHeight;
                DrawNpcIdField(position, param0);
            }

            if (selectedCondition == ConditionType.kWorldFlag)
            {
                position.y += propertyHeight;
                DrawWorldFlagField(position, param0);
            }

            position.y += propertyHeight;
            EditorGUI.PropertyField(position, negate);
        }

        /*------------------------------------------------------------------------
        | --- GetProperyHeight: Returns the height of the property in pixels --- |
        ------------------------------------------------------------------------*/
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float warningHeight = ConditionValidation.Problem(property) != null ? WarningHeight : 0f;
            return GetFieldsHeight(property) + warningHeight;
        }

        /*---------------------------------------------------------------------------------
        | --- GetFieldsHeight: Returns the height of the condition's fields in pixels --- |
        ---------------------------------------------------------------------------------*/
        private static float GetFieldsHeight(SerializedProperty property)
        {
            SerializedProperty condition = property.FindPropertyRelative("m_condition");
            float propertyHeight = EditorGUI.GetPropertyHeight(condition);
            ConditionType selectedCondition = (ConditionType)condition.enumValueIndex;

            switch (selectedCondition)
            {
                case ConditionType.kSelect:
                    return propertyHeight;

                // Condition + Quest filter + Item
                case ConditionType.kHasItem:
                case ConditionType.kHasItemEquipped:
                    return propertyHeight * 4.0f;

                // Condition + Quest filter + Item + Quantity
                case ConditionType.kHasItems:
                    return propertyHeight * 5.0f;

                // Condition + Quest
                case ConditionType.kHasQuest:
                case ConditionType.kCompletedQuest:
                case ConditionType.kHasLevel:
                    return propertyHeight * 3.0f;

                // Condition + Quest + Objective
                case ConditionType.kCompletedObjective:
                    return propertyHeight * 4.0f;

                // Condition + NPC ID dropdown
                case ConditionType.kHasSpokenTo:
                    return propertyHeight * 3.0f;

                // Condition + World Flag dropdown
                case ConditionType.kWorldFlag:
                    return propertyHeight * 3.0f;
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

        /*-------------------------------------------------------------------------------------------------
        | --- DrawWorldFlagField: Draws a dropdown of the WorldFlags the game can find (in Resources) --- |
        -------------------------------------------------------------------------------------------------*/
        private void DrawWorldFlagField(Rect position, SerializedProperty element)
        {
            // Index 0 is "nothing chosen"; then every flag; then the current value if no flag has it, so it isn't lost silently
            List<string> values = new() { "" };
            List<string> labels = new() { "Select World Flag" };
            foreach (WorldFlag flag in ConditionValidation.WorldFlags.Where(f => f != null).OrderBy(f => f.ID))
            {
                if (string.IsNullOrWhiteSpace(flag.ID) || values.Contains(flag.ID))
                    continue;

                values.Add(flag.ID);
                labels.Add(flag.ID);
            }

            string current = element.stringValue;
            if (!values.Contains(current))
            {
                values.Add(current);
                labels.Add($"{current}  (no WorldFlag has this ID)");
            }

            EditorGUI.BeginProperty(position, new GUIContent("Flag:"), element);
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUI.Popup(position, "Flag:", values.IndexOf(current), labels.ToArray());
            if (EditorGUI.EndChangeCheck())
                element.stringValue = values[selected];
            EditorGUI.EndProperty();
        }

        /*--------------------------------------------------------------------------------
        | --- DrawNpcIdField: Draws a dropdown of the NPC IDs that exist in the game --- |
        --------------------------------------------------------------------------------*/
        private void DrawNpcIdField(Rect position, SerializedProperty element)
        {
            // Index 0 is "nothing chosen"; then every NPC; then the current value if no NPC has it, so it isn't lost silently
            List<string> values = new() { "" };
            List<string> labels = new() { "Select NPC" };
            foreach (NpcCatalog.Entry npc in NpcCatalog.All.OrderBy(n => n.ID))
            {
                if (string.IsNullOrWhiteSpace(npc.ID) || values.Contains(npc.ID))
                    continue;

                values.Add(npc.ID);
                labels.Add($"{npc.ID}  ({npc.DisplayName})");
            }

            string current = element.stringValue;
            if (!values.Contains(current))
            {
                values.Add(current);
                labels.Add($"{current}  (no NPC has this ID)");
            }

            EditorGUI.BeginProperty(position, new GUIContent("NPC ID:"), element);
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUI.Popup(position, "NPC ID:", values.IndexOf(current), labels.ToArray());
            if (EditorGUI.EndChangeCheck())
                element.stringValue = values[selected];
            EditorGUI.EndProperty();
        }
    }
}
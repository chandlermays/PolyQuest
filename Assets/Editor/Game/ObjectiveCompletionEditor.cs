using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
//---------------------------------
using PolyQuest.Quests;

namespace PolyQuest.Edit
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Custom Inspector for ObjectiveCompletion that filters available objectives            *
     *       based on the currently selected Quest, mirroring the ConditionPropertyDrawer          *
     *       behaviour for quest/objective predicate pairs.                                        *
     * ------------------------------------------------------------------------------------------- */
    [CustomEditor(typeof(ObjectiveCompletion))]
    public class ObjectiveCompletionEditor : Editor
    {
        // Cached asset dictionaries (rebuilt whenever the quest selection changes)
        private Dictionary<string, Quest> m_quests;
        private Dictionary<string, List<QuestObjective>> m_objectivesByQuest;

        // Serialized property references
        private SerializedProperty m_questManagerProp;
        private SerializedProperty m_questProp;
        private SerializedProperty m_objectiveProp;

        /*-------------------------------------------------------------
        | --- OnEnable: Fetch serialized property references once --- |
        -------------------------------------------------------------*/
        private void OnEnable()
        {
            m_questManagerProp = serializedObject.FindProperty("m_questManager");
            m_questProp = serializedObject.FindProperty("m_quest");
            m_objectiveProp = serializedObject.FindProperty("m_objective");
        }

        /*--------------------------------------------------------------
        | --- OnInspectorGUI: Draw the custom inspector             --- |
        --------------------------------------------------------------*/
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // --- Quest Manager (plain object field) ---
            EditorGUILayout.PropertyField(m_questManagerProp);

            // --- Quest dropdown ---
            LoadQuestDictionary();
            List<string> questNames = m_quests.Keys.ToList();

            Quest currentQuest = m_questProp.objectReferenceValue as Quest;
            int questIndex = currentQuest != null ? questNames.IndexOf(currentQuest.name) : -1;

            int newQuestIndex = EditorGUILayout.Popup("Quest", questIndex, questNames.ToArray());
            if (newQuestIndex >= 0 && newQuestIndex != questIndex)
            {
                m_questProp.objectReferenceValue = m_quests[questNames[newQuestIndex]];
                // Clear the objective when the quest changes to avoid a stale reference
                m_objectiveProp.objectReferenceValue = null;
            }

            // --- Objective dropdown (filtered to the selected quest) ---
            Quest selectedQuest = m_questProp.objectReferenceValue as Quest;
            if (selectedQuest != null)
            {
                LoadObjectiveDictionary();

                if (m_objectivesByQuest.TryGetValue(selectedQuest.name, out List<QuestObjective> objectives)
                    && objectives.Count > 0)
                {
                    List<string> objectiveNames = objectives.Select(o => o.name).ToList();

                    QuestObjective currentObjective = m_objectiveProp.objectReferenceValue as QuestObjective;
                    int objectiveIndex = currentObjective != null
                        ? objectives.IndexOf(currentObjective)
                        : -1;

                    int newObjectiveIndex = EditorGUILayout.Popup("Objective", objectiveIndex, objectiveNames.ToArray());
                    if (newObjectiveIndex >= 0 && newObjectiveIndex != objectiveIndex)
                    {
                        m_objectiveProp.objectReferenceValue = objectives[newObjectiveIndex];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("The selected Quest has no objectives.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Popup("Objective", -1, new[] { "Select a Quest first" });
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }

        /*-----------------------------------------------------------------
        | --- LoadQuestDictionary: Populate the quest lookup (cached) --- |
        -----------------------------------------------------------------*/
        private void LoadQuestDictionary()
        {
            if (m_quests != null)
                return;

            m_quests = new Dictionary<string, Quest>();
            foreach (Quest quest in Resources.LoadAll<Quest>(""))
            {
                m_quests[quest.name] = quest;
            }
        }

        /*-----------------------------------------------------------------------
        | --- LoadObjectiveDictionary: Populate objective lookup (cached)    --- |
        -----------------------------------------------------------------------*/
        private void LoadObjectiveDictionary()
        {
            if (m_objectivesByQuest != null)
                return;

            LoadQuestDictionary();
            m_objectivesByQuest = new Dictionary<string, List<QuestObjective>>();

            foreach (var (questName, quest) in m_quests)
            {
                m_objectivesByQuest[questName] = quest.Objectives.ToList();
            }
        }
    }
}
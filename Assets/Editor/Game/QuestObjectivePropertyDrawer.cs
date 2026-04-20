using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Quests;

namespace PolyQuest.Edit
{
    public class QuestObjectivePropertyDrawer
    {
        private QuestRegion m_selectedRegion;
        private Dictionary<string, Quest> m_quests;
        private Dictionary<string, List<QuestObjective>> m_objectivesByQuest;

        private readonly SerializedProperty m_questProperty;
        private readonly SerializedProperty m_objectiveProperty;

        public QuestObjectivePropertyDrawer(SerializedProperty questProperty, SerializedProperty objectiveProperty)
        {
            m_questProperty = questProperty;
            m_objectiveProperty = objectiveProperty;
        }

        public void OnInspectorGUI()
        {
            LoadQuestDictionary();

            Quest currentQuest = m_questProperty.objectReferenceValue as Quest;
            QuestRegion selectedRegion = currentQuest != null ? currentQuest.Region : m_selectedRegion;
            QuestRegion newRegion = (QuestRegion)EditorGUILayout.EnumPopup("Region", selectedRegion);
            m_selectedRegion = newRegion;

            if (currentQuest != null && currentQuest.Region != newRegion)
            {
                m_questProperty.objectReferenceValue = null;
                m_objectiveProperty.objectReferenceValue = null;
                currentQuest = null;
            }

            List<Quest> questsForRegion = GetQuestsForRegion(m_selectedRegion);
            List<string> questNames = questsForRegion.Select(q => q.name).ToList();
            int questIndex = currentQuest != null ? questNames.IndexOf(currentQuest.name) : -1;

            if (questsForRegion.Count > 0)
            {
                int newQuestIndex = EditorGUILayout.Popup("Quest", questIndex, questNames.ToArray());
                if (newQuestIndex >= 0 && newQuestIndex != questIndex)
                {
                    m_questProperty.objectReferenceValue = questsForRegion[newQuestIndex];
                    m_objectiveProperty.objectReferenceValue = null;
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Popup("Quest", -1, new[] { "No Quests available" });
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox("The selected Region has no Quests.", MessageType.Warning);
            }

            Quest selectedQuest = m_questProperty.objectReferenceValue as Quest;
            if (selectedQuest != null && selectedQuest.Region == m_selectedRegion)
            {
                LoadObjectiveDictionary();

                if (m_objectivesByQuest.TryGetValue(selectedQuest.name, out List<QuestObjective> objectives)
                    && objectives.Count > 0)
                {
                    List<string> objectiveNames = objectives.Select(o => o.name).ToList();
                    QuestObjective currentObjective = m_objectiveProperty.objectReferenceValue as QuestObjective;
                    int objectiveIndex = currentObjective != null ? objectives.IndexOf(currentObjective) : -1;

                    int newObjectiveIndex = EditorGUILayout.Popup("Objective", objectiveIndex, objectiveNames.ToArray());
                    if (newObjectiveIndex >= 0 && newObjectiveIndex != objectiveIndex)
                    {
                        m_objectiveProperty.objectReferenceValue = objectives[newObjectiveIndex];
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
        }

        private List<Quest> GetQuestsForRegion(QuestRegion region)
            => m_quests.Values.Where(q => q.Region == region).ToList();

        private void LoadQuestDictionary()
        {
            if (m_quests != null) return;
            m_quests = new Dictionary<string, Quest>();
            foreach (Quest quest in Resources.LoadAll<Quest>(""))
                m_quests[quest.name] = quest;
        }

        private void LoadObjectiveDictionary()
        {
            if (m_objectivesByQuest != null) return;
            LoadQuestDictionary();
            m_objectivesByQuest = new Dictionary<string, List<QuestObjective>>();
            foreach (var (questName, quest) in m_quests)
                m_objectivesByQuest[questName] = quest.Objectives.ToList();
        }
    }
}
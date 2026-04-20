/*---------------------------
File: ObjectiveCompletionEditor.cs
Author: Chandler Mays
----------------------------*/
using UnityEditor;
//---------------------------------
using PolyQuest.Quests;

namespace PolyQuest.Edit
{
    [CustomEditor(typeof(ObjectiveCompletion))]
    public class ObjectiveCompletionEditor : Editor
    {
        private QuestObjectiveSelectorDrawer m_selector;

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_selector = new QuestObjectiveSelectorDrawer(
                serializedObject.FindProperty("m_quest"),
                serializedObject.FindProperty("m_objective")
            );
        }

        /*------------------------------------------------------------------------------
        | --- OnInspectorGUI: Renders the custom inspector GUI in the Unity Editor --- |
        ------------------------------------------------------------------------------*/
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_questManager"));
            m_selector.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
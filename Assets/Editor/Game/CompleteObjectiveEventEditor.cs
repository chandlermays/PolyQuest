using UnityEditor;
//---------------------------------
using PolyQuest.Dialogues;

namespace PolyQuest.Edit
{
    [CustomEditor(typeof(CompleteObjectiveEvent))]
    public class CompleteObjectiveEventEditor : Editor
    {
        private QuestObjectivePropertyDrawer m_selector;

        private void OnEnable()
        {
            m_selector = new QuestObjectivePropertyDrawer(
                serializedObject.FindProperty("m_quest"),
                serializedObject.FindProperty("m_objective")
            );
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_selector.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
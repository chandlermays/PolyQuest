using UnityEditor;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Edit
{
    [CustomPropertyDrawer(typeof(SceneField))]
    public class SceneFieldPropertyDrawer : PropertyDrawer
    {
        /*----------------------------------------------------------------------------------- 
        | --- OnGUI: Overwritten to Create a Custom Inspector Field for Scene Selection --- |
        -----------------------------------------------------------------------------------*/
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sceneAsset = property.FindPropertyRelative("m_sceneAsset");
            SerializedProperty sceneName = property.FindPropertyRelative("m_sceneName");

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            if (sceneAsset != null)
            {
                EditorGUI.BeginChangeCheck();
                Object value = EditorGUI.ObjectField(position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    sceneAsset.objectReferenceValue = value;
                    if (sceneAsset.objectReferenceValue != null)
                    {
                        string path = AssetDatabase.GetAssetPath(sceneAsset.objectReferenceValue);
                        sceneName.stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
                    }
                    else
                    {
                        sceneName.stringValue = "";
                    }
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
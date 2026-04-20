/*---------------------------
File: SceneFieldPropertyDrawer.cs
Author: Chandler Mays
----------------------------*/
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.SceneManagement;

namespace PolyQuest.Edit
{
    /* ----------------------------------------------------------------------------------------------------
     * Role: Provides a custom property drawer for SceneField, enabling scene selection in the Inspector. *
     *                                                                                                    *
     * Responsibilities:                                                                                  *
     *      - Renders a custom object field for selecting SceneAsset references in the Inspector.         *
     *      - Updates the associated scene name string when a scene asset is assigned or cleared.         *
     *      - Integrates with Unity's property drawer system for seamless editor experience.              *
     * -------------------------------------------------------------------------------------------------- */
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
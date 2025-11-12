using UnityEditor;
using UnityEngine;
using System.Collections;
//---------------------------------
using PolyQuest.Tools;

namespace PolyQuest.Edit
{
    [CustomPropertyDrawer(typeof(Condition.Disjunction))]
    public class DisjunctionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.alignment = TextAnchor.MiddleCenter;
            SerializedProperty or = property.FindPropertyRelative("m_OR");
            float propertyHeight = EditorGUIUtility.singleLineHeight;

            Rect upPosition = position;
            upPosition.width -= EditorGUIUtility.labelWidth;
            upPosition.x = position.xMax - upPosition.width;
            upPosition.width /= 3.0f;
            upPosition.height = propertyHeight;

            Rect downPosition = upPosition;
            downPosition.x += upPosition.width;

            Rect deletePosition = upPosition;
            deletePosition.x = position.xMax - deletePosition.width;

            IEnumerator enumerator = or.GetEnumerator();

            int index = 0;
            int itemToRemove = -1;
            int itemToMoveUp = -1;
            int itemToMoveDown = -1;

            while (enumerator.MoveNext())
            {
                if (index > 0)
                {
                    if (GUI.Button(downPosition, "↓"))
                        itemToMoveDown = index - 1;

                    EditorGUI.DropShadowLabel(position, "------OR------", style);
                    position.y += propertyHeight;
                }
                SerializedProperty prop = enumerator.Current as SerializedProperty;
                position.height = EditorGUI.GetPropertyHeight(prop);
                EditorGUI.PropertyField(position, prop);
                position.y += position.height;
                position.height = propertyHeight;
                upPosition.y = deletePosition.y = downPosition.y = position.y;

                if (GUI.Button(deletePosition, "Remove"))
                    itemToRemove = index;

                if (index > 0 && GUI.Button(upPosition, "↑"))
                    itemToMoveUp = index;

                position.y += propertyHeight;
                ++index;
            }

            if (itemToRemove >= 0)
            {
                or.DeleteArrayElementAtIndex(itemToRemove);
            }

            if (itemToMoveUp >= 0)
            {
                or.MoveArrayElement(itemToMoveUp, itemToMoveUp - 1);
            }

            if (itemToMoveDown >= 0)
            {
                or.MoveArrayElement(itemToMoveDown, itemToMoveDown + 1);
            }

            if (GUI.Button(position, "Add Condition"))
            {
                or.InsertArrayElementAtIndex(index);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float result = 0f;
            float propertyHeight = EditorGUIUtility.singleLineHeight;
            IEnumerator enumerator = property.FindPropertyRelative("m_OR").GetEnumerator();
            bool multiple = false;
            while (enumerator.MoveNext())
            {
                SerializedProperty prop = enumerator.Current as SerializedProperty;
                result += EditorGUI.GetPropertyHeight(prop) + propertyHeight;
                if (multiple)
                    result += propertyHeight;

                multiple = true;
            }

            return result += propertyHeight * 1.5f;
        }
    }
}
/*---------------------------
File: Array2DPropertyDrawer.cs
Author: Chandler Mays
----------------------------*/
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.PCG;

namespace PolyQuest.Edit
{
    [CustomPropertyDrawer(typeof(Array2D<TileType>))]
    public class TileTypePropertyDrawer : Array2DWrapperPropertyDrawer<TileType> { }

    public class Array2DWrapperPropertyDrawer<T> : PropertyDrawer
    {
        private const string kArray = "m_array";
        private const string kWidth = "m_width";
        private const string kHeight = "m_height";

        private SerializedProperty m_arrayProperty;
        private SerializedProperty m_widthProperty;
        private SerializedProperty m_heightProperty;

        /*----------------------------------------------------------------------------------------
        | --- GetPropertyHeight: Calculates the height needed to display the property drawer --- |
        ----------------------------------------------------------------------------------------*/
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty heightProp = property.FindPropertyRelative(kHeight);

            int height = Mathf.Max(0, heightProp.intValue);

            float line = EditorGUIUtility.singleLineHeight;
            float vsp = EditorGUIUtility.standardVerticalSpacing;

            float header = line * 3 + vsp * 3;
            float grid = height * (line + vsp);

            return header + grid;
        }

        /*-----------------------------------------------------------------------
        | --- OnGUI: Renders the custom property drawer in the Unity Editor --- |
        -----------------------------------------------------------------------*/
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_arrayProperty = property.FindPropertyRelative(kArray);
            m_widthProperty = property.FindPropertyRelative(kWidth);
            m_heightProperty = property.FindPropertyRelative(kHeight);

            Object target = property.serializedObject.targetObject;

            float line = EditorGUIUtility.singleLineHeight;
            float vsp = EditorGUIUtility.standardVerticalSpacing;

            Rect labelRect = new(position.x, position.y, position.width, line);
            EditorGUI.LabelField(labelRect, label);

            Rect widthRect = new(position.x, labelRect.yMax + vsp, position.width, line);
            Rect heightRect = new(position.x, widthRect.yMax + vsp, position.width, line);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(widthRect, m_widthProperty, new GUIContent("Width"));
            EditorGUI.PropertyField(heightRect, m_heightProperty, new GUIContent("Height"));
            bool sizeChanged = EditorGUI.EndChangeCheck();

            int oldWidth = Mathf.Max(0, m_widthProperty.intValue);
            int oldHeight = Mathf.Max(0, m_heightProperty.intValue);

            if (sizeChanged)
            {
                int newWidth = Mathf.Max(0, m_widthProperty.intValue);
                int newHeight = Mathf.Max(0, m_heightProperty.intValue);

                Undo.RegisterCompleteObjectUndo(target, "Change Pattern Size");
                ResizeArray(m_arrayProperty, newWidth, newHeight, oldWidth, oldHeight);

                EditorUtility.SetDirty(target);
                property.serializedObject.ApplyModifiedProperties();
            }

            int width = Mathf.Max(0, m_widthProperty.intValue);
            int height = Mathf.Max(0, m_heightProperty.intValue);

            float rowY = heightRect.yMax + vsp;
            float cellHeight = line;
            float cellWidth = Mathf.Max(18f, (position.width - 6f) / Mathf.Max(1, width));

            for (int y = 0; y < height; ++y)
            {
                float cellY = rowY + y * (cellHeight + vsp);
                for (int x = 0; x < width; ++x)
                {
                    int index = x + (height - 1 - y) * width;
                    Rect cellRect = new(position.x + x * cellWidth, cellY, cellWidth - 2f, cellHeight);

                    EditorGUI.BeginChangeCheck();
                    SerializedProperty enumProperty = m_arrayProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(cellRect, enumProperty, GUIContent.none);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Change Tile Type");
                        EditorUtility.SetDirty(target);
                    }
                }
            }
        }

        /*----------------------------------------------------------------------------------
        | --- ResizeArray: Resizes the underlying array while preserving existing data --- |
        ----------------------------------------------------------------------------------*/
        private void ResizeArray(SerializedProperty arrayProperty, int newWidth, int newHeight, int oldWidth, int oldHeight)
        {
            int newSize = newWidth * newHeight;
            int oldSize = arrayProperty.arraySize;

            int[] oldValues = new int[oldSize];
            for (int i = 0; i < oldSize; ++i)
            {
                oldValues[i] = arrayProperty.GetArrayElementAtIndex(i).enumValueIndex;
            }

            arrayProperty.arraySize = newSize;

            for (int i = 0; i < newSize; ++i)
            {
                arrayProperty.GetArrayElementAtIndex(i).enumValueIndex = (int)TileType.kEmpty;
            }

            int copyWidth = Mathf.Min(newWidth, oldWidth);
            int copyHeight = Mathf.Min(newHeight, oldHeight);

            for (int y = 0; y < copyHeight; ++y)
            {
                for (int x = 0; x < copyWidth; ++x)
                {
                    int oldIndex = x + y * oldWidth;
                    int newIndex = x + y * newWidth;
                    if (oldIndex >= 0 && oldIndex < oldSize && newIndex >= 0 && newIndex < newSize)
                    {
                        arrayProperty.GetArrayElementAtIndex(newIndex).enumValueIndex = oldValues[oldIndex];
                    }
                }
            }
        }
    }
}
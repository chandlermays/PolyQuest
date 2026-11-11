/*---------------------------
| File: SerializedTestUtil.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Tests
{
    internal static class SerializedTestUtil
    {
        public static void SetInt(Object target, string field, int value)
        {
            Apply(target, field, p => p.intValue = value);
        }

        public static void SetFloat(Object target, string field, float value)
        {
            Apply(target, field, p => p.floatValue = value);
        }

        public static void SetBool(Object target, string field, bool value)
        {
            Apply(target, field, p => p.boolValue = value);
        }

        public static void SetObject(Object target, string field, Object value)
        {
            Apply(target, field, p => p.objectReferenceValue = value);
        }

        public static void SetIntArray(Object target, string field, params int[] values)
        {
            Apply(target, field, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; ++i)
                    p.GetArrayElementAtIndex(i).intValue = values[i];
            });
        }

        private static void Apply(Object target, string field, System.Action<SerializedProperty> set)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(field);

            Assert.That(property, Is.Not.Null, $"'{field}' was not found on {target.GetType().Name}. Was it renamed?");

            set(property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
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
        /*---------------------------------------------
        | --- SetInt: Sets a serialized int field --- |
        ---------------------------------------------*/
        public static void SetInt(Object target, string field, int value)
        {
            Apply(target, field, p => p.intValue = value);
        }

        /*-------------------------------------------------
        | --- SetFloat: Sets a serialized float field --- |
        -------------------------------------------------*/
        public static void SetFloat(Object target, string field, float value)
        {
            Apply(target, field, p => p.floatValue = value);
        }

        /*-----------------------------------------------
        | --- SetBool: Sets a serialized bool field --- |
        -----------------------------------------------*/
        public static void SetBool(Object target, string field, bool value)
        {
            Apply(target, field, p => p.boolValue = value);
        }

        /*---------------------------------------------------
        | --- SetString: Sets a serialized string field --- |
        ---------------------------------------------------*/
        public static void SetString(Object target, string field, string value)
        {
            Apply(target, field, p => p.stringValue = value);
        }

        /*-------------------------------------------------------------
        | --- SetObject: Sets a serialized object reference field --- |
        -------------------------------------------------------------*/
        public static void SetObject(Object target, string field, Object value)
        {
            Apply(target, field, p => p.objectReferenceValue = value);
        }

        /*--------------------------------------------------------------------
        | --- SetEnum: Sets a serialized enum field by its value's index --- |
        --------------------------------------------------------------------*/
        public static void SetEnum<T>(Object target, string field, T value) where T : System.Enum
        {
            int index = System.Array.IndexOf(System.Enum.GetValues(typeof(T)), value);

            Apply(target, field, p => p.enumValueIndex = index);
        }

        /*----------------------------------------------------------------------------
        | --- SetIntArray: Sets a serialized int array field to the given values --- |
        ----------------------------------------------------------------------------*/
        public static void SetIntArray(Object target, string field, params int[] values)
        {
            Apply(target, field, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; ++i)
                    p.GetArrayElementAtIndex(i).intValue = values[i];
            });
        }

        /*--------------------------------------------------------
        | --- SetArraySize: Resizes a serialized array field --- |
        --------------------------------------------------------*/
        public static void SetArraySize(Object target, string field, int size)
        {
            Apply(target, field, p => p.arraySize = size);
        }

        /*-------------------------------------------------------------------------------------------------
        | --- Apply: Finds a serialized field, applies the change, and fails if the field was renamed --- |
        -------------------------------------------------------------------------------------------------*/
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
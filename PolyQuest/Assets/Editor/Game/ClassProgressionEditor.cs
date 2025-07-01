using UnityEditor;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Edit
{
    /*--------------------------------------------------------------------------------------------------- 
    | --- A Custom Editor for Designing the Health and Experience Progression for a Character Class --- |
    ---------------------------------------------------------------------------------------------------*/

    // Represents the Names of Serialized Fields in the 'ClassProgression' class
    public static class Property
    {
        public const string kInitialHealth = "m_initialHealth";
        public const string kHealthIncreasePercentage = "m_healthIncreasePercentage";
        public const string kInitialExperience = "m_initialExperience";
        public const string kExperienceIncreasePercentage = "m_experienceIncreasePercentage";
        public const string kMaxHealthAmounts = "m_maxHealthAmounts";
        public const string kMaxExperienceAmounts = "m_maxExperienceAmounts";
    }

    // Represents the Labels for the Custom Editor Fields
    public static class Labels
    {
        public const string kProgression = "Progression";
        public const string kInitialHealth = "Initial Health";
        public const string kHealthIncreasePercentage = "Health Increase %";
        public const string kInitialExperience = "Initial Experience";
        public const string kExperienceIncreasePercentage = "Experience Increase %";
        public const string kRecalculateHealthButton = "Recalculate Health Amounts";
        public const string kRecalculateExperienceButton = "Recalculate Experience Amounts";
        public const string kMaxHealthAmounts = "Max Health Amounts";
        public const string kMaxExperienceAmounts = "Max Experience Amounts";
        public const string kAddLevelButton = "Add Level";
        public const string kRemoveLevelButton = "Remove Last Level";
    }

    [CustomEditor(typeof(ClassProgression), true)]
    public class ClassProgressionEditor : Editor
    {
        private SerializedProperty m_initialHealth;
        private SerializedProperty m_healthIncreasePercentage;
        private SerializedProperty m_initialExperience;
        private SerializedProperty m_experienceIncreasePercentage;
        private SerializedProperty m_maxHealthAmounts;
        private SerializedProperty m_maxExperienceAmounts;

        /*---------------------------------------------------------------------------------------------------
        | --- InitializeSerializedProperties: Cache the Serialized Properties for the Class Progression --- |
        ---------------------------------------------------------------------------------------------------*/
        private void InitializeSerializedProperties()
        {
            m_initialHealth = serializedObject.FindProperty(Property.kInitialHealth);
            m_healthIncreasePercentage = serializedObject.FindProperty(Property.kHealthIncreasePercentage);
            m_initialExperience = serializedObject.FindProperty(Property.kInitialExperience);
            m_experienceIncreasePercentage = serializedObject.FindProperty(Property.kExperienceIncreasePercentage);
            m_maxHealthAmounts = serializedObject.FindProperty(Property.kMaxHealthAmounts);
            m_maxExperienceAmounts = serializedObject.FindProperty(Property.kMaxExperienceAmounts);
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            InitializeSerializedProperties();
        }

        /*----------------------------------------------------
        | --- OnInspectorGUI: Creates a Custom Inspector --- |
        ----------------------------------------------------*/
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(Labels.kProgression, EditorStyles.boldLabel);

            // Property Fields
            DisplayPropertyField(m_initialHealth, Labels.kInitialHealth);
            DisplayPropertyField(m_healthIncreasePercentage, Labels.kHealthIncreasePercentage);
            DisplayPropertyField(m_initialExperience, Labels.kInitialExperience);
            DisplayPropertyField(m_experienceIncreasePercentage, Labels.kExperienceIncreasePercentage);

            // Recalculate Buttons
            DisplayRecalculateButton(Labels.kRecalculateHealthButton,
                () => ((ClassProgression)target).RecalculateMaxHealthAmounts());

            DisplayRecalculateButton(Labels.kRecalculateExperienceButton,
                () => ((ClassProgression)target).RecalculateMaxExperienceAmounts());

            // Array Properties
            DisplayArrayProperty(m_maxHealthAmounts, Labels.kMaxHealthAmounts);
            DisplayArrayProperty(m_maxExperienceAmounts, Labels.kMaxExperienceAmounts);

            DisplayAddRemoveLevelButtons();

            serializedObject.ApplyModifiedProperties();
        }

        /*------------------------------------------------------------------------------------------- 
        | --- DisplayPropertyField: A Parameterized Method for Displaying Serialized Properties --- |
        -------------------------------------------------------------------------------------------*/
        private void DisplayPropertyField(SerializedProperty property, string label)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }

        /*----------------------------------------------------------------------------------------------- 
        | --- DisplayRecalculateButton: A Parameterized Button for Recalculating Progression Values --- |
        -----------------------------------------------------------------------------------------------*/
        private void DisplayRecalculateButton(string buttonLabel, System.Action recalculateAction)
        {
            if (GUILayout.Button(buttonLabel))
            {
                recalculateAction.Invoke();
            }
        }

        /*------------------------------------------------------------------------------------------------- 
        | --- DisplayArrayProperty: A Parameterized Method for Displaying Serialized Array Properties --- |
        -------------------------------------------------------------------------------------------------*/
        private void DisplayArrayProperty(SerializedProperty arrayProperty, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(element, new GUIContent($"Level {i + 1}"));
            }
        }

        /*------------------------------------------------------------------------------------------------------------ 
        | --- DisplayAddRemoveLevelButtons: A Clickable Button to Add and Remove Levels from the Character Class --- |
        ------------------------------------------------------------------------------------------------------------*/
        private void DisplayAddRemoveLevelButtons()
        {
            if (GUILayout.Button(Labels.kAddLevelButton))
            {
                AddLevel();
            }

            if (GUILayout.Button(Labels.kRemoveLevelButton))
            {
                RemoveLastLevel();
            }
        }

        /*---------------------------------------------------------------------------------- 
        | --- AddLevel: A Method to Add a New Level to the Character Class Progression --- |
        ----------------------------------------------------------------------------------*/
        private void AddLevel()
        {
            int newIndex = m_maxHealthAmounts.arraySize;
            m_maxHealthAmounts.InsertArrayElementAtIndex(newIndex);
            m_maxExperienceAmounts.InsertArrayElementAtIndex(newIndex);

            if (newIndex == 0)
            {
                m_maxHealthAmounts.GetArrayElementAtIndex(newIndex).intValue = m_initialHealth.intValue;
                m_maxExperienceAmounts.GetArrayElementAtIndex(newIndex).intValue = 0;
            }
            else
            {
                int previousHealth = m_maxHealthAmounts.GetArrayElementAtIndex(newIndex - 1).intValue;
                float increaseFactor = 1 + m_healthIncreasePercentage.floatValue / 100f;
                m_maxHealthAmounts.GetArrayElementAtIndex(newIndex).intValue = Mathf.RoundToInt(previousHealth * increaseFactor);
                m_maxExperienceAmounts.GetArrayElementAtIndex(newIndex).intValue = 0;
            }
        }

        /*------------------------------------------------------------------------------------------------- 
        | --- RemoveLastLevel: A Method to Remove the Last Level from the Character Class Progression --- |
        -------------------------------------------------------------------------------------------------*/
        private void RemoveLastLevel()
        {
            if (m_maxHealthAmounts.arraySize > 0)
            {
                m_maxHealthAmounts.DeleteArrayElementAtIndex(m_maxHealthAmounts.arraySize - 1);
                m_maxExperienceAmounts.DeleteArrayElementAtIndex(m_maxExperienceAmounts.arraySize - 1);
            }
        }
    }
}
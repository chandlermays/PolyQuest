/*---------------------------
File: PlayerProgressionEditor.cs
Author: Chandler Mays
----------------------------*/
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Edit
{
    /* -------------------------------------------------------------------------------------------------------
     * Role: Provides a custom Unity Editor inspector for designing player progression.                      *
     *                                                                                                       *
     * Responsibilities:                                                                                     *
     *      - Displays and edits player experience progression values.                                       *
     *      - Allows recalculation of experience arrays via custom buttons.                                  *
     *      - Enables direct editing of per-level experience values.                                         *
     *      - Supports adding and removing levels interactively in the inspector.                            *
     * ----------------------------------------------------------------------------------------------------- */

    [CustomEditor(typeof(PlayerProgression), true)]
    public class PlayerProgressionEditor : BaseProgressionEditor
    {
        private SerializedProperty m_initialExperience;
        private SerializedProperty m_experienceIncreasePercentage;
        private SerializedProperty m_maxExperienceAmounts;

        private SerializedProperty m_initialAttributePoints;
        private SerializedProperty m_attributePointIncreasePercentage;
        private SerializedProperty m_maxAttributePoints;

        // Property names
        private const string kInitialExperience = "m_initialExperience";
        private const string kExperienceIncreasePercentage = "m_experienceIncreasePercentage";
        private const string kMaxExperienceAmounts = "m_maxExperienceAmounts";

        private const string kInitialAttributePoints = "m_initialAttributePoints";
        private const string kAttributePointIncreasePercentage = "m_attributePointIncreasePercentage";
        private const string kMaxAttributePoints = "m_maxAttributePoints";

        // Labels
        private const string kInitialExperienceLabel = "Initial Experience";
        private const string kExperienceIncreasePercentageLabel = "Experience Increase %";
        private const string kRecalculateExperienceButton = "Recalculate Experience Amounts";
        private const string kMaxExperienceAmountsLabel = "Max Experience Amounts";

        private const string kInitialAttributePointsLabel = "Initial Attribute Points";
        private const string kAttributePointIncreasePercentageLabel = "Attribute Point Increase %";
        private const string kRecalculateAttributePointsButton = "Recalculate Attribute Points";
        private const string kMaxAttributePointsLabel = "Max Attribute Points";

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        protected override void OnEnable()
        {
            base.OnEnable();

            m_initialExperience = serializedObject.FindProperty(kInitialExperience);
            m_experienceIncreasePercentage = serializedObject.FindProperty(kExperienceIncreasePercentage);
            m_maxExperienceAmounts = serializedObject.FindProperty(kMaxExperienceAmounts);

            m_initialAttributePoints = serializedObject.FindProperty(kInitialAttributePoints);
            m_attributePointIncreasePercentage = serializedObject.FindProperty(kAttributePointIncreasePercentage);
            m_maxAttributePoints = serializedObject.FindProperty(kMaxAttributePoints);
        }

        /*--------------------------------------------------------------------------------------
        | --- DisplaySubclassFields: Display Player-Specific Experience Progression Fields --- |
        --------------------------------------------------------------------------------------*/
        protected override void DisplaySubclassFields()
        {
            DisplayPropertyField(m_initialExperience, kInitialExperienceLabel);
            DisplayPropertyField(m_experienceIncreasePercentage, kExperienceIncreasePercentageLabel);

            DisplayRecalculateButton(kRecalculateExperienceButton,
                () => ((PlayerProgression)target).RecalculateMaxExperienceAmounts());

            EditorGUILayout.Space();

            DisplayPropertyField(m_initialAttributePoints, kInitialAttributePointsLabel);
            DisplayPropertyField(m_attributePointIncreasePercentage, kAttributePointIncreasePercentageLabel);
            DisplayRecalculateButton(kRecalculateAttributePointsButton,
                () => ((PlayerProgression)target).RecalculateTotalAttributePoints());
        }

        /*--------------------------------------------------------------------------
        | --- DisplaySubclassArrays: Display Player-Specific Experience Arrays --- |
        --------------------------------------------------------------------------*/
        protected override void DisplaySubclassArrays()
        {
            DisplayArrayProperty(m_maxExperienceAmounts, kMaxExperienceAmountsLabel);
            DisplayArrayProperty(m_maxAttributePoints, kMaxAttributePointsLabel);
        }

        /*---------------------------------------------------------------------------
        | --- AddLevelToSubclassArrays: Add a Level to Player Experience Arrays --- |
        ---------------------------------------------------------------------------*/
        protected override void AddLevelToSubclassArrays(int newIndex)
        {
            m_maxExperienceAmounts.InsertArrayElementAtIndex(newIndex);

            if (newIndex == 0)
            {
                m_maxExperienceAmounts.GetArrayElementAtIndex(newIndex).intValue = 0;
            }
            else
            {
                int previousGoal = m_maxExperienceAmounts.GetArrayElementAtIndex(newIndex - 1).intValue;
                int newGoal = Mathf.RoundToInt(m_initialExperience.intValue *
                    Mathf.Pow(1 + m_experienceIncreasePercentage.floatValue / 100f, newIndex));
                m_maxExperienceAmounts.GetArrayElementAtIndex(newIndex).intValue = previousGoal + newGoal;
            }

            m_maxAttributePoints.InsertArrayElementAtIndex(newIndex);

            if (newIndex == 0)
            {
                m_maxAttributePoints.GetArrayElementAtIndex(newIndex).intValue = 0;
            }
            else
            {
                int previousPoints = m_maxAttributePoints.GetArrayElementAtIndex(newIndex - 1).intValue;
                int newPoints = Mathf.RoundToInt(m_initialAttributePoints.intValue *
                    Mathf.Pow(1 + m_attributePointIncreasePercentage.floatValue / 100f, newIndex));
                m_maxAttributePoints.GetArrayElementAtIndex(newIndex).intValue = previousPoints + newPoints;
            }
        }

        /*-------------------------------------------------------------------------------------
        | --- RemoveLevelFromSubclassArrays: Remove a Level from Player Experience Arrays --- |
        -------------------------------------------------------------------------------------*/
        protected override void RemoveLevelFromSubclassArrays()
        {
            if (m_maxExperienceAmounts.arraySize > 0)
            {
                m_maxExperienceAmounts.DeleteArrayElementAtIndex(m_maxExperienceAmounts.arraySize - 1);
            }

            if (m_maxAttributePoints.arraySize > 0)
            {
                m_maxAttributePoints.DeleteArrayElementAtIndex(m_maxAttributePoints.arraySize - 1);
            }
        }
    }
}
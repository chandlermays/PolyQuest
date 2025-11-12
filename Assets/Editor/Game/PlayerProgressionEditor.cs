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

        // Property names
        private const string kInitialExperience = "m_initialExperience";
        private const string kExperienceIncreasePercentage = "m_experienceIncreasePercentage";
        private const string kMaxExperienceAmounts = "m_maxExperienceAmounts";

        // Labels
        private const string kInitialExperienceLabel = "Initial Experience";
        private const string kExperienceIncreasePercentageLabel = "Experience Increase %";
        private const string kRecalculateExperienceButton = "Recalculate Experience Amounts";
        private const string kMaxExperienceAmountsLabel = "Max Experience Amounts";

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        protected override void OnEnable()
        {
            base.OnEnable();

            m_initialExperience = serializedObject.FindProperty(kInitialExperience);
            m_experienceIncreasePercentage = serializedObject.FindProperty(kExperienceIncreasePercentage);
            m_maxExperienceAmounts = serializedObject.FindProperty(kMaxExperienceAmounts);
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
        }

        /*--------------------------------------------------------------------------
        | --- DisplaySubclassArrays: Display Player-Specific Experience Arrays --- |
        --------------------------------------------------------------------------*/
        protected override void DisplaySubclassArrays()
        {
            DisplayArrayProperty(m_maxExperienceAmounts, kMaxExperienceAmountsLabel);
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
        }
    }
}
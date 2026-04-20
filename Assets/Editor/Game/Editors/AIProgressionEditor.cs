/*---------------------------
File: AIProgressionEditor.cs
Author: Chandler Mays
----------------------------*/
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Edit
{
    /* ----------------------------------------------------------------------------------
     * Role: Provides a custom Unity Editor inspector for designing AI progression.     *
     *                                                                                  *
     * Responsibilities:                                                                *
     *      - Displays and edits AI experience reward progression values.               *
     *      - Allows recalculation of experience reward arrays via custom buttons.      *
     *      - Enables direct editing of per-level experience reward values.             *
     *      - Supports adding and removing levels interactively in the inspector.       *
     * -------------------------------------------------------------------------------- */

    [CustomEditor(typeof(AIProgression), true)]
    public class AIProgressionEditor : BaseProgressionEditor
    {
        private SerializedProperty m_initialExperienceReward;
        private SerializedProperty m_experienceRewardIncreasePercentage;
        private SerializedProperty m_experienceRewardAmounts;

        // Property names
        private const string kInitialExperienceReward = "m_initialExperienceReward";
        private const string kExperienceRewardIncreasePercentage = "m_experienceRewardIncreasePercentage";
        private const string kExperienceRewardAmounts = "m_experienceRewardAmounts";

        // Labels
        private const string kInitialExperienceRewardLabel = "Initial Experience Reward";
        private const string kExperienceRewardIncreasePercentageLabel = "Experience Reward Increase %";
        private const string kRecalculateExperienceRewardButton = "Recalculate Experience Reward Amounts";
        private const string kExperienceRewardAmountsLabel = "Experience Reward Amounts";

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        protected override void OnEnable()
        {
            base.OnEnable();

            m_initialExperienceReward = serializedObject.FindProperty(kInitialExperienceReward);
            m_experienceRewardIncreasePercentage = serializedObject.FindProperty(kExperienceRewardIncreasePercentage);
            m_experienceRewardAmounts = serializedObject.FindProperty(kExperienceRewardAmounts);
        }

        /*-----------------------------------------------------------------------------
        | --- DisplaySubclassFields: Display AI-Specific Experience Reward Fields --- |
        -----------------------------------------------------------------------------*/
        protected override void DisplaySubclassFields()
        {
            DisplayPropertyField(m_initialExperienceReward, kInitialExperienceRewardLabel);
            DisplayPropertyField(m_experienceRewardIncreasePercentage, kExperienceRewardIncreasePercentageLabel);

            DisplayRecalculateButton(kRecalculateExperienceRewardButton,
                () => ((AIProgression)target).RecalculateExperienceRewardAmounts());
        }

        /*-----------------------------------------------------------------------------
        | --- DisplaySubclassArrays: Display AI-Specific Experience Reward Arrays --- |
        -----------------------------------------------------------------------------*/
        protected override void DisplaySubclassArrays()
        {
            DisplayArrayProperty(m_experienceRewardAmounts, kExperienceRewardAmountsLabel);
        }

        /*------------------------------------------------------------------------------
        | --- AddLevelToSubclassArrays: Add a Level to AI Experience Reward Arrays --- |
        ------------------------------------------------------------------------------*/
        protected override void AddLevelToSubclassArrays(int newIndex)
        {
            m_experienceRewardAmounts.InsertArrayElementAtIndex(newIndex);

            if (newIndex == 0)
            {
                m_experienceRewardAmounts.GetArrayElementAtIndex(newIndex).intValue = m_initialExperienceReward.intValue;
            }
            else
            {
                int previousReward = m_experienceRewardAmounts.GetArrayElementAtIndex(newIndex - 1).intValue;
                float increaseFactor = 1 + m_experienceRewardIncreasePercentage.floatValue / 100f;
                m_experienceRewardAmounts.GetArrayElementAtIndex(newIndex).intValue = Mathf.RoundToInt(previousReward * increaseFactor);
            }
        }

        /*----------------------------------------------------------------------------------------
        | --- RemoveLevelFromSubclassArrays: Remove a Level from AI Experience Reward Arrays --- |
        ----------------------------------------------------------------------------------------*/
        protected override void RemoveLevelFromSubclassArrays()
        {
            if (m_experienceRewardAmounts.arraySize > 0)
            {
                m_experienceRewardAmounts.DeleteArrayElementAtIndex(m_experienceRewardAmounts.arraySize - 1);
            }
        }
    }
}
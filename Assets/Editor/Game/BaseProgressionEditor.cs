using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Edit
{
    /* -------------------------------------------------------------------------------------------------
     * Role: Base editor class for progression editors, handling common stat progression UI/logic.     *
     *                                                                                                 *
     * Responsibilities:                                                                               *
     *      - Displays and edits stat-related progression fields (health, damage, defense, mana).      *
     *      - Provides shared utility methods for property display and array manipulation.             *
     *      - Handles add/remove level functionality for stat arrays.                                  *
     * ----------------------------------------------------------------------------------------------- */

    public abstract class BaseProgressionEditor : Editor
    {
        // Health Properties
        protected SerializedProperty m_initialHealth;
        protected SerializedProperty m_healthIncreasePercentage;
        protected SerializedProperty m_maxHealthAmounts;

        // Damage Properties
        protected SerializedProperty m_initialDamage;
        protected SerializedProperty m_damageIncreasePercentage;
        protected SerializedProperty m_damageAmounts;

        // Defense Properties
        protected SerializedProperty m_initialDefense;
        protected SerializedProperty m_defenseIncreasePercentage;
        protected SerializedProperty m_defenseAmounts;

        // Mana Properties
        protected SerializedProperty m_initialMana;
        protected SerializedProperty m_manaIncreasePercentage;
        protected SerializedProperty m_manaAmounts;

        // Mana Regen Rate Properties
        protected SerializedProperty m_initialManaRegenRate;
        protected SerializedProperty m_manaRegenIncreasePercentage;
        protected SerializedProperty m_manaRegenRates;

        // Property names for stat fields
        protected const string kInitialHealth = "m_initialHealth";
        protected const string kHealthIncreasePercentage = "m_healthIncreasePercentage";
        protected const string kMaxHealthAmounts = "m_maxHealthAmounts";

        protected const string kInitialDamage = "m_initialDamage";
        protected const string kDamageIncreasePercentage = "m_damageIncreasePercentage";
        protected const string kDamageAmounts = "m_damageAmounts";

        protected const string kInitialDefense = "m_initialDefense";
        protected const string kDefenseIncreasePercentage = "m_defenseIncreasePercentage";
        protected const string kDefenseAmounts = "m_defenseAmounts";

        protected const string kInitialMana = "m_initialMana";
        protected const string kManaIncreasePercentage = "m_manaIncreasePercentage";
        protected const string kManaAmounts = "m_manaAmounts";

        protected const string kInitialManaRegenRate = "m_initialManaRegenRate";
        protected const string kManaRegenIncreasePercentage = "m_manaRegenIncreasePercentage";
        protected const string kManaRegenRates = "m_manaRegenRates";

        // Labels for stat fields
        protected const string kProgression = "Progression";
        protected const string kInitialHealthLabel = "Initial Health";
        protected const string kHealthIncreasePercentageLabel = "Health Increase %";
        protected const string kRecalculateHealthButton = "Recalculate Health Amounts";
        protected const string kMaxHealthAmountsLabel = "Max Health Amounts";

        protected const string kInitialDamageLabel = "Initial Damage";
        protected const string kDamageIncreasePercentageLabel = "Damage Increase %";
        protected const string kRecalculateDamageButton = "Recalculate Damage Amounts";
        protected const string kDamageAmountsLabel = "Damage Amounts";

        protected const string kInitialDefenseLabel = "Initial Defense";
        protected const string kDefenseIncreasePercentageLabel = "Defense Increase %";
        protected const string kRecalculateDefenseButton = "Recalculate Defense Amounts";
        protected const string kDefenseAmountsLabel = "Defense Amounts";

        protected const string kInitialManaLabel = "Initial Mana";
        protected const string kManaIncreasePercentageLabel = "Mana Increase %";
        protected const string kRecalculateManaButton = "Recalculate Mana Amounts";
        protected const string kManaAmountsLabel = "Mana Amounts";

        protected const string kInitialManaRegenRateLabel = "Initial Mana Regen Rate";
        protected const string kManaRegenIncreasePercentageLabel = "Mana Regen Increase %";
        protected const string kRecalculateManaRegenButton = "Recalculate Mana Regen Rates";
        protected const string kManaRegenRatesLabel = "Mana Regen Rates";

        protected const string kAddLevelButton = "Add Level";
        protected const string kRemoveLevelButton = "Remove Last Level";

        /*---------------------------------------------------------------------------------------------
        | --- InitializeBaseProperties: Cache the Serialized Properties for Base Stat Progression --- |
        ---------------------------------------------------------------------------------------------*/
        protected virtual void InitializeBaseProperties()
        {
            // Health
            m_initialHealth = serializedObject.FindProperty(kInitialHealth);
            m_healthIncreasePercentage = serializedObject.FindProperty(kHealthIncreasePercentage);
            m_maxHealthAmounts = serializedObject.FindProperty(kMaxHealthAmounts);

            // Damage
            m_initialDamage = serializedObject.FindProperty(kInitialDamage);
            m_damageIncreasePercentage = serializedObject.FindProperty(kDamageIncreasePercentage);
            m_damageAmounts = serializedObject.FindProperty(kDamageAmounts);

            // Defense
            m_initialDefense = serializedObject.FindProperty(kInitialDefense);
            m_defenseIncreasePercentage = serializedObject.FindProperty(kDefenseIncreasePercentage);
            m_defenseAmounts = serializedObject.FindProperty(kDefenseAmounts);

            // Mana
            m_initialMana = serializedObject.FindProperty(kInitialMana);
            m_manaIncreasePercentage = serializedObject.FindProperty(kManaIncreasePercentage);
            m_manaAmounts = serializedObject.FindProperty(kManaAmounts);

            // Mana Regen Rate
            m_initialManaRegenRate = serializedObject.FindProperty(kInitialManaRegenRate);
            m_manaRegenIncreasePercentage = serializedObject.FindProperty(kManaRegenIncreasePercentage);
            m_manaRegenRates = serializedObject.FindProperty(kManaRegenRates);
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        protected virtual void OnEnable()
        {
            InitializeBaseProperties();
        }

        /*----------------------------------------------------
        | --- OnInspectorGUI: Creates a Custom Inspector --- |
        ----------------------------------------------------*/
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(kProgression, EditorStyles.boldLabel);

            // Health Fields
            DisplayPropertyField(m_initialHealth, kInitialHealthLabel);
            DisplayPropertyField(m_healthIncreasePercentage, kHealthIncreasePercentageLabel);
            DisplayRecalculateButton(kRecalculateHealthButton,
                () => ((BaseProgression)target).RecalculateMaxHealthAmounts());

            EditorGUILayout.Space();

            // Damage Fields
            DisplayPropertyField(m_initialDamage, kInitialDamageLabel);
            DisplayPropertyField(m_damageIncreasePercentage, kDamageIncreasePercentageLabel);
            DisplayRecalculateButton(kRecalculateDamageButton,
                () => ((BaseProgression)target).RecalculateDamageAmounts());

            EditorGUILayout.Space();

            // Defense Fields
            DisplayPropertyField(m_initialDefense, kInitialDefenseLabel);
            DisplayPropertyField(m_defenseIncreasePercentage, kDefenseIncreasePercentageLabel);
            DisplayRecalculateButton(kRecalculateDefenseButton,
                () => ((BaseProgression)target).RecalculateDefenseAmounts());

            EditorGUILayout.Space();

            // Mana Fields
            DisplayPropertyField(m_initialMana, kInitialManaLabel);
            DisplayPropertyField(m_manaIncreasePercentage, kManaIncreasePercentageLabel);
            DisplayRecalculateButton(kRecalculateManaButton,
                () => ((BaseProgression)target).RecalculateMaxManaAmounts());

            EditorGUILayout.Space();

            // Mana Regen Rate Fields
            DisplayPropertyField(m_initialManaRegenRate, kInitialManaRegenRateLabel);
            DisplayPropertyField(m_manaRegenIncreasePercentage, kManaRegenIncreasePercentageLabel);
            DisplayRecalculateButton(kRecalculateManaRegenButton,
                () => ((BaseProgression)target).RecalculateManaRegenRates());

            // Subclass-specific Property Fields
            DisplaySubclassFields();

            EditorGUILayout.Space();

            // Array Properties
            DisplayArrayProperty(m_maxHealthAmounts, kMaxHealthAmountsLabel);
            DisplayArrayProperty(m_damageAmounts, kDamageAmountsLabel);
            DisplayArrayProperty(m_defenseAmounts, kDefenseAmountsLabel);
            DisplayArrayProperty(m_manaAmounts, kManaAmountsLabel);
            DisplayArrayProperty(m_manaRegenRates, kManaRegenRatesLabel);

            // Subclass-specific Array Properties
            DisplaySubclassArrays();

            EditorGUILayout.Space();

            // Add/Remove Level Buttons
            DisplayAddRemoveLevelButtons();

            serializedObject.ApplyModifiedProperties();
        }

        /*------------------------------------------------------------------------------------------- 
        | --- DisplayPropertyField: A Parameterized AdjustValue for Displaying Serialized Properties --- |
        -------------------------------------------------------------------------------------------*/
        protected void DisplayPropertyField(SerializedProperty property, string label)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }

        /*----------------------------------------------------------------------------------------------- 
        | --- DisplayRecalculateButton: A Parameterized Button for Recalculating Progression Values --- |
        -----------------------------------------------------------------------------------------------*/
        protected void DisplayRecalculateButton(string buttonLabel, System.Action recalculateAction)
        {
            if (GUILayout.Button(buttonLabel))
            {
                Undo.RecordObject(target, buttonLabel);
                recalculateAction.Invoke();
                EditorUtility.SetDirty(target);
            }
        }

        /*------------------------------------------------------------------------------------------------- 
        | --- DisplayArrayProperty: A Parameterized AdjustValue for Displaying Serialized Array Properties --- |
        -------------------------------------------------------------------------------------------------*/
        protected void DisplayArrayProperty(SerializedProperty arrayProperty, string label)
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
        protected virtual void DisplayAddRemoveLevelButtons()
        {
            if (GUILayout.Button(kAddLevelButton))
            {
                AddLevel();
            }

            if (GUILayout.Button(kRemoveLevelButton))
            {
                RemoveLastLevel();
            }
        }

        /*---------------------------------------------------------------------------------- 
        | --- AddLevel: A AdjustValue to Add a New Level to the Character Class Progression --- |
        ----------------------------------------------------------------------------------*/
        protected virtual void AddLevel()
        {
            if (!ValidateArraySynchronization())
            {
                Debug.LogError("Arrays are out of sync. Cannot add level safely.");
                return;
            }

            Undo.RecordObject(target, kAddLevelButton);

            int newIndex = m_maxHealthAmounts.arraySize;

            // Add to base stat arrays
            m_maxHealthAmounts.InsertArrayElementAtIndex(newIndex);
            m_damageAmounts.InsertArrayElementAtIndex(newIndex);
            m_defenseAmounts.InsertArrayElementAtIndex(newIndex);
            m_manaAmounts.InsertArrayElementAtIndex(newIndex);

            // Calculate Health
            if (newIndex == 0)
            {
                m_maxHealthAmounts.GetArrayElementAtIndex(newIndex).intValue = m_initialHealth.intValue;
            }
            else
            {
                int previousHealth = m_maxHealthAmounts.GetArrayElementAtIndex(newIndex - 1).intValue;
                float increaseFactor = 1 + m_healthIncreasePercentage.floatValue / 100f;
                m_maxHealthAmounts.GetArrayElementAtIndex(newIndex).intValue = Mathf.RoundToInt(previousHealth * increaseFactor);
            }

            // Calculate Damage
            if (newIndex == 0)
            {
                m_damageAmounts.GetArrayElementAtIndex(newIndex).intValue = m_initialDamage.intValue;
            }
            else
            {
                int previousDamage = m_damageAmounts.GetArrayElementAtIndex(newIndex - 1).intValue;
                float increaseFactor = 1 + m_damageIncreasePercentage.floatValue / 100f;
                m_damageAmounts.GetArrayElementAtIndex(newIndex).intValue = Mathf.RoundToInt(previousDamage * increaseFactor);
            }

            // Calculate Defense
            if (newIndex == 0)
            {
                m_defenseAmounts.GetArrayElementAtIndex(newIndex).intValue = m_initialDefense.intValue;
            }
            else
            {
                int previousDefense = m_defenseAmounts.GetArrayElementAtIndex(newIndex - 1).intValue;
                float increaseFactor = 1 + m_defenseIncreasePercentage.floatValue / 100f;
                m_defenseAmounts.GetArrayElementAtIndex(newIndex).intValue = Mathf.RoundToInt(previousDefense * increaseFactor);
            }

            // Calculate Mana
            if (newIndex == 0)
            {
                m_manaAmounts.GetArrayElementAtIndex(newIndex).intValue = m_initialMana.intValue;
            }
            else
            {
                int previousMana = m_manaAmounts.GetArrayElementAtIndex(newIndex - 1).intValue;
                float increaseFactor = 1 + m_manaIncreasePercentage.floatValue / 100f;
                m_manaAmounts.GetArrayElementAtIndex(newIndex).intValue = Mathf.RoundToInt(previousMana * increaseFactor);
            }

            // Calculate Mana Regen Rates
            if (newIndex == 0)
            {
                m_manaRegenRates.GetArrayElementAtIndex(newIndex).intValue = m_initialManaRegenRate.intValue;
            }
            else
            {
                int previousManaRegen = m_manaRegenRates.GetArrayElementAtIndex(newIndex - 1).intValue;
                float increaseFactor = 1 + m_manaRegenIncreasePercentage.floatValue / 100f;
                m_manaRegenRates.GetArrayElementAtIndex(newIndex).intValue = Mathf.RoundToInt(previousManaRegen * increaseFactor);
            }

            // Add to subclass arrays
            AddLevelToSubclassArrays(newIndex);

            EditorUtility.SetDirty(target);
        }

        /*------------------------------------------------------------------------------------------------- 
        | --- RemoveLastLevel: A AdjustValue to Remove the Last Level from the Character Class Progression --- |
        -------------------------------------------------------------------------------------------------*/
        protected virtual void RemoveLastLevel()
        {
            if (m_maxHealthAmounts.arraySize > 0)
            {
                m_maxHealthAmounts.DeleteArrayElementAtIndex(m_maxHealthAmounts.arraySize - 1);
                m_damageAmounts.DeleteArrayElementAtIndex(m_damageAmounts.arraySize - 1);
                m_defenseAmounts.DeleteArrayElementAtIndex(m_defenseAmounts.arraySize - 1);
                m_manaAmounts.DeleteArrayElementAtIndex(m_manaAmounts.arraySize - 1);
                m_manaRegenRates.DeleteArrayElementAtIndex(m_manaRegenRates.arraySize - 1);

                // Remove from subclass arrays
                RemoveLevelFromSubclassArrays();
            }
        }

        /*--------------------------------------------------------------------------------------- 
        | --- ValidateArraySynchronization: Ensure All Stat Arrays are Synchronized in Size --- |
        ---------------------------------------------------------------------------------------*/
        private bool ValidateArraySynchronization()
        {
            int expectedSize = m_maxHealthAmounts.arraySize;

            return m_damageAmounts.arraySize   ==  expectedSize &&
                   m_defenseAmounts.arraySize  ==  expectedSize &&
                   m_manaAmounts.arraySize     ==  expectedSize &&
                   m_manaRegenRates.arraySize  ==  expectedSize;
        }

        // Abstract methods for subclasses to implement
        protected abstract void DisplaySubclassFields();
        protected abstract void DisplaySubclassArrays();
        protected abstract void AddLevelToSubclassArrays(int newIndex);
        protected abstract void RemoveLevelFromSubclassArrays();
    }
}
/*---------------------------
| File: BaseProgression.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Attributes
{
    public abstract class BaseProgression : ScriptableObject
    {
        [SerializeField] private float m_healthIncreasePercentage;
        [SerializeField] private int m_initialHealth;
        [SerializeField] private int[] m_maxHealthAmounts;

        [SerializeField] private float m_damageIncreasePercentage;
        [SerializeField] private int m_initialDamage;
        [SerializeField] private int[] m_damageAmounts;

        [SerializeField] private float m_defenseIncreasePercentage;
        [SerializeField] private int m_initialDefense;
        [SerializeField] private int[] m_defenseAmounts;

        [SerializeField] private float m_manaIncreasePercentage;
        [SerializeField] private int m_initialMana;
        [SerializeField] private int[] m_manaAmounts;

        [SerializeField] private float m_manaRegenIncreasePercentage;
        [SerializeField] private int m_initialManaRegenRate;
        [Tooltip("Generated, unrounded. After this field changed from whole numbers, press Recalculate Mana Regen Rates to regenerate it.")]
        [SerializeField] private float[] m_manaRegenRates;

        /*-------------------------------------------------------------------------------------------------------
        | --- MaxLevel: The progression table's row count (its health rows); every other table should match --- |
        -------------------------------------------------------------------------------------------------------*/
        public int MaxLevel => Mathf.Max(1, m_maxHealthAmounts?.Length ?? 0);

        /*------------------------------------------------------------------------ 
        | --- OnEnable: Called when the Object becomes Enabled and is Active --- |
        ------------------------------------------------------------------------*/
        protected virtual void OnEnable()
        {
            if (m_maxHealthAmounts == null || m_maxHealthAmounts.Length == 0)
            {
                m_maxHealthAmounts = new int[1] { m_initialHealth };
            }

            if (m_damageAmounts == null || m_damageAmounts.Length == 0)
            {
                m_damageAmounts = new int[1] { m_initialDamage };
            }

            if (m_defenseAmounts == null || m_defenseAmounts.Length == 0)
            {
                m_defenseAmounts = new int[1] { m_initialDefense };
            }

            if (m_manaAmounts == null || m_manaAmounts.Length == 0)
            {
                m_manaAmounts = new int[1] { m_initialMana };
            }

            if (m_manaRegenRates == null || m_manaRegenRates.Length == 0)
            {
                m_manaRegenRates = new float[1] { m_initialManaRegenRate };
            }
        }

        /*----------------------------------------------------------------------------------------------
        | --- RecalculateMaxHealthAmounts: Regenerates the health rows from the exact growth curve --- |
        ----------------------------------------------------------------------------------------------*/
        public void RecalculateMaxHealthAmounts()
        {
            GrowthCurve.Fill(m_maxHealthAmounts, m_initialHealth, m_healthIncreasePercentage);
        }

        /*-------------------------------------------------------------------------------------------
        | --- RecalculateDamageAmounts: Regenerates the damage rows from the exact growth curve --- |
        -------------------------------------------------------------------------------------------*/
        public void RecalculateDamageAmounts()
        {
            GrowthCurve.Fill(m_damageAmounts, m_initialDamage, m_damageIncreasePercentage);
        }

        /*---------------------------------------------------------------------------------------------
        | --- RecalculateDefenseAmounts: Regenerates the defense rows from the exact growth curve --- |
        ---------------------------------------------------------------------------------------------*/
        public void RecalculateDefenseAmounts()
        {
            GrowthCurve.Fill(m_defenseAmounts, m_initialDefense, m_defenseIncreasePercentage);
        }

        /*------------------------------------------------------------------------------------------
        | --- RecalculateMaxManaAmounts: Regenerates the mana rows from the exact growth curve --- |
        ------------------------------------------------------------------------------------------*/
        public void RecalculateMaxManaAmounts()
        {
            GrowthCurve.Fill(m_manaAmounts, m_initialMana, m_manaIncreasePercentage);
        }

        /*----------------------------------------------------------------------------------------------------
        | --- RecalculateManaRegenRates: Regenerates the mana regen rows from the exact curve, unrounded --- |
        ----------------------------------------------------------------------------------------------------*/
        public void RecalculateManaRegenRates()
        {
            GrowthCurve.Fill(m_manaRegenRates, m_initialManaRegenRate, m_manaRegenIncreasePercentage);
        }

        /*-----------------------------------------------------------------------------------------------
        | --- RecalculateAllRows: Regenerates every table (used after adding a level in the editor) --- |
        -----------------------------------------------------------------------------------------------*/
        public virtual void RecalculateAllRows()
        {
            RecalculateMaxHealthAmounts();
            RecalculateDamageAmounts();
            RecalculateDefenseAmounts();
            RecalculateMaxManaAmounts();
            RecalculateManaRegenRates();
        }

        /*---------------------------------------------------------------------- 
        | --- GetHealth: Returns the Health Amount for the Specified Level --- |
        ----------------------------------------------------------------------*/
        public float GetHealth(int level)
        {
            if (level < 1 || level > m_maxHealthAmounts.Length)
            {
                Debug.LogWarning("Level out of range");
                return 0;
            }

            return m_maxHealthAmounts[level - 1];
        }

        /*---------------------------------------------------------------------- 
        | --- GetDamage: Returns the Damage Amount for the Specified Level --- |
        ----------------------------------------------------------------------*/
        public float GetDamage(int level)
        {
            if (level < 1 || level > m_damageAmounts.Length)
            {
                Debug.LogWarning("Level out of range");
                return 0;
            }

            return m_damageAmounts[level - 1];
        }

        /*------------------------------------------------------------------------ 
        | --- GetDefense: Returns the Defense Amount for the Specified Level --- |
        ------------------------------------------------------------------------*/
        public float GetDefense(int level)
        {
            if (level < 1 || level > m_defenseAmounts.Length)
            {
                Debug.LogWarning("Level out of range");
                return 0;
            }

            return m_defenseAmounts[level - 1];
        }

        /*------------------------------------------------------------------ 
        | --- GetMana: Returns the Mana Amount for the Specified Level --- |
        ------------------------------------------------------------------*/
        public float GetMana(int level)
        {
            if (level < 1 || level > m_manaAmounts.Length)
            {
                Debug.LogWarning("Level out of range");
                return 0;
            }

            return m_manaAmounts[level - 1];
        }

        /*------------------------------------------------------------------------------- 
        | --- GetManaRegenRate: Returns the Mana Regen Rate for the Specified Level --- |
        -------------------------------------------------------------------------------*/
        public float GetManaRegenRate(int level)
        {
            if (level < 1 || level > m_manaRegenRates.Length)
            {
                Debug.LogWarning("Level out of range");
                return 0;
            }

            return m_manaRegenRates[level - 1];
        }

        /*------------------------------------------------------------------------------------------------
        | --- TableLengths: Every per-level table and its row count; subclasses add their own tables --- |
        ------------------------------------------------------------------------------------------------*/
        protected virtual IEnumerable<(string table, int rows)> TableLengths()
        {
            yield return ("Damage", m_damageAmounts?.Length ?? 0);
            yield return ("Defense", m_defenseAmounts?.Length ?? 0);
            yield return ("Mana", m_manaAmounts?.Length ?? 0);
            yield return ("Mana Regen", m_manaRegenRates?.Length ?? 0);
        }

#if UNITY_EDITOR
        /*------------------------------------------------------------------------------------------------------
        | --- OnValidate: Editor warning when a table has a different row count than the max level's table --- |
        ------------------------------------------------------------------------------------------------------*/
        protected virtual void OnValidate()
        {
            List<string> mismatched = new();
            foreach ((string table, int rows) in TableLengths())
            {
                if (rows != MaxLevel)
                    mismatched.Add($"{table} has {rows}");
            }

            if (mismatched.Count > 0)
                Debug.LogWarning($"Progression '{name}': the max level is {MaxLevel} (the health rows), but {string.Join(", ", mismatched)}. Give every table one row per level.", this);
        }
#endif
    }
}
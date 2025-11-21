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
        [SerializeField] private int[] m_manaRegenRates;

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
                m_manaRegenRates = new int[1] { m_initialManaRegenRate };
            }
        }

        /*----------------------------------------------------------------------------------------------- 
        | --- RecalculateMaxHealthAmounts: Recalculate the Health Amounts for the Class Progression --- |
        -----------------------------------------------------------------------------------------------*/
        public void RecalculateMaxHealthAmounts()
        {
            if (m_maxHealthAmounts.Length == 0)
                return;

            m_maxHealthAmounts[0] = m_initialHealth;
            for (int i = 1; i < m_maxHealthAmounts.Length; ++i)
            {
                m_maxHealthAmounts[i] = Mathf.RoundToInt(m_maxHealthAmounts[i - 1] * (1 + m_healthIncreasePercentage / 100f));
            }
        }

        /*-------------------------------------------------------------------------------------------- 
        | --- RecalculateDamageAmounts: Recalculate the Damage Amounts for the Class Progression --- |
        --------------------------------------------------------------------------------------------*/
        public void RecalculateDamageAmounts()
        {
            if (m_damageAmounts.Length == 0)
                return;

            m_damageAmounts[0] = m_initialDamage;
            for (int i = 1; i < m_damageAmounts.Length; ++i)
            {
                m_damageAmounts[i] = Mathf.RoundToInt(m_damageAmounts[i - 1] * (1 + m_damageIncreasePercentage / 100f));
            }
        }

        /*---------------------------------------------------------------------------------------------- 
        | --- RecalculateDefenseAmounts: Recalculate the Defense Amounts for the Class Progression --- |
        ----------------------------------------------------------------------------------------------*/
        public void RecalculateDefenseAmounts()
        {
            if (m_defenseAmounts.Length == 0)
                return;

            m_defenseAmounts[0] = m_initialDefense;
            for (int i = 1; i < m_defenseAmounts.Length; ++i)
            {
                m_defenseAmounts[i] = Mathf.RoundToInt(m_defenseAmounts[i - 1] * (1 + m_defenseIncreasePercentage / 100f));
            }
        }

        /*------------------------------------------------------------------------------------------- 
        | --- RecalculateMaxManaAmounts: Recalculate the Mana Amounts for the Class Progression --- |
        -------------------------------------------------------------------------------------------*/
        public void RecalculateMaxManaAmounts()
        {
            if (m_manaAmounts.Length == 0)
                return;

            m_manaAmounts[0] = m_initialMana;
            for (int i = 1; i < m_manaAmounts.Length; ++i)
            {
                m_manaAmounts[i] = Mathf.RoundToInt(m_manaAmounts[i - 1] * (1 + m_manaIncreasePercentage / 100f));
            }
        }

        /*----------------------------------------------------------------------------------------------- 
        | --- RecalculateManaRegenRates: Recalculate the Mana Regen Rates for the Class Progression --- |
        -----------------------------------------------------------------------------------------------*/
        public void RecalculateManaRegenRates()
        {
            if (m_manaRegenRates.Length == 0)
                return;

            m_manaRegenRates[0] = m_initialManaRegenRate;
            for (int i = 1; i < m_manaRegenRates.Length; ++i)
            {
                m_manaRegenRates[i] = Mathf.RoundToInt(m_manaRegenRates[i - 1] * (1 + m_manaRegenIncreasePercentage / 100f));
            }
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
    }
}
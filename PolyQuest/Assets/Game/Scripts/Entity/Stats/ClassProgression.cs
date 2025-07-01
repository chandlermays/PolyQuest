using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    public abstract class ClassProgression : ScriptableObject
    {
        [SerializeField] private float m_healthIncreasePercentage;
        [SerializeField] private float m_experienceIncreasePercentage;
        [SerializeField] private int m_initialHealth;
        [SerializeField] private int m_initialExperience;
        [SerializeField] private int[] m_maxHealthAmounts;
        [SerializeField] private int[] m_maxExperienceAmounts;

        public float HealthIncreasePercentage => m_healthIncreasePercentage;
        public float ExperienceIncreasePercentage => m_experienceIncreasePercentage;
        public int[] MaxHealthAmounts => m_maxHealthAmounts;
        public int[] ExperienceAmounts => m_maxExperienceAmounts;

        /*------------------------------------------------------------------------ 
        | --- OnEnable: Called when the Object becomes Enabled and is Active --- |
        ------------------------------------------------------------------------*/
        private void OnEnable()
        {
            if (m_maxHealthAmounts == null || m_maxHealthAmounts.Length == 0)
            {
                m_maxHealthAmounts = new int[1] { m_initialHealth };
            }

            if (m_maxExperienceAmounts == null || m_maxExperienceAmounts.Length == 0)
            {
                m_maxExperienceAmounts = new int[1] { m_initialExperience };
            }
        }

        /*----------------------------------------------------------------------------------------------- 
        | --- RecalculateMaxHealthAmounts: Recalculate the Health Amounts for the Class Progression --- |
        -----------------------------------------------------------------------------------------------*/
        public void RecalculateMaxHealthAmounts()
        {
            if (m_maxHealthAmounts.Length == 0) return;

            m_maxHealthAmounts[0] = m_initialHealth;
            for (int i = 1; i < m_maxHealthAmounts.Length; i++)
            {
                m_maxHealthAmounts[i] = Mathf.RoundToInt(m_maxHealthAmounts[i - 1] * (1 + m_healthIncreasePercentage / 100f));
            }
        }

        /*------------------------------------------------------------------------------------------------------- 
        | --- RecalculateMaxExperienceAmounts: Recalculate the Experience Amounts for the Class Progression --- |
        -------------------------------------------------------------------------------------------------------*/
        public void RecalculateMaxExperienceAmounts()
        {
            if (m_maxExperienceAmounts.Length == 0) return;

            m_maxExperienceAmounts[0] = m_initialExperience;
            for (int i = 1; i < m_maxExperienceAmounts.Length; i++)
            {
                int previousGoal = m_maxExperienceAmounts[i - 1];
                int newGoal = Mathf.RoundToInt(m_initialExperience * Mathf.Pow(1 + m_experienceIncreasePercentage / 100f, i));
                m_maxExperienceAmounts[i] = previousGoal + newGoal;
            }
        }

        /*---------------------------------------------------------------------------------------------- 
        | --- GetHealth: Returns the Health Amount for the Specified Level of the Associated Class --- |
        ----------------------------------------------------------------------------------------------*/
        public float GetHealth(int level)
        {
            if (level < 1 || level > m_maxHealthAmounts.Length)
            {
                Debug.LogWarning("Level out of range");
                return 0;
            }

            return m_maxHealthAmounts[level - 1];
        }

        /*------------------------------------------------------------------------------------------------------ 
        | --- GetExperience: Returns the Experience Amount for the Specified Level of the Associated Class --- |
        ------------------------------------------------------------------------------------------------------*/
        public float GetExperience(int level)
        {
            if (level < 1 || level > m_maxExperienceAmounts.Length)
            {
                Debug.LogWarning("Level out of range");
                return 0;
            }

            return m_maxExperienceAmounts[level - 1];
        }
    }
}
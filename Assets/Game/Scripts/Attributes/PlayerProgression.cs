using UnityEngine;
//---------------------------------

namespace PolyQuest.Attributes
{
    [CreateAssetMenu(fileName = "PlayerProgression", menuName = "PolyQuest/Progression/Player", order = 0)]
    public class PlayerProgression : BaseProgression
    {
        [SerializeField] private float m_experienceIncreasePercentage;
        [SerializeField] private int m_initialExperience;
        [SerializeField] private int[] m_maxExperienceAmounts;

        public float ExperienceIncreasePercentage => m_experienceIncreasePercentage;
        public int[] ExperienceAmounts => m_maxExperienceAmounts;

        /*------------------------------------------------------------------------ 
        | --- OnEnable: Called when the Object becomes Enabled and is Active --- |
        ------------------------------------------------------------------------*/
        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_maxExperienceAmounts == null || m_maxExperienceAmounts.Length == 0)
            {
                m_maxExperienceAmounts = new int[1] { m_initialExperience };
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
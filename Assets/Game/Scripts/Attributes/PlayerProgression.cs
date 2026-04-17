using UnityEngine;
//---------------------------------

namespace PolyQuest.Attributes
{
    [CreateAssetMenu(menuName = "PolyQuest/Progression/Player", fileName = "PlayerProgression")]
    public class PlayerProgression : BaseProgression
    {
        [SerializeField] private int m_initialExperience;
        [SerializeField] private float m_experienceIncreasePercentage;
        [SerializeField] private int[] m_maxExperienceAmounts;

        [SerializeField] private int m_initialAttributePoints;
        [SerializeField] private float m_attributePointIncreasePercentage;
        [SerializeField] private int[] m_maxAttributePoints;

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

            if (m_maxAttributePoints == null || m_maxAttributePoints.Length == 0)
            {
                m_maxAttributePoints = new int[1] { m_initialAttributePoints };
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

        /*-----------------------------------------------------------------------------------------------------------
        | --- RecalculateTotalAttributePoints: Recalculate the Total Attribute Points for the Class Progression --- |
        -----------------------------------------------------------------------------------------------------------*/
        public void RecalculateTotalAttributePoints()
        {
            if (m_maxAttributePoints.Length == 0) return;

            m_maxAttributePoints[0] = m_initialAttributePoints;
            for (int i = 1; i < m_maxAttributePoints.Length; i++)
            {
                int gainedThisLevel = Mathf.RoundToInt(m_initialAttributePoints * Mathf.Pow(1 + m_attributePointIncreasePercentage / 100f, i));
                m_maxAttributePoints[i] = m_maxAttributePoints[i - 1] + gainedThisLevel;
            }
        }

        /*----------------------------------------------------------------------------------------------- 
        | --- GetTotalAttributePoints: Returns cumulative attribute points available at given level --- |
        -----------------------------------------------------------------------------------------------*/
        public int GetTotalAttributePoints(int level)
        {
            if (level < 1 || level > m_maxAttributePoints.Length)
            {
                Debug.LogWarning("Level out of range");
                return 0;
            }
            return m_maxAttributePoints[level - 1];
        }
    }
}
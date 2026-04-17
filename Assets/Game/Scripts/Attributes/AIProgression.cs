using UnityEngine;
//---------------------------------

namespace PolyQuest.Attributes
{
    [CreateAssetMenu(menuName = "PolyQuest/Progression/New AI Progression", fileName = "New AI Progression")]
    public class AIProgression : BaseProgression
    {
        [SerializeField] private float m_experienceRewardIncreasePercentage;
        [SerializeField] private int m_initialExperienceReward;
        [SerializeField] private int[] m_experienceRewardAmounts;

        /*------------------------------------------------------------------------ 
        | --- OnEnable: Called when the Object becomes Enabled and is Active --- |
        ------------------------------------------------------------------------*/
        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_experienceRewardAmounts == null || m_experienceRewardAmounts.Length == 0)
            {
                m_experienceRewardAmounts = new int[1] { m_initialExperienceReward };
            }
        }

        /*----------------------------------------------------------------------------------------------------------------- 
        | --- RecalculateExperienceRewardAmounts: Recalculate the Experience Reward Amounts for the AI Progression --- |
        -----------------------------------------------------------------------------------------------------------------*/
        public void RecalculateExperienceRewardAmounts()
        {
            if (m_experienceRewardAmounts.Length == 0) return;

            m_experienceRewardAmounts[0] = m_initialExperienceReward;
            for (int i = 1; i < m_experienceRewardAmounts.Length; ++i)
            {
                m_experienceRewardAmounts[i] = Mathf.RoundToInt(m_experienceRewardAmounts[i - 1] * (1 + m_experienceRewardIncreasePercentage / 100f));
            }
        }

        /*-------------------------------------------------------------------------------------------------------- 
        | --- GetExperienceReward: Returns the Experience Reward Amount for the Specified Level of the AI --- |
        --------------------------------------------------------------------------------------------------------*/
        public float GetExperienceReward(int level)
        {
            if (level < 1 || level > m_experienceRewardAmounts.Length)
            {
                Debug.LogWarning("Level out of range");
                return 0;
            }

            return m_experienceRewardAmounts[level - 1];
        }
    }
}
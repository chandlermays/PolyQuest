/*---------------------------
| File: AIProgression.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
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

        /*--------------------------------------------------------------------------------------------------------------
        | --- RecalculateExperienceRewardAmounts: Recalculate the Experience Reward Amounts for the AI Progression --- |
        --------------------------------------------------------------------------------------------------------------*/
        public void RecalculateExperienceRewardAmounts()
        {
            GrowthCurve.Fill(m_experienceRewardAmounts, m_initialExperienceReward, m_experienceRewardIncreasePercentage);
        }

        /*-------------------------------------------------------------------
        | --- RecalculateAllRows: Regenerates every table, the AI's too --- |
        -------------------------------------------------------------------*/
        public override void RecalculateAllRows()
        {
            base.RecalculateAllRows();
            RecalculateExperienceRewardAmounts();
        }

        /*----------------------------------------------------------------
        | --- TableLengths: Every per-level table, the AI's included --- |
        ----------------------------------------------------------------*/
        protected override IEnumerable<(string table, int rows)> TableLengths()
        {
            foreach ((string table, int rows) length in base.TableLengths())
                yield return length;

            yield return ("Experience Reward", m_experienceRewardAmounts?.Length ?? 0);
        }

        /*-----------------------------------------------------------------------------------------------------
        | --- GetExperienceReward: Returns the Experience Reward Amount for the Specified Level of the AI --- |
        -----------------------------------------------------------------------------------------------------*/
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
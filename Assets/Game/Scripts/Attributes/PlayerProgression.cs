/*---------------------------
| File: PlayerProgression.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
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
            // Cumulative: each level's total adds that level's step on the curve
            GrowthCurve.FillCumulative(m_maxExperienceAmounts, m_initialExperience, m_experienceIncreasePercentage);
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
            // Cumulative: each level's total adds the points gained at that level
            GrowthCurve.FillCumulative(m_maxAttributePoints, m_initialAttributePoints, m_attributePointIncreasePercentage);
        }

        /*-----------------------------------------------------------------------
        | --- RecalculateAllRows: Regenerates every table, the Player's too --- |
        -----------------------------------------------------------------------*/
        public override void RecalculateAllRows()
        {
            base.RecalculateAllRows();
            RecalculateMaxExperienceAmounts();
            RecalculateTotalAttributePoints();
        }

        /*--------------------------------------------------------------------
        | --- TableLengths: Every per-level table, the Player's included --- |
        --------------------------------------------------------------------*/
        protected override IEnumerable<(string table, int rows)> TableLengths()
        {
            foreach ((string table, int rows) length in base.TableLengths())
                yield return length;

            yield return ("Experience", m_maxExperienceAmounts?.Length ?? 0);
            yield return ("Attribute Points", m_maxAttributePoints?.Length ?? 0);
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
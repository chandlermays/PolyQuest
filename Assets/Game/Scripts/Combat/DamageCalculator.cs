/*---------------------------
| File: Attributee.cs
| Author: Chandler Mays
----------------------------*/

using UnityEngine.UIElements;

namespace PolyQuest.Combat
{
    public static class DamageCalculator
    {
        /*----------------------------------------------------------------------------------------------------
        | --- CalculateMitigation:
        ----------------------------------------------------------------------------------------------------*/
        public static float CalculateMitigation(float defense, float halfMitigationPoint, float maxMitigation)
        {
            float cap = Clamp01(maxMitigation);

            if (defense <= 0f || cap <= 0f)
                return 0f;

            if (halfMitigationPoint <= 0f)
                return cap;

            float mitigation = defense / (defense + halfMitigationPoint);
            return mitigation < cap ? mitigation : cap;
        }

        /*----------------------------------------------------------------------------------------------------
        | --- CalculateDamage:
        ----------------------------------------------------------------------------------------------------*/
        public static float CalculateDamage(float rawDamage, float defense, float halfMitigationPoint, float maxMitigation)
        {
            if (rawDamage <= 0f)
                return 0f;

            float mitigation = CalculateMitigation(defense, halfMitigationPoint, maxMitigation);
            return rawDamage * (1f - mitigation);
        }

        /*----------------------------------------------------------------------------------------------------
        | --- DefenseNeededForCap:
        ----------------------------------------------------------------------------------------------------*/
        public static float DefenseNeededForCap(float halfMitigationPoint, float maxMitigation)
        {
            float cap = Clamp01(maxMitigation);

            if (cap <= 0f || halfMitigationPoint <= 0f)
                return 0f;

            if (cap >= 1f)
                return float.PositiveInfinity;

            return cap * halfMitigationPoint / (1f - cap);
        }

        /*----------------------------------------------------------------------------------------------------
        | --- Clamp01:
        ----------------------------------------------------------------------------------------------------*/
        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }
    }
}
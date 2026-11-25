/*---------------------------
| File: GrowthCurve.cs
| Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.Attributes
{
    /* ---------------------------------------------------------------------------------------------
     * Role: The one growth curve every generated progression row comes from.                      *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Evaluates initial × (1 + percentage / 100)^step on the exact curve.                  *
     *      - Fills a table of rows from it: rounded, unrounded, or as a cumulative sum.           *
     *      - Never builds a row from the previous rounded row, so rounding can't compound.        *
     * ------------------------------------------------------------------------------------------- */
    public static class GrowthCurve
    {
        /*------------------------------------------------------------------------------
        | --- Evaluate: The exact value at a step (row index, from 0) of the curve --- |
        ------------------------------------------------------------------------------*/
        public static float Evaluate(float initial, float percentage, int step)
        {
            return initial * Mathf.Pow(1f + percentage / 100f, step);
        }

        /*----------------------------------------------------------------------------
        | --- Fill: Each row is the curve at its step, rounded to a whole number --- |
        ----------------------------------------------------------------------------*/
        public static void Fill(int[] rows, float initial, float percentage)
        {
            if (rows == null)
                return;

            for (int i = 0; i < rows.Length; ++i)
            {
                rows[i] = Mathf.RoundToInt(Evaluate(initial, percentage, i));
            }
        }

        /*--------------------------------------------------------------
        | --- Fill: Each row is the curve at its step, not rounded --- |
        --------------------------------------------------------------*/
        public static void Fill(float[] rows, float initial, float percentage)
        {
            if (rows == null)
                return;

            for (int i = 0; i < rows.Length; ++i)
            {
                rows[i] = Evaluate(initial, percentage, i);
            }
        }

        /*------------------------------------------------------------------------------------------------
        | --- FillCumulative: Each row adds the curve's rounded step to the previous row (XP totals) --- |
        ------------------------------------------------------------------------------------------------*/
        public static void FillCumulative(int[] rows, float initial, float percentage)
        {
            if (rows == null)
                return;

            int total = 0;
            for (int i = 0; i < rows.Length; ++i)
            {
                total += Mathf.RoundToInt(Evaluate(initial, percentage, i));
                rows[i] = total;
            }
        }
    }
}
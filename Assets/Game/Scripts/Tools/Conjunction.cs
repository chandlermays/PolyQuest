/*---------------------------
File: Conjunction.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Tools
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Represents a flexible, serializable logic structure for evaluating complex conditions  *
     *       against collections of objects in the game.                                            *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Stores and evaluates complex logical expressions composed of AND/OR conditions.       *
     *      - Evaluates conditions by querying IConditionChecker objects with specified conditions. *
     *      - Supports negation and parameterization of individual conditions.                      *
     *      - Enables designers to define reusable, data-driven conditions for quests, dialogue,    *
     *        inventory checks, and other systems.                                                  *
     * -------------------------------------------------------------------------------------------- */
    [System.Serializable]
    public class Conjunction
    {
        [SerializeField] private Disjunction[] m_AND;

        /*--------------------------------------------------------------------------
        | --- Check: Evaluates the conjunction against a collection of objects --- |
        --------------------------------------------------------------------------*/
        public bool Check(IEnumerable<IConditionChecker> objects)
        {
            foreach (Disjunction disjunction in m_AND)
            {
                if (!disjunction.Check(objects))
                    return false;
            }
            return true;
        }

        [System.Serializable]
        public class Disjunction
        {
            [SerializeField] private Condition[] m_OR;

            /*--------------------------------------------------------------------------
            | --- Check: Evaluates the disjunction against a collection of objects --- |
            --------------------------------------------------------------------------*/
            public bool Check(IEnumerable<IConditionChecker> objects)
            {
                foreach (Condition condition in m_OR)
                {
                    if (condition.Check(objects))
                        return true;
                }
                return false;
            }
        }

        [System.Serializable]
        public class Condition
        {
            [SerializeField] private ConditionType m_condition;
            [SerializeField] private string[] m_parameters;
            [SerializeField] private bool m_negate = false;

            /*------------------------------------------------------------------------
            | --- Check: Evaluates the condition against a collection of objects --- |
            ------------------------------------------------------------------------*/
            public bool Check(IEnumerable<IConditionChecker> objects)
            {
                foreach (var obj in objects)
                {
                    bool? result = obj.Evaluate(m_condition, m_parameters);

                    if (result == null)
                        continue;

                    if (result == m_negate)
                        return false;
                }
                return true;
            }
        }
    }
}
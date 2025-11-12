using System.Collections.Generic;
using UnityEngine;

namespace PolyQuest.Tools
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Represents a flexible, serializable logic structure for evaluating complex conditions  *
     *       against collections of objects in the game.                                            *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Stores and organizes predicates using AND/OR logic for condition evaluation.          *
     *      - Evaluates conditions by querying IConditionChecker objects with specified predicates. *
     *      - Supports negation and parameterization of individual predicates.                      *
     *      - Enables designers to define reusable, data-driven conditions for quests, dialogue,    *
     *        inventory checks, and other systems.                                                  *
     * -------------------------------------------------------------------------------------------- */
    [System.Serializable]
    public class Condition
    {
        [SerializeField] private Disjunction[] m_AND;

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
            [SerializeField] private Predicate[] m_OR;

            public bool Check(IEnumerable<IConditionChecker> objects)
            {
                foreach (Predicate predicate in m_OR)
                {
                    if (predicate.Check(objects))
                        return true;
                }
                return false;
            }
        }

        [System.Serializable]
        public class Predicate
        {
            [SerializeField] private PredicateType m_predicate;
            [SerializeField] private string[] m_parameters;
            [SerializeField] private bool m_negate = false;

            /*------------------------------------------------------------------------
            | --- Check: Evaluates the condition against a collection of objects --- |
            ------------------------------------------------------------------------*/
            public bool Check(IEnumerable<IConditionChecker> objects)
            {
                foreach (var obj in objects)
                {
                    bool? result = obj.Evaluate(m_predicate, m_parameters);

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
using System.Collections.Generic;
using UnityEngine;

namespace PolyQuest
{
    [System.Serializable]
    public class Condition
    {
        [SerializeField] private string m_predicate;
        [SerializeField] private string[] m_parameters;

        public bool Check(IEnumerable<IConditionChecker> objects)
        {
            foreach (var obj in objects)
            {
                bool? result = obj.Evaluate(m_predicate, m_parameters);

                if (result == null)
                    continue;

                if (result == false)
                    return false;
            }
            return true;
        }
    }
}
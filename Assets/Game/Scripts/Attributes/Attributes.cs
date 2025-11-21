using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Attributes
{
    public class Attributes : MonoBehaviour
    {
        private Dictionary<Attribute, int> m_variable = new();

        private int m_unspentPoints = 10;
        public int PointsAvailable => m_unspentPoints;

        public int GetPoints(Attribute attribute)
        {
            if (m_variable.TryGetValue(attribute, out int value))
            {
                return value;
            }
            return 0;
        }

        public void AssignPoints(Attribute attribute, int points)
        {
            if (!CanAssignPoints(attribute, points))
                return;

            m_variable[attribute] = GetPoints(attribute) + points;
            m_unspentPoints -= points;

        }

        public bool CanAssignPoints(Attribute attribute, int points)
        {
            if (GetPoints(attribute) + points < 0)
                return false;

            if (m_unspentPoints < points)
                return false;

            return true;
        }
    }
}
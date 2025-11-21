using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Attributes
{
    public class Attributes : MonoBehaviour
    {
        private readonly Dictionary<Attribute, int> m_assignedPoints = new();
        private readonly Dictionary<Attribute, int> m_pendingPoints = new();

        private BaseStats m_baseStats;

        private void Awake()
        {
            m_baseStats = GetComponent<BaseStats>();
            Utilities.CheckForNull(m_baseStats, nameof(BaseStats));
        }

        public int GetAssignedPoints(Attribute attribute)       =>      m_assignedPoints.GetValueOrDefault(attribute);
        public int GetPendingPoints(Attribute attribute)        =>      m_pendingPoints.GetValueOrDefault(attribute);
        public int GetTotalAvailablePoints()                    =>      (int)m_baseStats.GetStat(Stat.kTotalAttributePoints);
        public int GetRemainingPoints()                         =>      GetTotalAvailablePoints() - CalculateTotalAllocatedPoints();
        public int GetTotalPoints(Attribute attribute)          =>      GetAssignedPoints(attribute) + GetPendingPoints(attribute);

        private int CalculateTotalAllocatedPoints()
        {
            int total = 0;

            foreach (int points in m_assignedPoints.Values)
                total += points;

            foreach (int points in m_pendingPoints.Values)
                total += points;

            return total;
        }

        public void AssignPoints(Attribute attribute, int points)
        {
            if (!CanAssignPoints(attribute, points))
                return;

            m_pendingPoints[attribute] = GetPendingPoints(attribute) + points;
        }

        public bool CanAssignPoints(Attribute attribute, int points)
        {
            if (GetPendingPoints(attribute) + points < 0)
                return false;

            if (GetRemainingPoints() < points)
                return false;

            return true;
        }

        public void Confirm()
        {
            foreach (Attribute attribute in m_pendingPoints.Keys)
            {
                m_assignedPoints[attribute] = GetTotalPoints(attribute);
            }

            m_pendingPoints.Clear();
        }
    }
}
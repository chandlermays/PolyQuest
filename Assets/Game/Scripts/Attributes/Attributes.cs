using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Attributes
{
    public class Attributes : MonoBehaviour, IStatModifier, ISaveable
    {
        [SerializeField] private AttributeStatConfig[] m_attributeModifiers;

        [System.Serializable]
        private class AttributeStatConfig
        {
            public Attribute m_attribute;
            public Stat m_stat;
            public float m_additiveBonus;
            public float m_percentageBonus;
        }

        private Dictionary<Attribute, int> m_assignedPoints = new();
        private readonly Dictionary<Attribute, int> m_pendingPoints = new();
        
        private readonly Dictionary<Stat, Dictionary<Attribute, float>> m_additiveModifiers = new();
        private readonly Dictionary<Stat, Dictionary<Attribute, float>> m_percentageModifiers = new();

        private BaseStats m_baseStats;

        public int GetAssignedPoints(Attribute attribute)       =>      m_assignedPoints.GetValueOrDefault(attribute);
        public int GetPendingPoints(Attribute attribute)        =>      m_pendingPoints.GetValueOrDefault(attribute);
        public int GetTotalAvailablePoints()                    =>      (int)m_baseStats.GetStat(Stat.kTotalAttributePoints);
        public int GetRemainingPoints()                         =>      GetTotalAvailablePoints() - CalculateTotalAllocatedPoints();
        public int GetTotalPoints(Attribute attribute)          =>      GetAssignedPoints(attribute) + GetPendingPoints(attribute);

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_baseStats = GetComponent<BaseStats>();
            Utilities.CheckForNull(m_baseStats, nameof(BaseStats));

            foreach (var config in m_attributeModifiers)
            {
                if (!m_additiveModifiers.ContainsKey(config.m_stat))
                {
                    m_additiveModifiers[config.m_stat] = new();
                }

                if (!m_percentageModifiers.ContainsKey(config.m_stat))
                {
                    m_percentageModifiers[config.m_stat] = new();
                }

                m_additiveModifiers[config.m_stat][config.m_attribute] = config.m_additiveBonus;
                m_percentageModifiers[config.m_stat][config.m_attribute] = config.m_percentageBonus;
            }
        }

        /*------------------------------------------------------------------------------------
        | --- CalculateTotalAllocatedPoints: Calculates total allocated attribute points --- |
        ------------------------------------------------------------------------------------*/
        private int CalculateTotalAllocatedPoints()
        {
            int total = 0;

            foreach (int points in m_assignedPoints.Values)
                total += points;

            foreach (int points in m_pendingPoints.Values)
                total += points;

            return total;
        }

        /*--------------------------------------------------------------
        | --- AssignPoints: Assigns points to a specific attribute --- |
        --------------------------------------------------------------*/
        public void AssignPoints(Attribute attribute, int points)
        {
            if (!CanAssignPoints(attribute, points))
                return;

            m_pendingPoints[attribute] = GetPendingPoints(attribute) + points;
        }

        /*---------------------------------------------------------------------------
        | --- CanAssignPoints: Checks if points can be assigned to an attribute --- |
        ---------------------------------------------------------------------------*/
        public bool CanAssignPoints(Attribute attribute, int points)
        {
            if (GetPendingPoints(attribute) + points < 0)
                return false;

            if (GetRemainingPoints() < points)
                return false;

            return true;
        }

        /*-------------------------------------------------------------------
        | --- Confirm: Confirms the pending attribute point assignments --- |
        -------------------------------------------------------------------*/
        public void Confirm()
        {
            foreach (Attribute attribute in m_pendingPoints.Keys)
            {
                m_assignedPoints[attribute] = GetTotalPoints(attribute);
            }

            m_pendingPoints.Clear();
            m_baseStats.NotifyStatModified();
        }

        /*---------------------------------------------------------------------------
        | --- GetAdditiveModifiers: Gets additive modifiers for a specific stat --- |
        ---------------------------------------------------------------------------*/
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (!m_additiveModifiers.ContainsKey(stat))
                yield break;

            foreach (Attribute attribute in m_additiveModifiers[stat].Keys)
            {
                float bonus = m_additiveModifiers[stat][attribute];
                yield return bonus * GetTotalPoints(attribute);
            }
        }

        /*-------------------------------------------------------------------------------
        | --- GetPercentageModifiers: Gets percentage modifiers for a specific stat --- |
        -------------------------------------------------------------------------------*/
        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if (!m_percentageModifiers.ContainsKey(stat))
                yield break;

            foreach (Attribute attribute in m_percentageModifiers[stat].Keys)
            {
                float bonus = m_percentageModifiers[stat][attribute];
                yield return bonus * GetTotalPoints(attribute);
            }
        }

        /*-------------------------------------------------------------------------------
        | --- CaptureState: Captures the current state of assigned attribute points --- |
        -------------------------------------------------------------------------------*/
        public object CaptureState()
        {
            return m_assignedPoints;
        }

        /*-------------------------------------------------------------------------------
        | --- RestoreState: Restores the assigned attribute points from saved state --- |
        -------------------------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            m_assignedPoints = new Dictionary<Attribute, int>((Dictionary<Attribute, int>)state);
            m_baseStats.NotifyStatModified();
        }
    }
}
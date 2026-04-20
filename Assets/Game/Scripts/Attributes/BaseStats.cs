/*---------------------------
File: BaseStats.cs
Author: Chandler Mays
----------------------------*/
using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Attributes
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Manages the core stats and progression for a character.                               *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores the character's class, progression data, and current level.                   *
     *      - Provides access to current health and experience requirements for leveling up.       *
     *      - Handles leveling up and replenishing health on level increase.                       *
     *      - Integrates with BaseProgression for stat scaling and progression logic.              *
     * ------------------------------------------------------------------------------------------- */
    public class BaseStats : MonoBehaviour
    {
        [Header("BaseStats")]
        [SerializeField] private BaseProgression m_baseProgression;
        [SerializeField] private bool m_useStatModifiers = false;

        [Range(1, 99)]
        [SerializeField] private int m_level = 1;

        public int Level => m_level;
        public BaseProgression Progression => m_baseProgression;

        public event Action OnStatModified;

        public float GetHealth()                    =>      GetStat(Stat.kHealth);
        public float GetDamage()                    =>      GetStat(Stat.kDamage);
        public float GetDefense()                   =>      GetStat(Stat.kDefense);
        public float GetMana()                      =>      GetStat(Stat.kMana);
        public float GetManaRegenRate()             =>      GetStat(Stat.kManaRegenRate);
        public float GetExperienceReward()          =>      GetStat(Stat.kExperienceReward);
        public float GetExperienceToLevelUp()       =>      GetStat(Stat.kExperienceToLevelUp);
        public float GetTotalAttributePoints()      =>      GetStat(Stat.kTotalAttributePoints);

        /*--------------------------------------------------------------
        | --- NotifyStatModified: Invokes the OnStatModified Event --- |
        --------------------------------------------------------------*/
        public void NotifyStatModified()
        {
            OnStatModified?.Invoke();
        }

        /*-------------------------------------------------------------------------------
        | --- GetStat: Returns the Value of the Specified Stat at the current Level --- |
        -------------------------------------------------------------------------------*/
        public float GetStat(Stat stat)
        {
            float baseStat = GetBaseStat(stat);

            if (m_useStatModifiers)
            {
                float additive = GetAdditiveModifier(stat);
                float percentage = GetPercentageModifier(stat);

                return (baseStat + additive) * (1.0f + percentage / 100.0f);
            }

            return baseStat;
        }

        /*-------------------------------------------------------------------
        | --- GetBaseStat: Returns the Base Value from Progression Data --- |
        -------------------------------------------------------------------*/
        private float GetBaseStat(Stat stat)
        {
            switch (stat)
            {
                case Stat.kHealth:
                    return m_baseProgression.GetHealth(m_level);

                case Stat.kDamage:
                    return m_baseProgression.GetDamage(m_level);

                case Stat.kDefense:
                    return m_baseProgression.GetDefense(m_level);

                case Stat.kMana:
                    return m_baseProgression.GetMana(m_level);

                case Stat.kManaRegenRate:
                    return m_baseProgression.GetManaRegenRate(m_level);

                case Stat.kExperienceReward:
                    if (m_baseProgression is AIProgression aiProgression)
                        return aiProgression.GetExperienceReward(m_level);
                    return 0;

                case Stat.kExperienceToLevelUp:
                    if (m_baseProgression is PlayerProgression playerProgression)
                        return playerProgression.GetExperience(m_level);
                    return 0;

                case Stat.kTotalAttributePoints:
                    if (m_baseProgression is PlayerProgression playerProg)
                        return playerProg.GetTotalAttributePoints(m_level);
                    return 0;

                default:
                    Debug.LogWarning($"Stat {stat} not implemented in GetBaseStat");
                    return 0;
            }
        }

        /*-----------------------------------------------
        | --- LevelUp: Increment the Player's Level --- |
        -----------------------------------------------*/
        public void LevelUp()
        {
            ++m_level;
            OnStatModified?.Invoke();
        }

        /*------------------------------------------------------------------------
        | --- SetLevel: Set the Player's Level directly (used for save/load) --- |
        ------------------------------------------------------------------------*/
        public void SetLevel(int level)
        {
            m_level = Mathf.Clamp(level, 1, 99);
            OnStatModified?.Invoke();
        }

        /*----------------------------------------------------------------------------
        | --- GetAdditiveModifier: Calculates Additive Modifiers from Components --- |
        ----------------------------------------------------------------------------*/
        private float GetAdditiveModifier(Stat stat)
        {
            if (!m_useStatModifiers)
                return 0;

            float total = 0;

            foreach (IStatModifier modifier in GetComponents<IStatModifier>())
            {
                foreach (float additive in modifier.GetAdditiveModifiers(stat))
                {
                    total += additive;
                }
            }

            return total;
        }

        /*--------------------------------------------------------------------------------
        | --- GetPercentageModifier: Calculates Percentage Modifiers from Components --- |
        --------------------------------------------------------------------------------*/
        private float GetPercentageModifier(Stat stat)
        {
            if (!m_useStatModifiers)
                return 0;

            float total = 0;

            foreach (IStatModifier modifier in GetComponents<IStatModifier>())
            {
                foreach (float percentage in modifier.GetPercentageModifiers(stat))
                {
                    total += percentage;
                }
            }

            return total;
        }
    }
}
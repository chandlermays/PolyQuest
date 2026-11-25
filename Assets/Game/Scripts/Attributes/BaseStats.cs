/*---------------------------
| File: BaseStats.cs
| Author: Chandler Mays
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

        [Tooltip("Clamped to the progression's max level (its row count).")]
        [Min(1)]
        [SerializeField] private int m_level = 1;

        public int Level => m_level;
        public BaseProgression Progression => m_baseProgression;
        public int MaxLevel => m_baseProgression != null ? m_baseProgression.MaxLevel : 1;

        // The level every stat lookup reads. Just the level for now: zone downscaling will plug in here later.
        public int EffectiveLevel => m_level;

        public event Action OnStatModified;

        public float GetHealth() => GetStat(Stat.kHealth);
        public float GetDamage() => GetStat(Stat.kDamage);
        public float GetDefense() => GetStat(Stat.kDefense);
        public float GetMana() => GetStat(Stat.kMana);
        public float GetManaRegenRate() => GetStat(Stat.kManaRegenRate);
        public float GetExperienceReward() => GetStat(Stat.kExperienceReward);
        public float GetExperienceToLevelUp() => GetStat(Stat.kExperienceToLevelUp);
        public float GetTotalAttributePoints() => GetStat(Stat.kTotalAttributePoints);

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
                    return m_baseProgression.GetHealth(EffectiveLevel);

                case Stat.kDamage:
                    return m_baseProgression.GetDamage(EffectiveLevel);

                case Stat.kDefense:
                    return m_baseProgression.GetDefense(EffectiveLevel);

                case Stat.kMana:
                    return m_baseProgression.GetMana(EffectiveLevel);

                case Stat.kManaRegenRate:
                    return m_baseProgression.GetManaRegenRate(EffectiveLevel);

                case Stat.kExperienceReward:
                    if (m_baseProgression is AIProgression aiProgression)
                        return aiProgression.GetExperienceReward(EffectiveLevel);
                    return 0;

                case Stat.kExperienceToLevelUp:
                    if (m_baseProgression is PlayerProgression playerProgression)
                        return playerProgression.GetExperience(EffectiveLevel);
                    return 0;

                case Stat.kTotalAttributePoints:
                    if (m_baseProgression is PlayerProgression playerProg)
                        return playerProg.GetTotalAttributePoints(EffectiveLevel);
                    return 0;

                default:
                    Debug.LogWarning($"Stat {stat} not implemented in GetBaseStat");
                    return 0;
            }
        }

        /*------------------------------------------------------------------------------
        | --- LevelUp: Increment the Player's Level; does nothing at the max level --- |
        ------------------------------------------------------------------------------*/
        public void LevelUp()
        {
            if (m_level >= MaxLevel)
                return;

            ++m_level;
            OnStatModified?.Invoke();
        }

        /*---------------------------------------------------------------------------------------------------
        | --- SetLevel: Set the Player's Level directly (used for save/load), clamped to [1, max level] --- |
        ---------------------------------------------------------------------------------------------------*/
        public void SetLevel(int level)
        {
            m_level = Mathf.Clamp(level, 1, MaxLevel);
            OnStatModified?.Invoke();
        }

#if UNITY_EDITOR
        /*------------------------------------------------------------------------------------
        | --- OnValidate: Keeps the Inspector's level within the progression's max level --- |
        ------------------------------------------------------------------------------------*/
        private void OnValidate()
        {
            if (m_baseProgression != null)
                m_level = Mathf.Clamp(m_level, 1, MaxLevel);
        }
#endif

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
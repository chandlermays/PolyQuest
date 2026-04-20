/*---------------------------
File: StatsEquipableItem.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Inventories
{
    [CreateAssetMenu(menuName = "PolyQuest/Items/Stats Equipable Item", fileName = "New Stats Equipable Item")]
    public class StatsEquipableItem : EquipableItem, IStatModifier
    {
        [System.Serializable]
        private struct Modifier
        {
            [SerializeField] private Stat m_stat;
            [SerializeField] private float m_value;

            public readonly Stat Stat => m_stat;
            public readonly float Value => m_value;
        }

        [SerializeField] private Modifier[] m_additiveModifiers;
        [SerializeField] private Modifier[] m_percentageModifiers;

        /*-----------------------------------------------------------------------------------
        | --- GetAdditiveModifiers: Get all additive modifiers from this equipable item --- |
        -----------------------------------------------------------------------------------*/
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            foreach (var modifier in m_additiveModifiers)
            {
                if (modifier.Stat == stat)
                {
                    yield return modifier.Value;
                }
            }
        }

        /*---------------------------------------------------------------------------------------
        | --- GetPercentageModifiers: Get all percentage modifiers from this equipable item --- |
        ---------------------------------------------------------------------------------------*/
        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            foreach (var modifier in m_percentageModifiers)
            {
                if (modifier.Stat == stat)
                {
                    yield return modifier.Value;
                }
            }
        }
    }
}
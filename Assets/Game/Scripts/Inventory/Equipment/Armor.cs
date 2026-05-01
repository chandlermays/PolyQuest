/*---------------------------
File: Weapon.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Inventories
{
    [CreateAssetMenu(menuName = "PolyQuest/Items/Armor", fileName = "New Armor")]
    public class Armor : EquipableItem, IStatModifier
    {
        [Header("Armor Settings")]
        [SerializeField] private float m_baseDefense = 0f;

        /*--------------------------------------------------------------------------
        | --- GetAdditiveModifiers: Get all additive modifiers from this armor --- |
        --------------------------------------------------------------------------*/
        public override IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (stat == Stat.kDefense)
                yield return m_baseDefense;

            foreach (var mod in base.GetAdditiveModifiers(stat))
                yield return mod;
        }
    }
}
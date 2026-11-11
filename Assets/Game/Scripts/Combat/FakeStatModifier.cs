#if UNITY_EDITOR
/*---------------------------
| File: FakeStatModifier.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Tests
{
    public class FakeStatModifier : MonoBehaviour, IStatModifier
    {
        public Stat TargetStat = Stat.kHealth;
        public float Additive = 0f;
        public float Percentage = 0f;

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (stat == TargetStat)
                yield return Additive;
        }

        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if (stat == TargetStat)
                yield return Percentage;
        }
    }
}
#endif
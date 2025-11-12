using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(fileName = "New Targeting Strategy", menuName = "PolyQuest/Abilities/Targeting/New Targeting Strategy", order = 0)]
    public class TargetingTest : TargetingStrategy
    {
        public override void StartTargeting(AbilityConfig config, Action onComplete)
        {
            Debug.Log("Targeting Test Started");
            onComplete();
        }
    }
}
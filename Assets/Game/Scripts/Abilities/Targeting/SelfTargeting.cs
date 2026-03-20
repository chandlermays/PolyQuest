using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(fileName = "New Self-Targeting Strategy", menuName = "PolyQuest/Abilities/Targeting/New Self-Targeting Strategy", order = 0)]
    public class SelfTargeting : TargetingStrategy
    {
        public override void StartTargeting(AbilityConfig config, Action onComplete)
        {
            config.SetSingleTarget(config.User);
            config.TargetPoint = config.User.transform.position;
            onComplete?.Invoke();
        }
    }
}
using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Targeting/New Self-Targeting Strategy", fileName = "New Self-Targeting Strategy")]
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
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
            // Targets is expecting an IEnumerable (any container) and not a single item so we store the user in a single-element array
            // *** NOTE: See about refactoring this so that it could take in a single target without having to call 'new' ***
            config.Targets = (new GameObject[] { config.User });
            config.TargetPoint = config.User.transform.position;
            onComplete?.Invoke();
        }
    }
}
using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(fileName = "New Cast At Target Effect", menuName = "PolyQuest/Abilities/Effects/New Cast At Target Effect", order = 0)]
    public class CastAtTargetEffect : EffectStrategy
    {
        public override void StartEffect(AbilityConfig config, Action onComplete)
        {
            config.User.transform.LookAt(config.TargetPoint);
            onComplete?.Invoke();
        }
    }
}
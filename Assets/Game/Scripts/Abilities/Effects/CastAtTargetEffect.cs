using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Effects/New Cast At Target Effect", fileName = "New Cast At Target Effect")]
    public class CastAtTargetEffect : EffectStrategy
    {
        public override void StartEffect(AbilityConfig config, Action onComplete)
        {
            config.User.transform.LookAt(config.TargetPoint);
            onComplete?.Invoke();
        }
    }
}
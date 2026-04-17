using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Effects/New Animation Effect", fileName = "New Animation Effect")]
    public class AnimationEffect : EffectStrategy
    {
        [SerializeField] private string m_animationTrigger;

        /*---------------------------------------------------------------- 
        | --- StartEffect: Triggers an animation on the ability user --- |
        ----------------------------------------------------------------*/
        public override void StartEffect(AbilityConfig config, Action onComplete)
        {
            if (config.User.TryGetComponent<Animator>(out var animator))
            {
                animator.SetTrigger(m_animationTrigger);
                onComplete?.Invoke();

                // The animation clip has an event that should indicate when the ability's effect gets applied.
                // Figure out how to use that event to call onComplete at the right time.
            }
        }
    }
}
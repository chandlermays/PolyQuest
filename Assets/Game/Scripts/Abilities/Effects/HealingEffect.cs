using System;
using UnityEngine;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(fileName = "New Healing Effect", menuName = "PolyQuest/Abilities/Effects/New Healing Effect", order = 0)]
    public class HealingEffect : EffectStrategy
    {
        [SerializeField] private int m_healAmount;

        /*--------------------------------------------------------------------------
        | --- StartEffect: Replenishes health to each target's HealthComponent --- |
        --------------------------------------------------------------------------*/
        public override void StartEffect(AbilityConfig config, Action onComplete)
        {
            foreach (GameObject target in config.Targets)
            {
                if (target.TryGetComponent<HealthComponent>(out var health))
                {
                    health.ReplenishHealth(m_healAmount);
                }
            }

            onComplete?.Invoke();
        }
    }
}
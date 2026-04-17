using System;
using UnityEngine;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Effects/New Damage Effect", fileName = "New Damage Effect")]
    public class DamageEffect : EffectStrategy
    {
        [SerializeField] private int m_damageAmount;

        /*----------------------------------------------------------------------
        | --- StartEffect: Applies damage to each target's HealthComponent --- |
        ----------------------------------------------------------------------*/
        public override void StartEffect(AbilityConfig config, Action onComplete)
        {
            foreach (GameObject target in config.Targets)
            {
                if (target.TryGetComponent<HealthComponent>(out var health))
                {
                    health.TakeDamage(config.User, m_damageAmount);
                }
            }

            onComplete();
        }
    }
}
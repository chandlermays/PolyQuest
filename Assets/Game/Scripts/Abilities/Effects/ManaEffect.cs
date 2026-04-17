using System;
using UnityEngine;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Effects/New Mana Effect", fileName = "New Mana Effect")]
    public class ManaEffect : EffectStrategy
    {
        [SerializeField] private int m_manaAmount;

        /*----------------------------------------------------------------------
        | --- StartEffect: Replenishes mana to each target's ManaComponent --- |
        ----------------------------------------------------------------------*/
        public override void StartEffect(AbilityConfig config, Action onComplete)
        {
            foreach (GameObject target in config.Targets)
            {
                if (target.TryGetComponent<ManaComponent>(out var mana))
                {

                }
            }

            onComplete?.Invoke();
        }
    }
}
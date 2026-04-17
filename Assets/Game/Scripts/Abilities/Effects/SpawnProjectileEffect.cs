using System;
using UnityEngine;
//---------------------------------
using PolyQuest.Combat;
using PolyQuest.Components;

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Effects/New Spawn Projectile Effect", fileName = "New Spawn Projectile Effect")]
    public class SpawnProjectileEffect : EffectStrategy
    {
        [SerializeField] private Projectile m_projectilePrefab;
        [SerializeField] private float m_damage;
        [SerializeField] private bool m_isRightHanded = true;

        public override void StartEffect(AbilityConfig config, Action onComplete)
        {
            CombatComponent combatComponent = config.User.GetComponent<CombatComponent>();
            Vector3 spawnPosition = combatComponent.GetHandTransform(m_isRightHanded).position;

            foreach (GameObject target in config.Targets)
            {
                Projectile projectile = Instantiate(m_projectilePrefab);
                projectile.transform.position = spawnPosition;
                projectile.SetTarget(target.transform, config.User, m_damage);
            }

            onComplete();
        }
    }
}
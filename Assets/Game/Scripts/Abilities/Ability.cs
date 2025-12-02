using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Components;

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(fileName = "New Ability", menuName = "PolyQuest/Abilities/New Ability", order = 0)]
    public class Ability : ActionItem
    {
        [Header("Ability Config")]
        [SerializeField] private float m_manaCost = 10f;
        [SerializeField] private float m_cooldownTime = 5f;
        [SerializeField] private TargetingStrategy m_targetingStrategy;
        [SerializeField] private EffectStrategy[] m_effects;
        [SerializeField] private FilteringStrategy[] m_filters;

        /*-------------------------------------------------------------- 
        | --- Use: Initiates the targeting process for the ability --- |
        --------------------------------------------------------------*/
        public override bool Use(GameObject user)
        {
            ManaComponent mana = user.GetComponent<ManaComponent>();
            Utilities.CheckForNull(mana, nameof(mana));

            if (mana.CurrentMana < m_manaCost)
                return false;

            Cooldowns cooldowns = user.GetComponent<Cooldowns>();
            Utilities.CheckForNull(cooldowns, nameof(cooldowns));

            if (cooldowns.GetRemainingCooldown(this) > 0f)
                return false;

            AbilityConfig config = new(user);
            m_targetingStrategy.StartTargeting(config, () => TargetAcquired(config));
            return true;
        }

        /*------------------------------------------------------------------------------------------------ 
        | --- TargetAcquired: Applies filters to the acquired targets and starts the effects on them --- |
        ------------------------------------------------------------------------------------------------*/
        private void TargetAcquired(AbilityConfig config)
        {
            if (config.IsCancelled)
                return;

            ManaComponent mana = config.User.GetComponent<ManaComponent>();
            if (!mana.UseMana(m_manaCost))
                return;

            Cooldowns cooldowns = config.User.GetComponent<Cooldowns>();
            cooldowns.StartCooldown(this, m_cooldownTime);

            foreach (FilteringStrategy filterStrategy in m_filters)
            {
                config.Targets = filterStrategy.Filter(config.Targets);
            }

            foreach (EffectStrategy effect in m_effects)
            {
                effect.StartEffect(config, EffectCompleted);
            }
        }

        /*------------------------------------------------------------------- 
        | --- EffectCompleted: Callback for when an effect is completed --- |
        -------------------------------------------------------------------*/
        private void EffectCompleted()
        {
            //...
        }
    }
}
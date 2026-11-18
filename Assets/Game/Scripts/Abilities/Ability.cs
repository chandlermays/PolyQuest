/*---------------------------
| File: Ability.cs
| Author: Chandler Mays
----------------------------*/
using System;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Components;
using PolyQuest.Core;

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/New Ability", fileName = "New Ability")]
    public class Ability : ActionItem
    {
        [Header("Ability Config")]
        [SerializeField] private float m_manaCost = 10f;
        [SerializeField] private float m_cooldownTime = 5f;
        [Tooltip("Abilities with the same group share one cooldown (e.g. Potions). Leave empty for a cooldown of its own.")]
        [SerializeField] private string m_cooldownGroup = "";
        [SerializeField] private TargetingStrategy m_targetingStrategy;
        [SerializeField] private EffectStrategy[] m_effects;
        [SerializeField] private FilteringStrategy[] m_filters;

        public string CooldownGroup => m_cooldownGroup;

        /*--------------------------------------------------------------
        | --- Use: Initiates the targeting process for the ability --- |
        --------------------------------------------------------------*/
        public override bool Use(GameObject user, Action onUsed)
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

            ActionManager actionManager = user.GetComponent<ActionManager>();
            Utilities.CheckForNull(actionManager, nameof(actionManager));

            actionManager.StartAction(config);

            m_targetingStrategy.StartTargeting(config, () => TargetAcquired(config, onUsed));
            return true;
        }

        /*------------------------------------------------------------------------------------------------
        | --- TargetAcquired: Applies filters to the acquired targets and starts the effects on them --- |
        ------------------------------------------------------------------------------------------------*/
        private void TargetAcquired(AbilityConfig config, Action onUsed)
        {
            if (config.IsCancelled)
                return;

            ManaComponent mana = config.User.GetComponent<ManaComponent>();
            if (!mana.UseMana(m_manaCost))
                return;

            Cooldowns cooldowns = config.User.GetComponent<Cooldowns>();
            cooldowns.StartCooldown(this, m_cooldownTime);

            // The cast is happening: a consumable is used up now, not when targeting started
            onUsed?.Invoke();

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
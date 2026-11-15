/*---------------------------
| File: AbilityTests.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
//---------------------------------
using PolyQuest.Abilities;
using PolyQuest.Components;
using PolyQuest.Core;
using PolyQuest.Inventories;
using UnityEditor;
using UnityEngine;

namespace PolyQuest.Tests
{
    // Casting an ability: mana, cooldowns, cancelled targeting, filters and effects, and when a consumable ability is
    // used up. Targeting is test-controlled (ManualTargeting) or the shipped SelfTargeting; no cursor is involved.
    public class AbilityTests
    {
        private const float kTolerance = 1e-4f;

        private InventoryTestKit m_items;
        private EntityTestKit m_entities;

        private GameObject m_caster;
        private ManaComponent m_mana;
        private HealthComponent m_health;
        private Cooldowns m_cooldowns;
        private Actions m_actions;
        private ManualTargeting m_targeting;

        [SetUp]
        public void SetUp()
        {
            m_items = new InventoryTestKit();
            m_entities = new EntityTestKit();

            m_caster = m_entities.Entity("Caster", health: 100, mana: 100);
            EntityTestKit.SetFaction(m_caster, Faction.kPlayer);
            m_health = EntityTestKit.AddHealth(m_caster);
            m_mana = EntityTestKit.AddMana(m_caster);
            m_cooldowns = m_caster.AddComponent<Cooldowns>();
            m_caster.AddComponent<ActionManager>();
            m_items.NewInventory(m_caster, 4);
            m_actions = m_caster.AddComponent<Actions>();
            InventoryTestKit.InvokeAwake(m_actions);

            m_targeting = m_items.Track(ScriptableObject.CreateInstance<ManualTargeting>());
        }

        [TearDown]
        public void TearDown()
        {
            m_entities.Dispose();
            m_items.Dispose();
        }

        /* --- Helpers --- */

        private HealthComponent Target(string name, Faction faction, string tag = "Untagged")
        {
            GameObject target = m_entities.Entity(name, health: 100);
            target.tag = tag;
            EntityTestKit.SetFaction(target, faction);
            return EntityTestKit.AddHealth(target);
        }

        private T Effect<T>(string field, int amount) where T : EffectStrategy
        {
            T effect = m_items.Track(ScriptableObject.CreateInstance<T>());
            SerializedTestUtil.SetInt(effect, field, amount);
            return effect;
        }

        private DamageEffect Damage(int amount) => Effect<DamageEffect>("m_damageAmount", amount);
        private HealingEffect Healing(int amount) => Effect<HealingEffect>("m_healAmount", amount);
        private ManaEffect ManaRestore(int amount) => Effect<ManaEffect>("m_manaAmount", amount);

        private TagFilter OnlyTagged(string tag)
        {
            TagFilter filter = m_items.Track(ScriptableObject.CreateInstance<TagFilter>());
            SerializedTestUtil.SetString(filter, "m_filteredTag", tag);
            return filter;
        }

        private Ability NewAbility(string id, float manaCost, float cooldown, TargetingStrategy targeting, EffectStrategy[] effects,
                                   FilteringStrategy[] filters = null, bool consumable = false)
        {
            Ability ability = m_items.Item<Ability>(id, stackable: consumable);
            SerializedTestUtil.SetFloat(ability, "m_manaCost", manaCost);
            SerializedTestUtil.SetFloat(ability, "m_cooldownTime", cooldown);
            SerializedTestUtil.SetBool(ability, "m_isConsumable", consumable);
            SerializedTestUtil.SetObject(ability, "m_targetingStrategy", targeting);
            SetObjects(ability, "m_effects", effects);
            SetObjects(ability, "m_filters", filters ?? new FilteringStrategy[0]);
            return ability;
        }

        private static void SetObjects(Object target, string field, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty array = so.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; ++i)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void Cancel() => m_caster.GetComponent<ActionManager>().CancelCurrentAction();

        /* ============================== Casting ============================== */

        [Test]
        [Description("A cast spends the mana cost once, starts the cooldown, and applies every effect to the targets.")]
        public void Cast_SpendsManaOnce_StartsTheCooldown_AndAppliesEveryEffect()
        {
            HealthComponent bandit = Target("Bandit", Faction.kEnemy);
            m_targeting.Targets.Add(bandit.gameObject);
            Ability bolt = NewAbility("bolt", manaCost: 30f, cooldown: 5f, m_targeting, new EffectStrategy[] { Damage(10), Damage(5) });

            Assert.That(bolt.Use(m_caster), Is.True);
            m_targeting.Finish();

            Assert.That(m_mana.CurrentMana, Is.EqualTo(70f).Within(kTolerance));
            Assert.That(m_cooldowns.GetRemainingCooldown(bolt), Is.EqualTo(5f).Within(kTolerance));
            Assert.That(bandit.CurrentHealth, Is.EqualTo(85f).Within(kTolerance));
        }

        [Test]
        [Description("With too little mana the cast is refused: Use returns false, and no mana, cooldown or effect is spent.")]
        public void Use_WithTooLittleMana_IsRefused()
        {
            HealthComponent bandit = Target("Bandit", Faction.kEnemy);
            m_targeting.Targets.Add(bandit.gameObject);
            Ability bolt = NewAbility("bolt", manaCost: 150f, cooldown: 5f, m_targeting, new EffectStrategy[] { Damage(10) });

            Assert.That(bolt.Use(m_caster), Is.False);

            Assert.That(m_mana.CurrentMana, Is.EqualTo(100f).Within(kTolerance));
            Assert.That(m_cooldowns.GetRemainingCooldown(bolt), Is.EqualTo(0f));
            Assert.That(bandit.CurrentHealth, Is.EqualTo(100f).Within(kTolerance));
        }

        [Test]
        [Description("While the ability is on cooldown the cast is refused: Use returns false and nothing is spent.")]
        public void Use_WhileOnCooldown_IsRefused()
        {
            Ability bolt = NewAbility("bolt", manaCost: 30f, cooldown: 5f, m_targeting, new EffectStrategy[] { Damage(10) });
            m_cooldowns.StartCooldown(bolt, 3f);

            Assert.That(bolt.Use(m_caster), Is.False);

            Assert.That(m_mana.CurrentMana, Is.EqualTo(100f).Within(kTolerance));
        }

        [Test]
        [Description("Targeting cancelled before it finishes spends nothing: no mana, no cooldown, no effect.")]
        public void CancelledTargeting_SpendsNothing()
        {
            HealthComponent bandit = Target("Bandit", Faction.kEnemy);
            m_targeting.Targets.Add(bandit.gameObject);
            Ability bolt = NewAbility("bolt", manaCost: 30f, cooldown: 5f, m_targeting, new EffectStrategy[] { Damage(10) });
            bolt.Use(m_caster);

            Cancel();
            m_targeting.Finish();

            Assert.That(m_mana.CurrentMana, Is.EqualTo(100f).Within(kTolerance));
            Assert.That(m_cooldowns.GetRemainingCooldown(bolt), Is.EqualTo(0f));
            Assert.That(bandit.CurrentHealth, Is.EqualTo(100f).Within(kTolerance));
        }

        [Test]
        [Description("If mana drops below the cost while targeting, the cast doesn't happen: no cooldown and no effect.")]
        public void ManaSpentWhileTargeting_TheCastDoesNotHappen()
        {
            HealthComponent bandit = Target("Bandit", Faction.kEnemy);
            m_targeting.Targets.Add(bandit.gameObject);
            Ability bolt = NewAbility("bolt", manaCost: 30f, cooldown: 5f, m_targeting, new EffectStrategy[] { Damage(10) });
            bolt.Use(m_caster);
            m_mana.UseMana(80f);

            m_targeting.Finish();

            Assert.That(m_mana.CurrentMana, Is.EqualTo(20f).Within(kTolerance));
            Assert.That(m_cooldowns.GetRemainingCooldown(bolt), Is.EqualTo(0f));
            Assert.That(bandit.CurrentHealth, Is.EqualTo(100f).Within(kTolerance));
        }

        /* ============================== Filters and effects ============================== */

        [Test]
        [Description("Filters run before the effects: only targets with the filter's tag are hit.")]
        public void Filters_RunBeforeTheEffects()
        {
            HealthComponent tagged = Target("Tagged", Faction.kEnemy, "Enemy");
            HealthComponent untagged = Target("Untagged", Faction.kEnemy);
            m_targeting.Targets.Add(tagged.gameObject);
            m_targeting.Targets.Add(untagged.gameObject);
            Ability bolt = NewAbility("bolt", 0f, 0f, m_targeting, new EffectStrategy[] { Damage(10) }, new FilteringStrategy[] { OnlyTagged("Enemy") });

            bolt.Use(m_caster);
            m_targeting.Finish();

            Assert.That(tagged.CurrentHealth, Is.EqualTo(90f).Within(kTolerance));
            Assert.That(untagged.CurrentHealth, Is.EqualTo(100f).Within(kTolerance));
        }

        [Test]
        [Description("A damage effect hits through the normal damage rules: an ally caught in it isn't hurt.")]
        public void DamageEffect_DoesNotHurtAllies()
        {
            HealthComponent guard = Target("Guard", Faction.kNPC);
            m_targeting.Targets.Add(guard.gameObject);
            Ability bolt = NewAbility("bolt", 0f, 0f, m_targeting, new EffectStrategy[] { Damage(10) });

            bolt.Use(m_caster);
            m_targeting.Finish();

            Assert.That(guard.CurrentHealth, Is.EqualTo(100f).Within(kTolerance));
        }

        [Test]
        [Description("A healing effect heals each target, never past its maximum and never a dead one.")]
        public void HealingEffect_Heals_NotPastTheMaximum_NotTheDead()
        {
            HealthComponent hurt = Target("Hurt", Faction.kNPC);
            HealthComponent fallen = Target("Fallen", Faction.kNPC);
            GameObject bandit = m_entities.Attacker("Bandit", Faction.kEnemy);
            hurt.TakeDamage(bandit, 40f);
            fallen.TakeDamage(bandit, 100f);
            float fallenHealth = fallen.CurrentHealth;
            m_targeting.Targets.Add(hurt.gameObject);
            m_targeting.Targets.Add(fallen.gameObject);
            Ability mend = NewAbility("mend", 0f, 0f, m_targeting, new EffectStrategy[] { Healing(50) });

            mend.Use(m_caster);
            m_targeting.Finish();

            Assert.That(hurt.CurrentHealth, Is.EqualTo(100f).Within(kTolerance));
            Assert.That(fallen.CurrentHealth, Is.EqualTo(fallenHealth));
            Assert.That(fallen.IsDead, Is.True);
        }

        [Test]
        [Description("A mana effect restores each target's mana, never past its maximum.")]
        public void ManaEffect_RestoresMana_NotPastTheMaximum()
        {
            m_mana.UseMana(30f);
            m_targeting.Targets.Add(m_caster);
            Ability focus = NewAbility("focus", 0f, 0f, m_targeting, new EffectStrategy[] { ManaRestore(20) });

            focus.Use(m_caster);
            m_targeting.Finish();
            Assert.That(m_mana.CurrentMana, Is.EqualTo(90f).Within(kTolerance));

            focus.Use(m_caster);
            m_targeting.Finish();
            Assert.That(m_mana.CurrentMana, Is.EqualTo(100f).Within(kTolerance));
        }

        /* ============================== Consumable abilities ============================== */

        [Test]
        [Description("A consumable ability with click targeting is used up only when the cast happens, not when targeting starts.")]
        public void ConsumableWithClickTargeting_IsUsedUpWhenTheCastHappens()
        {
            HealthComponent bandit = Target("Bandit", Faction.kEnemy);
            m_targeting.Targets.Add(bandit.gameObject);
            Ability scroll = NewAbility("fire-scroll", 0f, 0f, m_targeting, new EffectStrategy[] { Damage(10) }, consumable: true);
            m_actions.AddActionItem(scroll, 0, 2);

            m_actions.Use(m_caster, 0);
            Assert.That(m_actions.GetQuantity(0), Is.EqualTo(2), "used up before the cast");

            m_targeting.Finish();
            Assert.That(m_actions.GetQuantity(0), Is.EqualTo(1));
        }

        [Test]
        [Description("A consumable ability whose click targeting is cancelled is not used up.")]
        public void ConsumableWithClickTargeting_Cancelled_IsNotUsedUp()
        {
            Ability scroll = NewAbility("fire-scroll", 0f, 0f, m_targeting, new EffectStrategy[] { Damage(10) }, consumable: true);
            m_actions.AddActionItem(scroll, 0, 2);

            m_actions.Use(m_caster, 0);
            Cancel();
            m_targeting.Finish();

            Assert.That(m_actions.GetQuantity(0), Is.EqualTo(2));
        }

        [Test]
        [Description("A self-targeted potion casts at once and is used up, even at full health.")]
        public void SelfTargetedPotion_AtFullHealth_IsUsedUp()
        {
            SelfTargeting self = m_items.Track(ScriptableObject.CreateInstance<SelfTargeting>());
            Ability potion = NewAbility("test-health-potion", 0f, 0f, self, new EffectStrategy[] { Healing(50) }, consumable: true);
            m_actions.AddActionItem(potion, 0, 3);
            Assert.That(m_health.CurrentHealth, Is.EqualTo(100f).Within(kTolerance), "fixture: the caster must be at full health");

            m_actions.Use(m_caster, 0);

            Assert.That(m_actions.GetQuantity(0), Is.EqualTo(2));
        }
    }
}
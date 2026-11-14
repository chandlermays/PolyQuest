/*---------------------------
| File: HealthComponentTests.cs
| Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using NUnit.Framework;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Components;
using System.Collections.Generic;
using UnityEngine;

namespace PolyQuest.Tests
{
    // Taking damage, dying, friendly fire, replenishing, max-health changes and the save data.
    public class HealthComponentTests
    {
        private const float kTolerance = 1e-4f;

        private EntityTestKit m_kit;
        private GameObject m_bandit;
        private HealthComponent m_health;
        private GameObject m_player;

        private readonly List<string> m_events = new();
        private int m_hits, m_deaths;

        [SetUp]
        public void SetUp()
        {
            m_kit = new EntityTestKit();
            m_bandit = m_kit.Entity("Bandit", health: 100);
            EntityTestKit.SetFaction(m_bandit, Faction.kEnemy);
            m_health = EntityTestKit.AddHealth(m_bandit);
            m_player = m_kit.Attacker("Player", Faction.kPlayer);

            m_events.Clear();
            m_hits = m_deaths = 0;
            m_health.OnHit += _ => { ++m_hits; m_events.Add("hit"); };
            m_health.OnDeath += () => { ++m_deaths; m_events.Add("death"); };
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        private void Hit(float damage) => m_health.TakeDamage(m_player, damage);

        /* ============================== Taking damage ============================== */

        [Test]
        [Description("A hostile attacker's hit lowers health by the damage and raises OnHit with the attacker.")]
        public void HostileHit_LowersHealth_AndRaisesOnHitWithTheAttacker()
        {
            GameObject reportedAttacker = null;
            m_health.OnHit += attacker => reportedAttacker = attacker;

            Hit(30f);

            Assert.That(m_health.CurrentHealth, Is.EqualTo(70f).Within(kTolerance));
            Assert.That(reportedAttacker, Is.SameAs(m_player));
            Assert.That(m_health.IsDead, Is.False);
        }

        [TestCase(Faction.kNPC, Faction.kPlayer, TestName = "NonHostileHit_ChangesNothing(guard hits player)")]
        [TestCase(Faction.kEnemy, Faction.kEnemy, TestName = "NonHostileHit_ChangesNothing(bandit hits bandit)")]
        [Description("A hit from an attacker that isn't hostile to the target deals no damage and raises no OnHit.")]
        public void NonHostileHit_ChangesNothing(Faction attackerFaction, Faction targetFaction)
        {
            GameObject target = m_kit.Entity("Target", health: 100);
            EntityTestKit.SetFaction(target, targetFaction);
            HealthComponent health = EntityTestKit.AddHealth(target);
            int hits = 0;
            health.OnHit += _ => ++hits;

            health.TakeDamage(m_kit.Attacker("Attacker", attackerFaction), 30f);

            Assert.That(health.CurrentHealth, Is.EqualTo(100f).Within(kTolerance));
            Assert.That(hits, Is.EqualTo(0));
        }

        [TestCase(false, true, TestName = "HitWithoutAFaction_DealsDamage(attacker has none)")]
        [TestCase(true, false, TestName = "HitWithoutAFaction_DealsDamage(target has none)")]
        [Description("When the attacker or the target has no faction, the hit deals its damage.")]
        public void HitWithoutAFaction_DealsDamage(bool attackerHasFaction, bool targetHasFaction)
        {
            GameObject target = m_kit.Entity("Target", health: 100);
            if (targetHasFaction)
                EntityTestKit.SetFaction(target, Faction.kEnemy);
            HealthComponent health = EntityTestKit.AddHealth(target);

            health.TakeDamage(m_kit.Attacker("Attacker", attackerHasFaction ? Faction.kPlayer : null), 30f);

            Assert.That(health.CurrentHealth, Is.EqualTo(70f).Within(kTolerance));
        }

        /* ============================== Death ============================== */

        [Test]
        [Description("A killing blow larger than the remaining health leaves health at 0, and the entity dies once.")]
        public void KillingBlow_LeavesHealthAtZero_AndDiesOnce()
        {
            Hit(150f);

            Assert.That(m_health.IsDead, Is.True);
            Assert.That(m_health.CurrentHealth, Is.EqualTo(0f));
            Assert.That(m_deaths, Is.EqualTo(1));
        }

        [Test]
        [Description("The killing blow still counts as a hit: OnHit is raised, then OnDeath.")]
        public void KillingBlow_RaisesOnHit_ThenOnDeath()
        {
            Hit(100f);

            Assert.That(m_events, Is.EqualTo(new[] { "hit", "death" }));
        }

        [Test]
        [Description("Hitting a dead entity changes nothing: no damage, no OnHit, no second death.")]
        public void Dead_IgnoresFurtherHits()
        {
            Hit(100f);
            float healthAtDeath = m_health.CurrentHealth;

            Hit(30f);

            Assert.That(m_health.CurrentHealth, Is.EqualTo(healthAtDeath));
            Assert.That(m_hits, Is.EqualTo(1));
            Assert.That(m_deaths, Is.EqualTo(1));
        }

        [Test]
        [Description("A dead entity can't be replenished, partly or fully, and stays dead.")]
        public void Dead_CannotBeReplenished()
        {
            Hit(100f);
            float healthAtDeath = m_health.CurrentHealth;

            m_health.ReplenishHealth(50f);
            m_health.FullyReplenishHealth();

            Assert.That(m_health.CurrentHealth, Is.EqualTo(healthAtDeath));
            Assert.That(m_health.IsDead, Is.True);
        }

        /* ============================== Replenishing ============================== */

        [Test]
        [Description("ReplenishHealth adds health but never past the maximum.")]
        public void ReplenishHealth_StopsAtTheMaximum()
        {
            Hit(30f);

            m_health.ReplenishHealth(10f);
            Assert.That(m_health.CurrentHealth, Is.EqualTo(80f).Within(kTolerance));

            m_health.ReplenishHealth(500f);
            Assert.That(m_health.CurrentHealth, Is.EqualTo(100f).Within(kTolerance));
        }

        /* ============================== Max-health changes ============================== */

        [Test]
        [Description("When the maximum changes, health keeps the same percentage: at 20/100, losing a +50 item gives 10/50, and getting it back gives 20/100.")]
        public void MaxHealthChange_KeepsTheSamePercentage_BothWays()
        {
            GameObject knight = m_kit.Entity("Knight", health: 50);
            FakeStatModifier item = EntityTestKit.Modifier(knight, Stat.kHealth, 50f);
            HealthComponent health = EntityTestKit.AddHealth(knight);
            BaseStats stats = knight.GetComponent<BaseStats>();
            health.TakeDamage(m_kit.Attacker("Attacker", null), 80f);
            Assert.That(health.CurrentHealth, Is.EqualTo(20f).Within(kTolerance), "fixture: the knight must be at 20/100");

            item.Additive = 0f;
            stats.NotifyStatModified();
            Assert.That(stats.GetHealth(), Is.EqualTo(50f).Within(kTolerance), "fixture: the maximum must drop to 50");
            Assert.That(health.CurrentHealth, Is.EqualTo(10f).Within(kTolerance), "after losing the item");

            item.Additive = 50f;
            stats.NotifyStatModified();
            Assert.That(health.CurrentHealth, Is.EqualTo(20f).Within(kTolerance), "after getting the item back");
        }

        [Test]
        [Description("A drop in the maximum never takes a living entity's health to 0.")]
        public void MaxHealthDrop_NeverTakesALivingEntityToZero()
        {
            GameObject knight = m_kit.Entity("Knight", health: 50);
            FakeStatModifier item = EntityTestKit.Modifier(knight, Stat.kHealth, 50f);
            HealthComponent health = EntityTestKit.AddHealth(knight);
            health.TakeDamage(m_kit.Attacker("Attacker", null), 99f);
            Assert.That(health.CurrentHealth, Is.EqualTo(1f).Within(kTolerance), "fixture: the knight must be at 1/100");

            item.Additive = -49f;
            knight.GetComponent<BaseStats>().NotifyStatModified();

            Assert.That(health.CurrentHealth, Is.GreaterThan(0f));
            Assert.That(health.IsDead, Is.False);
        }

        /* ============================== Saving ============================== */

        private HealthComponent FreshBandit()
        {
            GameObject bandit = m_kit.Entity("FreshBandit", health: 100);
            EntityTestKit.SetFaction(bandit, Faction.kEnemy);
            return EntityTestKit.AddHealth(bandit);
        }

        [Test]
        [Description("Capturing a hurt entity's health and restoring it into a fresh one restores that health, alive.")]
        public void CaptureThenRestore_RestoresTheHealth()
        {
            Hit(63f);
            HealthComponent fresh = FreshBandit();

            fresh.RestoreState(m_health.CaptureState());

            Assert.That(fresh.CurrentHealth, Is.EqualTo(37f).Within(kTolerance));
            Assert.That(fresh.IsDead, Is.False);
        }

        [Test]
        [Description("Restoring a dead entity's save leaves it dead, and it ignores hits.")]
        public void CaptureThenRestore_Dead_StaysDead()
        {
            Hit(100f);
            HealthComponent fresh = FreshBandit();

            fresh.RestoreState(m_health.CaptureState());
            fresh.TakeDamage(m_player, 30f);

            Assert.That(fresh.IsDead, Is.True);
            Assert.That(fresh.CurrentHealth, Is.EqualTo(0f).Within(kTolerance));
        }

        [Test]
        [Description("Health save data that can't be read is ignored: health and life are unchanged.")]
        public void RestoreState_Unreadable_IsIgnored()
        {
            Hit(40f);

            m_health.RestoreState(new JValue("full"));
            m_health.RestoreState(new JArray(1, 2));
            m_health.RestoreState(new JObject { ["health"] = "lots", ["isDead"] = false });

            Assert.That(m_health.CurrentHealth, Is.EqualTo(60f).Within(kTolerance));
            Assert.That(m_health.IsDead, Is.False);
        }
    }
}
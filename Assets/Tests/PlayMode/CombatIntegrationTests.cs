/*---------------------------
| File: CombatIntegrationTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Combat;
using PolyQuest.Components;
using PolyQuest.Inventories;
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    // Combat on the real Player: weapons, the attack loop, projectiles, kill XP, level-up refills and mana regeneration.
    public class CombatIntegrationTests
    {
        private const BindingFlags kAny = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private const string kIronSword = "Items/Weapons/Swords/Iron Sword";
        private const string kWoodenBow = "Items/Weapons/Bows/Wooden Bow";
        private const float kTolerance = 1e-3f;

        private PlayerRig m_rig;
        private readonly List<Object> m_created = new();

        [SetUp]
        public void SetUp()
        {
            m_rig = new PlayerRig();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Projectile projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                Object.DestroyImmediate(projectile.gameObject);
            for (int i = m_created.Count - 1; i >= 0; --i)
            {
                if (m_created[i] != null)
                    Object.DestroyImmediate(m_created[i]);
            }
            m_created.Clear();
            m_rig.Dispose();
        }

        /* --- Helpers --- */

        private ManaComponent Mana => m_rig.Player.GetComponent<ManaComponent>();
        private CombatComponent Combat => m_rig.Player.GetComponent<CombatComponent>();

        private T Track<T>(T obj) where T : Object
        {
            m_created.Add(obj);
            return obj;
        }

        private static T Load<T>(string path) where T : Object
        {
            T asset = Resources.Load<T>(path);
            Assert.That(asset, Is.Not.Null, $"fixture: '{path}' is missing from Resources");
            return asset;
        }

        // A bare bandit: enemy faction, a capsule on the Player's target layers, and nothing that fights back.
        private HealthComponent Bandit(string name, Vector3 offset, int health = 20, int experienceReward = 30)
        {
            AIProgression progression = Track(ScriptableObject.CreateInstance<AIProgression>());
            PlayModeTestKit.Set(progression, "m_maxHealthAmounts", new[] { health });
            PlayModeTestKit.Set(progression, "m_experienceRewardAmounts", new[] { experienceReward });

            GameObject bandit = Track(new GameObject(name));
            bandit.SetActive(false);
            bandit.transform.position = m_rig.Player.transform.position + offset;
            bandit.layer = FirstLayerIn(Combat.TargetLayers);
            bandit.AddComponent<Animator>();
            bandit.AddComponent<NavMeshAgent>();
            bandit.AddComponent<CapsuleCollider>();
            BaseStats stats = bandit.AddComponent<BaseStats>();
            PlayModeTestKit.Set(stats, "m_baseProgression", progression);
            PlayModeTestKit.Set(bandit.AddComponent<FactionComponent>(), "m_faction", Faction.kEnemy);
            HealthComponent healthComponent = bandit.AddComponent<HealthComponent>();
            bandit.SetActive(true);
            return healthComponent;
        }

        private GameObject Guard()
        {
            GameObject guard = Track(new GameObject("Guard"));
            PlayModeTestKit.Set(guard.AddComponent<FactionComponent>(), "m_faction", Faction.kNPC);
            return guard;
        }

        private static int FirstLayerIn(LayerMask mask)
        {
            for (int layer = 0; layer < 32; ++layer)
            {
                if ((mask.value & (1 << layer)) != 0)
                    return layer;
            }
            Assert.Fail("fixture: the Player's CombatComponent has no target layers");
            return 0;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string failure)
        {
            for (float waited = 0f; !condition(); waited += Time.unscaledDeltaTime)
            {
                Assert.That(waited, Is.LessThan(timeout), failure);
                yield return null;
            }
        }

        private static float Sum(IEnumerable<float> values) => values.Sum();

        /* ============================== Weapons ============================== */

        [UnityTest]
        [Description("The Player's unarmed weapon deals real damage.")]
        public IEnumerator UnarmedWeapon_DealsRealDamage()
        {
            yield return null;
            Weapon unarmed = PlayModeTestKit.Get<Weapon>(Combat, "m_unarmedWeapon");
            Assert.That(unarmed, Is.Not.Null, "fixture: the Player has no unarmed weapon");

            Assert.That(Sum(unarmed.GetAdditiveModifiers(Stat.kDamage)), Is.GreaterThan(0f));
        }

        [UnityTest]
        [Description("Equipping a weapon adds its damage to the Player's damage stat.")]
        public IEnumerator EquippingAWeapon_AddsItsDamageToThePlayer()
        {
            yield return null;
            Weapon sword = Load<Weapon>(kIronSword);
            m_rig.Equipment.RemoveItem(EquipmentSlot.kWeapon);
            float bareHanded = m_rig.Stats.GetDamage();

            m_rig.Equipment.AddItem(EquipmentSlot.kWeapon, sword);

            Assert.That(m_rig.Stats.GetDamage(), Is.EqualTo(bareHanded + Sum(sword.GetAdditiveModifiers(Stat.kDamage))).Within(kTolerance));
        }

        /* ============================== The attack loop ============================== */

        [UnityTest]
        [Description("With a melee weapon, the Player keeps hitting the selected target until it dies, then stops: no further hits, no target, and combat ends.")]
        public IEnumerator MeleeAttack_KeepsHittingTheTargetUntilItDies_ThenStops()
        {
            yield return null;
            m_rig.Equipment.AddItem(EquipmentSlot.kWeapon, Load<Weapon>(kIronSword));
            HealthComponent bandit = Bandit("Bandit", new Vector3(0f, 0f, 1f));
            yield return null;
            float damage = m_rig.Stats.GetDamage();
            int hitsToKill = Mathf.CeilToInt(bandit.CurrentHealth / damage);
            Assert.That(hitsToKill, Is.GreaterThan(1), "fixture: the bandit must survive the first hit");
            int hits = 0, combatExits = 0;
            bandit.OnHit += _ => ++hits;
            m_rig.Health.OnCombatExited += () => ++combatExits;

            Combat.SetTarget(bandit.gameObject);
            yield return WaitUntil(() => bandit.IsDead, 15f, "the Player never killed the bandit");
            yield return new WaitForSeconds(1f);

            Assert.That(hits, Is.EqualTo(hitsToKill));
            Assert.That(Combat.Target, Is.Null);
            Assert.That(combatExits, Is.EqualTo(1));
        }

        [UnityTest]
        [Category("ClickModel")]
        [Description("After a kill, the Player automatically targets the nearest enemy within auto-attack range.")]
        public IEnumerator AfterAKill_TargetsTheNearestEnemyInRange()
        {
            yield return null;
            HealthComponent first = Bandit("First", new Vector3(0f, 0f, 1.5f));
            HealthComponent next = Bandit("Next", new Vector3(2.5f, 0f, 0f));
            yield return null;
            Combat.SetTarget(first.gameObject);

            first.TakeDamage(m_rig.Player, 10000f);
            yield return null;
            yield return null;

            Assert.That(Combat.Target, Is.SameAs(next.gameObject));
        }

        /* ============================== Projectiles ============================== */

        private Projectile LaunchArrow(HealthComponent target, float damage)
        {
            Projectile prefab = PlayModeTestKit.Get<Projectile>(Load<Weapon>(kWoodenBow), "m_projectile");
            Assert.That(prefab, Is.Not.Null, "fixture: the Wooden Bow has no projectile");
            Vector3 start = target.transform.position + new Vector3(0f, 1f, -30f);
            Projectile arrow = Object.Instantiate(prefab, start, Quaternion.identity);
            arrow.SetTarget(target.transform, m_rig.Player, damage);
            return arrow;
        }

        private static void Touch(Projectile arrow, Collider other)
            => typeof(Projectile).GetMethod("OnTriggerEnter", kAny).Invoke(arrow, new object[] { other });

        [UnityTest]
        [Description("A projectile reaching its target deals its damage to it and is destroyed.")]
        public IEnumerator Projectile_ReachingItsTarget_DealsItsDamage_AndIsDestroyed()
        {
            yield return null;
            HealthComponent bandit = Bandit("Bandit", new Vector3(0f, 0f, 3f));
            yield return null;
            float before = bandit.CurrentHealth;
            Projectile arrow = LaunchArrow(bandit, 6f);

            Touch(arrow, bandit.GetComponent<Collider>());
            yield return null;

            Assert.That(bandit.CurrentHealth, Is.EqualTo(before - 6f).Within(kTolerance));
            Assert.That(arrow == null, Is.True, "the projectile is still in the world");
        }

        [UnityTest]
        [Description("A projectile touching something other than its target deals no damage and keeps flying.")]
        public IEnumerator Projectile_TouchingSomethingElse_DealsNoDamage_AndKeepsFlying()
        {
            yield return null;
            HealthComponent target = Bandit("Target", new Vector3(0f, 0f, 3f));
            HealthComponent bystander = Bandit("Bystander", new Vector3(3f, 0f, 3f));
            yield return null;
            Projectile arrow = LaunchArrow(target, 6f);

            Touch(arrow, bystander.GetComponent<Collider>());
            yield return null;

            Assert.That(bystander.CurrentHealth, Is.EqualTo(bystander.GetComponent<BaseStats>().GetHealth()).Within(kTolerance));
            Assert.That(target.CurrentHealth, Is.EqualTo(target.GetComponent<BaseStats>().GetHealth()).Within(kTolerance));
            Assert.That(arrow != null, Is.True, "the projectile was destroyed");
        }

        /* ============================== Kill XP ============================== */

        [UnityTest]
        [Description("Killing an enemy gives its experience reward to the killer.")]
        public IEnumerator KillingAnEnemy_GivesItsExperienceToTheKiller()
        {
            yield return null;
            HealthComponent bandit = Bandit("Bandit", new Vector3(0f, 0f, 3f), experienceReward: 30);
            yield return null;
            Assert.That(m_rig.Stats.GetExperienceToLevelUp(), Is.GreaterThan(30f), "fixture: the reward must not level the Player up");
            float before = m_rig.Experience.ExperiencePoints;

            bandit.TakeDamage(m_rig.Player, 10000f);

            Assert.That(m_rig.Experience.ExperiencePoints, Is.EqualTo(before + 30f).Within(kTolerance));
        }

        [UnityTest]
        [Description("When a guard kills an enemy, nobody gains experience, including a Player standing nearby.")]
        public IEnumerator AGuardKillingAnEnemy_GivesNoOneExperience()
        {
            yield return null;
            HealthComponent bandit = Bandit("Bandit", new Vector3(0f, 0f, 3f));
            yield return null;
            float before = m_rig.Experience.ExperiencePoints;

            bandit.TakeDamage(Guard(), 10000f);

            Assert.That(bandit.IsDead, Is.True, "fixture: the guard must kill the bandit");
            Assert.That(m_rig.Experience.ExperiencePoints, Is.EqualTo(before).Within(kTolerance));
        }

        /* ============================== Level-up and mana ============================== */

        [UnityTest]
        [Description("Leveling up refills health and mana to the new maximums.")]
        public IEnumerator LevelingUp_RefillsHealthAndMana()
        {
            yield return null;
            m_rig.Damage(10f);
            Assert.That(Mana.UseMana(10f), Is.True, "fixture: the Player must be able to spend mana");

            m_rig.LevelUpTo(2);

            Assert.That(m_rig.Health.CurrentHealth, Is.EqualTo(m_rig.Stats.GetHealth()).Within(kTolerance));
            Assert.That(Mana.CurrentMana, Is.EqualTo(m_rig.Stats.GetMana()).Within(kTolerance));
        }

        [UnityTest]
        [Description("Spent mana regenerates over time, up to the maximum and no further.")]
        public IEnumerator Mana_RegeneratesUpToTheMaximum()
        {
            yield return null;
            Assert.That(m_rig.Stats.GetManaRegenRate(), Is.GreaterThan(0f), "fixture: the Player must regenerate mana");
            Mana.UseMana(2f);

            yield return new WaitForSeconds(2f);

            Assert.That(Mana.CurrentMana, Is.EqualTo(m_rig.Stats.GetMana()).Within(kTolerance));
        }

        [UnityTest]
        [Description("A Player whose mana was loaded from a save before its first frame still regenerates mana.")]
        public IEnumerator AfterLoadingASave_ManaStillRegenerates()
        {
            yield return null;
            Mana.UseMana(2f);
            SaveManager.Instance.Save();

            m_rig.SpawnPlayer();
            SaveManager.Instance.Load();                    // restored before Start, as when a scene loads
            Assert.That(Mana.CurrentMana, Is.LessThan(m_rig.Stats.GetMana()), "fixture: the save must hold spent mana");
            yield return new WaitForSeconds(2f);

            Assert.That(Mana.CurrentMana, Is.EqualTo(m_rig.Stats.GetMana()).Within(kTolerance));
        }
    }
}
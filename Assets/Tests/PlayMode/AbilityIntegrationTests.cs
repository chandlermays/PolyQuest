/*---------------------------
| File: AbilityIntegrationTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
//---------------------------------
using PolyQuest.Abilities;
using PolyQuest.Combat;
using PolyQuest.Inventories;
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    // Abilities on the real Player: cooldown timing and saving, the shared potion cooldown, and projectile effects.
    public class AbilityIntegrationTests
    {
        private const string kFireball = "Abilities/Abilities/Fireball";
        private const string kScorch = "Abilities/Abilities/Scorch";
        private const string kHealthPotion = "Items/Consumables/Health Potion";
        private const string kManaPotion = "Items/Consumables/Mana Potion";
        private const string kWoodenBow = "Items/Weapons/Bows/Wooden Bow";

        private PlayerRig m_rig;
        private readonly List<Object> m_created = new();

        /*---------------------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for ability integration testing --- |
        ---------------------------------------------------------------------------------------*/
        [SetUp]
        public void SetUp()
        {
            m_rig = new PlayerRig();
        }

        /*-------------------------------------------------------------------
        | --- TearDown: Release test fixtures and game object resources --- |
        -------------------------------------------------------------------*/
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


        /*-----------------
        | --- Helpers --- |
        -----------------*/
        /*-------------------------------------------
        | --- Cooldowns: The Player's Cooldowns --- |
        -------------------------------------------*/
        private Cooldowns Cooldowns => m_rig.Player.GetComponent<Cooldowns>();

        /*--------------------------------------
        | --- Bar: The Player's action bar --- |
        --------------------------------------*/
        private Actions Bar => m_rig.Player.GetComponent<Actions>();

        /*----------------------------------------------------------------------
        | --- Load: Loads an asset from Resources, failing if it's missing --- |
        ----------------------------------------------------------------------*/
        private static T Load<T>(string path) where T : Object
        {
            T asset = Resources.Load<T>(path);
            Assert.That(asset, Is.Not.Null, $"fixture: '{path}' is missing from Resources");
            return asset;
        }

        /*--------------------------------------------------------------------------------------
        | --- WaitUntil: Waits frames until the condition holds, failing after the timeout --- |
        --------------------------------------------------------------------------------------*/
        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string failure)
        {
            for (float waited = 0f; !condition(); waited += Time.unscaledDeltaTime)
            {
                Assert.That(waited, Is.LessThan(timeout), failure);
                yield return null;
            }
        }


        /*-------------------
        | --- Cooldowns --- |
        -------------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------------
        | --- Cooldown_CountsDown_AndEnds: A cooldown counts down from its full length and ends; the remaining share goes from 1 toward 0 --- |
        -------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("A cooldown counts down from its full length and ends; the remaining share goes from 1 toward 0.")]
        public IEnumerator Cooldown_CountsDown_AndEnds()
        {
            yield return null;
            Ability fireball = Load<Ability>(kFireball);

            Cooldowns.StartCooldown(fireball, 0.5f);
            Assert.That(Cooldowns.GetRemainingPercentage(fireball), Is.EqualTo(1f).Within(1e-4f));
            yield return new WaitForSeconds(0.2f);
            Assert.That(Cooldowns.GetRemainingCooldown(fireball), Is.InRange(0.01f, 0.45f));
            Assert.That(Cooldowns.GetRemainingPercentage(fireball), Is.InRange(0.01f, 0.95f));

            yield return WaitUntil(() => Cooldowns.GetRemainingCooldown(fireball) <= 0f, 2f, "the cooldown never ended");
            Assert.That(Cooldowns.GetRemainingPercentage(fireball), Is.EqualTo(0f));
        }

        /*-----------------------------------------------------------------------------------------------------
        | --- DifferentAbilities_HaveIndependentCooldowns: Different abilities have independent cooldowns --- |
        -----------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("Different abilities have independent cooldowns.")]
        public IEnumerator DifferentAbilities_HaveIndependentCooldowns()
        {
            yield return null;
            Ability fireball = Load<Ability>(kFireball);
            Ability scorch = Load<Ability>(kScorch);

            Cooldowns.StartCooldown(fireball, 5f);

            Assert.That(Cooldowns.GetRemainingCooldown(fireball), Is.GreaterThan(0f));
            Assert.That(Cooldowns.GetRemainingCooldown(scorch), Is.EqualTo(0f));
        }

        /*------------------------------------------------------------------------------------------------------------------------------------
        | --- Cooldown_SurvivesASaveAndLoad: A cooldown survives saving and loading: after loading, it resumes with the time it had left --- |
        ------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("A cooldown survives saving and loading: after loading, it resumes with the time it had left.")]
        public IEnumerator Cooldown_SurvivesASaveAndLoad()
        {
            yield return null;
            Ability fireball = Load<Ability>(kFireball);
            Cooldowns.StartCooldown(fireball, 30f);
            SaveManager.Instance.Save();

            m_rig.SpawnPlayer();
            yield return null;
            Assert.That(Cooldowns.GetRemainingCooldown(fireball), Is.EqualTo(0f), "fixture: the fresh Player must have no cooldowns");
            SaveManager.Instance.Load();

            Assert.That(Cooldowns.GetRemainingCooldown(fireball), Is.InRange(25f, 30f));
        }


        /*-----------------
        | --- Potions --- |
        -----------------*/
        /*------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Potions_ShareOneShortCooldown: All potions share one short cooldown: drinking one puts every potion on cooldown, a second potion can't be drunk until it ends, and it ends --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("All potions share one short cooldown: drinking one puts every potion on cooldown, a second potion can't be drunk until it ends, and it ends.")]
        public IEnumerator Potions_ShareOneShortCooldown()
        {
            yield return null;
            Ability healthPotion = Load<Ability>(kHealthPotion);
            Ability manaPotion = Load<Ability>(kManaPotion);
            Bar.AddActionItem(healthPotion, 0, 2);
            Bar.AddActionItem(manaPotion, 1, 2);

            Bar.Use(m_rig.Player, 0);

            Assert.That(Bar.GetQuantity(0), Is.EqualTo(1), "fixture: the health potion must be drunk");
            Assert.That(Cooldowns.GetRemainingCooldown(healthPotion), Is.GreaterThan(0f), "the health potion has no cooldown");
            Assert.That(Cooldowns.GetRemainingCooldown(manaPotion), Is.GreaterThan(0f), "the mana potion isn't on the shared cooldown");
            Bar.Use(m_rig.Player, 1);
            Assert.That(Bar.GetQuantity(1), Is.EqualTo(2), "a second potion was drunk during the cooldown");

            yield return WaitUntil(() => Cooldowns.GetRemainingCooldown(manaPotion) <= 0f, 5f, "the potion cooldown never ended");
            Bar.Use(m_rig.Player, 1);
            Assert.That(Bar.GetQuantity(1), Is.EqualTo(1));
        }


        /*----------------------------
        | --- Projectile Effects --- |
        ----------------------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- ProjectileEffect_LaunchesOneProjectilePerTarget: A projectile effect launches one projectile per target, each aimed at its target, owned by the caster and carrying the effect's damage --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("A projectile effect launches one projectile per target, each aimed at its target, owned by the caster and carrying the effect's damage.")]
        public IEnumerator ProjectileEffect_LaunchesOneProjectilePerTarget()
        {
            yield return null;
            Projectile prefab = PlayModeTestKit.Get<Projectile>(Load<Weapon>(kWoodenBow), "m_projectile");
            SpawnProjectileEffect effect = ScriptableObject.CreateInstance<SpawnProjectileEffect>();
            m_created.Add(effect);
            PlayModeTestKit.Set(effect, "m_projectilePrefab", prefab);
            PlayModeTestKit.Set(effect, "m_damage", 7f);
            Transform[] targets = new Transform[2];
            for (int i = 0; i < targets.Length; ++i)
            {
                GameObject target = new GameObject("Target " + i);
                m_created.Add(target);
                target.AddComponent<CapsuleCollider>();
                target.transform.position = m_rig.Player.transform.position + new Vector3(4f * (i + 1), 0f, 10f);
                targets[i] = target.transform;
            }
            AbilityConfig config = new AbilityConfig(m_rig.Player) { Targets = targets.Select(t => t.gameObject).ToList() };

            effect.StartEffect(config, () => { });

            Projectile[] launched = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            Assert.That(launched.Select(p => PlayModeTestKit.Get<Transform>(p, "m_targetTransform")), Is.EquivalentTo(targets));
            Assert.That(launched.All(p => PlayModeTestKit.Get<GameObject>(p, "m_instigator") == m_rig.Player), Is.True, "a projectile isn't owned by the caster");
            Assert.That(launched.All(p => Mathf.Approximately(PlayModeTestKit.Get<float>(p, "m_damage"), 7f)), Is.True, "a projectile carries the wrong damage");
        }
    }
}
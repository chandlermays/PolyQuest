/*---------------------------
| File: ManaComponentTests.cs
| Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using NUnit.Framework;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Components;
using UnityEngine;

namespace PolyQuest.Tests
{
    // Spending and replenishing mana, max-mana changes and the save data. Regeneration runs in PlayMode.
    public class ManaComponentTests
    {
        private const float kTolerance = 1e-4f;

        private EntityTestKit m_kit;
        private GameObject m_mage;
        private FakeStatModifier m_item;
        private ManaComponent m_mana;

        [SetUp]
        public void SetUp()
        {
            m_kit = new EntityTestKit();
            m_mage = m_kit.Entity("Mage", mana: 50);
            m_item = EntityTestKit.Modifier(m_mage, Stat.kMana, 50f);     // max mana 100
            m_mana = EntityTestKit.AddMana(m_mage);
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        private BaseStats Stats => m_mage.GetComponent<BaseStats>();

        /* ============================== Spending ============================== */

        [Test]
        [Description("UseMana with enough mana spends it and reports success.")]
        public void UseMana_WithEnough_SpendsIt()
        {
            Assert.That(m_mana.UseMana(30f), Is.True);
            Assert.That(m_mana.CurrentMana, Is.EqualTo(70f).Within(kTolerance));
        }

        [Test]
        [Description("UseMana with too little mana fails and spends nothing.")]
        public void UseMana_WithTooLittle_FailsAndSpendsNothing()
        {
            m_mana.UseMana(80f);

            Assert.That(m_mana.UseMana(30f), Is.False);
            Assert.That(m_mana.CurrentMana, Is.EqualTo(20f).Within(kTolerance));
        }

        /* ============================== Replenishing ============================== */

        [Test]
        [Description("ReplenishMana adds mana but never past the maximum.")]
        public void ReplenishMana_StopsAtTheMaximum()
        {
            m_mana.UseMana(30f);

            m_mana.ReplenishMana(10f);
            Assert.That(m_mana.CurrentMana, Is.EqualTo(80f).Within(kTolerance));

            m_mana.ReplenishMana(500f);
            Assert.That(m_mana.CurrentMana, Is.EqualTo(100f).Within(kTolerance));
        }

        /* ============================== Max-mana changes ============================== */

        [Test]
        [Description("When the maximum changes, mana keeps the same percentage: at 20/100, losing a +50 item gives 10/50, and getting it back gives 20/100.")]
        public void MaxManaChange_KeepsTheSamePercentage_BothWays()
        {
            m_mana.UseMana(80f);
            Assert.That(m_mana.CurrentMana, Is.EqualTo(20f).Within(kTolerance), "fixture: the mage must be at 20/100");

            m_item.Additive = 0f;
            Stats.NotifyStatModified();
            Assert.That(Stats.GetMana(), Is.EqualTo(50f).Within(kTolerance), "fixture: the maximum must drop to 50");
            Assert.That(m_mana.CurrentMana, Is.EqualTo(10f).Within(kTolerance), "after losing the item");

            m_item.Additive = 50f;
            Stats.NotifyStatModified();
            Assert.That(m_mana.CurrentMana, Is.EqualTo(20f).Within(kTolerance), "after getting the item back");
        }

        /* ============================== Saving ============================== */

        [Test]
        [Description("Capturing mana and restoring it into a fresh entity restores that mana.")]
        public void CaptureThenRestore_RestoresTheMana()
        {
            m_mana.UseMana(63f);
            GameObject freshMage = m_kit.Entity("FreshMage", mana: 50);
            EntityTestKit.Modifier(freshMage, Stat.kMana, 50f);
            ManaComponent fresh = EntityTestKit.AddMana(freshMage);

            fresh.RestoreState(m_mana.CaptureState());

            Assert.That(fresh.CurrentMana, Is.EqualTo(37f).Within(kTolerance));
        }

        [Test]
        [Description("Mana save data that can't be read is ignored: mana is unchanged.")]
        public void RestoreState_Unreadable_IsIgnored()
        {
            m_mana.UseMana(40f);

            m_mana.RestoreState(new JValue("full"));
            m_mana.RestoreState(new JObject { ["mana"] = 10 });
            m_mana.RestoreState(new JArray(1, 2));

            Assert.That(m_mana.CurrentMana, Is.EqualTo(60f).Within(kTolerance));
        }
    }
}
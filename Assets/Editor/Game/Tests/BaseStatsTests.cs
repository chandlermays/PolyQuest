/*---------------------------
| File: BaseStatsTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Tests
{
    public class BaseStatsTests
    {
        private GameObject m_gameObject;
        private BaseStats m_stats;
        private PlayerProgression m_player;
        private AIProgression m_ai;

        [SetUp]
        public void SetUp()
        {
            m_player = ScriptableObject.CreateInstance<PlayerProgression>();
            SerializedTestUtil.SetIntArray(m_player, "m_maxHealthAmounts", 100, 110, 121, 133, 146);
            SerializedTestUtil.SetIntArray(m_player, "m_defenseAmounts", 5, 6, 7, 8, 9);
            SerializedTestUtil.SetIntArray(m_player, "m_maxExperienceAmounts", 100, 250, 475, 813, 1300);

            m_ai = ScriptableObject.CreateInstance<AIProgression>();
            SerializedTestUtil.SetIntArray(m_ai, "m_maxHealthAmounts", 30, 40);
            SerializedTestUtil.SetIntArray(m_ai, "m_experienceRewardAmounts", 15, 20);

            m_gameObject = new GameObject("BaseStatsTestSubject");
            m_stats = m_gameObject.AddComponent<BaseStats>();
            SerializedTestUtil.SetObject(m_stats, "m_baseProgression", m_player);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_gameObject);
            Object.DestroyImmediate(m_player);
            Object.DestroyImmediate(m_ai);
        }

        private void SetLevel(int level) => SerializedTestUtil.SetInt(m_stats, "m_level", level);
        private void UseModifiers(bool use) => SerializedTestUtil.SetBool(m_stats, "m_useStatModifiers", use);

        private FakeStatModifier AddModifier(Stat stat, float additive, float percentage)
        {
            FakeStatModifier modifier = m_gameObject.AddComponent<FakeStatModifier>();
            modifier.TargetStat = stat;
            modifier.Additive = additive;
            modifier.Percentage = percentage;
            return modifier;
        }

        /* --- Base stats --- */

        [Test]
        public void Level_DefaultsToOne()
        {
            Assert.That(m_stats.Level, Is.EqualTo(1));
        }

        [Test]
        public void GetHealth_ReadsProgressionAtCurrentLevel()
        {
            SetLevel(3);

            Assert.That(m_stats.GetHealth(), Is.EqualTo(121f));
        }

        [Test]
        public void GetStat_BeyondProgressionTable_ReturnsZeroAndWarns()
        {
            SetLevel(6);

            LogAssert.Expect(LogType.Warning, "Level out of range");

            Assert.That(m_stats.GetHealth(), Is.EqualTo(0f));
        }

        /* --- Experience-related stats depend on the progression TYPE --- */

        [Test]
        public void ExperienceToLevelUp_OnPlayerProgression_ReadsXpTable()
        {
            SetLevel(2);

            Assert.That(m_stats.GetExperienceToLevelUp(), Is.EqualTo(250f));
        }

        [Test]
        public void ExperienceReward_OnPlayerProgression_IsZero()
        {
            Assert.That(m_stats.GetExperienceReward(), Is.EqualTo(0f));
        }

        [Test]
        public void ExperienceReward_OnAIProgression_ReadsRewardTable()
        {
            SerializedTestUtil.SetObject(m_stats, "m_baseProgression", m_ai);
            SetLevel(2);

            Assert.That(m_stats.GetExperienceReward(), Is.EqualTo(20f));
        }

        [Test]
        public void ExperienceToLevelUp_OnAIProgression_IsZero()
        {
            SerializedTestUtil.SetObject(m_stats, "m_baseProgression", m_ai);

            Assert.That(m_stats.GetExperienceToLevelUp(), Is.EqualTo(0f));
        }

        [Test]
        public void UnimplementedStat_ReturnsZeroAndWarns()
        {
            LogAssert.Expect(LogType.Warning, new Regex("not implemented in GetBaseStat"));

            Assert.That(m_stats.GetStat((Stat)99), Is.EqualTo(0f));
        }

        /* --- Modifiers --- */

        [Test]
        public void Modifiers_AreIgnored_WhenFlagIsOff()
        {
            AddModifier(Stat.kHealth, additive: 50f, percentage: 100f);

            Assert.That(m_stats.GetHealth(), Is.EqualTo(100f));
        }

        [Test]
        public void Modifiers_Formula_IsAdditiveThenPercentage()
        {
            UseModifiers(true);
            SetLevel(2);
            AddModifier(Stat.kHealth, additive: 20f, percentage: 50f);

            // (110 + 20) * (1 + 50/100) = 195
            Assert.That(m_stats.GetHealth(), Is.EqualTo(195f).Within(1e-4f));
        }

        [Test]
        public void Modifiers_FromMultipleComponents_AreSummed_NotMultiplied()
        {
            UseModifiers(true);
            AddModifier(Stat.kHealth, additive: 10f, percentage: 10f);
            AddModifier(Stat.kHealth, additive: 5f, percentage: 20f);

            // (100 + 10 + 5) * (1 + (10 + 20)/100) = 115 * 1.3 = 149.5
            Assert.That(m_stats.GetHealth(), Is.EqualTo(149.5f).Within(1e-4f));
        }

        [Test]
        public void Modifiers_OnlyAffectTheirOwnStat()
        {
            UseModifiers(true);
            AddModifier(Stat.kDefense, additive: 10f, percentage: 100f);

            Assert.That(m_stats.GetHealth(), Is.EqualTo(100f));
            Assert.That(m_stats.GetDefense(), Is.EqualTo(30f).Within(1e-4f));      // (5 + 10) * 2
        }

        [Test]
        public void Modifiers_NegativeValues_ReduceTheStat()
        {
            UseModifiers(true);
            AddModifier(Stat.kHealth, additive: -50f, percentage: 0f);

            Assert.That(m_stats.GetHealth(), Is.EqualTo(50f).Within(1e-4f));
        }

        /* --- Leveling --- */

        [Test]
        public void LevelUp_IncrementsLevel_AndNotifies()
        {
            int notifications = 0;
            m_stats.OnStatModified += () => ++notifications;

            m_stats.LevelUp();

            Assert.That(m_stats.Level, Is.EqualTo(2));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [TestCase(-5, 1)]
        [TestCase(0, 1)]
        [TestCase(7, 7)]
        [TestCase(99, 99)]
        [TestCase(500, 99)]
        public void SetLevel_ClampsToValidRange_AndNotifies(int requested, int expected)
        {
            int notifications = 0;
            m_stats.OnStatModified += () => ++notifications;

            m_stats.SetLevel(requested);

            Assert.That(m_stats.Level, Is.EqualTo(expected));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        [Description("[Characterizes quirk] SetLevel clamps to 99 but LevelUp does not.")]
        public void LevelUp_AtMaxLevel_IsNotClamped()
        {
            m_stats.SetLevel(99);

            m_stats.LevelUp();

            Assert.That(m_stats.Level, Is.EqualTo(100));
        }

        [Test]
        public void NotifyStatModified_RaisesEvent()
        {
            int notifications = 0;
            m_stats.OnStatModified += () => ++notifications;

            m_stats.NotifyStatModified();

            Assert.That(notifications, Is.EqualTo(1));
        }
    }
}
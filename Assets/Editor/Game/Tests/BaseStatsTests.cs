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
        [Description("A freshly added BaseStats starts at level 1.")]
        public void Level_DefaultsToOne()
        {
            Assert.That(m_stats.Level, Is.EqualTo(1));
        }

        [Test]
        [Description("GetHealth returns the progression table's entry for the current level (index level - 1).")]
        public void GetHealth_ReadsProgressionAtCurrentLevel()
        {
            SetLevel(3);

            Assert.That(m_stats.GetHealth(), Is.EqualTo(121f));
        }

        [Test]
        [Description("A level past the end of the progression table returns 0 and logs 'Level out of range'.")]
        public void GetStat_BeyondProgressionTable_ReturnsZeroAndWarns()
        {
            SetLevel(6);

            LogAssert.Expect(LogType.Warning, "Level out of range");

            Assert.That(m_stats.GetHealth(), Is.EqualTo(0f));
        }

        /* --- Experience-related stats depend on the progression TYPE --- */

        [Test]
        [Description("With a PlayerProgression, GetExperienceToLevelUp reads the XP table at the current level.")]
        public void ExperienceToLevelUp_OnPlayerProgression_ReadsXpTable()
        {
            SetLevel(2);

            Assert.That(m_stats.GetExperienceToLevelUp(), Is.EqualTo(250f));
        }

        [Test]
        [Description("A PlayerProgression has no reward table, so GetExperienceReward returns 0.")]
        public void ExperienceReward_OnPlayerProgression_IsZero()
        {
            Assert.That(m_stats.GetExperienceReward(), Is.EqualTo(0f));
        }

        [Test]
        [Description("With an AIProgression, GetExperienceReward reads the reward table at the current level.")]
        public void ExperienceReward_OnAIProgression_ReadsRewardTable()
        {
            SerializedTestUtil.SetObject(m_stats, "m_baseProgression", m_ai);
            SetLevel(2);

            Assert.That(m_stats.GetExperienceReward(), Is.EqualTo(20f));
        }

        [Test]
        [Description("An AIProgression has no XP table, so GetExperienceToLevelUp returns 0.")]
        public void ExperienceToLevelUp_OnAIProgression_IsZero()
        {
            SerializedTestUtil.SetObject(m_stats, "m_baseProgression", m_ai);

            Assert.That(m_stats.GetExperienceToLevelUp(), Is.EqualTo(0f));
        }

        [Test]
        [Description("A Stat value with no case in GetBaseStat returns 0 and logs a 'not implemented' warning.")]
        public void UnimplementedStat_ReturnsZeroAndWarns()
        {
            LogAssert.Expect(LogType.Warning, new Regex("not implemented in GetBaseStat"));

            Assert.That(m_stats.GetStat((Stat)99), Is.EqualTo(0f));
        }

        /* --- Modifiers --- */

        [Test]
        [Description("With m_useStatModifiers off, modifier components have no effect on the stat.")]
        public void Modifiers_AreIgnored_WhenFlagIsOff()
        {
            AddModifier(Stat.kHealth, additive: 50f, percentage: 100f);

            Assert.That(m_stats.GetHealth(), Is.EqualTo(100f));
        }

        [Test]
        [Description("A modified stat is (base + additive) * (1 + percentage / 100).")]
        public void Modifiers_Formula_IsAdditiveThenPercentage()
        {
            UseModifiers(true);
            SetLevel(2);
            AddModifier(Stat.kHealth, additive: 20f, percentage: 50f);

            // (110 + 20) * (1 + 50/100) = 195
            Assert.That(m_stats.GetHealth(), Is.EqualTo(195f).Within(1e-4f));
        }

        [Test]
        [Description("Additive and percentage values from multiple modifier components are each summed, not compounded.")]
        public void Modifiers_FromMultipleComponents_AreSummed_NotMultiplied()
        {
            UseModifiers(true);
            AddModifier(Stat.kHealth, additive: 10f, percentage: 10f);
            AddModifier(Stat.kHealth, additive: 5f, percentage: 20f);

            // (100 + 10 + 5) * (1 + (10 + 20)/100) = 115 * 1.3 = 149.5
            Assert.That(m_stats.GetHealth(), Is.EqualTo(149.5f).Within(1e-4f));
        }

        [Test]
        [Description("A modifier targeting one stat leaves every other stat unchanged.")]
        public void Modifiers_OnlyAffectTheirOwnStat()
        {
            UseModifiers(true);
            AddModifier(Stat.kDefense, additive: 10f, percentage: 100f);

            Assert.That(m_stats.GetHealth(), Is.EqualTo(100f));
            Assert.That(m_stats.GetDefense(), Is.EqualTo(30f).Within(1e-4f));      // (5 + 10) * 2
        }

        [Test]
        [Description("A negative additive modifier lowers the stat.")]
        public void Modifiers_NegativeValues_ReduceTheStat()
        {
            UseModifiers(true);
            AddModifier(Stat.kHealth, additive: -50f, percentage: 0f);

            Assert.That(m_stats.GetHealth(), Is.EqualTo(50f).Within(1e-4f));
        }

        /* --- Leveling --- */

        [Test]
        [Description("LevelUp increases the level by one and raises OnStatModified once.")]
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
        [Description("SetLevel clamps the requested level to [1, 99] and raises OnStatModified once.")]
        public void SetLevel_ClampsToValidRange_AndNotifies(int requested, int expected)
        {
            int notifications = 0;
            m_stats.OnStatModified += () => ++notifications;

            m_stats.SetLevel(requested);

            Assert.That(m_stats.Level, Is.EqualTo(expected));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        [Description("SetLevel clamps to 99 but LevelUp does not.")]
        public void LevelUp_AtMaxLevel_IsNotClamped()
        {
            m_stats.SetLevel(99);

            m_stats.LevelUp();

            Assert.That(m_stats.Level, Is.EqualTo(100));
        }

        [Test]
        [Description("NotifyStatModified raises OnStatModified once.")]
        public void NotifyStatModified_RaisesEvent()
        {
            int notifications = 0;
            m_stats.OnStatModified += () => ++notifications;

            m_stats.NotifyStatModified();

            Assert.That(notifications, Is.EqualTo(1));
        }
    }
}
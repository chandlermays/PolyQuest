/*---------------------------
| File: ProgressionTests.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Tests
{
    public class ProgressionTests
    {
        private PlayerProgression m_player;
        private AIProgression m_ai;

        [SetUp]
        public void SetUp()
        {
            m_player = ScriptableObject.CreateInstance<PlayerProgression>();
            m_ai = ScriptableObject.CreateInstance<AIProgression>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_player);
            Object.DestroyImmediate(m_ai);
        }

        ///* --- Recalculate: growth curve --- *///

        [Test]
        [Description("Each health row is round(initial * (1 + percentage)^(level - 1)), computed from the exact curve rather than from the previous rounded row.")]
        public void RecalculateHealth_FollowsTheExactCurve()
        {
            SerializedTestUtil.SetInt(m_player, "m_initialHealth", 20);
            SerializedTestUtil.SetFloat(m_player, "m_healthIncreasePercentage", 15f);
            SerializedTestUtil.SetIntArray(m_player, "m_maxHealthAmounts", 0, 0, 0, 0, 0);

            m_player.RecalculateMaxHealthAmounts();

            // 20 * 1.15^(0..4) = 20, 23, 26.45, 30.42, 34.98
            Assert.That(m_player.GetHealth(1), Is.EqualTo(20f));
            Assert.That(m_player.GetHealth(2), Is.EqualTo(23f));
            Assert.That(m_player.GetHealth(3), Is.EqualTo(26f));
            Assert.That(m_player.GetHealth(4), Is.EqualTo(30f));
            Assert.That(m_player.GetHealth(5), Is.EqualTo(35f));
        }

        [Test]
        [Description("Recalculating overwrites every entry, including hand-edited ones, starting from the initial value.")]
        public void Recalculate_OverwritesHandEditedFirstLevel()
        {
            SerializedTestUtil.SetInt(m_player, "m_initialDamage", 8);
            SerializedTestUtil.SetFloat(m_player, "m_damageIncreasePercentage", 0f);
            SerializedTestUtil.SetIntArray(m_player, "m_damageAmounts", 999, 999, 999);

            m_player.RecalculateDamageAmounts();

            Assert.That(m_player.GetDamage(1), Is.EqualTo(8f));
            Assert.That(m_player.GetDamage(2), Is.EqualTo(8f));
            Assert.That(m_player.GetDamage(3), Is.EqualTo(8f));
        }

        [Test]
        [Description("A 0% increase keeps the value flat at every level.")]
        public void Recalculate_ZeroPercent_KeepsValueFlat()
        {
            SerializedTestUtil.SetInt(m_player, "m_initialDefense", 40);
            SerializedTestUtil.SetFloat(m_player, "m_defenseIncreasePercentage", 0f);
            SerializedTestUtil.SetIntArray(m_player, "m_defenseAmounts", 0, 0, 0);

            m_player.RecalculateDefenseAmounts();

            Assert.That(m_player.GetDefense(3), Is.EqualTo(40f));
        }

        [Test]
        [Description("Mana follows the exact curve of its own percentage, independent of the other stats' percentages.")]
        public void RecalculateMana_UsesItsOwnPercentage_OnTheExactCurve()
        {
            SerializedTestUtil.SetInt(m_player, "m_initialMana", 50);
            SerializedTestUtil.SetFloat(m_player, "m_manaIncreasePercentage", 20f);
            SerializedTestUtil.SetIntArray(m_player, "m_manaAmounts", 0, 0, 0, 0, 0);

            m_player.RecalculateMaxManaAmounts();

            // 50 * 1.2^(0..4) = 50, 60, 72, 86.4, 103.68
            Assert.That(m_player.GetMana(1), Is.EqualTo(50f));
            Assert.That(m_player.GetMana(2), Is.EqualTo(60f));
            Assert.That(m_player.GetMana(3), Is.EqualTo(72f));
            Assert.That(m_player.GetMana(4), Is.EqualTo(86f));
            Assert.That(m_player.GetMana(5), Is.EqualTo(104f));
        }

        [Test]
        [Description("A small value still grows once the exact curve rounds up: 1 * 1.1^5 = 1.61 rounds to 2 at level 6.")]
        public void RecalculateManaRegen_SmallValues_GrowOnTheExactCurve()
        {
            SerializedTestUtil.SetInt(m_player, "m_initialManaRegenRate", 1);
            SerializedTestUtil.SetFloat(m_player, "m_manaRegenIncreasePercentage", 10f);
            SerializedTestUtil.SetIntArray(m_player, "m_manaRegenRates", 0, 0, 0, 0, 0, 0);

            m_player.RecalculateManaRegenRates();

            // 1 * 1.1^(0..5) = 1, 1.1, 1.21, 1.33, 1.46, 1.61
            for (int level = 1; level <= 5; ++level)
                Assert.That(m_player.GetManaRegenRate(level), Is.EqualTo(1f), $"level {level}");
            Assert.That(m_player.GetManaRegenRate(6), Is.EqualTo(2f));
        }

        ///* --- PlayerProgression: experience table --- *///

        [Test]
        [Description("The XP table is cumulative: each level adds round(initial * growth^i), using Mathf.RoundToInt's half-to-even rounding.")]
        public void RecalculateExperience_IsCumulative_AndRoundsHalfToEven()
        {
            SerializedTestUtil.SetInt(m_player, "m_initialExperience", 100);
            SerializedTestUtil.SetFloat(m_player, "m_experienceIncreasePercentage", 50f);
            SerializedTestUtil.SetIntArray(m_player, "m_maxExperienceAmounts", 0, 0, 0, 0);

            m_player.RecalculateMaxExperienceAmounts();

            // step(i) = round(initial * 1.5^i); table[i] = table[i-1] + step(i)
            // step(3) = 337.5, which Mathf.RoundToInt rounds to 338 (half-to-even), so 475 + 338 = 813.
            Assert.That(m_player.GetExperience(1), Is.EqualTo(100f));
            Assert.That(m_player.GetExperience(2), Is.EqualTo(250f));
            Assert.That(m_player.GetExperience(3), Is.EqualTo(475f));
            Assert.That(m_player.GetExperience(4), Is.EqualTo(813f));
        }

        ///* --- AIProgression: experience reward --- *///

        [Test]
        [Description("Each XP reward row is round(initial * (1 + percentage)^(level - 1)), computed from the exact curve rather than from the previous rounded row.")]
        public void RecalculateExperienceReward_FollowsTheExactCurve()
        {
            SerializedTestUtil.SetInt(m_ai, "m_initialExperienceReward", 10);
            SerializedTestUtil.SetFloat(m_ai, "m_experienceRewardIncreasePercentage", 20f);
            SerializedTestUtil.SetIntArray(m_ai, "m_experienceRewardAmounts", 0, 0, 0, 0, 0);

            m_ai.RecalculateExperienceRewardAmounts();

            // 10 * 1.2^(0..4) = 10, 12, 14.4, 17.28, 20.736
            Assert.That(m_ai.GetExperienceReward(1), Is.EqualTo(10f));
            Assert.That(m_ai.GetExperienceReward(2), Is.EqualTo(12f));
            Assert.That(m_ai.GetExperienceReward(3), Is.EqualTo(14f));
            Assert.That(m_ai.GetExperienceReward(4), Is.EqualTo(17f));
            Assert.That(m_ai.GetExperienceReward(5), Is.EqualTo(21f));
        }
    }
}
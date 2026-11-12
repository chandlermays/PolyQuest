/*---------------------------
| File: ProgressionTests.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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

        /* --- Fresh instance --- */

        [Test]
        [Description("A new progression asset has exactly one level, seeded from the initial values (0 by default).")]
        public void FreshInstance_HasExactlyOneLevel()
        {
            // OnEnable seeds every table with a single entry equal to its initial value (0 by default).
            Assert.That(m_player.GetHealth(1), Is.EqualTo(0f));

            LogAssert.Expect(LogType.Warning, "Level out of range");
            Assert.That(m_player.GetHealth(2), Is.EqualTo(0f));
        }

        /* --- Recalculate: compounding growth --- */

        [Test]
        [Description("Health grows by its percentage each level, rounding from the previous level's already-rounded value.")]
        public void RecalculateHealth_CompoundsPercentagePerLevel()
        {
            SerializedTestUtil.SetInt(m_player, "m_initialHealth", 100);
            SerializedTestUtil.SetFloat(m_player, "m_healthIncreasePercentage", 10f);
            SerializedTestUtil.SetIntArray(m_player, "m_maxHealthAmounts", 0, 0, 0, 0, 0);

            m_player.RecalculateMaxHealthAmounts();

            // Each level rounds from the already-rounded previous level (not from the exact curve).
            Assert.That(m_player.GetHealth(1), Is.EqualTo(100f));
            Assert.That(m_player.GetHealth(2), Is.EqualTo(110f));
            Assert.That(m_player.GetHealth(3), Is.EqualTo(121f));
            Assert.That(m_player.GetHealth(4), Is.EqualTo(133f));
            Assert.That(m_player.GetHealth(5), Is.EqualTo(146f));
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
        [Description("Mana compounds by the mana percentage, independent of the other stats' percentages.")]
        public void RecalculateMana_UsesItsOwnPercentage()
        {
            SerializedTestUtil.SetInt(m_player, "m_initialMana", 50);
            SerializedTestUtil.SetFloat(m_player, "m_manaIncreasePercentage", 20f);
            SerializedTestUtil.SetIntArray(m_player, "m_manaAmounts", 0, 0, 0);

            m_player.RecalculateMaxManaAmounts();

            Assert.That(m_player.GetMana(1), Is.EqualTo(50f));
            Assert.That(m_player.GetMana(2), Is.EqualTo(60f));
            Assert.That(m_player.GetMana(3), Is.EqualTo(72f));
        }

        [Test]
        [Description("Per-level rounding means a small value never grows: 1 * 1.1 rounds back to 1 every level.")]
        public void RecalculateManaRegen_SmallValues_NeverGrow()
        {
            SerializedTestUtil.SetInt(m_player, "m_initialManaRegenRate", 1);
            SerializedTestUtil.SetFloat(m_player, "m_manaRegenIncreasePercentage", 10f);
            SerializedTestUtil.SetIntArray(m_player, "m_manaRegenRates", 0, 0, 0, 0, 0);

            m_player.RecalculateManaRegenRates();

            for (int level = 1; level <= 5; ++level)
                Assert.That(m_player.GetManaRegenRate(level), Is.EqualTo(1f), $"level {level}");
        }

        /* --- Out of range --- */

        [TestCase(0)]
        [TestCase(-3)]
        [TestCase(4)]
        [Description("A level below 1 or past the table's end returns 0 and logs 'Level out of range'.")]
        public void GetHealth_OutOfRange_ReturnsZeroAndWarns(int level)
        {
            SerializedTestUtil.SetIntArray(m_player, "m_maxHealthAmounts", 10, 20, 30);

            LogAssert.Expect(LogType.Warning, "Level out of range");

            Assert.That(m_player.GetHealth(level), Is.EqualTo(0f));
        }

        /* --- PlayerProgression: experience table --- */

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

        [Test]
        [Description("Asking for XP past the final level returns 0 and logs 'Level out of range'.")]
        public void GetExperience_PastFinalLevel_ReturnsZero()
        {
            SerializedTestUtil.SetIntArray(m_player, "m_maxExperienceAmounts", 100, 250);

            LogAssert.Expect(LogType.Warning, "Level out of range");

            Assert.That(m_player.GetExperience(3), Is.EqualTo(0f));
        }

        [Test]
        [Description("Asking for XP at the final level returns the table's last entry.")]
        public void GetExperience_FinalLevel_ReturnsLastEntry()
        {
            SerializedTestUtil.SetIntArray(m_player, "m_maxExperienceAmounts", 100, 250);

            Assert.That(m_player.GetExperience(2), Is.EqualTo(250f));
        }

        /* --- AIProgression: experience reward --- */

        [Test]
        [Description("The XP reward compounds by its percentage per level, rounding from the previous rounded value.")]
        public void RecalculateExperienceReward_CompoundsAndRounds()
        {
            SerializedTestUtil.SetInt(m_ai, "m_initialExperienceReward", 10);
            SerializedTestUtil.SetFloat(m_ai, "m_experienceRewardIncreasePercentage", 20f);
            SerializedTestUtil.SetIntArray(m_ai, "m_experienceRewardAmounts", 0, 0, 0, 0);

            m_ai.RecalculateExperienceRewardAmounts();

            Assert.That(m_ai.GetExperienceReward(1), Is.EqualTo(10f));
            Assert.That(m_ai.GetExperienceReward(2), Is.EqualTo(12f));
            Assert.That(m_ai.GetExperienceReward(3), Is.EqualTo(14f));     // 14.4
            Assert.That(m_ai.GetExperienceReward(4), Is.EqualTo(17f));     // 16.8
        }

        [Test]
        [Description("An out-of-range level for the XP reward returns 0 and logs 'Level out of range'.")]
        public void GetExperienceReward_OutOfRange_ReturnsZeroAndWarns()
        {
            LogAssert.Expect(LogType.Warning, "Level out of range");

            Assert.That(m_ai.GetExperienceReward(2), Is.EqualTo(0f));
        }
    }
}
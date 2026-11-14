/*---------------------------
| File: DamageCalculatorTests.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
//---------------------------------
using PolyQuest.Combat;

namespace PolyQuest.Tests
{
    // Defense mitigation: a hyperbolic curve, defense / (defense + K), capped at 75%.
    public class DamageCalculatorTests
    {
        private const float kK = 50f;               // the half-mitigation point; CombatBalance will own the real value
        private const float kCap = 0.75f;
        private const float kTolerance = 1e-5f;

        [TestCase(0f, 0f)]
        [TestCase(10f, 10f / 60f)]
        [TestCase(25f, 25f / 75f)]
        [TestCase(50f, 0.5f)]
        [TestCase(100f, 100f / 150f)]
        [Description("Below the cap, mitigation is defense / (defense + K): none at 0 defense, half at defense K.")]
        public void Mitigation_FollowsTheCurve(float defense, float expected)
        {
            Assert.That(DamageCalculator.CalculateMitigation(defense, kK, kCap), Is.EqualTo(expected).Within(kTolerance));
        }

        [TestCase(150f)]
        [TestCase(1000f)]
        [TestCase(1000000f)]
        [Description("Mitigation never exceeds the 75% cap, however high defense goes.")]
        public void Mitigation_IsCappedAt75Percent(float defense)
        {
            Assert.That(DamageCalculator.CalculateMitigation(defense, kK, kCap), Is.EqualTo(kCap).Within(kTolerance));
        }

        [TestCase(100f, 0f, 100f)]
        [TestCase(100f, 50f, 50f)]
        [TestCase(100f, 1000f, 25f)]
        [TestCase(0f, 50f, 0f)]
        [Description("Damage taken is the raw damage reduced by the mitigation; no raw damage means none taken.")]
        public void Damage_IsReducedByTheMitigation(float raw, float defense, float expected)
        {
            Assert.That(DamageCalculator.CalculateDamage(raw, defense, kK, kCap), Is.EqualTo(expected).Within(kTolerance));
        }

        [Test]
        [Description("DefenseNeededForCap is the defense where mitigation first reaches the cap: 3K for a 75% cap.")]
        public void DefenseNeededForCap_IsWhereTheCapIsReached()
        {
            float needed = DamageCalculator.DefenseNeededForCap(kK, kCap);

            Assert.That(needed, Is.EqualTo(3f * kK).Within(kTolerance));
            Assert.That(DamageCalculator.CalculateMitigation(needed, kK, kCap), Is.EqualTo(kCap).Within(kTolerance));
            Assert.That(DamageCalculator.CalculateMitigation(needed - 1f, kK, kCap), Is.LessThan(kCap));
        }
    }
}
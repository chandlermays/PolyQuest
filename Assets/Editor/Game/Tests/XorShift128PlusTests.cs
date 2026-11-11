/*---------------------------
| File: XorShift128PlusTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using NUnit.Framework;
//---------------------------------
using PolyQuest.PCG;

namespace PolyQuest.Tests
{
    public class XorShift128PlusTests
    {
        private const ulong kSeed = 12345UL;

        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            XorShift128Plus a = new(kSeed);
            XorShift128Plus b = new(kSeed);

            for (int i = 0; i < 100; ++i)
                Assert.That(a.RandomUInt64(), Is.EqualTo(b.RandomUInt64()), $"draw {i}");
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            XorShift128Plus a = new(1UL);
            XorShift128Plus b = new(2UL);

            Assert.That(a.RandomUInt64(), Is.Not.EqualTo(b.RandomUInt64()));
        }

        [Test]
        public void Reseeding_RestartsTheSequence()
        {
            XorShift128Plus rng = new(kSeed);
            ulong first = rng.RandomUInt64();
            rng.RandomUInt64();

            rng.Seed(kSeed);

            Assert.That(rng.RandomUInt64(), Is.EqualTo(first));
        }

        /* --- Golden values --- */

        [Test]
        public void RandomUInt64_MatchesGoldenSequence()
        {
            ulong[] expected = { 7969838621625804229UL, 13180479620179066700UL, 5207263364142799798UL, 16177105593269097936UL, 6979496376105069975UL };
            XorShift128Plus rng = new(kSeed);

            for (int i = 0; i < expected.Length; ++i)
                Assert.That(rng.RandomUInt64(), Is.EqualTo(expected[i]), $"draw {i}");
        }

        [Test]
        public void RandomUInt32_IsTheUpper32BitsOfTheFirst64BitDraw()
        {
            XorShift128Plus rng = new(kSeed);

            Assert.That(rng.RandomUInt32(), Is.EqualTo(1855622656u));
        }

        [Test]
        public void RandomDouble_UsesTheTop53Bits()
        {
            XorShift128Plus rng = new(kSeed);

            Assert.That(rng.RandomDouble(), Is.EqualTo(0.4320458173962787).Within(1e-15));
        }

        [Test]
        public void RandomRangeInt_MatchesGoldenSequence()
        {
            int[] expected = { 64, 32, 44, 71, 77, 62, 99, 70 };
            XorShift128Plus rng = new(777UL);

            for (int i = 0; i < expected.Length; ++i)
                Assert.That(rng.RandomRange(0, 100), Is.EqualTo(expected[i]), $"draw {i}");
        }

        [Test]
        public void RandomRangeInt_WithNegativeMin_MatchesGoldenSequence()
        {
            int[] expected = { -1, -3, -1, -4, 2, -3, 4, -5 };
            XorShift128Plus rng = new(777UL);

            for (int i = 0; i < expected.Length; ++i)
                Assert.That(rng.RandomRange(-5, 5), Is.EqualTo(expected[i]), $"draw {i}");
        }

        /* --- Ranges and bounds --- */

        [Test]
        public void RandomRangeInt_StaysInHalfOpenRange_AndCoversEveryValue()
        {
            XorShift128Plus rng = new(99UL);
            HashSet<int> seen = new();

            for (int i = 0; i < 2000; ++i)
            {
                int value = rng.RandomRange(0, 5);
                Assert.That(value, Is.InRange(0, 4));
                seen.Add(value);
            }

            Assert.That(seen.Count, Is.EqualTo(5));
        }

        [TestCase(5, 5)]
        [TestCase(5, 3)]
        public void RandomRangeInt_EmptyOrInvertedRange_ReturnsMin(int min, int max)
        {
            XorShift128Plus rng = new(kSeed);

            Assert.That(rng.RandomRange(min, max), Is.EqualTo(min));
        }

        [Test]
        public void RandomRangeFloat_StaysInRange_AndEmptyRangeReturnsMin()
        {
            XorShift128Plus rng = new(kSeed);

            for (int i = 0; i < 500; ++i)
                Assert.That(rng.RandomRange(2f, 4f), Is.InRange(2f, 4f));

            Assert.That(rng.RandomRange(3f, 3f), Is.EqualTo(3f));
            Assert.That(rng.RandomRange(3f, 1f), Is.EqualTo(3f));
        }

        [Test]
        public void RandomRangeDouble_StaysInRange_AndEmptyRangeReturnsMin()
        {
            XorShift128Plus rng = new(kSeed);

            for (int i = 0; i < 500; ++i)
                Assert.That(rng.RandomRange(-1.0, 1.0), Is.InRange(-1.0, 1.0));

            Assert.That(rng.RandomRange(7.0, 7.0), Is.EqualTo(7.0));
        }

        [Test]
        public void RandomDoubleAndFloat_StayInUnitRange()
        {
            XorShift128Plus rng = new(kSeed);

            for (int i = 0; i < 1000; ++i)
            {
                Assert.That(rng.RandomDouble(), Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
                Assert.That(rng.RandomFloat(), Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void RandomBool_ProducesBothValues()
        {
            XorShift128Plus rng = new(kSeed);
            bool sawTrue = false, sawFalse = false;

            for (int i = 0; i < 200; ++i)
            {
                if (rng.RandomBool()) sawTrue = true; else sawFalse = true;
            }

            Assert.That(sawTrue && sawFalse, Is.True);
        }
    }
}
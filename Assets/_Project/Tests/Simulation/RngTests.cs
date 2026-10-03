#nullable enable
using System;
using AwesomeAssertions;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class RngTests
    {
        private const int Seed = 1337;
        private const int ManyDraws = 10000;

        [Test]
        public void NextUInt64_SameSeed_ProducesSameSequence()
        {
            var first = new Rng(Seed);
            var second = new Rng(Seed);

            ulong[] firstDraws = Draw(first, 64);
            ulong[] secondDraws = Draw(second, 64);

            firstDraws.Should().Equal(secondDraws);
        }

        [Test]
        public void NextUInt64_DifferentSeeds_ProduceDifferentSequences()
        {
            var first = new Rng(Seed);
            var second = new Rng(Seed + 1);

            ulong[] firstDraws = Draw(first, 64);
            ulong[] secondDraws = Draw(second, 64);

            firstDraws.Should().NotEqual(secondDraws);
        }

        [Test]
        public void NextInt_ManyDraws_CoverExactlyTheRequestedRange()
        {
            var rng = new Rng(Seed);
            int min = int.MaxValue;
            int max = int.MinValue;

            for (int i = 0; i < ManyDraws; i++)
            {
                int value = rng.NextInt(-3, 5);
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }

            min.Should().Be(-3);
            max.Should().Be(4);
        }

        [Test]
        public void NextInt_SingleValueRange_ReturnsThatValue()
        {
            var rng = new Rng(Seed);

            int value = rng.NextInt(7, 8);

            value.Should().Be(7);
        }

        [Test]
        public void NextInt_EmptyRange_Throws()
        {
            var rng = new Rng(Seed);

            Action act = () => rng.NextInt(5, 5);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void NextFloat_ManyDraws_StayInUnitInterval()
        {
            var rng = new Rng(Seed);
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < ManyDraws; i++)
            {
                float value = rng.NextFloat();
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }

            min.Should().BeGreaterThanOrEqualTo(0f);
            max.Should().BeLessThan(1f);
        }

        [Test]
        public void NextFloat_WithBounds_StaysWithinThem()
        {
            var rng = new Rng(Seed);
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < ManyDraws; i++)
            {
                float value = rng.NextFloat(2f, 4f);
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }

            min.Should().BeGreaterThanOrEqualTo(2f);
            max.Should().BeLessThanOrEqualTo(4f);
        }

        [Test]
        public void NextBool_ProbabilityZero_IsNeverTrue()
        {
            var rng = new Rng(Seed);
            int trueCount = 0;

            for (int i = 0; i < ManyDraws; i++)
            {
                if (rng.NextBool(0f))
                {
                    trueCount++;
                }
            }

            trueCount.Should().Be(0);
        }

        [Test]
        public void NextBool_ProbabilityOne_IsAlwaysTrue()
        {
            var rng = new Rng(Seed);
            int trueCount = 0;

            for (int i = 0; i < ManyDraws; i++)
            {
                if (rng.NextBool(1f))
                {
                    trueCount++;
                }
            }

            trueCount.Should().Be(ManyDraws);
        }

        [Test]
        public void NextBool_ProbabilityHalf_IsTrueAboutHalfTheTime()
        {
            var rng = new Rng(Seed);
            int trueCount = 0;

            for (int i = 0; i < ManyDraws; i++)
            {
                if (rng.NextBool(0.5f))
                {
                    trueCount++;
                }
            }

            trueCount.Should().BeInRange(ManyDraws * 45 / 100, ManyDraws * 55 / 100);
        }

        [Test]
        public void NextBool_ProbabilityAboveOne_Throws()
        {
            var rng = new Rng(Seed);

            Action act = () => rng.NextBool(1.5f);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void State_RestoredIntoNewRng_ContinuesTheSameSequence()
        {
            var original = new Rng(Seed);
            Draw(original, 10);
            ulong state = original.State;
            var restored = new Rng(Seed, state);

            ulong[] originalContinuation = Draw(original, 5);
            ulong[] restoredContinuation = Draw(restored, 5);

            restoredContinuation.Should().Equal(originalContinuation);
        }

        private static ulong[] Draw(Rng rng, int count)
        {
            var values = new ulong[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = rng.NextUInt64();
            }

            return values;
        }
    }
}

#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Stats;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Stats
{
    public sealed class BaseStatsPreviewTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void WithAdded_TwoPoints_ReturnsACopyAndLeavesTheOriginalAlone()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);
            stats.GrantPoints(2);

            BaseStats copy = stats.WithAdded(StatIds.Strength, 2);

            copy.Get(StatIds.Strength).Should().Be(2);
            copy.UnspentPoints.Should().Be(2);
            stats.Get(StatIds.Strength).Should().Be(0);
            stats.UnspentPoints.Should().Be(2);
        }

        [Test]
        public void WithAdded_ChainedOverTwoStats_AddsBoth()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);

            BaseStats copy = stats.WithAdded(StatIds.Strength, 1).WithAdded(StatIds.Agility, 3);

            copy.Get(StatIds.Strength).Should().Be(1);
            copy.Get(StatIds.Agility).Should().Be(3);
            copy.Get(StatIds.Constitution).Should().Be(0);
        }

        [Test]
        public void WithAdded_NegativePoints_Throws()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);

            Action act = () => stats.WithAdded(StatIds.Strength, -1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void WithAdded_UnknownStat_Throws()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);

            Action act = () => stats.WithAdded(new StatId("stat.nope"), 1);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void From_PreviewWithSixConstitution_DoublesTheHpMultiplier()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);

            DerivedStats preview = DerivedStats.From(stats.WithAdded(StatIds.Constitution, 6), TestContent.Tuning);

            preview.HpMultiplier.Should().BeApproximately(2f, Tolerance);
            DerivedStats.From(stats, TestContent.Tuning).HpMultiplier.Should().BeApproximately(1f, Tolerance);
        }
    }
}

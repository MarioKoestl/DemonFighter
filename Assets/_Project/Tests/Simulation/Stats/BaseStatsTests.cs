#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Stats;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Stats
{
    public sealed class BaseStatsTests
    {
        [Test]
        public void Get_FreshBlock_IsZeroForEveryStat()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);

            int strength = stats.Get(StatIds.Strength);

            strength.Should().Be(0);
            stats.Get(StatIds.Constitution).Should().Be(0);
            stats.Get(StatIds.Agility).Should().Be(0);
            stats.UnspentPoints.Should().Be(0);
        }

        [Test]
        public void Get_UnknownStat_Throws()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);

            Action act = () => stats.Get(new StatId("stat.will"));

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void TrySpendPoint_WithoutPoints_IsFalseAndChangesNothing()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);

            bool spent = stats.TrySpendPoint(StatIds.Strength);

            spent.Should().BeFalse();
            stats.Get(StatIds.Strength).Should().Be(0);
        }

        [Test]
        public void TrySpendPoint_AfterAGrant_MovesOnePoint()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);
            stats.GrantPoints(3);

            bool spent = stats.TrySpendPoint(StatIds.Agility);

            spent.Should().BeTrue();
            stats.Get(StatIds.Agility).Should().Be(1);
            stats.UnspentPoints.Should().Be(2);
        }

        [Test]
        public void Set_StartingValue_IsReadBack()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);

            stats.Set(StatIds.Constitution, 20);

            stats.Get(StatIds.Constitution).Should().Be(20);
        }
    }
}

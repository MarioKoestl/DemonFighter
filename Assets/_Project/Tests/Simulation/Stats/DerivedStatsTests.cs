#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Stats;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Stats
{
    public sealed class DerivedStatsTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void From_NoPoints_IsNeutral()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);

            DerivedStats derived = DerivedStats.From(stats, TestContent.Tuning);

            derived.DamageMultiplier.Should().BeApproximately(1f, Tolerance);
            derived.MoveSpeedMultiplier.Should().BeApproximately(1f, Tolerance);
            derived.AttackSpeedMultiplier.Should().BeApproximately(1f, Tolerance);
            derived.HpMultiplier.Should().BeApproximately(1f, Tolerance);
            derived.MaxStamina.Should().BeApproximately(100f, Tolerance);
            derived.RegenPerSecond.Should().BeApproximately(0.5f, Tolerance);
            derived.BleedDurationFactor.Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void From_TwoStrength_RaisesDamageByTenPercent()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);
            stats.Set(StatIds.Strength, 2);

            DerivedStats derived = DerivedStats.From(stats, TestContent.Tuning);

            derived.DamageMultiplier.Should().BeApproximately(1.1f, Tolerance);
        }

        [Test]
        public void From_ThreeConstitution_ScalesHpAndRegeneration()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);
            stats.Set(StatIds.Constitution, 3);

            DerivedStats derived = DerivedStats.From(stats, TestContent.Tuning);

            derived.HpMultiplier.Should().BeApproximately(1.5f, Tolerance);
            derived.RegenPerSecond.Should().BeApproximately(1.25f, Tolerance);
            derived.BleedDurationFactor.Should().BeApproximately(0.85f, Tolerance);
        }

        [Test]
        public void From_FourAgility_RaisesSpeedAndStamina()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);
            stats.Set(StatIds.Agility, 4);

            DerivedStats derived = DerivedStats.From(stats, TestContent.Tuning);

            derived.MoveSpeedMultiplier.Should().BeApproximately(1.12f, Tolerance);
            derived.AttackSpeedMultiplier.Should().BeApproximately(1.12f, Tolerance);
            derived.MaxStamina.Should().BeApproximately(140f, Tolerance);
        }

        [Test]
        public void From_HugeConstitution_FloorsTheBleedFactor()
        {
            var stats = new BaseStats(TestContent.Tuning.Stats);
            stats.Set(StatIds.Constitution, 30);

            DerivedStats derived = DerivedStats.From(stats, TestContent.Tuning);

            derived.BleedDurationFactor.Should().BeApproximately(0.2f, Tolerance);
        }
    }
}

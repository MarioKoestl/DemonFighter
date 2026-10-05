#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation.Combat;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class GoreMathTests
    {
        private const float Tolerance = 0.001f;
        private const float StartPerMeter = 0.7f;
        private const float MaxPerMeter = 1.6f;
        private const float MetersPerBiomass = 0.015f;

        [Test]
        public void PoolDiameter_AtTheStart_IsTheStartFractionOfTheBody()
        {
            float diameter = GoreMath.PoolDiameter(2f, 40f, StartPerMeter, MaxPerMeter, MetersPerBiomass, 0f);

            diameter.Should().BeApproximately(1.4f, Tolerance);
        }

        [Test]
        public void PoolDiameter_FullyGrown_AddsTheBiomassBonus()
        {
            float diameter = GoreMath.PoolDiameter(2f, 40f, StartPerMeter, MaxPerMeter, MetersPerBiomass, 1f);

            diameter.Should().BeApproximately(3.2f + 0.6f, Tolerance);
        }

        [Test]
        public void PoolDiameter_GrowsFastFirstAndNeverShrinks()
        {
            float quarter = GoreMath.PoolDiameter(1f, 10f, StartPerMeter, MaxPerMeter, MetersPerBiomass, 0.25f);
            float half = GoreMath.PoolDiameter(1f, 10f, StartPerMeter, MaxPerMeter, MetersPerBiomass, 0.5f);
            float full = GoreMath.PoolDiameter(1f, 10f, StartPerMeter, MaxPerMeter, MetersPerBiomass, 1f);
            float beyond = GoreMath.PoolDiameter(1f, 10f, StartPerMeter, MaxPerMeter, MetersPerBiomass, 2f);

            quarter.Should().BeLessThan(half);
            half.Should().BeLessThan(full);
            (quarter - 0.7f).Should().BeGreaterThan((half - quarter) * 0.9f);
            beyond.Should().BeApproximately(full, Tolerance);
        }

        [Test]
        public void PoolDiameter_NegativeBiomass_CountsAsNone()
        {
            float diameter = GoreMath.PoolDiameter(1f, -50f, StartPerMeter, MaxPerMeter, MetersPerBiomass, 1f);

            diameter.Should().BeApproximately(1.6f, Tolerance);
        }

        [Test]
        public void BurstCount_ScalesWithSizeAndNeverDropsBelowOne()
        {
            GoreMath.BurstCount(2f, 6f).Should().Be(12);
            GoreMath.BurstCount(0.01f, 6f).Should().Be(1);
        }

        [Test]
        public void ShrinkFactor_FullUntilTheShrinkStartsThenDownToNothing()
        {
            GoreMath.ShrinkFactor(10f, 25f, 4f).Should().Be(1f);
            GoreMath.ShrinkFactor(21f, 25f, 4f).Should().Be(1f);
            GoreMath.ShrinkFactor(23f, 25f, 4f).Should().BeApproximately(0.5f, Tolerance);
            GoreMath.ShrinkFactor(25f, 25f, 4f).Should().Be(0f);
            GoreMath.ShrinkFactor(30f, 25f, 4f).Should().Be(0f);
        }
    }
}

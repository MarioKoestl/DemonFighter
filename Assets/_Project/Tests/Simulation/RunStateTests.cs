#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class RunStateTests
    {
        private const float TimeTolerance = 0.0001f;

        [Test]
        public void Tick_OneHundredTimes_AdvancesTheClockByFiveSeconds()
        {
            RunState state = new RunStateBuilder().WithSeed(2026).Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());

            for (int i = 0; i < 100; i++)
            {
                ticker.Tick();
            }

            state.Tick.Should().Be(100);
            state.Time.Should().BeApproximately(5f, TimeTolerance);
            state.Seed.Should().Be(2026);
        }

        [Test]
        public void Constructor_Fresh_StartsAtTickZero()
        {
            RunState state = new RunStateBuilder().Build();

            long tick = state.Tick;

            tick.Should().Be(0);
            state.Time.Should().BeApproximately(0f, TimeTolerance);
        }

        [Test]
        public void Constructor_SameSeed_StartsIdenticalRandomSequences()
        {
            RunState first = new RunStateBuilder().WithSeed(7).Build();
            RunState second = new RunStateBuilder().WithSeed(7).Build();

            ulong firstDraw = first.Rng.NextUInt64();
            ulong secondDraw = second.Rng.NextUInt64();

            firstDraw.Should().Be(secondDraw);
        }

        [Test]
        public void Constructor_WithTicksPerSecond_UsesItForTheClock()
        {
            RunState state = new RunStateBuilder().WithTicksPerSecond(10).Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());

            ticker.Tick();

            state.Time.Should().BeApproximately(0.1f, TimeTolerance);
        }

        [Test]
        public void DemonIds_IssuedFromTheRun_AreUniqueAndIncreasing()
        {
            RunState state = new RunStateBuilder().Build();

            var first = new DemonId(state.DemonIds.Next());
            var second = new DemonId(state.DemonIds.Next());

            first.Should().NotBe(second);
            second.Value.Should().BeGreaterThan(first.Value);
        }
    }
}

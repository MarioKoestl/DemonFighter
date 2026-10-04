#nullable enable
using System;
using AwesomeAssertions;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class SimulationConfigTests
    {
        [Test]
        public void Default_Always_UsesTwentyTicksPerSecond()
        {
            SimulationConfig config = SimulationConfig.Default;

            int ticksPerSecond = config.TicksPerSecond;

            ticksPerSecond.Should().Be(20);
        }

        [Test]
        public void TickSeconds_TwentyTicksPerSecond_IsFiftyMilliseconds()
        {
            var config = new SimulationConfig(20);

            float tickSeconds = config.TickSeconds;

            tickSeconds.Should().BeApproximately(0.05f, 0.000001f);
        }

        [Test]
        public void Constructor_ZeroTicksPerSecond_Throws()
        {
            Action act = () => _ = new SimulationConfig(0);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}

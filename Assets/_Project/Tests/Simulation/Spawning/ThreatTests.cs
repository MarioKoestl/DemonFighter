#nullable enable
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Spawning
{
    public sealed class ThreatTests
    {
        private const float Tolerance = 0.01f;

        // Thirty levels per minute is one level every two seconds, so the tests stay short; the cap is two.
        private static readonly BiomeSpec Biome = BiomeSpec.AshCavern with { ThreatPerMinute = 30f, ThreatMaxLevel = 2 };

        [Test]
        public void Threat_WithoutAWorld_IsZero()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());

            Tick(ticker, 100);

            state.Threat.Should().Be(0f);
            state.ThreatLevel.Should().Be(0);
        }

        [Test]
        public void Threat_RisesWithTimeAndCapsAtTheMax()
        {
            RunState state = WorldRun(Biome);
            var ticker = new SimulationTicker(state, new SimulationEvents());

            Tick(ticker, 41);
            float afterTwoSeconds = state.Threat;
            int levelAfterTwoSeconds = state.ThreatLevel;
            Tick(ticker, 40);
            int levelAfterFourSeconds = state.ThreatLevel;
            Tick(ticker, 200);

            afterTwoSeconds.Should().BeApproximately(1.025f, Tolerance);
            levelAfterTwoSeconds.Should().Be(1);
            levelAfterFourSeconds.Should().Be(2);
            state.Threat.Should().Be(2f);
            state.ThreatLevel.Should().Be(2);
        }

        [Test]
        public void Tick_WhenAWholeLevelIsCrossed_AnnouncesItOnce()
        {
            RunState state = WorldRun(Biome);
            var events = new SimulationEvents();
            var ticker = new SimulationTicker(state, events);
            var announced = new List<ThreatLevelChanged>();
            events.Subscribe<ThreatLevelChanged>(announced.Add);

            Tick(ticker, 200);

            announced.Should().HaveCount(2);
            announced[0].Level.Should().Be(1);
            announced[1].Level.Should().Be(2);
        }

        [Test]
        public void AshCavern_RisesHalfALevelPerMinuteUpToTen()
        {
            BiomeSpec.AshCavern.ThreatPerMinute.Should().Be(0.5f);
            BiomeSpec.AshCavern.ThreatMaxLevel.Should().Be(10);
        }

        private static RunState WorldRun(BiomeSpec biome)
        {
            RunState state = new RunStateBuilder().Build();
            state.AttachWorld(new CavernWorldGenerator().Generate(state.Seed, biome));
            return state;
        }

        private static void Tick(SimulationTicker ticker, int times)
        {
            for (int i = 0; i < times; i++)
            {
                ticker.Tick();
            }
        }
    }
}

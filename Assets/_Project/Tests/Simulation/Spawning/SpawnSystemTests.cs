#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Spawning
{
    public sealed class SpawnSystemTests
    {
        private static readonly BiomeSpec Biome = BiomeSpec.AshCavern;

        [Test]
        public void Tick_AfterTheRespawnTimeWithTooFewBlobs_SpawnsOneBlobWithABrainAwayFromThePlayer()
        {
            RunState state = WorldRun(Biome);
            var events = new SimulationEvents();
            var ticker = new SimulationTicker(state, events);
            var spawned = new List<DemonSpawned>();
            events.Subscribe<DemonSpawned>(spawned.Add);
            Demon player = state.SpawnDemon(ControllerKind.Player, Biome.BlobDemon, state.World!.PlayerSpawn, 0f);

            Tick(ticker, state.Config.TicksFor(Biome.RespawnSeconds) + 1);

            state.Demons.Should().HaveCount(2);
            Demon blob = state.Demons[1];
            blob.Controller.Should().Be(ControllerKind.Ai);
            blob.Spec.Id.Should().Be(Biome.BlobDemon.Id);
            ticker.Ai.Brains.Should().ContainSingle().Which.Demon.Should().BeSameAs(blob);
            spawned.Should().ContainSingle().Which.Demon.Should().Be(blob.Id);
            PlanarDistance(blob.Position, player.Position).Should().BeInRange(Biome.RespawnMinDistance, Biome.RespawnMaxDistance);
            state.World.Bounds.Contains(blob.Position).Should().BeTrue();
        }

        [Test]
        public void Tick_WithTheFullPopulation_SpawnsNothing()
        {
            RunState state = WorldRun(Biome);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            SpawnPopulation(state, ticker);

            Tick(ticker, 2 * state.Config.TicksFor(Biome.RespawnSeconds) + 2);

            state.Demons.Should().HaveCount(Biome.InitialBlobs + 1);
        }

        [Test]
        public void Tick_AfterABlobDies_RefillsThePopulationOnce()
        {
            RunState state = WorldRun(Biome);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            SpawnPopulation(state, ticker);
            Demon victim = state.Demons[1];
            ticker.Damage.ApplyDamage(victim, victim.Body.Core, 100000f, DamageType.Pierce, DemonId.None);

            Tick(ticker, 2 * state.Config.TicksFor(Biome.RespawnSeconds) + 2);

            state.Demons.Should().HaveCount(Biome.InitialBlobs + 2);
            CountLivingAi(state).Should().Be(Biome.InitialBlobs);
        }

        [Test]
        public void Tick_WithRespawnDisabled_SpawnsNothing()
        {
            RunState state = WorldRun(Biome with { RespawnSeconds = 0f });
            var ticker = new SimulationTicker(state, new SimulationEvents());
            state.SpawnDemon(ControllerKind.Player, Biome.BlobDemon, state.World!.PlayerSpawn, 0f);

            Tick(ticker, 400);

            state.Demons.Should().HaveCount(1);
        }

        private static RunState WorldRun(BiomeSpec biome)
        {
            RunState state = new RunStateBuilder().Build();
            state.AttachWorld(new CavernWorldGenerator().Generate(state.Seed, biome));
            return state;
        }

        // The player and one blob per remaining spawn point, as the run controller does it.
        private static void SpawnPopulation(RunState state, SimulationTicker ticker)
        {
            WorldLayout world = state.World!;
            state.SpawnDemon(ControllerKind.Player, world.Biome.BlobDemon, world.PlayerSpawn, 0f);
            for (int i = 1; i < world.SpawnPoints.Count; i++)
            {
                Demon blob = state.SpawnDemon(ControllerKind.Ai, world.Biome.BlobDemon, world.SpawnPoints[i], 0f);
                ticker.Ai.AddBrain(blob, world.Biome.BlobArchetype, world.Bounds, null);
            }
        }

        private static void Tick(SimulationTicker ticker, int times)
        {
            for (int i = 0; i < times; i++)
            {
                ticker.Tick();
            }
        }

        private static int CountLivingAi(RunState state)
        {
            int count = 0;
            for (int i = 0; i < state.Demons.Count; i++)
            {
                if (state.Demons[i].Controller == ControllerKind.Ai && state.Demons[i].IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            return MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
        }
    }
}

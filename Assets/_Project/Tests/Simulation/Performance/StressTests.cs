#nullable enable
using System;
using System.Diagnostics;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Performance
{
    /// <summary>The stress target of M4 (ARCHITECTURE, "Performance notes"): 50 demons and 200 food items under 2 ms per tick.</summary>
    public sealed class StressTests
    {
        private const int Demons = 50;
        private const int FoodItems = 200;
        private const int WarmupTicks = 20;
        private const int MeasuredTicks = 200;
        private const double MaxMillisecondsPerTick = 2.0;

        [Test]
        public void Tick_WithFiftyDemonsAndTwoHundredFoodItems_StaysUnderTwoMilliseconds()
        {
            RunState state = new RunStateBuilder().Build();
            BiomeSpec biome = BiomeSpec.AshCavern with { RespawnSeconds = 0f };
            state.AttachWorld(new CavernWorldGenerator().Generate(state.Seed, biome));
            WorldLayout world = state.World!;
            var ticker = new SimulationTicker(state, new SimulationEvents());
            state.SpawnDemon(ControllerKind.Player, biome.BlobDemon, world.PlayerSpawn, 0f);
            for (int i = 1; i < Demons; i++)
            {
                Vector3 position = world.Bounds.Clamp(Ring(i, 8f + i * 1.5f));
                Demon demon = state.SpawnDemon(ControllerKind.Ai, biome.BlobDemon, position, 0f);
                ticker.Ai.AddBrain(demon, biome.BlobArchetype, world.Bounds, null);
            }

            for (int i = 0; i < FoodItems; i++)
            {
                state.SpawnFood(FoodKind.Corpse, world.Bounds.Clamp(Ring(i * 3, 5f + i * 0.4f)), 20f, DemonId.None, 0);
            }

            for (int i = 0; i < WarmupTicks; i++)
            {
                ticker.Tick();
            }

            var watch = Stopwatch.StartNew();
            for (int i = 0; i < MeasuredTicks; i++)
            {
                ticker.Tick();
            }

            watch.Stop();
            double perTick = watch.Elapsed.TotalMilliseconds / MeasuredTicks;
            TestContext.WriteLine("Average tick with " + Demons + " demons and " + FoodItems + " food items: " + perTick.ToString("0.000") + " ms");

            state.Demons.Count.Should().Be(Demons);
            perTick.Should().BeLessThan(MaxMillisecondsPerTick);
        }

        private static Vector3 Ring(int index, float radius)
        {
            float angle = index * 0.7f;
            return new Vector3(MathF.Sin(angle) * radius, 0f, MathF.Cos(angle) * radius);
        }
    }
}

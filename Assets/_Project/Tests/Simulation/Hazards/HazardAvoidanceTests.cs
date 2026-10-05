#nullable enable
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Hazards;
using DemonFighter.Simulation.Playtest;
using DemonFighter.Simulation.Tests.Ai;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Hazards
{
    public sealed class HazardAvoidanceTests
    {
        private const int Seconds = 30;

        [Test]
        public void Hunter_PreyBehindLava_WalksAroundTheLavaAndReachesIt()
        {
            RunState state = WorldRun(out SimulationTicker ticker);
            Demon hunter = new DemonBuilder().At(-14f, 0f).SpawnInto(state);
            Demon prey = new DemonBuilder().At(14f, 0f).SpawnInto(state);
            ticker.Ai.AddBrain(hunter, TestArchetypes.Hunter, state.World!.Bounds, null);
            ticker.Ai.AddBrain(prey, TestArchetypes.Rester, state.World.Bounds, null);
            float start = Vector3.Distance(hunter.Position, prey.Position);
            bool enteredLava = false;

            for (int i = 0; i < state.Config.TicksFor(Seconds) && prey.IsAlive; i++)
            {
                ticker.Tick();
                enteredLava |= state.Hazards.KindAt(hunter.Position) == HazardKind.Lava;
            }

            enteredLava.Should().BeFalse("AI demons do not walk into lava");
            hunter.Body.Core.Hp.Should().Be(hunter.Body.Core.MaxHp);
            Vector3.Distance(hunter.Position, prey.Position).Should().BeLessThan(start * 0.5f, "it went around and got close");
        }

        [Test]
        public void Wanderers_NearLava_NeverStepIntoIt()
        {
            RunState state = WorldRun(out SimulationTicker ticker);
            var wanderers = new Demon[6];
            for (int i = 0; i < wanderers.Length; i++)
            {
                float angle = i * System.MathF.PI * 2f / wanderers.Length;
                wanderers[i] = new DemonBuilder().At(System.MathF.Sin(angle) * 11f, System.MathF.Cos(angle) * 11f).SpawnInto(state);
                ticker.Ai.AddBrain(wanderers[i], TestArchetypes.Wanderer, state.World!.Bounds, null);
            }

            bool enteredLava = false;
            for (int i = 0; i < state.Config.TicksFor(Seconds); i++)
            {
                ticker.Tick();
                foreach (Demon wanderer in wanderers)
                {
                    enteredLava |= state.Hazards.KindAt(wanderer.Position) == HazardKind.Lava;
                }
            }

            enteredLava.Should().BeFalse();
        }

        [Test]
        public void Eater_FoodInLava_LeavesIt()
        {
            RunState state = WorldRun(out SimulationTicker ticker);
            Demon eater = new DemonBuilder().At(-12f, 0f).SpawnInto(state);
            state.SpawnFood(FoodKind.Corpse, Vector3.Zero, 30f, DemonId.None, 0);
            ticker.Ai.AddBrain(eater, TestArchetypes.Eater, state.World!.Bounds, null);
            bool enteredLava = false;

            for (int i = 0; i < state.Config.TicksFor(10f); i++)
            {
                ticker.Tick();
                enteredLava |= state.Hazards.KindAt(eater.Position) == HazardKind.Lava;
            }

            enteredLava.Should().BeFalse();
            eater.BiomassEaten.Should().Be(0f);
        }

        // Lava is a trap: a hunter follows prey that stands in it and burns there (D-086, Mario).
        [Test]
        public void Hunter_PreyStandingInLava_FollowsItInAndBurns()
        {
            RunState state = WorldRun(out SimulationTicker ticker);
            Demon hunter = new DemonBuilder().At(-12f, 0f).SpawnInto(state);
            Demon prey = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            ticker.Commands.Submit(new SetTestModeCommand(prey.Id, true));
            ticker.Ai.AddBrain(hunter, TestArchetypes.Hunter, state.World!.Bounds, null);
            float max = hunter.Body.Core.MaxHp;
            bool enteredLava = false;

            for (int i = 0; i < state.Config.TicksFor(Seconds); i++)
            {
                ticker.Tick();
                enteredLava |= state.Hazards.KindAt(hunter.Position) == HazardKind.Lava;
            }

            enteredLava.Should().BeTrue();
            (hunter.IsAlive ? hunter.Body.Core.Hp : 0f).Should().BeLessThan(max);
        }

        // An AI demon caught in lava (born there, knocked or dragged in) burns like the player while it walks out (D-086).
        [Test]
        public void Wanderer_CaughtInLava_BurnsWhileItWalksOut()
        {
            RunState state = WorldRun(out SimulationTicker ticker);
            Demon wanderer = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            ticker.Ai.AddBrain(wanderer, TestArchetypes.Wanderer, state.World!.Bounds, null);
            float max = wanderer.Body.Core.MaxHp;

            for (int i = 0; i < state.Config.TicksFor(1f); i++)
            {
                ticker.Tick();
            }

            wanderer.Hazard.Should().Be(HazardKind.Lava);
            wanderer.Body.Core.Hp.Should().BeLessThan(max * 0.9f);
        }

        private static RunState WorldRun(out SimulationTicker ticker)
        {
            RunState state = new RunStateBuilder().Build();
            state.AttachWorld(HazardWorlds.Flat(state.Seed));
            ticker = new SimulationTicker(state, new SimulationEvents());
            return state;
        }
    }
}

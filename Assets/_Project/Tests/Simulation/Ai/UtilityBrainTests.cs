#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Ai
{
    public sealed class UtilityBrainTests
    {
        private static readonly GroundBounds Arena = GroundBounds.CenteredSquare(100f);

        [Test]
        public void Decide_WandererAtRest_SubmitsAMoveCommandThatMovesTheDemon()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            UtilityBrain brain = ticker.Ai.AddBrain(demon, TestArchetypes.Wanderer, Arena, null);
            Vector3 start = demon.Position;

            ticker.Tick();
            ticker.Tick();

            brain.CurrentGoal.Should().Be(AiGoal.Wander);
            demon.Intent.IsMoving.Should().BeTrue();
            Vector3.Distance(demon.Position, start).Should().BeGreaterThan(0f);
        }

        [Test]
        public void Decide_Wanderer_MovesTowardItsTarget()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            UtilityBrain brain = ticker.Ai.AddBrain(demon, TestArchetypes.Wanderer, Arena, null);
            ticker.Tick();
            float before = Vector3.Distance(demon.Position, brain.CurrentTarget);

            ticker.Tick();

            float after = Vector3.Distance(demon.Position, brain.CurrentTarget);
            after.Should().BeLessThan(before);
        }

        [Test]
        public void Decide_Wanderer_ReachesTargetsAndPicksNewOnesInsideTheBounds()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            GroundBounds small = GroundBounds.CenteredSquare(6f);
            UtilityBrain brain = ticker.Ai.AddBrain(demon, TestArchetypes.Wanderer, small, null);
            var targets = new HashSet<Vector3>();

            for (int i = 0; i < 600; i++)
            {
                ticker.Tick();
                targets.Add(brain.CurrentTarget);
                small.Contains(brain.CurrentTarget).Should().BeTrue();
            }

            targets.Count.Should().BeGreaterThan(3);
        }

        [Test]
        public void Decide_Rester_StandsStillForTheRestDuration()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            UtilityBrain brain = ticker.Ai.AddBrain(demon, TestArchetypes.Rester, Arena, null);
            Vector3 start = demon.Position;

            for (int i = 0; i < 40; i++)
            {
                ticker.Tick();
            }

            brain.CurrentGoal.Should().Be(AiGoal.Rest);
            demon.Position.Should().Be(start);
            demon.Intent.IsMoving.Should().BeFalse();
        }

        [Test]
        public void Decide_PatrollerWithRoute_WalksTheWaypointsInOrderAndLoops()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            var route = new List<Vector3> { new Vector3(2f, 0f, 0f), new Vector3(2f, 0f, 2f), new Vector3(0f, 0f, 2f) };
            UtilityBrain brain = ticker.Ai.AddBrain(demon, TestArchetypes.Patroller, Arena, route);
            var visited = new List<int>();

            for (int i = 0; i < 200; i++)
            {
                ticker.Tick();
                if (visited.Count == 0 || visited[visited.Count - 1] != brain.RouteIndex)
                {
                    visited.Add(brain.RouteIndex);
                }
            }

            brain.CurrentGoal.Should().Be(AiGoal.Patrol);
            visited.Should().StartWith(new[] { 0, 1, 2, 0 });
        }

        [Test]
        public void Decide_PatrollerWithoutRoute_FallsBackToResting()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            UtilityBrain brain = ticker.Ai.AddBrain(demon, TestArchetypes.Patroller, Arena, null);

            ticker.Tick();

            brain.CurrentGoal.Should().Be(AiGoal.Rest);
        }

        [Test]
        public void Decide_SameSeed_ProducesTheSamePositionsAfterManyTicks()
        {
            Vector3 first = RunWanderer(seed: 77, ticks: 300);
            Vector3 second = RunWanderer(seed: 77, ticks: 300);

            first.Should().Be(second);
        }

        [Test]
        public void Decide_DifferentSeeds_ProduceDifferentPositions()
        {
            Vector3 first = RunWanderer(seed: 77, ticks: 300);
            Vector3 second = RunWanderer(seed: 78, ticks: 300);

            first.Should().NotBe(second);
        }

        [Test]
        public void Constructor_PlayerDemon_Throws()
        {
            RunState state = new RunStateBuilder().Build();
            Demon player = new DemonBuilder().AsPlayer().SpawnInto(state);

            Action act = () => _ = new UtilityBrain(player, TestArchetypes.Wanderer, Arena, null);

            act.Should().Throw<ArgumentException>();
        }

        private static Vector3 RunWanderer(int seed, int ticks)
        {
            RunState state = new RunStateBuilder().WithSeed(seed).Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            ticker.Ai.AddBrain(demon, TestArchetypes.Wanderer, Arena, null);
            for (int i = 0; i < ticks; i++)
            {
                ticker.Tick();
            }

            return demon.Position;
        }
    }
}

#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Ai
{
    public sealed class AiSystemTests
    {
        private static readonly GroundBounds Arena = GroundBounds.CenteredSquare(100f);

        [Test]
        public void Think_TwoBrainsDecidingEveryTwoTicks_TakeTurns()
        {
            RunState state = new RunStateBuilder().Build();
            Demon first = new DemonBuilder().SpawnInto(state);
            Demon second = new DemonBuilder().SpawnInto(state);
            var ai = new AiSystem();
            ArchetypeSpec everyTwo = TestArchetypes.WandererEvery(2);
            ai.AddBrain(first, everyTwo, Arena, null);
            ai.AddBrain(second, everyTwo, Arena, null);
            var commands = new CommandQueue();

            ai.Think(state, commands);
            int atTickZero = commands.PendingCount;
            new SimulationTicker(state, new SimulationEvents()).Tick();
            ai.Think(state, commands);
            int atTickOne = commands.PendingCount - atTickZero;

            atTickZero.Should().Be(1);
            atTickOne.Should().Be(1);
        }

        [Test]
        public void Think_WithoutBrains_SubmitsNothing()
        {
            RunState state = new RunStateBuilder().Build();
            var ai = new AiSystem();
            var commands = new CommandQueue();

            ai.Think(state, commands);

            commands.PendingCount.Should().Be(0);
        }

        [Test]
        public void AddBrain_Always_IsListedInOrder()
        {
            RunState state = new RunStateBuilder().Build();
            Demon first = new DemonBuilder().SpawnInto(state);
            Demon second = new DemonBuilder().SpawnInto(state);
            var ai = new AiSystem();

            UtilityBrain a = ai.AddBrain(first, TestArchetypes.Wanderer, Arena, null);
            UtilityBrain b = ai.AddBrain(second, TestArchetypes.Rester, Arena, null);

            ai.Brains.Should().Equal(a, b);
        }
    }
}

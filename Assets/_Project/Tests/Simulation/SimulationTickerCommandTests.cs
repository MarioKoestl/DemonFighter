#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class SimulationTickerCommandTests
    {
        [Test]
        public void ApplyPendingCommands_SpendStatPoint_AppliesWithoutAdvancingTime()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon demon = new DemonBuilder().AsPlayer().SpawnInto(state);
            demon.Stats.GrantPoints(1);
            ticker.Commands.Submit(new SpendStatPointCommand(demon.Id, StatIds.Agility));

            ticker.ApplyPendingCommands();

            state.Tick.Should().Be(0);
            demon.Stats.Get(StatIds.Agility).Should().Be(1);
            ticker.Commands.PendingCount.Should().Be(0);
        }
    }
}

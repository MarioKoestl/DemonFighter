#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Events;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class SimulationTickerTests
    {
        [Test]
        public void Tick_Once_AdvancesTheRunByOneStep()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());

            ticker.Tick();

            state.Tick.Should().Be(1);
        }

        [Test]
        public void Tick_WithQueuedEvent_DeliversItAtTheEndOfTheStep()
        {
            RunState state = new RunStateBuilder().Build();
            var events = new SimulationEvents();
            var ticker = new SimulationTicker(state, events);
            int received = 0;
            events.Subscribe<TestEvent>(evt => received = evt.Value);
            events.Publish(new TestEvent(9));

            ticker.Tick();

            received.Should().Be(9);
            events.PendingCount.Should().Be(0);
        }
    }
}

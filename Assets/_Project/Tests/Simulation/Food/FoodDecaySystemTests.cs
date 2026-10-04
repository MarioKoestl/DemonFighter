#nullable enable
using System.Collections.Generic;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Food
{
    public sealed class FoodDecaySystemTests
    {
        [Test]
        public void Tick_DecayTimePasses_RemovesTheFoodAndReportsIt()
        {
            RunState state = new RunStateBuilder().Build();
            var events = new SimulationEvents();
            var ticker = new SimulationTicker(state, events);
            var removed = new List<FoodRemoved>();
            events.Subscribe<FoodRemoved>(removed.Add);
            FoodItem food = state.SpawnFood(FoodKind.SeveredPart, Vector3.Zero, 5f, new DemonId(1), 0);
            long decayTicks = food.DecayTicksLeft;

            for (long i = 0; i < decayTicks - 1; i++)
            {
                ticker.Tick();
            }

            state.Food.Should().Equal(food);
            ticker.Tick();
            state.Food.Should().BeEmpty();
            removed.Should().ContainSingle();
            removed[0].Food.Should().Be(food.Id);
            removed[0].Reason.Should().Be(FoodRemovalReason.Decayed);
        }
    }
}

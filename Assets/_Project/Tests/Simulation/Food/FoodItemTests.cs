#nullable enable
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Food
{
    public sealed class FoodItemTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void SpawnFood_Always_IsListedAndFoundById()
        {
            RunState state = new RunStateBuilder().Build();

            FoodItem food = state.SpawnFood(FoodKind.Corpse, new Vector3(1f, 0f, 2f), 20f, new DemonId(1), 0);

            state.Food.Should().Equal(food);
            state.TryGetFood(food.Id, out FoodItem? found).Should().BeTrue();
            found.Should().BeSameAs(food);
            food.DecayTicksLeft.Should().Be(state.Config.TicksFor(state.Catalog.Tuning.FoodDecaySeconds));
        }

        [Test]
        public void Take_MoreThanLeft_TakesOnlyTheRest()
        {
            RunState state = new RunStateBuilder().Build();
            FoodItem food = state.SpawnFood(FoodKind.SeveredPart, Vector3.Zero, 5f, new DemonId(1), 0);

            float taken = food.Take(8f);

            taken.Should().BeApproximately(5f, Tolerance);
            food.IsDepleted.Should().BeTrue();
        }

        [Test]
        public void Decay_CountsDownToDecayed()
        {
            RunState state = new RunStateBuilder().Build();
            FoodItem food = state.SpawnFood(FoodKind.SeveredPart, Vector3.Zero, 5f, new DemonId(1), 0);
            long ticks = food.DecayTicksLeft;

            for (long i = 0; i < ticks; i++)
            {
                food.Decay();
            }

            food.IsDecayed.Should().BeTrue();
        }

        [Test]
        public void RemoveFood_Known_RemovesIt()
        {
            RunState state = new RunStateBuilder().Build();
            FoodItem food = state.SpawnFood(FoodKind.Corpse, Vector3.Zero, 20f, new DemonId(1), 0);

            bool removed = state.RemoveFood(food.Id);

            removed.Should().BeTrue();
            state.Food.Should().BeEmpty();
            state.TryGetFood(food.Id, out _).Should().BeFalse();
            state.RemoveFood(food.Id).Should().BeFalse();
        }
    }
}

#nullable enable
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Food
{
    /// <summary>Rots every food item by one tick and removes what has rotted away (ARCHITECTURE "Tick", food decay).</summary>
    internal static class FoodDecaySystem
    {
        /// <summary>Advances decay by one step; iterates backwards because removal shortens the list.</summary>
        public static void Advance(RunState state, SimulationEvents events)
        {
            for (int i = state.Food.Count - 1; i >= 0; i--)
            {
                FoodItem food = state.Food[i];
                food.Decay();
                if (food.IsDecayed)
                {
                    state.RemoveFood(food.Id);
                    events.Publish(new FoodRemoved(food.Id, FoodRemovalReason.Decayed));
                }
            }
        }
    }
}

#nullable enable
using DemonFighter.Simulation.Food;

namespace DemonFighter.Simulation.Events
{
    /// <summary>A food item left the world, eaten up or rotted away; Presentation removes its view.</summary>
    public readonly struct FoodRemoved : ISimulationEvent
    {
        public FoodRemoved(FoodId food, FoodRemovalReason reason)
        {
            Food = food;
            Reason = reason;
        }

        public FoodId Food { get; }

        public FoodRemovalReason Reason { get; }
    }
}

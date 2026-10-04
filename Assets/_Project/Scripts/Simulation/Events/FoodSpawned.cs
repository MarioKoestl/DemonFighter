#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A new food item exists; Presentation creates or converts its view.</summary>
    public readonly struct FoodSpawned : ISimulationEvent
    {
        public FoodSpawned(FoodId food)
        {
            Food = food;
        }

        public FoodId Food { get; }
    }
}

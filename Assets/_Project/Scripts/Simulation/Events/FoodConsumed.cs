#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A demon took one tick of Biomass out of a food item; the HUD counter and the eating view follow it.</summary>
    public readonly struct FoodConsumed : ISimulationEvent
    {
        public FoodConsumed(DemonId eater, FoodId food, float biomassGained)
        {
            Eater = eater;
            Food = food;
            BiomassGained = biomassGained;
        }

        public DemonId Eater { get; }

        public FoodId Food { get; }

        /// <summary>Biomass the eater gained this tick, after reward scaling.</summary>
        public float BiomassGained { get; }
    }
}

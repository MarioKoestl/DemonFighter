#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A limb or tail came off and lies on the ground as the given food item.</summary>
    public readonly struct PartSevered : ISimulationEvent
    {
        public PartSevered(DemonId demon, int partIndex, FoodId food)
        {
            Demon = demon;
            PartIndex = partIndex;
            Food = food;
        }

        public DemonId Demon { get; }

        public int PartIndex { get; }

        public FoodId Food { get; }
    }
}

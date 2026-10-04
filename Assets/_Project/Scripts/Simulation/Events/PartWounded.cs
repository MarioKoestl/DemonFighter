#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A part dropped below the wounded threshold; its view switches to the wounded look.</summary>
    public readonly struct PartWounded : ISimulationEvent
    {
        public PartWounded(DemonId demon, int partIndex)
        {
            Demon = demon;
            PartIndex = partIndex;
        }

        public DemonId Demon { get; }

        public int PartIndex { get; }
    }
}

#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A hide or sensory part was destroyed; it leaves no food behind.</summary>
    public readonly struct PartDestroyed : ISimulationEvent
    {
        public PartDestroyed(DemonId demon, int partIndex)
        {
            Demon = demon;
            PartIndex = partIndex;
        }

        public DemonId Demon { get; }

        public int PartIndex { get; }
    }
}

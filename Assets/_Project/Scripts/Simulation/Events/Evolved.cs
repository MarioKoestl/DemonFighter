#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A demon took an evolution; its body reshapes until the given tick and its tier rose by one.</summary>
    public readonly struct Evolved : ISimulationEvent
    {
        public Evolved(DemonId demon, string evolutionId, int stage, long untilTick)
        {
            Demon = demon;
            EvolutionId = evolutionId;
            Stage = stage;
            UntilTick = untilTick;
        }

        public DemonId Demon { get; }

        public string EvolutionId { get; }

        /// <summary>1 for the first evolution, 2 for the second.</summary>
        public int Stage { get; }

        /// <summary>First tick the demon can act and be hurt again.</summary>
        public long UntilTick { get; }
    }
}

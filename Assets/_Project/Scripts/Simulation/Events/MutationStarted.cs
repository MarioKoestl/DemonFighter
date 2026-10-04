#nullable enable
using DemonFighter.Simulation.Mutation;

namespace DemonFighter.Simulation.Events
{
    /// <summary>A mutation was confirmed and paid; the body changed and the demon transforms until the given tick.</summary>
    public readonly struct MutationStarted : ISimulationEvent
    {
        public MutationStarted(DemonId demon, MutationKind kind, int partIndex, string partId, float cost, long untilTick)
        {
            Demon = demon;
            Kind = kind;
            PartIndex = partIndex;
            PartId = partId;
            Cost = cost;
            UntilTick = untilTick;
        }

        public DemonId Demon { get; }

        public MutationKind Kind { get; }

        /// <summary>The part that was attached, upgraded or regrown.</summary>
        public int PartIndex { get; }

        public string PartId { get; }

        /// <summary>Biomass spent.</summary>
        public float Cost { get; }

        /// <summary>First tick the demon can act and be hurt again.</summary>
        public long UntilTick { get; }
    }
}

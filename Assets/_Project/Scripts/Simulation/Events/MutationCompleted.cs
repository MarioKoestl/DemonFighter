#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>The transformation is over; the demon acts and bleeds again.</summary>
    public readonly struct MutationCompleted : ISimulationEvent
    {
        public MutationCompleted(DemonId demon)
        {
            Demon = demon;
        }

        public DemonId Demon { get; }
    }
}

#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A grab holds a demon in place until the given tick; it cannot move, attack or eat meanwhile.</summary>
    public readonly struct DemonHeld : ISimulationEvent
    {
        public DemonHeld(DemonId target, DemonId by, long untilTick)
        {
            Target = target;
            By = by;
            UntilTick = untilTick;
        }

        public DemonId Target { get; }

        public DemonId By { get; }

        public long UntilTick { get; }
    }
}

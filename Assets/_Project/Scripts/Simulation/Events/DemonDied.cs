#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A core was destroyed. The body becomes the given corpse; the killer is None for deaths without one.</summary>
    public readonly struct DemonDied : ISimulationEvent
    {
        public DemonDied(DemonId demon, DemonId killer, FoodId corpse)
        {
            Demon = demon;
            Killer = killer;
            Corpse = corpse;
        }

        public DemonId Demon { get; }

        public DemonId Killer { get; }

        public FoodId Corpse { get; }
    }
}

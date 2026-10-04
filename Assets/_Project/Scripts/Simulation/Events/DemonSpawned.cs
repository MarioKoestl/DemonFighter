#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A demon was born mid-run; the App layer gives it a body like the ones from run start.</summary>
    public readonly struct DemonSpawned : ISimulationEvent
    {
        public DemonSpawned(DemonId demon)
        {
            Demon = demon;
        }

        public DemonId Demon { get; }
    }
}

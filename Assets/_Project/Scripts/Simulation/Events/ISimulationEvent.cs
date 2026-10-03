#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>
    /// Marker for everything the simulation announces to the outside. Events are structs, so publishing allocates
    /// nothing in the tick path.
    /// </summary>
    public interface ISimulationEvent
    {
    }
}

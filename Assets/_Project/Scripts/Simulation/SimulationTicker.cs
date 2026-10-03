#nullable enable
using System;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Advances a run by one fixed step at a time, in the order from ARCHITECTURE "Tick": commands, status effects,
    /// AI, threat and spawning, food decay, then the event flush. In M0 only the clock and the flush exist; the other
    /// stages are added with the milestones that need them.
    /// </summary>
    public sealed class SimulationTicker
    {
        /// <summary>Binds a run to the event bus its observers listen on.</summary>
        public SimulationTicker(RunState state, SimulationEvents events)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>The run being advanced.</summary>
        public RunState State { get; }

        /// <summary>Where every change of this run is announced.</summary>
        public SimulationEvents Events { get; }

        /// <summary>Applies one fixed step. The caller invokes it TicksPerSecond times per simulated second.</summary>
        public void Tick()
        {
            State.AdvanceTick();
            Events.Flush();
        }
    }
}

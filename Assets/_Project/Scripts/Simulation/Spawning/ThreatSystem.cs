#nullable enable
using System;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Spawning
{
    /// <summary>
    /// Announces the run-wide threat level (GAME_DESIGN, "The run"; D-016, D-069). The level itself is derived from
    /// the tick by <see cref="RunState.Threat"/>, so it needs no state of its own; this stage only publishes
    /// <see cref="ThreatLevelChanged"/> once per whole level crossed, for the HUD and everything that paces itself by it.
    /// </summary>
    internal sealed class ThreatSystem
    {
        private readonly SimulationEvents _events;
        private int _announced;

        public ThreatSystem(SimulationEvents events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>Publishes the level when it differs from the last one announced; after a load the first tick catches up.</summary>
        public void Advance(RunState state)
        {
            int level = state.ThreatLevel;
            if (level == _announced)
            {
                return;
            }

            _announced = level;
            _events.Publish(new ThreatLevelChanged(level));
        }
    }
}

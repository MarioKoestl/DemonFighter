#nullable enable
using System;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Events;

namespace DemonFighter.App
{
    /// <summary>
    /// Composition root. Created once by the bootstrap scene, lives for the whole application and is handed to the
    /// objects that need it. Nothing reaches it through a static (ARCHITECTURE, principle 6). Content catalog and
    /// settings join in later milestones.
    /// </summary>
    internal sealed class GameServices
    {
        public GameServices(SimulationConfig simulationConfig, SimulationEvents events)
        {
            SimulationConfig = simulationConfig ?? throw new ArgumentNullException(nameof(simulationConfig));
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>Tick timing every run starts with.</summary>
        public SimulationConfig SimulationConfig { get; }

        /// <summary>The event bus Presentation, UI and audio subscribe to.</summary>
        public SimulationEvents Events { get; }
    }
}

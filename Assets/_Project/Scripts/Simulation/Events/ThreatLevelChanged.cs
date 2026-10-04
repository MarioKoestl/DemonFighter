#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>The run-wide threat crossed a whole level (D-069); the HUD meter and anything pacing itself by it listen.</summary>
    public readonly struct ThreatLevelChanged : ISimulationEvent
    {
        public ThreatLevelChanged(int level)
        {
            Level = level;
        }

        /// <summary>The whole level reached now.</summary>
        public int Level { get; }
    }
}

#nullable enable
using System;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Fixed-step timing of the simulation. D-011 fixes 20 ticks per second for v1; the rate is configurable here and
    /// nowhere else, and it never depends on the frame rate.
    /// </summary>
    public sealed record SimulationConfig
    {
        /// <summary>The v1 tick rate from D-011.</summary>
        public const int DefaultTicksPerSecond = 20;

        /// <summary>Creates a config with the given tick rate; zero or negative rates are programming errors.</summary>
        public SimulationConfig(int ticksPerSecond)
        {
            if (ticksPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "Tick rate must be positive.");
            }

            TicksPerSecond = ticksPerSecond;
            TickSeconds = 1f / ticksPerSecond;
        }

        /// <summary>The config every run uses unless a test or a setting says otherwise.</summary>
        public static SimulationConfig Default { get; } = new SimulationConfig(DefaultTicksPerSecond);

        /// <summary>How many fixed steps make one simulated second.</summary>
        public int TicksPerSecond { get; }

        /// <summary>Length of one fixed step in simulated seconds.</summary>
        public float TickSeconds { get; }

        /// <summary>Whole ticks that cover the given duration; durations round up so nothing ends early.</summary>
        public int TicksFor(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Durations are never negative.");
            }

            return (int)MathF.Ceiling(seconds * TicksPerSecond);
        }
    }
}

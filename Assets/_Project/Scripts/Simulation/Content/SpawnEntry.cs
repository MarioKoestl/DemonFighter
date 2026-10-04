#nullable enable
using System;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// One line of a biome spawn table (D-070): a demon kind, the whole threat level from which it may spawn and its
    /// weight among the kinds that qualify. Stronger spawns are kinds born with parts, Biomass, levels or an evolution.
    /// </summary>
    public sealed record SpawnEntry
    {
        public SpawnEntry(DemonSpec demon, int minThreat, float weight)
        {
            Demon = demon ?? throw new ArgumentNullException(nameof(demon));
            if (minThreat < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minThreat), minThreat, "MinThreat is never negative.");
            }

            if (weight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "A spawn entry needs a positive weight.");
            }

            MinThreat = minThreat;
            Weight = weight;
        }

        /// <summary>The kind spawned.</summary>
        public DemonSpec Demon { get; }

        /// <summary>The whole threat level from which this kind spawns.</summary>
        public int MinThreat { get; }

        /// <summary>Relative chance among the entries that qualify at the current threat.</summary>
        public float Weight { get; }
    }
}

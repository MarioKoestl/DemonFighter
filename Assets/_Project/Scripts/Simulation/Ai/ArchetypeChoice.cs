#nullable enable
using System;

namespace DemonFighter.Simulation.Ai
{
    /// <summary>One personality a biome hands to new blobs, with its weight among the others (D-071).</summary>
    public sealed record ArchetypeChoice
    {
        public ArchetypeChoice(ArchetypeSpec archetype, float weight)
        {
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            if (weight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "An archetype choice needs a positive weight.");
            }

            Weight = weight;
        }

        public ArchetypeSpec Archetype { get; }

        /// <summary>Relative chance of this personality for a new blob.</summary>
        public float Weight { get; }
    }
}

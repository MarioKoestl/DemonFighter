#nullable enable
using System;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// The body a demon is spawned with in M1: tier, size and movement numbers. A stand-in for the stats, parts and
    /// archetype specs of M2 and M3; content data, never tuned in behaviour code.
    /// </summary>
    public sealed record DemonTemplate
    {
        /// <summary>Validates the numbers; a template with a zero size or a sprint slower than walking is a content bug.</summary>
        public DemonTemplate(string name, int tier, float sizeMeters, float moveSpeed, float sprintMultiplier)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A template needs a name.", nameof(name));
            }

            if (tier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "Tier is never negative.");
            }

            if (sizeMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeMeters), sizeMeters, "Size must be positive.");
            }

            if (moveSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(moveSpeed), moveSpeed, "Move speed is never negative.");
            }

            if (sprintMultiplier < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(sprintMultiplier), sprintMultiplier, "Sprinting is never slower than walking.");
            }

            Name = name;
            Tier = tier;
            SizeMeters = sizeMeters;
            MoveSpeed = moveSpeed;
            SprintMultiplier = sprintMultiplier;
        }

        /// <summary>Content name for logs and the HUD.</summary>
        public string Name { get; }

        /// <summary>Tier at spawn; drives placeholder color and camera distance (D-018).</summary>
        public int Tier { get; }

        /// <summary>Body height in meters; Tier 0 is about 1 m, elders about 15 m (D-018).</summary>
        public float SizeMeters { get; }

        /// <summary>Walking speed in meters per second.</summary>
        public float MoveSpeed { get; }

        /// <summary>Factor applied to the walking speed while sprinting.</summary>
        public float SprintMultiplier { get; }
    }
}

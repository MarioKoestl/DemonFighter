#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// Immutable description of a demon kind as it spawns: tier, size, movement, which core it is born with and its
    /// starting stats. Player and AI demons use the same specs (GAME_DESIGN, "Everyone plays by the same rules").
    /// Created from a DemonDefinition asset; the defaults describe the Tier 0 blob.
    /// </summary>
    public sealed record DemonSpec
    {
        /// <summary>Stable content id, lowercase and dotted, for example demon.blob.</summary>
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        /// <summary>Tier at spawn; drives placeholder color, camera distance and reward scaling (D-018, D-028).</summary>
        public int Tier { get; init; }

        /// <summary>Body height in meters; Tier 0 is about 1 m, elders about 15 m (D-018).</summary>
        public float SizeMeters { get; init; } = 1.2f;

        /// <summary>Walking speed in meters per second before Agility.</summary>
        public float MoveSpeed { get; init; } = 4f;

        /// <summary>Factor applied to the walking speed while sprinting.</summary>
        public float SprintMultiplier { get; init; } = 1.6f;

        /// <summary>Id of the body part this demon is born as; must be a core.</summary>
        public string CoreId { get; init; } = "part.core";

        /// <summary>Base stat points the demon starts with; unspecified stats start at zero.</summary>
        public IReadOnlyList<StatValue> StartingStats { get; init; } = Array.Empty<StatValue>();

        /// <summary>Throws with the first content error found.</summary>
        public void Validate()
        {
            Require(!string.IsNullOrWhiteSpace(Id), "Id is required.");
            Require(!string.IsNullOrWhiteSpace(Name), "Name is required.");
            Require(Tier >= 0, "Tier is never negative.");
            Require(SizeMeters > 0f, "SizeMeters must be positive.");
            Require(MoveSpeed >= 0f, "MoveSpeed is never negative.");
            Require(SprintMultiplier >= 1f, "Sprinting is never slower than walking.");
            Require(!string.IsNullOrWhiteSpace(CoreId), "CoreId is required.");
            Require(StartingStats != null, "StartingStats must not be null.");
        }

        private void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new ContentException("Demon " + Id + ": " + message);
            }
        }
    }
}

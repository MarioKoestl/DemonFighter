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

        /// <summary>Parts attached at spawn, free and without a transformation (D-070); a part without a free socket is skipped.</summary>
        public IReadOnlyList<string> StartingPartIds { get; init; } = Array.Empty<string>();

        /// <summary>Biomass the demon carries at spawn.</summary>
        public float StartingBiomass { get; init; }

        /// <summary>Character level at spawn; the levels above one grant their stat points unspent.</summary>
        public int StartingLevel { get; init; } = 1;

        /// <summary>Evolution applied at spawn as its package; empty for none.</summary>
        public string StartingEvolutionId { get; init; } = string.Empty;

        /// <summary>
        /// Chance, 0 to 1, that each socket slot the starting package left free gets a random part that fits it, unlocks,
        /// levels and Biomass aside (D-095); 0 for none. Drawn from the run's seed.
        /// </summary>
        public float RandomPartChance { get; init; }

        /// <summary>When true, every part the demon is born with starts at its highest upgrade (D-095).</summary>
        public bool StartingPartsAtMaxUpgrade { get; init; }

        /// <summary>
        /// Evolution stages the demon is born having taken, after its starting evolution: the first of a random line,
        /// the rest of the same line (D-095). Each raises the tier by one, so the kind's tier is the one before them.
        /// </summary>
        public int RandomEvolutionStages { get; init; }

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
            Require(StartingPartIds != null && StartingEvolutionId != null, "Starting lists must not be null.");
            Require(StartingBiomass >= 0f, "StartingBiomass is never negative.");
            Require(StartingLevel >= 1, "StartingLevel starts at 1.");
            Require(RandomPartChance >= 0f && RandomPartChance <= 1f, "RandomPartChance lies between 0 and 1.");
            Require(RandomEvolutionStages >= 0, "RandomEvolutionStages is never negative.");
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

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// One evolution option (GAME_DESIGN, "Evolution"; O-009): a line such as Brute or Stalker at one of the two
    /// stages, with the package it grants: stat points to allocate, raised stat caps, free parts, skills no part
    /// grants and part kinds it unlocks. The line also names the stat it favors, which orders the options by fit.
    /// </summary>
    public sealed record EvolutionSpec
    {
        /// <summary>Stable content id, lowercase and dotted, for example evolution.brute.1.</summary>
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        /// <summary>Which evolution this is an option for: 1 at the first threshold, 2 at the second.</summary>
        public int Stage { get; init; } = 1;

        /// <summary>Stat points granted to allocate.</summary>
        public int StatPoints { get; init; } = 4;

        /// <summary>Raises of the allocation cap per stat.</summary>
        public IReadOnlyList<StatValue> StatCapBonuses { get; init; } = Array.Empty<StatValue>();

        /// <summary>Stat points bound to a stat (D-067): the stat and its cap rise by the value, for example +10 Strength for a Brute.</summary>
        public IReadOnlyList<StatValue> StatBonuses { get; init; } = Array.Empty<StatValue>();

        /// <summary>Parts attached for free when a socket is free for them.</summary>
        public IReadOnlyList<string> FreeMutationPartIds { get; init; } = Array.Empty<string>();

        /// <summary>Part kinds this evolution unlocks for buying.</summary>
        public IReadOnlyList<string> UnlockedPartIds { get; init; } = Array.Empty<string>();

        /// <summary>Skills granted directly, with no part behind them.</summary>
        public IReadOnlyList<string> ExtraSkillIds { get; init; } = Array.Empty<string>();

        /// <summary>The stat this line favors; options are offered best fit first.</summary>
        public StatId FitStat { get; init; } = StatIds.Strength;

        /// <summary>Throws with the first content error found.</summary>
        public void Validate()
        {
            Require(!string.IsNullOrWhiteSpace(Id), "Id is required.");
            Require(!string.IsNullOrWhiteSpace(Name), "Name is required.");
            Require(Stage >= 1, "Stage starts at 1.");
            Require(StatPoints >= 0, "StatPoints is never negative.");
            Require(StatCapBonuses != null && StatBonuses != null && FreeMutationPartIds != null && UnlockedPartIds != null && ExtraSkillIds != null, "Package lists must not be null.");
            Require(FitStat.IsValid, "FitStat is required.");
            for (int i = 0; i < StatCapBonuses.Count; i++)
            {
                Require(StatCapBonuses[i].Value >= 0, "Cap bonuses are never negative.");
            }

            for (int i = 0; i < StatBonuses.Count; i++)
            {
                Require(StatBonuses[i].Value >= 0, "Stat bonuses are never negative.");
            }
        }

        private void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new ContentException("Evolution " + Id + ": " + message);
            }
        }
    }
}

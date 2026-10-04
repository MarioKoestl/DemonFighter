#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Mutation;

namespace DemonFighter.Simulation.Evolution
{
    /// <summary>
    /// When a demon may evolve and into what (GAME_DESIGN, "Evolution"): a threshold level per evolution from the
    /// tuning, up to three options of the matching stage ordered by how well the line fits the body, and the same
    /// alive-and-not-transforming rule as mutations before a confirm (the calm rule is suspended, D-060).
    /// </summary>
    public static class EvolutionRules
    {
        public const int OptionCount = 3;
        public const string NothingPending = "No evolution pending";
        public const string NotOffered = "Not one of the offered evolutions";

        /// <summary>The stage the demon may evolve into now: 1 or 2, or 0 when no threshold is pending.</summary>
        public static int PendingStage(Demon demon, CombatTuning tuning)
        {
            IReadOnlyList<int> levels = tuning.EvolutionLevels;
            if (demon.Evolutions >= levels.Count || demon.Level < levels[demon.Evolutions])
            {
                return 0;
            }

            return demon.Evolutions + 1;
        }

        /// <summary>The character level the next evolution needs, or 0 when none is left.</summary>
        public static int NextThreshold(Demon demon, CombatTuning tuning)
        {
            IReadOnlyList<int> levels = tuning.EvolutionLevels;
            return demon.Evolutions < levels.Count ? levels[demon.Evolutions] : 0;
        }

        /// <summary>The options for the pending stage, best fit first; empty when nothing is pending.</summary>
        public static IReadOnlyList<EvolutionSpec> Options(RunState state, Demon demon)
        {
            int stage = PendingStage(demon, state.Catalog.Tuning);
            var options = new List<EvolutionSpec>();
            if (stage == 0)
            {
                return options;
            }

            foreach (EvolutionSpec spec in state.Catalog.Evolutions)
            {
                if (spec.Stage == stage)
                {
                    options.Add(spec);
                }
            }

            Stats.BaseStats stats = demon.EffectiveStats();
            options.Sort((a, b) =>
            {
                int fit = Fit(stats, b).CompareTo(Fit(stats, a));
                return fit != 0 ? fit : string.CompareOrdinal(a.Id, b.Id);
            });

            if (options.Count > OptionCount)
            {
                options.RemoveRange(OptionCount, options.Count - OptionCount);
            }

            return options;
        }

        /// <summary>True when the demon may take this evolution now; the first failing rule is the reason.</summary>
        public static bool CanEvolve(Demon demon, EvolutionSpec spec, RunState state, out string reason)
        {
            if (!MutationRules.CanMutateNow(demon, state, out reason))
            {
                return false;
            }

            if (PendingStage(demon, state.Catalog.Tuning) != spec.Stage)
            {
                reason = NothingPending;
                return false;
            }

            IReadOnlyList<EvolutionSpec> options = Options(state, demon);
            for (int i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i].Id, spec.Id, StringComparison.Ordinal))
                {
                    reason = string.Empty;
                    return true;
                }
            }

            reason = NotOffered;
            return false;
        }

        private static int Fit(Stats.BaseStats stats, EvolutionSpec spec)
        {
            return stats.Has(spec.FitStat) ? stats.Get(spec.FitStat) : 0;
        }
    }
}

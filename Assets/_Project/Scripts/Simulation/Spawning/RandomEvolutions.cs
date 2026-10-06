#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Evolution;

namespace DemonFighter.Simulation.Spawning
{
    /// <summary>
    /// The evolutions a kind born evolved has already taken, such as the elder (D-095): for each of its stages after
    /// the ones it already holds, one evolution of that stage, the first drawn at random and the later ones from the
    /// same line (the one favoring the same stat), as a demon that stays with its line takes them. Each comes as its
    /// full package: stat gains, points, unlocks, skills and free parts, and a tier. Drawn from the run's seed.
    /// </summary>
    internal static class RandomEvolutions
    {
        /// <summary>Applies the random evolutions of the demon's kind; a kind with no random stages takes none.</summary>
        public static void Apply(Demon demon, ContentCatalog catalog, Rng rng)
        {
            if (demon == null)
            {
                throw new ArgumentNullException(nameof(demon));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            int stages = demon.Spec.RandomEvolutionStages;
            if (stages <= 0)
            {
                return;
            }

            StatId? line = string.IsNullOrEmpty(demon.Spec.StartingEvolutionId) ? null : catalog.GetEvolution(demon.Spec.StartingEvolutionId).FitStat;
            for (int n = 0; n < stages; n++)
            {
                List<EvolutionSpec> options = OfStage(catalog, demon.Evolutions + 1, line);
                if (options.Count == 0)
                {
                    return;
                }

                EvolutionSpec pick = options[rng.NextInt(0, options.Count)];
                EvolutionPackage.Apply(demon, pick, catalog);
                line = pick.FitStat;
            }
        }

        // The evolutions of a stage in id order, only those of the line when it has any there.
        private static List<EvolutionSpec> OfStage(ContentCatalog catalog, int stage, StatId? line)
        {
            var all = new List<EvolutionSpec>();
            var ofLine = new List<EvolutionSpec>();
            foreach (EvolutionSpec spec in catalog.Evolutions)
            {
                if (spec.Stage != stage)
                {
                    continue;
                }

                all.Add(spec);
                if (line.HasValue && spec.FitStat.Equals(line.Value))
                {
                    ofLine.Add(spec);
                }
            }

            List<EvolutionSpec> options = ofLine.Count > 0 ? ofLine : all;
            options.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return options;
        }
    }
}
